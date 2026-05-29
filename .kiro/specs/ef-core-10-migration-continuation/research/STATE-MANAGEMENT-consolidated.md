# State Management & Caching — Consolidated Research

**Consolidated from:** 20260522-t1-07-state-manager-research.md, 20260522-t1-08-release-cache-research.md

---

## 1. StateManager Duplicate Analysis

### 1.1 Two StateManager Implementations

| Aspect          | `Core/Persistence/StateManager.cs`                        | `Infrastructure/StateManager.cs`            |
| --------------- | --------------------------------------------------------- | ------------------------------------------- |
| Namespace       | `CSharpScripts.Core`                                      | `CSharpScripts.Infrastructure`              |
| Access          | `internal static class`                                   | `public static class`                       |
| Logging         | Serilog (`Log.Warning`, `Log.Debug`)                      | Console (`Console.Warning`, `Console.Info`) |
| Pipeline cached | YES (`ConcurrentDictionary`)                              | NO (new pipeline per call)                  |
| Async-first     | YES (`LoadStateAsync<T>()`, `SaveStateAsync<T>()`)        | NO (sync only)                              |
| Features        | All (playlist cache CRUD, release cache CRUD, migrations) | Minimal (retry only)                        |

### 1.2 Verdict

**Core version is the canonical implementation.** Infrastructure version is a legacy synchronous copy used by older code that was never migrated.

**Action:** Delete `Infrastructure/StateManager.cs` and `Infrastructure/Paths.cs` (duplicate).

---

## 2. StateManager Usage Reference

### 2.1 Active Callers (Core StateManager)

| Consumer                    | File                                                                         | Methods Called                                                                            |
| --------------------------- | ---------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| ScrobbleSyncOrchestrator    | `src/Orchestrators/ScrobbleSyncOrchestrator.cs:34,173,176`                   | `LoadStateAsync<FetchState>()`, `SaveStateAsync()`                                        |
| YouTubePlaylistOrchestrator | `src/Orchestrators/YouTubePlaylistOrchestrator.cs:41,51,261,685,714,792,796` | `LoadStateAsync<YouTubeFetchState>()`, `SaveStateAsync()`, `MigratePlaylistFiles()`, etc. |
| LastFmService (modern)      | `src/Services/Sync/LastFmService.cs:130,166,169`                             | `SaveStateAsync()`, `LoadStateAsync<List<Scrobble>>()`, `Delete()`                        |
| CleanResetCommand           | `src/CLI/Clean/CleanResetCommand.cs:53,58,67,72`                             | `LoadStateAsync<>()`, `DeleteLastFmStates()`, `DeleteAllYouTubeStates()`                  |
| CleanCacheCommand           | `src/CLI/Clean/CleanCacheCommand.cs:31,39`                                   | `DeleteLastFmStates()`, `DeleteAllYouTubeStates()`                                        |
| MusicSearchCommand          | `src/CLI/MusicSearchCommand.cs:792,807,840,892,1007`                         | `DeleteReleaseCache()`, `LoadReleaseCache<>()`, `SaveReleaseCache<>()`                    |

### 2.2 JsonIndented/JsonCompact Consumers

| Consumer              | File                                            | Usage                       |
| --------------------- | ----------------------------------------------- | --------------------------- |
| TranslationClient     | `src/Services/Language/TranslationClient.cs:99` | `StateManager.JsonCompact`  |
| MusicBrainzService    | `src/Services/Music/MusicBrainzService.cs:140`  | `StateManager.JsonIndented` |
| Logger                | `src/Infrastructure/Logger.cs:236,305`          | `StateManager.JsonCompact`  |
| SyncCommands (legacy) | `src/CLI/SyncCommands.cs:391,425`               | `StateManager.JsonIndented` |

---

## 3. Target Directory — `csharp/src/Data/State/`

- **Current state**: Directory does NOT exist. `csharp/src/Data/` exists with subdirectories `Configuration/`, `Entities/`.
- **Need to create**: `csharp/src/Data/State/`
- **Target namespace**: `CSharpScripts.Data.State` (follows existing pattern — `ScriptsDbContext` is in `CSharpScripts.Data`)

