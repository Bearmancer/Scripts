# Async/Sync Design Decisions

This document addresses common questions about async/sync coexistence in the codebase.

---

## 1. How Libraries Handle Async/Sync Coexistence

### The Problem
Some libraries only provide sync APIs (Google APIs SDK uses `.Execute()` synchronously by default), while modern C# prefers async. Mixing can cause:
- Deadlocks (`.Result` or `.Wait()` on UI thread)
- Thread pool starvation
- Lost exception context

### Our Approach

**Pattern A: Provide Both Versions (Preferred)**
```csharp
// Async version (primary)
public async Task<T> GetDataAsync(CancellationToken ct) { ... }

// Sync wrapper for backward compatibility
public T GetData() => GetDataAsync(CancellationToken.None).GetAwaiter().GetResult();
```

**Pattern B: Task.Run for CPU-bound sync code**
```csharp
await Task.Run(() => SyncOnlyLibraryCall());
```

**Why `.GetAwaiter().GetResult()` over `.Result`?**
- Unwraps `AggregateException` to show inner exception
- Slightly better stack traces

### Google APIs Example
```csharp
// Google SDK provides both:
service.Spreadsheets.Get(id).Execute();      // Sync
service.Spreadsheets.Get(id).ExecuteAsync(); // Async
```

---

## 2. Verbosity Added by Async Migration

### Minimal Impact
The async migration adds approximately:
- 1 line for `async` keyword in signature
- 1 line for `await` keyword per call
- Return type changes from `T` to `Task<T>`

### Example Comparison

**Before (Sync):**
```csharp
public int DoWork()
{
    var result = GetData();
    return result.Count;
}
```

**After (Async):**
```csharp
public async Task<int> DoWorkAsync()
{
    var result = await GetDataAsync();
    return result.Count;
}
```

**Net change: +2 keywords, same line count**

---

## 3. Spectre.Console Live Update Async Support

### Yes, Spectre supports async!

```csharp
// Sync version
AnsiConsole.Progress().Start(ctx => { ... });

// Async version
await AnsiConsole.Progress().StartAsync(async ctx => { ... });
```

### Full Async Support
- `AnsiConsole.Status().StartAsync()`
- `AnsiConsole.Progress().StartAsync()`
- `AnsiConsole.Live().StartAsync()`

### Current Status
The `ExportEachSheetAsCSV` sync version still uses `Progress().Start()` because it's called from sync contexts. The new `ExportEachSheetAsCSVAsync` doesn't use progress display to keep it pure async.

---

## 4. Disallow Parallel API Calls

### Already Implemented via Semaphore

```csharp
// In Resilience.cs
private static readonly SemaphoreSlim Semaphore = new(1, 1);

public static async Task<T> ExecuteAsync<T>(...)
{
    await Semaphore.WaitAsync(ct);  // Only one at a time
    try { ... }
    finally { Semaphore.Release(); }
}
```

This ensures:
- Sequential API calls (prevents rate limiting)
- Throttle delay between calls (3 seconds)
- No parallel execution even if multiple `await` calls happen

---

## 5. Async Suffix Naming Convention

### Rule: Only add `Async` suffix when BOTH versions exist

**When both exist:**
```csharp
UserCredential GetCredential(...)      // Sync
Task<UserCredential> GetCredentialAsync(...)  // Async
```

**When only async exists:**
```csharp
Task<List<Track>> SearchAsync(...)  // Keep Async - common pattern for service methods
// OR
Task<List<Track>> Search(...)       // Also acceptable if only async exists
```

### Current Codebase Convention
- `GoogleCredentialService`: Both versions → `GetCredential` / `GetCredentialAsync`
- `MusicBrainzService`: Only async → `SearchAsync` (async-only, suffix retained)
- `DiscogsService`: Only async → `SearchAsync` (async-only, suffix retained)

---

## 6. Tests for Async Methods

### Already Covered
- `Resilience.ExecuteAsync` tested via `ResilienceTests`
- Service methods tested via integration/unit tests
- Commands tested via `MusicSearchCommandTests`

### New Tests Needed
- `GoogleCredentialService.GetCredentialAsync` (requires mocking OAuth)
- `GoogleSheetsService.ExportEachSheetAsCSVAsync` (requires API credentials)

These are integration tests requiring live credentials.

---

## 7. ETag: Cached vs Used for Conditional Requests

### The Distinction

| Term       | Meaning                             | Current Status |
| ---------- | ----------------------------------- | -------------- |
| **Cached** | ETag value is stored locally        | ✅ Yes          |
| **Used**   | ETag sent in `If-None-Match` header | ❌ No           |

### What We Have (Cached)
```csharp
public record YouTubePlaylist(
    string Id,
    string Title,
    ...
    string? ETag  // ← Stored in state file on disk
);
```

The ETag IS cached (saved to disk), so it survives app restarts.

### What We Don't Do (Conditional Requests)
We don't send `If-None-Match: <etag>` header to get 304 responses. This would save bandwidth.

```csharp
// NOT IMPLEMENTED YET:
var request = service.Playlists.List("snippet");
request.RequestHeaders.IfNoneMatch = savedETag;
var response = request.Execute();
// If ETag matches: 304 Not Modified (no body = saves bandwidth)
// If ETag differs: 200 OK with full response
```

### Why Not Used for Conditional Requests
- Google APIs SDK doesn't expose `RequestHeaders` directly for most methods
- Would require custom `HttpClientHandler` or `DelegatingHandler`
- Current approach: Always fetch full data, compare locally in `YouTubeChangeDetector`

---

## Summary

| Question                | Answer                                                                    |
| ----------------------- | ------------------------------------------------------------------------- |
| Async/sync coexistence  | `.GetAwaiter().GetResult()` wrapper pattern                               |
| Verbosity               | Minimal (+2 keywords per method)                                          |
| Spectre async           | Full support via `.StartAsync()`                                          |
| Parallel API prevention | `SemaphoreSlim(1,1)` in Resilience                                        |
| Naming convention       | `Async` suffix only when both exist                                       |
| Async tests             | 5 new tests in `ResilienceAsyncTests.cs`                                  |
| ETag                    | **Cached** (stored to disk) but **not used** for 304 conditional requests |
