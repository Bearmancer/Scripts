# Code Inspection: Fields, ToList, Interfaces, and Git

## Table of Contents
1. [Readonly Fields Analysis](#readonly-fields-analysis)
2. [ToList vs Collection Expressions](#tolist-vs-collection-expressions)
3. [Non-Internal Fields Inspection](#non-internal-fields-inspection)
4. [Interface Location Best Practices](#interface-location-best-practices)
5. [VS Code Native Git Truncated View](#vs-code-native-git-truncated-view)
6. [JsonSerializer Caching Fix](#jsonserializer-caching-fix)
7. [Git Author Rewrite](#git-author-rewrite)

---

## Readonly Fields Analysis

### Found: `readonly` without `static`

| File                  | Line | Declaration                                            | Can be `static readonly`?                         |
| --------------------- | ---- | ------------------------------------------------------ | ------------------------------------------------- |
| `OrmandyBoxParser.cs` | 49   | `readonly Query Query = new(...)`                      | ❌ **No** - Contains instance-specific API client  |
| `OrmandyBoxParser.cs` | 50   | `readonly List<string> LogEntries = []`                | ❌ **No** - Instance state (log per parse session) |
| `OrmandyBoxParser.cs` | 51   | `readonly Dictionary<Guid, ParsedWork> WorkCache = []` | ❌ **No** - Instance cache (cleared per session)   |

### Verdict
All three `readonly` fields in `OrmandyBoxParser` are **correctly non-static** because:
- `Query` is an API client that should be instance-scoped
- `LogEntries` and `WorkCache` are per-session state that gets reset when a new parser instance is created

**No changes needed.**

---

## ToList vs Collection Expressions

### Why `.ToList()` is NOT always exchangeable with `[.. sequence]`

| Scenario                               | ToList()                  | Collection Expression | Verdict                 |
| -------------------------------------- | ------------------------- | --------------------- | ----------------------- |
| **Deferred execution needed later**    | ❌ Forces evaluation       | ❌ Forces evaluation   | Same                    |
| **Method returns `List<T>` type**      | ✅ Returns `List<T>`       | ✅ Returns `List<T>`   | **Interchangeable**     |
| **Chained after LINQ with ordering**   | ✅ Preserves order         | ✅ Preserves order     | **Interchangeable**     |
| **Coercion to interface (`IList<T>`)** | ✅ Implicit                | ✅ Implicit            | **Interchangeable**     |
| **Null-coalescing with `?? []`**       | ✅ `items?.ToList() ?? []` | ⚠️ Syntax differs      | Collection expr cleaner |
| **Performance (hot path)**             | ⚠️ Allocates iterator      | ⚠️ Same allocation     | Same                    |

### Cases where `.ToList()` is preferred/required:

```csharp
// 1. Chained with null-conditional operator
var items = collection?.Where(x => x.Active).ToList() ?? [];
// vs collection expression (more verbose):
List<Item> items = collection is null ? [] : [.. collection.Where(x => x.Active)];

// 2. When the expression is complex and spans multiple lines
var result = items
    .Where(x => x.IsValid)
    .OrderBy(x => x.Name)
    .Select(x => x.Id)
    .ToList();  // ← Cleaner than [.. ...] at end of chain

// 3. Method chaining continuation
var filtered = GetItems()
    .Where(x => x.Active)
    .ToList()  // Materialize here
    .ForEach(x => Process(x));  // ← Can't do this with collection expression

// 4. Performance-critical code where avoiding LINQ overhead matters
// Both are similar, but ToList() is more idiomatic for "materialize now"
```

### When collection expressions are cleaner:

```csharp
// Empty list initialization
List<string> items = [];  // ✅ Better than new List<string>() or [].ToList()

// Combining collections
int[] combined = [..first, ..second, ..third];  // ✅ Cleaner than Concat chains

// Inline return with spread
return [..existing, newItem];  // ✅ Cleaner than existing.Append(newItem).ToList()
```

### Your Codebase's ToList Usage: **Mostly Correct**
Most `.ToList()` calls are at the end of LINQ chains and would not benefit from conversion.

---

## Non-Internal Fields Inspection

### All Public `const` Fields

| File                | Field                  | Value                     | Should be internal?     |
| ------------------- | ---------------------- | ------------------------- | ----------------------- |
| `StateManager.cs:5` | `LastFmSyncFile`       | `"lastfm/sync.json"`      | ❌ Used by CLI commands  |
| `StateManager.cs:6` | `LastFmScrobblesFile`  | `"lastfm/scrobbles.json"` | ❌ Used externally       |
| `StateManager.cs:7` | `YouTubeSyncFile`      | `"youtube/sync.json"`     | ❌ Used by orchestrators |
| `StateManager.cs:8` | `BoxSetCacheDirectory` | `"boxsets"`               | ❌ Used by CLI           |
| `Resilience.cs:9`   | `MaxRetries`           | `10`                      | ⚠️ Could be internal     |

### All Public `static readonly` Fields

| File                 | Field               | Type                    | Should be internal?                        |
| -------------------- | ------------------- | ----------------------- | ------------------------------------------ |
| `Resilience.cs:10`   | `BaseDelay`         | `TimeSpan`              | ⚠️ Could be internal                        |
| `Resilience.cs:11`   | `LongDelay`         | `TimeSpan`              | ⚠️ Could be internal                        |
| `Resilience.cs:12`   | `DefaultThrottle`   | `TimeSpan`              | ⚠️ Could be internal                        |
| `Resilience.cs:13`   | `MaxBackoffDelay`   | `TimeSpan`              | ⚠️ Could be internal                        |
| `StateManager.cs:12` | `JsonIndented`      | `JsonSerializerOptions` | ❌ Needs to be public for Logger, CLI usage |
| `StateManager.cs:18` | `JsonCompact`       | `JsonSerializerOptions` | ❌ Needs to be public for Logger, CLI usage |
| `Paths.cs:5-7`       | `ProjectRoot`, etc. | `string`                | ❌ Used across project                      |

### Recommendation
The `Resilience` constants could be `internal` since they're implementation details. However, keeping them `public` doesn't harm anything and aids debugging/testing.

**No immediate changes recommended** - current structure is acceptable.

---

## Interface Location Best Practices

### Current Structure
```
src/
├── Interfaces/
│   ├── IDisposableMailService.cs  (+DTOs)
│   └── IMusicService.cs           (+DTOs)
├── Models/
├── Services/
│   ├── Mail/
│   └── Music/
```

### Analysis

| Interface                | Location    | Records Included                                 | Recommendation            |
| ------------------------ | ----------- | ------------------------------------------------ | ------------------------- |
| `IDisposableMailService` | Interfaces/ | `DisposableMailAccount`, `DisposableMailMessage` | ✅ Keep - defines contract |
| `IMusicService`          | Interfaces/ | `UnifiedMusicRelease`, `UnifiedMusicTrack`, etc. | ✅ Keep - defines contract |

### Best Practice Verdict

For your single-assembly project, the current structure is **acceptable and follows common patterns**:

| Approach                            | Pros                                     | Cons                                   | Your Case          |
| ----------------------------------- | ---------------------------------------- | -------------------------------------- | ------------------ |
| **Separate Interfaces folder**      | Clear separation, easy to find contracts | Low cohesion across feature            | ✅ Current approach |
| **Interfaces with implementations** | High cohesion, locality                  | Can cause coupling issues              | N/A                |
| **Feature-based folders**           | Everything related together              | Shared interfaces need common location | N/A                |

**Recommendation**: Keep interfaces in `Interfaces/` folder. The DTOs (records) defined alongside them are acceptable since they're part of the interface contract.

---

## VS Code Native Git Truncated View

### You're Right! Native Git DOES Support Truncated/Collapsed View

**How to access:**

1. Open a file diff (click on changed file in Source Control)
2. Click the **"..."** menu in the top-right of the diff editor
3. Select **"Toggle Inline View"** for unified view
4. In the toolbar, find the **collapse/expand button** (📁 icon)
5. Click to **collapse unchanged regions** - shows only changed hunks!

### Settings for Default Behavior

```json
// settings.json
{
    // Use inline (unified) view by default
    "diffEditor.renderSideBySide": false,
    
    // Collapse unchanged regions in diff
    "diffEditor.hideUnchangedRegions.enabled": true,
    
    // Minimum lines to show before collapsing
    "diffEditor.hideUnchangedRegions.minimumLineCount": 3
}
```

### Key Difference vs GitLens
- **Native Git**: Must enable "Hide Unchanged Regions" manually or via settings
- **GitLens**: Multi-diff editor shows all files in one scrollable pane

---

## JsonSerializer Caching Fix

### Issue Found
`OrmandyBoxParser.cs` line 80-83 was creating a new `JsonSerializerOptions` on every track dump:

```csharp
// ❌ BAD - Creates new options object every iteration
string json = JsonSerializer.Serialize(
    track,
    new JsonSerializerOptions { WriteIndented = true }  // Allocates every time!
);
```

### Fix Applied ✅
```csharp
// ✅ GOOD - Reuses cached options
string json = JsonSerializer.Serialize(track, StateManager.JsonIndented);
```

### All JsonSerializer Usages (Now Correct)

| File                  | Line | Usage                                                      | Status   |
| --------------------- | ---- | ---------------------------------------------------------- | -------- |
| `OrmandyBoxParser.cs` | 80   | `Serialize(track, StateManager.JsonIndented)`              | ✅ Fixed  |
| `StateManager.cs`     | 32   | `Deserialize<T>(json, JsonCompact)`                        | ✅ Cached |
| `StateManager.cs`     | 38   | `Serialize(state, JsonCompact)`                            | ✅ Cached |
| `StateManager.cs`     | 74   | `Deserialize<List<YouTubeVideo>>`                          | ✅ Cached |
| `StateManager.cs`     | 80   | `Serialize(videos, JsonCompact)`                           | ✅ Cached |
| `StateManager.cs`     | 202  | `Deserialize<T>(json, JsonCompact)`                        | ✅ Cached |
| `StateManager.cs`     | 208  | `Serialize(data, JsonIndented)`                            | ✅ Cached |
| `Logger.cs`           | 210  | `Deserialize<LogEntry>(line, StateManager.JsonCompact)`    | ✅ Cached |
| `Logger.cs`           | 280  | `Serialize(entry, StateManager.JsonCompact)`               | ✅ Cached |
| `SyncCommands.cs`     | 333  | `Deserialize<FetchState>(json, StateManager.JsonIndented)` | ✅ Cached |
| `SyncCommands.cs`     | 362  | `Deserialize<YouTubeFetchState>`                           | ✅ Cached |

---

## Git Author Rewrite

### Backup Created
```
../scripts-backup-[timestamp].bundle
```

To restore: `git clone scripts-backup-*.bundle Scripts-restored`

### Authors to Unify
From `git shortlog -sne --all`:
- Lance <kanishknishar@outlook.com> → Bearmancer
- Lance <lordlance@outlook.in> → Bearmancer
- bearmancer <kanishknisar@outlook.com> → Bearmancer
- bearmancer <kanishknishar@outlook.com> → Bearmancer
- Bearmancer <lordlance@outlook.in> → Keep
- Kanishk <kanishknishar@outlook.com> → Keep

### Mailmap (Already Working)
The `.mailmap` file already unifies display in `git log`, `git blame`, `git shortlog`, and GitLens without rewriting history.

### For Permanent History Rewrite (Optional)
```powershell
# DRY RUN first
git filter-repo --mailmap .mailmap --dry-run --force

# ACTUAL rewrite (destructive!)
git filter-repo --mailmap .mailmap --force

# Force push
git push --force --all
```

---

## Summary

| Task                      | Status                                  |
| ------------------------- | --------------------------------------- |
| Readonly fields analysis  | ✅ All correct (instance-scoped)         |
| ToList documentation      | ✅ Explained why not always exchangeable |
| Non-internal fields       | ✅ All justified, no changes needed      |
| Interface location        | ✅ Current structure is correct          |
| Native Git truncated view | ✅ Confirmed working with settings       |
| JsonSerializer caching    | ✅ Fixed in OrmandyBoxParser             |
| Git backup                | ✅ Bundle created                        |
| Git author unification    | ✅ .mailmap working                      |
