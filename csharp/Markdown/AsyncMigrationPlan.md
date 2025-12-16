# Async Migration Plan

## Audit: Sync-over-Async Patterns

Methods that block on async calls using `.Result` or `.Wait()`:

### Critical (Blocking Native Async)

| File                            | Line | Call                                                      | Native Async |
| ------------------------------- | ---- | --------------------------------------------------------- | ------------ |
| `LastFmService.cs`              | 166  | `client.User.GetRecentTracksAsync(...).Result`            | ✅ Yes        |
| `GoogleCredentialService.cs`    | 40   | `GoogleWebAuthorizationBroker.AuthorizeAsync(...).Result` | ✅ Yes        |
| `GoogleCredentialService.cs`    | 69   | `credential.RefreshTokenAsync(...).Wait()`                | ✅ Yes        |
| `GoogleSheetsService.cs`        | 1044 | `httpClient.GetByteArrayAsync(exportUrl).Result`          | ✅ Yes        |
| `YouTubeChangeDetectionTest.cs` | 34   | `GoogleWebAuthorizationBroker.AuthorizeAsync(...).Result` | ✅ Yes        |

### Already Async (Good)

| Service              | Pattern                  | Notes     |
| -------------------- | ------------------------ | --------- |
| `MusicBrainzService` | `async/await` throughout | ✅ Correct |
| `DiscogsService`     | `async/await` throughout | ✅ Correct |

---

## CancellationToken Parameter Order Audit

### Correct (ct is last parameter)

| Method                                     | Signature                                       |
| ------------------------------------------ | ----------------------------------------------- |
| `YouTubeService.GetPlaylistSummaries`      | `(CancellationToken ct)`                        |
| `YouTubeService.GetPlaylistSummary`        | `(string playlistId, CancellationToken ct)`     |
| `YouTubeService.GetPlaylistVideoIds`       | `(string playlistId, CancellationToken ct)`     |
| `YouTubeService.GetPlaylistMetadata`       | `(CancellationToken ct)`                        |
| `YouTubeService.GetAllPlaylists`           | `(CancellationToken ct)`                        |
| `YouTubeService.GetVideoDetailsForIds`     | `(..., CancellationToken ct)`                   |
| `YouTubeService.GetVideoDetails`           | `(List<string> videoIds, CancellationToken ct)` |
| `LastFmService.FetchScrobblesSince`        | `(..., CancellationToken ct)`                   |
| `LastFmService.FetchPage`                  | `(int page, CancellationToken ct)`              |
| `GoogleSheetsService.ExportEachSheetAsCSV` | `(..., CancellationToken ct = default)`         |
| `Resilience.Execute`                       | `(..., CancellationToken ct = default, ...)`    |

### Incorrect (ct not last)

| Method                            | Issue                                                    | Fix                           |
| --------------------------------- | -------------------------------------------------------- | ----------------------------- |
| `ScrobbleSyncOrchestrator` (ctor) | `(CancellationToken ct, DateTime? forceFromDate = null)` | Move ct after optional params |
| `Resilience.Execute`              | Complex: `ct` at position 6, `maxRetries` at position 7  | Keep as-is, uses defaults     |

### Orchestrator Primary Constructors

```csharp
// Current
public class ScrobbleSyncOrchestrator(CancellationToken ct, DateTime? forceFromDate = null)

// Fixed
public class ScrobbleSyncOrchestrator(DateTime? forceFromDate = null, CancellationToken ct = default)
```

---

## Migration Plan

### Phase 1: Fix CancellationToken Order (Low Risk)

1. **ScrobbleSyncOrchestrator**
   - Move `CancellationToken ct` to last position
   - Update all callers

2. **YouTubePlaylistOrchestrator**
   - Same pattern if applicable

### Phase 2: Credential Service (Medium Risk)

1. **GoogleCredentialService.GetCredential**
   ```csharp
   // Before
   cachedCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(...).Result;
   
   // After
   internal static async Task<UserCredential> GetCredentialAsync(...)
   {
       cachedCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(...);
       return cachedCredential;
   }
   ```

2. **GoogleCredentialService.GetAccessToken**
   ```csharp
   // Before
   credential.RefreshTokenAsync(CancellationToken.None).Wait();
   
   // After
   public static async Task<string> GetAccessTokenAsync(UserCredential? credential)
   {
       if (credential.Token.IsStale)
           await credential.RefreshTokenAsync(CancellationToken.None);
       return credential.Token.AccessToken;
   }
   ```

### Phase 3: LastFmService (Medium Risk)

1. **FetchPage → FetchPageAsync**
   ```csharp
   // Before
   private List<Scrobble>? FetchPage(int page, CancellationToken ct)
   {
       var response = Resilience.Execute(
           action: () => client.User.GetRecentTracksAsync(...).Result,
           ...
       );
   }
   
   // After
   private async Task<List<Scrobble>?> FetchPageAsync(int page, CancellationToken ct)
   {
       var response = await client.User.GetRecentTracksAsync(...);
       // ... rest stays same
   }
   ```