---

## 4. Migration Plan — Step-by-Step

### Phase A: Create Target Location
1. Create directory `csharp/src/Data/State/`
2. Move `csharp/src/Core/Persistence/StateManager.cs` → `csharp/src/Data/State/StateManager.cs`
3. Move `csharp/src/Core/Persistence/ReleaseProgressCache.cs` → `csharp/src/Data/State/ReleaseProgressCache.cs` (co-located)
4. Delete empty `csharp/src/Core/Persistence/` directory

### Phase B: Namespace Update
1. Change `namespace CSharpScripts.Core;` → `namespace CSharpScripts.Data.State;` in StateManager.cs
2. Verify `using` directives resolve (all dependencies are globalled)

### Phase C: Add Global Using
1. Add `global using CSharpScripts.Data.State;` to `csharp/src/GlobalUsings.cs`

### Phase D: Update Callers
1. All callers using `global using CSharpScripts.Core;` already have `CSharpScripts.Data;` globalled
2. With the new global using + same class name `StateManager`, callers will resolve correctly

### Phase E: Clean Up Infrastructure Duplicate
1. Remove or stub `csharp/src/Infrastructure/StateManager.cs`
2. Legacy `LastFm/LastFmService.cs` (sync) must be updated to use async API or removed
3. `Logger.cs` in Infrastructure needs to reference `StateManager.JsonCompact` via new namespace

---

## 5. ReleaseProgressCache Migration

### 5.1 Current Architecture — Dual Caching System

| Cache                       | File Format             | Storage Path                      | Data                                                      |
| --------------------------- | ----------------------- | --------------------------------- | --------------------------------------------------------- |
| `ReleaseProgressCache`      | CSV (CsvHelper)         | `state/cache/{releaseId}.csv`     | Per-track `TrackInfo` records, appended incrementally     |
| `StateManager.ReleaseCache` | JSON (System.Text.Json) | `state/releases/{releaseId}.json` | Batch `MusicBrainzEnrichmentState` (full list + metadata) |

### 5.2 Entity Design Recommendation

**Option A: Per-track entity (relational, incremental append)** — RECOMMENDED

```csharp
internal sealed record ReleaseProgress
{
    public long Id { get; set; }
    public string ReleaseId { get; set; } = null!;
    public int DiscNumber { get; set; }
    public int TrackNumber { get; set; }
    public string Title { get; set; } = null!;
    public string? Duration { get; set; }
    public int? RecordingYear { get; set; }
    public string? Composer { get; set; }
    public string? WorkName { get; set; }
    public string? Conductor { get; set; }
    public string? Orchestra { get; set; }
    public string? Soloists { get; set; }
    public string? Artist { get; set; }
    public string? RecordingVenue { get; set; }
    public string? RecordingId { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Pros:** Incremental append, crash-resilient, queryable per-track  
**Cons:** 14 columns, schema duplication of TrackInfo, migration complexity

### 5.3 Configuration

```csharp
b.ToTable("release_progress");
b.HasKey(e => e.Id);
b.Property(e => e.Id).ValueGeneratedOnAdd();
b.HasIndex(e => new { e.ReleaseId, e.DiscNumber, e.TrackNumber }).IsUnique();
b.Property(e => e.ReleaseId).HasColumnType("text");
b.Property(e => e.Soloists).HasColumnType("jsonb");
b.Property(e => e.CreatedAt).HasColumnType("timestamptz").HasDefaultValueSql("CURRENT_TIMESTAMP");
```

### 5.4 DbContext Addition

```csharp
public DbSet<ReleaseProgress> ReleaseProgress => Set<ReleaseProgress>();
```

---

## 6. File Paths

```
Current State Management:
  C:\Users\Lance\Dev\Scripts\csharp\src\Core\Persistence\StateManager.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Core\Persistence\ReleaseProgressCache.cs

Duplicate to Delete:
  C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\StateManager.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\Paths.cs

Target Location:
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\State\StateManager.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\State\ReleaseProgressCache.cs

Global Usings:
  C:\Users\Lance\Dev\Scripts\csharp\src\GlobalUsings.cs
```
