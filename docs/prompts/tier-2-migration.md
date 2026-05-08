# Tier 2 — Build & Migration Prompt (GSheets → PSQL)

> **Plan:** `.kilo/plans/cpm.md` | **Active Task:** `.kilo/prompt/active-task.md`
> **Tasks:** T04–T09 | **Strategy:** Slash-and-burn — delete all Google, rebuild clean against PostgresService

---

## T04 — Slash Google Files

### Objective

Delete all Google Sheets files, orchestrators, and global usings. Remove NuGet packages. Keep YouTube OAuth.

### Actions

1. **Delete 6 Google Sheets files:**

    ```
    csharp/src/Services/Sync/GoogleSheetsService.cs
    csharp/src/Services/Sync/GoogleSheetsContext.cs
    csharp/src/Services/Sync/SheetFormattingService.cs
    csharp/src/Services/Sync/SheetMetadataService.cs
    csharp/src/Services/Sync/SheetRowService.cs
    csharp/src/Services/Sync/SpreadsheetBootstrapper.cs
    ```

2. **Delete 2 orchestrators (rewritten in T07/T08):**

    ```
    csharp/src/Orchestrators/ScrobbleSyncOrchestrator.cs
    csharp/src/Orchestrators/YouTubePlaylistOrchestrator.cs
    ```

3. **Remove from `GlobalUsings.cs`:**
    - All `Google.Apis.Sheets.*` and `Google.Apis.Drive.*` usings
    - All `CSharpScripts.Services.Sync` namespace aliases for Sheets types
    - Narrowly-used usings per research (§5.2): `Frozen`, `ComponentModel`, `static Environment`, `static String`,
      `static StringComparison`, `CsvHelper`, `Auth`, `RestSharp`

4. **Remove NuGet packages from `.csproj`:**
    - `Google.Apis.Sheets.v4`
    - `Google.Apis.Drive.v3`

### Win Gate

```
dotnet restore && dotnet build 2>&1 | Select-String "error CS"
```

Google-related errors should be gone. Remaining errors will be non-Google (fixed in T05).

---

## T05 — Fix Build Errors

### Objective

Fix all remaining build errors after Google code removal.

### Error Categories & Fixes

| Pattern                       | Fix                                                                                                        |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------- | -------------------- |
| `CS1503` Exception→string     | Replace `Log.Error(ex)` with `Log.Error("{Message}", ex.Message)`. Search pattern: `catch.*\{.*Log\.(Error | Warning)._\([^"]_\)` |
| `CS0103 ConfigureServices`    | Add `static void ConfigureServices(IServiceCollection services)` method to `Program.cs`                    |
| `CS0246 SpectreTypeRegistrar` | Define `SpectreTypeRegistrar : ITypeRegistrar` in `Program.cs` or new file                                 |
| `CS0246 MailState`            | Remove `MailStateManager.cs` or define `MailState` record                                                  |
| IDE/CA rules (20×)            | Set `EnforceCodeStyleInBuild=false` in `Directory.Build.props` temporarily, or fix each                    |

### Win Gate

```
dotnet build csharp/CSharpScripts.csproj
```

Exit code 0. Gate G6 passes.

---

## T06 — Expand PostgresService

### Objective

Add all methods needed by the rewritten orchestrators.

### New Methods

```csharp
// Artist lookup-or-insert
Task<Guid> UpsertArtistAsync(string name, CancellationToken ct);

// Album lookup-or-insert
Task<Guid> UpsertAlbumAsync(string title, Guid artistId, CancellationToken ct);

// Track lookup-or-insert
Task<Guid> UpsertTrackAsync(string title, Guid artistId, Guid albumId, CancellationToken ct);

// Scrobble upsert with composite key (TrackId, Timestamp)
Task UpsertScrobbleAsync(Guid trackId, DateTimeOffset timestamp, string platform, CancellationToken ct);

// Get latest timestamp for incremental sync
Task<DateTimeOffset?> GetLatestScrobbleTimestampAsync(CancellationToken ct);

// Batch upsert scrobbles
Task BulkUpsertScrobblesAsync(IEnumerable<Data.Entities.Scrobble> scrobbles, CancellationToken ct);

// YouTube → FiberyEntity storage
Task InsertYouTubeVideoAsync(YouTubeVideo video, CancellationToken ct);
Task InsertYouTubePlaylistAsync(YouTubePlaylist playlist, CancellationToken ct);
```

### Also Fix

- Scrobble entity: drop `Id` column, use composite key `(TrackId, Timestamp)`
- Update `ScriptsDbContext.HasKey` for Scrobble
- Update `init_schema.sql` if needed

### Win Gate

```
Select-String "HasKey.*=>.*new.*TrackId.*Timestamp" csharp/src/Data/ScriptsDbContext.cs
```

Returns match.

---

## T07 — Rewrite ScrobbleSyncOrchestrator

### Objective

~150 lines. Constructor: `(LastFmService, PostgresService, DateTime?, CancellationToken)`.

### Preserved Logic

1. `CreateAsync` — creates `LastFmService`, loads state (simplified)
2. `ExecuteAsync` — fetches scrobbles, normalizes to entities, writes to PG
3. `FetchScrobblesAsync` — delegates to `LastFmService.FetchScrobblesSinceAsync()`
4. `ExecuteForceResyncAsync` — delete + re-fetch
5. `ExecuteIncrementalSyncAsync` — query latest timestamp from PG, fetch since

### Removed

- `GetOrCreateSpreadsheetAsync()`, `WriteToSheetsAsync()`
- `SpreadsheetBootstrapper`, `GoogleSheetsService`
- `latestInSheet = null` dead branch
- File-based `SaveStateAsync` → DB queries

### Win Gate

```
Select-String "SheetsService|Bootstrapper|WriteToSheets" csharp/src/Orchestrators/ScrobbleSyncOrchestrator.cs
```

Returns 0 matches.

---

## T08 — Rewrite YouTubePlaylistOrchestrator

### Objective

~400 lines. Constructor:
`(YouTubeService, PostgresService, YouTubeChangeDetector, bool previewMode, CancellationToken)`.

### Preserved Logic

1. Full sync — fetch all playlists, detect changes, store videos/playlists
2. Optimized sync — ETag-based incremental
3. YouTubeChangeDetector integration

### Changed

- Store as `FiberyEntity` rows (`entity_type='youtube_video'` / `'youtube_playlist'`)
- No Google Sheets writes
- No `SpreadsheetBootstrapper`

### Win Gate

```
Select-String "SheetsService|Bootstrapper" csharp/src/Orchestrators/YouTubePlaylistOrchestrator.cs
```

Returns 0 matches. Gate G8 passes.

---

## T09 — CleanResetCommand Fix

### Objective

Remove ~30 lines of Google Sheets code from reset methods.

### Changes

- `ResetLastFmAsync`: delete state files + `TRUNCATE scrobbles, tracks, albums, artists`
- `ResetYouTubeAsync`: delete state files + `DELETE FROM fibery_entities WHERE entity_type LIKE 'youtube_%'`
- Remove `GoogleSheetsService? sheets` parameter plumbing

### Win Gate

```
Select-String "GoogleSheetsService" csharp/src/CLI/Clean/CleanResetCommand.cs
```

Returns 0 matches.