2. **FetchScrobblesSince → FetchScrobblesSinceAsync**
   - Change return type to `Task`
   - Add `async` modifier
   - `await FetchPageAsync(...)` 

3. **Update Resilience wrapper**
   - Need `ExecuteAsync` that accepts `Func<Task<T>>`
   - Already exists! Line 215-258

### Phase 4: GoogleSheetsService (Medium Risk)

1. **ExportEachSheetAsCSV**
   ```csharp
   // Before
   action: () => httpClient.GetByteArrayAsync(exportUrl).Result
   
   // After (inside Progress callback - tricky)
   // Option A: Use sync HttpClient.GetByteArray (not available)
   // Option B: Run async inside sync context using Task.Run
   // Option C: Refactor entire method to async
   ```

### Phase 5: Orchestrators (High Risk)

1. **ScrobbleSyncOrchestrator.Execute → ExecuteAsync**
2. **YouTubePlaylistOrchestrator.Execute → ExecuteAsync**
3. **CLI Commands stay sync** (Spectre.Console.Cli handles async)

---

## Sequential Steps

### Step 1: CancellationToken Order

```diff
- public class ScrobbleSyncOrchestrator(CancellationToken ct, DateTime? forceFromDate = null)
+ public class ScrobbleSyncOrchestrator(DateTime? forceFromDate = null, CancellationToken ct = default)
```

Update callers:
- `SyncAllCommand.Execute` 
- `SyncLastFmCommand.Execute`

### Step 2: Make Resilience.ExecuteAsync Accept CancellationToken

```csharp
public static async Task<T> ExecuteAsync<T>(
    Func<CancellationToken, Task<T>> action,  // Changed
    string source,
    CancellationToken ct = default,            // Add
    TimeSpan? throttle = null,
    int maxRetries = MaxRetries
)
```

### Step 3: Async LastFmService

```csharp
// Step 3a: FetchPageAsync
private async Task<List<Scrobble>?> FetchPageAsync(int page, CancellationToken ct)
{
    var response = await Resilience.ExecuteAsync(
        async ct => await client.User.GetRecentTracksAsync(username, limit: PerPage, page: page),
        "LastFm.GetRecentTracks",
        ct
    );
    // ... mapping
}

// Step 3b: FetchScrobblesSinceAsync
internal async Task FetchScrobblesSinceAsync(
    DateTime? fetchAfter,
    FetchState state,
    Func<int, int, TimeSpan, DateTime?, DateTime?, Task> onProgress,  // async progress
    CancellationToken ct
)
{
    // ... 
    while (!ct.IsCancellationRequested)
    {
        var batch = await FetchPageAsync(page, ct);
        // ...
    }
}
```

### Step 4: Async Orchestrators

```csharp
internal async Task ExecuteAsync()
{
    // ...
    await lastFmService.FetchScrobblesSinceAsync(
        fetchAfter: fetchAfter,
        state: state,
        onProgress: async (page, total, elapsed, oldest, newest) =>
        {
            state.Update(page, total, oldest, newest);
            SaveState();
        },
        ct: ct
    );
}
```

### Step 5: Async CLI Commands

Spectre.Console.Cli supports `IAsyncCommand`:

```csharp
public sealed class SyncLastFmCommand : AsyncCommand<SyncLastFmCommand.Settings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context, 
        Settings settings
    )
    {
        var orchestrator = new ScrobbleSyncOrchestrator(
            settings.FromDate, 
            Program.Cts.Token
        );
        await orchestrator.ExecuteAsync();
        return 0;
    }
}
```

---

## Risk Assessment

| Phase            | Risk   | Reason                           |
| ---------------- | ------ | -------------------------------- |
| 1. CT Order      | Low    | Compile-time errors guide fixes  |
| 2. Credentials   | Medium | Auth flow is critical path       |
| 3. LastFm        | Medium | Core sync functionality          |
| 4. Sheets CSV    | Medium | Progress bar complicates async   |
| 5. Orchestrators | High   | Many consumers, state management |
| 6. CLI           | Low    | Spectre supports async naturally |

---

## Testing Strategy

1. **Unit tests** for each migrated method
2. **Integration test**: Full sync flow
3. **Manual verification**: 
   - `scripts sync lastfm`
   - `scripts sync yt`
   - `scripts sync all`

---

## Rollback Plan

Keep sync versions with `_Sync` suffix during migration:
```csharp
// Parallel implementation
internal void FetchScrobblesSince_Sync(...) { /* old code */ }
internal async Task FetchScrobblesSinceAsync(...) { /* new code */ }
```

Remove after validation.
