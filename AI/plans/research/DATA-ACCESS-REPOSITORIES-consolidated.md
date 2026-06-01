# Data Access & Repository Pattern — Consolidated Research

**Consolidated from:** 20260522-t1-06-repositories-research.md, 20260522-t1-09-sync-service-research.md

---

## 1. Current Data Access Architecture

### 1.1 Only Data Access Service: `PostgresService`

**File:** `csharp/src/Services/PostgresService.cs:7-41`

```csharp
internal sealed class PostgresService(IDbContextFactory<ScriptsDbContext> contextFactory)
{
    internal async Task UpsertScrobbleAsync(long id, int trackId, DateTimeOffset timestamp, string platform, CancellationToken ct);
    internal async Task BulkInsertTracksAsync(IEnumerable<Track> tracks, CancellationToken ct);
}
```

- **Injection pattern:** Primary constructor with `IDbContextFactory<ScriptsDbContext>`
- **Scope management:** Creates new context per method via `CreateDbContextAsync()`, disposes via `await using`
- **Both EF Core mutation patterns used:** `ExecuteUpdateAsync` (single-entity upsert) and `SaveChangesAsync` (bulk insert with `AddRange`)

### 1.2 Mutation Patterns in Use

| Operation            | Pattern              | File                    | Status                                        |
| -------------------- | -------------------- | ----------------------- | --------------------------------------------- |
| `ExecuteUpdateAsync` | Single-entity upsert | `PostgresService.cs:20` | ✅ Correct                                     |
| `SaveChangesAsync`   | Bulk insert          | `PostgresService.cs:39` | ⚠️ Should use `ExecuteUpdateAsync` for upserts |
| `ExecuteDeleteAsync` | Bulk delete          | —                       | ❌ Never used                                  |

---

## 2. Duplicate File Analysis

### 2.1 Two `LastFmService.cs` Files — Same Namespace, Same Class Name

| Aspect              | `Sync/LastFmService.cs` (175 lines)                    | `Sync/LastFm/LastFmService.cs` (165 lines)             |
| ------------------- | ------------------------------------------------------ | ------------------------------------------------------ |
| Namespace           | `CSharpScripts.Services.Sync.LastFm`                   | `CSharpScripts.Services.Sync.LastFm`                   |
| Class               | `internal sealed class LastFmService`                  | `public class LastFmService`                           |
| Models              | Uses `using Scrobble = CSharpScripts.Models.Scrobble;` | **Defines inline** `Scrobble` and `FetchState` records |
| StateManager        | Async (`SaveStateAsync`, `LoadStateAsync`)             | Sync (`Save`, `Load`)                                  |
| Logging             | `Log.Debug` / `Log.Information` / `Log.Warning`        | `Console.Info`                                         |
| `Scrobble.PlayedAt` | `DateTimeOffset?` (matches `Models/LastFm.cs`)         | `DateTime?` (mismatch)                                 |

### 2.2 Verdict

**`Sync/LastFm/LastFmService.cs` is the older/legacy duplicate.** It redefines models that already exist in `Models/LastFm.cs`, uses synchronous StateManager APIs, and has lower visibility.

**Action:** Delete `csharp/src/Services/Sync/LastFm/LastFmService.cs`. The entire `Sync/LastFm/` subdirectory can be removed.

---

## 3. ILike / EF.Functions.Like Usage

**Finding:** No `EF.Functions.ILike` or `EF.Functions.Like` references exist anywhere in the codebase. This is a greenfield capability — nothing to refactor, only to add.

### 3.1 Where ILike WILL Be Needed (Future DB Queries)

| Entity   | String Field | Query Pattern                             | Current DB Index                      |
| -------- | ------------ | ----------------------------------------- | ------------------------------------- |
| `Artist` | `Name`       | Lookup by name before insert              | `idx_artists_name` (unique)           |
| `Track`  | `Title`      | Lookup by title + artist_id before insert | `idx_tracks_title`                    |
| `Album`  | `Title`      | Lookup by title + artist_id before insert | `idx_albums_title` (unique composite) |

---

## 4. Repository Pattern Recommendation

**No legacy Repository\<T\> pattern.** Use thin wrappers per domain entity.

### 4.1 Recommended Structure

```
csharp/src/Data/Repositories/
├── IScrobbleRepository.cs + ScrobbleRepository.cs
├── IVideoRepository.cs + VideoRepository.cs
├── ITrackRepository.cs + TrackRepository.cs
├── IArtistRepository.cs + ArtistRepository.cs
├── IAlbumRepository.cs + AlbumRepository.cs
├── IExecutionLogRepository.cs + ExecutionLogRepository.cs
└── IFailedTaskRepository.cs + FailedTaskRepository.cs
```

### 4.2 Mutation Strategy

| Scenario                        | Recommendation                  | Reason              |
| ------------------------------- | ------------------------------- | ------------------- |
| Single-entity upsert (PK known) | `ExecuteUpdateAsync`            | No tracking, faster |
| Bulk insert                     | `AddRange` + `SaveChangesAsync` | Batching            |
| Bulk delete                     | `ExecuteDeleteAsync`            | EF mandate          |
| Bulk update                     | `ExecuteUpdateAsync`            | EF mandate          |

### 4.3 Interface Contracts (Recommended)

#### IScrobbleRepository
```csharp
Task UpsertAsync(long id, int trackId, DateTimeOffset timestamp, string platform, CancellationToken ct = default);
Task<List<Scrobble>> GetByTrackIdAsync(int trackId, CancellationToken ct = default);
Task<List<Scrobble>> GetByPlatformAsync(string platform, int limit, CancellationToken ct = default);
Task<Scrobble?> GetByIdAsync(long id, CancellationToken ct = default);
Task<int> DeleteByTrackIdAsync(int trackId, CancellationToken ct = default);
Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default);
```

#### ITrackRepository
```csharp
Task BulkInsertAsync(IEnumerable<Track> tracks, CancellationToken ct = default);
Task<Track?> GetByIdAsync(int id, CancellationToken ct = default);
Task<List<Track>> GetByArtistIdAsync(int artistId, CancellationToken ct = default);
Task<Track?> GetByTitleAndArtistAsync(string title, int artistId, CancellationToken ct = default);
```

#### IVideoRepository
```csharp
Task AddAsync(Video video, CancellationToken ct = default);
Task<Video?> GetByUrlAsync(string url, CancellationToken ct = default);
Task<Video?> GetByIdAsync(long id, CancellationToken ct = default);
Task<List<Video>> GetByChannelAsync(string channelName, CancellationToken ct = default);
Task<int> UpdateTitleAsync(long id, string title, CancellationToken ct = default);
Task<int> DeleteByIdAsync(long id, CancellationToken ct = default);
```

#### IArtistRepository
```csharp
Task<Artist?> GetByNameAsync(string name, CancellationToken ct = default);
Task<Artist?> GetByIdAsync(int id, CancellationToken ct = default);
Task AddAsync(Artist artist, CancellationToken ct = default);
Task<int> UpsertMetadataAsync(int id, JsonDocument metadata, CancellationToken ct = default);
```

#### IAlbumRepository
```csharp
Task<Album?> GetByArtistAndTitleAsync(int artistId, string title, CancellationToken ct = default);
Task<Album?> GetByIdAsync(int id, CancellationToken ct = default);
Task<List<Album>> GetByArtistIdAsync(int artistId, CancellationToken ct = default);
Task AddAsync(Album album, CancellationToken ct = default);
```

#### IExecutionLogRepository
```csharp
Task AddAsync(ExecutionLog log, CancellationToken ct = default);
Task<List<ExecutionLog>> GetRecentAsync(int limit, CancellationToken ct = default);
Task<ExecutionLog?> GetBySessionIdAsync(string sessionId, CancellationToken ct = default);
```

#### IFailedTaskRepository
```csharp
Task AddAsync(FailedTask task, CancellationToken ct = default);
Task<List<FailedTask>> GetUnresolvedAsync(CancellationToken ct = default);
Task<int> MarkResolvedAsync(int id, CancellationToken ct = default);
```

### 4.4 DI Registration

```csharp
services.AddScoped<IScrobbleRepository, ScrobbleRepository>();
services.AddScoped<IVideoRepository, VideoRepository>();
services.AddScoped<ITrackRepository, TrackRepository>();
services.AddScoped<IArtistRepository, ArtistRepository>();
services.AddScoped<IAlbumRepository, AlbumRepository>();
services.AddScoped<IExecutionLogRepository, ExecutionLogRepository>();
services.AddScoped<IFailedTaskRepository, FailedTaskRepository>();
```

### 4.5 Constructor & DI Pattern

Follow the existing `PostgresService` pattern:

```csharp
internal sealed class ScrobbleRepository(IDbContextFactory<ScriptsDbContext> contextFactory) : IScrobbleRepository
{
    public async Task UpsertAsync(...)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        // ... ExecuteUpdateAsync
    }
}
```

---

## 5. Summary of Actions Required

| #   | Action                                                                                   | File(s)                                     | Priority |
| --- | ---------------------------------------------------------------------------------------- | ------------------------------------------- | -------- |
| 1   | **Delete** duplicate `LastFmService.cs`                                                  | `src/Services/Sync/LastFm/LastFmService.cs` | HIGH     |
| 2   | **Delete** entire `LastFm/` subdirectory                                                 | `src/Services/Sync/LastFm/`                 | HIGH     |
| 3   | **Inject** `IDbContextFactory<ScriptsDbContext>` into `LastFmService`                    | `src/Services/Sync/LastFmService.cs`        | HIGH     |
| 4   | **Replace** `StateManager.SaveStateAsync` with `ExecuteUpdateAsync`/`ExecuteDeleteAsync` | `src/Services/Sync/LastFmService.cs`        | HIGH     |
| 5   | **Add** `EF.Functions.ILike` for artist/track/album name lookups                         | `src/Services/Sync/LastFmService.cs`        | MEDIUM   |
| 6   | **Replace** `SaveChangesAsync` with `ExecuteUpdateAsync` (upsert)                        | `src/Services/PostgresService.cs:39`        | MEDIUM   |
| 7   | **Create 7 repository pairs** (interface + implementation)                               | `src/Data/Repositories/`                    | HIGH     |

---

## 6. File Paths

```
Current Data Access:
  C:\Users\Lance\Dev\Scripts\csharp\src\Services\PostgresService.cs

Duplicate to Delete:
  C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFm\LastFmService.cs

Canonical LastFmService:
  C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFmService.cs

Target Repository Location:
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Repositories\
```
