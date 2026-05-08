# C# Migration Specification

> **Source:** Reconstructed from `.kilo/logs/csharp-migration-research.md`, `.kilo/logs/spec-analysis-boundaries.md`,
> `.kilo/logs/qa-verification-report.md`
> **Purpose:** Architecture decisions, file inventories, code mappings, and actionable plans for PG/Neon migration
> **Excludes:** §1 (index/roadmap) and all R&A commentary

---

## §2 — Architecture & Design Decisions

### §2.1 — Scrobble PK (Composite Key Design)

**Context:** The Hqub.Lastfm library does NOT return a scrobble ID. The only unique field is `PlayedAt` (timestamp).

**Resolution:** Drop `id BIGINT PRIMARY KEY`. Use composite key `(track_id, timestamp)` on scrobbles. Upsert on
`(track_id, timestamp)` conflict. This is correct because:

- A user can't scrobble the same track at the exact same second twice
- Timestamps come from Last.fm's server, not local clock
- The existing `GetNewScrobblesAsync` method already diffs by `PlayedAt`

**Entity changes:**

```csharp
// REMOVE from Scrobble.cs:
// public long Id { get; set; }

// In ScriptsDbContext.cs OnModelCreating:
entity.HasKey(e => new { e.TrackId, e.Timestamp });
```

**PostgresService changes:**

- Change `UpsertScrobbleAsync(long id, ...)` → `UpsertScrobbleAsync(Guid trackId, DateTimeOffset timestamp, ...)`
- Update `ON CONFLICT (id)` → `ON CONFLICT (track_id, timestamp) DO UPDATE`

---

### §2.2 — Google Sheets Deletion Scope (6 Files)

**Action:** Delete all 6 files post-migration. These form the Google Sheets sink being replaced by PostgreSQL.

| #   | File                                                  | Size         |
| --- | ----------------------------------------------------- | ------------ |
| 1   | `csharp/src/Services/Sync/GoogleSheetsService.cs`     | 36,203 chars |
| 2   | `csharp/src/Services/Sync/GoogleSheetsContext.cs`     | 2,376 chars  |
| 3   | `csharp/src/Services/Sync/SheetFormattingService.cs`  | 1,258 chars  |
| 4   | `csharp/src/Services/Sync/SheetMetadataService.cs`    | 11,126 chars |
| 5   | `csharp/src/Services/Sync/SheetRowService.cs`         | 10,969 chars |
| 6   | `csharp/src/Services/Sync/SpreadsheetBootstrapper.cs` | 1,165 chars  |

**Verification:** ALL 6 FILES EXIST as confirmed by QA report.

---

### §2.3 — DTO Types (Model vs Entity Separation)

**Name collision in current code:**

| Type                 | Namespace                              | Purpose                                                                       |
| -------------------- | -------------------------------------- | ----------------------------------------------------------------------------- |
| `Scrobble`           | `CSharpScripts.Models.Scrobble`        | **DTO** from Last.fm API — flat: `TrackName, ArtistName, AlbumName, PlayedAt` |
| `Scrobble` (implied) | `CSharpScripts.Data.Entities.Scrobble` | **Entity** for DB — relational: `TrackId, Timestamp, Platform`                |

**Current global alias:**

```csharp
global using Scrobble = CSharpScripts.Models.Scrobble;
```

**Resolution:** Keep the alias as-is (Models.Scrobble used in 10+ files). Only orchestrators need the entity type, and
they can fully-qualify it:

```csharp
var dto = new Scrobble(trackName, artistName, albumName, playedAt); // Models.Scrobble
var entity = new Data.Entities.Scrobble { TrackId = trackId, ... }; // fully qualified
```

---

### §2.4 — DbContext Enum Mapping

**Resolution:** Keep `string` for the `Platform` property in the entity. The SQL schema has:

```sql
CREATE TYPE platform AS ENUM ('lastfm', 'youtube', 'other');
```

Map in DbContext fluent config:

```csharp
entity.Property(e => e.Platform).HasColumnType("platform");
```

EF Core Npgsql supports enum mapping natively. No conversion logic needed in C#.

---

### §2.5 — StateManager Refactor Boundaries

**Delete lines 76-118:** Schema initialization code (one-time migration, already executed).
**Delete lines 243-293:** Legacy migration code (already executed).

**Result:** StateManager reduced by ~93 lines. Only lines 1-75 and 119-242 and 294-359 remain. These cover active state
management: save/load state, JSON serialization, file I/O.

**NOT deleted:** The remaining 266 lines of StateManager are still needed as intermediate cache layer for JSON state
files (`state/lastfm/scrobbles.json`, `state/youtube/playlists/*.json`).

---

### §2.6 — PostgresService Upsert Pattern

**Method:** `UpsertScrobbleAsync` uses `ExecuteUpdateAsync` — confirmed compliant with `.kilo/rules/standards.md`:

```csharp
// Pattern (simplified):
await context.Scrobbles
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(s => s.TrackId, trackId)
        .SetProperty(s => s.Timestamp, timestamp),
        cancellationToken);
```

**Standards compliance:**

- `ExecuteUpdate` / `ExecuteDelete` for mutations ✅ — no `SaveChanges()` loops
- No Dapper ✅ — only EF Core
- No legacy Repository pattern ✅ — `PostgresService` is a thin service

**Upsert signature (post-fix):**

```csharp
Task UpsertScrobbleAsync(Guid trackId, DateTimeOffset timestamp, ...);
```

---

### §2.7 — MigrationExecutor Design

**Purpose:** Execute SQL migration scripts against the PostgreSQL database. Handles `init_schema.sql` deployment and any
subsequent migration steps.

**Design principles:**

- Reads `.sql` files from `powershell/` directory
- Executes via `psql -U lance -d scripts_local -f <file>`
- Logs execution to `execution_logs` table
- Handles idempotent re-runs via `IF NOT EXISTS` guards
- Reports schema version after completion

**Dependencies:** `init_schema.sql` (7 tables: artists, albums, tracks, scrobbles, fibery_entities, execution_logs,
failed_tasks)

---

### §2.8 — SyncOrchestrator Retry Logic

**Components affected:**

- `ScrobbleSyncOrchestrator` — retry on Last.fm API failure
- `YouTubePlaylistOrchestrator` — retry on YouTube API failure

**Pattern:** Use exponential backoff with jitter. Max 3 retries. Log each attempt to `execution_logs`. On final failure,
write to `failed_tasks` table.

**Integration points:**

- `Resilience.cs` (1 file, uses `System.Collections.Frozen`) provides the retry policy
- State files serve as checkpoints: on resume, skip already-fetched pages
- `ExecutionLog` tracks per-sync status (Started/Running/Failed/Completed)

---

### §2.9 — FiberyEntity JSONB Redesign

**Entity schema (Data.Entities.FiberyEntity):**

| Property     | Type            | Notes                                                        |
| ------------ | --------------- | ------------------------------------------------------------ |
| `Id`         | `Guid`          | PK, generated                                                |
| `FiberyId`   | `string`        | Natural key from source (e.g., `VideoId` for YouTube)        |
| `EntityType` | `string`        | `"youtube_video"`, `"youtube_playlist"`, `"lastfm_scrobble"` |
| `RawData`    | `JsonDocument?` | JSONB — stores full source payload                           |

**YouTube storage pattern:**

- `entity_type = 'youtube_video'`, `fibery_id = VideoId`
- `entity_type = 'youtube_playlist'`, `fibery_id = Id` (playlist ID)
- RawData stores serialized `Models.YouTubeVideo` as JSONB

**Table rename (planned):** `fibery_entities` → `source_records` per §8.0 Fibery Expurgation plan. Currently deployed as
`fibery_entities`.

---

### §2.10 — ExecutionLog Refactor Approach

**Table structure (existing, 7 tables confirmed in QA):**

| Column              | Type         | Purpose                          |
| ------------------- | ------------ | -------------------------------- |
| `id`                | serial       | PK                               |
| `command`           | text         | CLI command executed             |
| `status`            | text         | Started/Running/Failed/Completed |
| `started_at`        | timestamptz  | Sync start                       |
| `completed_at`      | timestamptz? | Sync end                         |
| `error_message`     | text?        | Failure details                  |
| `records_processed` | int?         | Count of items synced            |

**Usage in code:**

- `SyncAllCommand`, `SyncLastFmCommand`, `SyncYouTubeCommand` write entries on start/complete
- `HistoryCommand.ShowLastFmStatusAsync` reads from `execution_logs` (replacing Google Sheets read)
- No schema changes needed — table already matches spec §4

---

### §2.11 — MailService Removal

**Files to delete (post-migration):** All files under `CSharpScripts.Services.Mail` namespace. Mail is an independent
feature with no DB dependency.

**Global usings to remove:**

```csharp
// Remove from GlobalUsings.cs:
global using CSharpScripts.Services.Mail;
```

**Reason:** Mail services (email notifications) are unrelated to the sync/migration pipeline. They can be removed as
dead code in a separate cleanup pass. The `MailState` type referenced in `Program.cs` is a build blocker that must be
resolved (either define the type or delete the reference).

---

## §3 — Feature Inventory & Compatibility Assessment

### §3.1 — CLI Commands Impact Matrix

| Command              | Pre-Migration Sink | Post-Migration Sink | Change                                 |
| -------------------- | ------------------ | ------------------- | -------------------------------------- |
| `SyncLastFmCommand`  | Google Sheets      | PostgreSQL          | Refactor orchestrator                  |
| `SyncYouTubeCommand` | Google Sheets      | PostgreSQL          | Refactor orchestrator                  |
| `SyncAllCommand`     | Google Sheets      | PostgreSQL          | Wrapper, minimal change                |
| `HistoryCommand`     | Google Sheets      | PostgreSQL          | Replace sheet read with DB query       |
| `CleanResetCommand`  | Google Sheets      | PostgreSQL          | Truncate tables instead of sheet clear |

### §3.2 — Package Reference Impact

| Package                                    | Action | Reason                         |
| ------------------------------------------ | ------ | ------------------------------ |
| `Google.Apis.Sheets.v4`                    | Remove | Sheets sink deleted            |
| `Google.Apis.Drive.v3`                     | Remove | Sheets sink deleted            |
| `Google.Apis.Auth`                         | Keep   | Still needed for YouTube OAuth |
| `Google.Apis.YouTube.v3`                   | Keep   | YouTube API access             |
| `Npgsql`                                   | Keep   | PostgreSQL provider            |
| `Microsoft.EntityFrameworkCore`            | Keep   | ORM                            |
| `Microsoft.Extensions.DependencyInjection` | Keep   | DI container                   |

### §3.3 — Global Usings Impact

**To remove (Google-specific):**

```csharp
global using Google.Apis.Drive.v3;
global using Google.Apis.Services;
global using Google.Apis.Sheets.v4;
global using Google.Apis.Sheets.v4.Data;
```

**To keep (YouTube + core):**

```csharp
global using Google.Apis.YouTube.v3.Data;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Serilog;
global using Spectre.Console;
global using Spectre.Console.Cli;
```

**To add (new):**

```csharp
global using CSharpScripts.Data;
global using CSharpScripts.Data.Entities;
```

---

## §4 — Schema-to-Entity Mapping

| SQL Table         | C# Entity                    | Notes                                                                    |
| ----------------- | ---------------------------- | ------------------------------------------------------------------------ |
| `artists`         | `Data.Entities.Artist`       | `Id` (Guid), `Name`, `Mbid`?, `Metadata` (JsonDocument?)                 |
| `albums`          | `Data.Entities.Album`        | `Id` (Guid), `Title`, `ReleaseDate` (DateOnly?), `ArtistId` (FK)         |
| `tracks`          | `Data.Entities.Track`        | `Id` (Guid), `Title`, `Duration` (int?), `ArtistId` (FK), `AlbumId` (FK) |
| `scrobbles`       | `Data.Entities.Scrobble`     | `TrackId` (Guid FK), `Timestamp` (DateTimeOffset), `Platform` (string)   |
| `fibery_entities` | `Data.Entities.FiberyEntity` | `Id` (Guid), `FiberyId`, `EntityType`, `RawData` (JsonDocument?)         |
| `execution_logs`  | `Data.Entities.ExecutionLog` | `Id` (int), `Command`, `Status`, timestamps                              |
| `failed_tasks`    | `Data.Entities.FailedTask`   | `Id` (int), error details                                                |

**Composite PK:** `scrobbles` uses `(track_id, timestamp)` — no surrogate `id` column.

---

## §5 — Existing Code → New Code Mapping

### §5.1 — ScrobbleSyncOrchestrator

| Old (Google Sheets)                                  | New (PostgreSQL)                      |
| ---------------------------------------------------- | ------------------------------------- |
| `GoogleSheetsService.CreateAsync()` → auth           | DI injected `PostgresService`         |
| `WriteScrobblesAsync()` → sheet append               | `UpsertScrobbleAsync()` → ON CONFLICT |
| `GetNewScrobblesAsync()` → sheet read max time       | `SELECT MAX(timestamp)` query         |
| `DeleteScrobblesOnOrAfterAsync()` → sheet row delete | `ExecuteDeleteAsync()`                |
| `GetLatestScrobbleTimeAsync()`                       | `SELECT MAX(timestamp)` query         |
| `EnsureSheetExistsAsync()`                           | DB schema already exists              |

### §5.2 — YouTubePlaylistOrchestrator

| Old (Google Sheets)                        | New (PostgreSQL)                |
| ------------------------------------------ | ------------------------------- |
| `GoogleSheetsService.CreateAsync()` → auth | DI injected `PostgresService`   |
| `WritePlaylistAsync()` → sheet write       | `FiberyEntity` upsert via JSONB |
| Bootstrapper sheet setup                   | Schema pre-deployed             |

### §5.3 — HistoryCommand

| Old (Google Sheets)                         | New (PostgreSQL)                                  |
| ------------------------------------------- | ------------------------------------------------- |
| `GoogleSheetsService.CreateAsync()` → auth  | DI injected `PostgresService` or direct DbContext |
| `GetScrobbleCountAsync()` → sheet cell      | `SELECT COUNT(*) FROM scrobbles`                  |
| `GetLatestScrobbleTimeAsync()` → sheet cell | `SELECT MAX(timestamp) FROM scrobbles`            |

---

## §6 — Pending Implementation To-Do

- [ ] `ConfigureServices` in Program.cs — PENDING (build blocker)
- [ ] `SpectreTypeRegistrar` — PENDING (build blocker)
- [ ] `MailState` type — PENDING (build blocker)
- [ ] Fix 15× CS1503 Exception→string logging errors
- [ ] Resolve 20× IDE/CA code style rule violations
- [x] DB schema deployed (7 tables confirmed)
- [x] Npgsql package referenced

---

## §8 — Critical Open Questions (RESOLVED)

### §8.0 — Fibery Expurgation: Rename Plan

**Current:** Table named `fibery_entities`. Entity class named `FiberyEntity`.

**Target:** Rename to `source_records`. All references updated across codebase.

| Current                  | Target              | Location                     |
| ------------------------ | ------------------- | ---------------------------- |
| `fibery_entities` (SQL)  | `source_records`    | `powershell/init_schema.sql` |
| `FiberyEntity` (C#)      | `SourceRecord`      | `csharp/src/Data/Entities/`  |
| `FiberyEntities` (DbSet) | `SourceRecords`     | `ScriptsDbContext.cs`        |
| `Set<FiberyEntity>`      | `Set<SourceRecord>` | All query references         |
| `fibery_id` column       | `source_id`         | SQL column rename            |
| `entity_type` column     | `source_type`       | SQL column rename            |

**Blocked on:** Schema rename not yet applied. DB QA confirms `fibery_entities` still deployed.

---

## §9 — Dependency Graph After Migration

```
┌─────────────┐    ┌──────────────────┐    ┌───────────────┐
│ CLI Command  │───▶│ Orchestrator     │───▶│ PostgresService│
│ (Spectre)    │    │ (sync logic)     │    │ (thin service) │
└─────────────┘    └──────────────────┘    └───────┬───────┘
                                                   │
                                                   ▼
                                            ┌──────────────┐
                                            │ ScriptsDbCont │
                                            │  (EF Core)    │
                                            └──────┬───────┘
                                                   │
                                                   ▼
                                            ┌──────────────┐
                                            │  PostgreSQL   │
                                            │ scripts_local │
                                            └──────────────┘
```

**Principle:** `PostgresService` is the only service with a DbContext dependency. Orchestrators depend on
`PostgresService` (abstraction), not EF Core directly. No generic repository pattern.

---

## §11 — Class Relationship Graph — FUTURE State

```
┌──────────────┐    ┌────────────────────┐    ┌───────────────┐
│ Program.cs   │───▶│ PostgresService    │◀───│ Orchestrators │
│ (DI setup)   │    │ (scoped, injected) │    │ (injected)    │
└──────────────┘    └────────┬───────────┘    └───────────────┘
                             │
                             ▼
                      ┌──────────────┐
                      │ ScriptsDbCont │
                      │ (DbContext)   │
                      └──────┬───────┘
                             │
                             ▼
                      ┌──────────────┐
                      │  PostgreSQL   │
                      └──────────────┘
```

**Key observation:** `GoogleSheetsService` is completely removed. `StateManager` remains as intermediate cache but is no
longer on the critical sync path for persistence. DI injection replaces all static factory calls.

---

## §12 — Gap Analysis

### §12.1 — Critical Gaps (G1–G5)

| ID  | Gap                                     | Severity | Resolution                               |
| --- | --------------------------------------- | -------- | ---------------------------------------- |
| G1  | No `PostgresService` in orchestrator DI | HIGH     | Add to `ConfigureServices` in Program.cs |
| G2  | Scrobble PK mismatch (entity vs schema) | HIGH     | Composite key fix (§2.1)                 |
| G3  | Build broken (38 errors)                | HIGH     | Fix CS1503 + missing types               |
| G4  | No upsert route for YouTube data        | MEDIUM   | FiberyEntity JSONB store                 |
| G5  | `HistoryCommand` reads from sheets      | MEDIUM   | Replace with DB query                    |

### §12.2 — Design Gaps (G6–G9)

| ID  | Gap                                     | Resolution                               |
| --- | --------------------------------------- | ---------------------------------------- |
| G6  | `StateManager` JSON ↔ DB dual write     | Keep JSON as cache only; DB is canonical |
| G7  | No normalization pipeline for scrobbles | Create `ScrobbleNormMapper`              |
| G8  | YouTube data untyped in JSONB           | Acceptable — query by `entity_type`      |
| G9  | `CleanResetCommand` targets sheets      | Change to `TRUNCATE scrobbles`           |

### §12.3 — Removed Dependencies

| Dependency              | Elimination            | Benefit              |
| ----------------------- | ---------------------- | -------------------- |
| `Google.Apis.Sheets.v4` | Sheets sink deleted    | -1 package, -6 files |
| `Google.Apis.Drive.v3`  | Sheets sink deleted    | -1 package           |
| Google Sheets API quota | Migrated to PostgreSQL | Unlimited queries    |
| Sheet formatting logic  | Not needed             | -3 service files     |
| Sheet bootstrapping     | Schema pre-deployed    | -1 file              |

---

## §13 — Global Usings Audit

### §13.3 — Google-Specific (Delete)

```csharp
// REMOVE all:
global using Google.Apis.Drive.v3;
global using Google.Apis.Services;
global using Google.Apis.Sheets.v4;
global using Google.Apis.Sheets.v4.Data;
// Plus any Google-related type aliases (FileList, etc.)
```

### §13.4 — YouTube-Specific (Keep)

```csharp
global using Google.Apis.YouTube.v3.Data;
```

### §13.5 — NEW Usings (Add)

```csharp
global using CSharpScripts.Data;
global using CSharpScripts.Data.Entities;
```

### §13.6 — Recommended Cleanup

**Final `GlobalUsings.cs` should contain:**

```csharp
global using System.Diagnostics;
global using System.Globalization;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using CSharpScripts.Core;
global using CSharpScripts.Models;
global using CSharpScripts.Orchestrators;
global using CSharpScripts.Data;
global using CSharpScripts.Data.Entities;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Serilog;
global using Spectre.Console;
global using Spectre.Console.Cli;
global using Log = CSharpScripts.Core.Log;
```

**Remove narrowly-used usings (move to individual files):**

```csharp
System.Collections.Frozen, System.ComponentModel, static System.Environment,
static System.String, static System.StringComparison, CsvHelper,
CsvHelper.Configuration, RestSharp
```

---

## §14 — Minimal Clean Architecture

### §14.1 — File Count & Size Reduction Targets

| Metric                           | Current  | Target  | Reduction    |
| -------------------------------- | -------- | ------- | ------------ |
| Files in `Services/Sync/Google/` | 6        | 0       | 100%         |
| Google global usings             | 18 lines | 0 lines | 100%         |
| StateManager migration code      | 93 lines | 0 lines | ~26% of file |

### §14.2 — Class Consolidation Map

```
CURRENT:                              FUTURE:
GoogleSheetsService.cs     ── DELETE ──▶ (gone)
GoogleSheetsContext.cs     ── DELETE ──▶ (gone)
SheetFormattingService.cs  ── DELETE ──▶ (gone)
SheetMetadataService.cs    ── DELETE ──▶ (gone)
SheetRowService.cs         ── DELETE ──▶ (gone)
SpreadsheetBootstrapper.cs ── DELETE ──▶ (gone)
StateManager.cs (lines 76-118) ── DEL ──▶ (removed)
StateManager.cs (lines 243-293) ── DEL ──▶ (removed)
Program.cs                 ── REWRITE ──▶ Add ConfigureServices + DI
ScrobbleSyncOrchestrator   ── REWRITE ──▶ Inject PostgresService
YouTubePlaylistOrchestrator ── REWRITE ──▶ Inject PostgresService
HistoryCommand             ── REWRITE ──▶ Use DB query
PostgresService            ── KEEP ────▶ Unchanged (already correct)
ScriptsDbContext           ── MODIFY ───▶ Fix HasKey
```

### §14.3 — Concrete Savings by File

| File                    | Lines (Current) | Lines (After)  | Saved    |
| ----------------------- | --------------- | -------------- | -------- |
| `StateManager.cs`       | ~359            | ~266           | 93       |
| `GlobalUsings.cs`       | ~53             | ~25            | 28       |
| Google Sheets (6 files) | ~63,000 chars   | 0              | ~63K     |
| `Program.cs`            | TBD             | +30 (DI setup) | Net: +30 |

### §14.4 — Duplication Elimination Points

| Pattern                       | Resolution                   |
| ----------------------------- | ---------------------------- |
| Sheets row ↔ DB row mapping   | Removed with sheets deletion |
| Sheets write ↔ DB upsert      | Single upsert path           |
| Factory pattern (CreateAsync) | DI injection                 |
| Schema initialization ×2      | Single SQL script            |
| Sheet formatting ×3 services  | Not needed                   |
| StateManager migration code   | Already executed             |

### §14.6 — Minimal Viable File List

```yaml
DELETE:
    - csharp/src/Services/Sync/GoogleSheetsService.cs
    - csharp/src/Services/Sync/GoogleSheetsContext.cs
    - csharp/src/Services/Sync/SheetFormattingService.cs
    - csharp/src/Services/Sync/SheetMetadataService.cs
    - csharp/src/Services/Sync/SheetRowService.cs
    - csharp/src/Services/Sync/SpreadsheetBootstrapper.cs

SIMPLIFY:
    - csharp/src/Core/StateManager.cs # Remove lines 76-118, 243-293

REWRITE:
    - csharp/src/Orchestrators/ScrobbleSyncOrchestrator.cs # Inject PostgresService
    - csharp/src/Orchestrators/YouTubePlaylistOrchestrator.cs # Inject PostgresService
    - csharp/src/CLI/Sync/HistoryCommand.cs # Replace sheet read with DB query

ADD:
    - csharp/src/Services/ScrobbleNormMapper.cs # Normalization pipeline

MODIFY:
    - csharp/src/Data/ScriptsDbContext.cs # Fix Scrobble HasKey
    - csharp/src/Data/Entities/Scrobble.cs # Remove Id property
    - csharp/src/Services/PostgresService.cs # Fix UpsertScrobbleAsync signature
    - csharp/src/GlobalUsings.cs # Strip Google, add Data namespace
    - csharp/src/Program.cs # Add ConfigureServices + SpectreTypeRegistrar

KEEP:
    - csharp/src/Data/Entities/Artist.cs
    - csharp/src/Data/Entities/Album.cs
    - csharp/src/Data/Entities/Track.cs
    - csharp/src/Data/Entities/FiberyEntity.cs # May rename to SourceRecord later
    - csharp/src/Data/Entities/ExecutionLog.cs
    - csharp/src/Data/Entities/FailedTask.cs
    - csharp/src/Services/PostgresService.cs
    - csharp/src/Services/LastFmService.cs
    - csharp/src/Services/YouTubeService.cs
    - csharp/src/Core/StateManager.cs # Keep as cache layer
```

---

## §15 — Mono-Repo Consolidation

### §15.2 — Post-Migration PowerShell→C# Relationship

| Concern           | After Migration                              |
| ----------------- | -------------------------------------------- |
| Sync commands     | C# orchestrators → PostgreSQL                |
| Status queries    | C# CLI reads from PostgreSQL                 |
| Migration scripts | Direct PSQL via `Invoke-FiberyMigration.ps1` |
| Scheduled tasks   | Windows Task Scheduler → C# CLI              |
| State cache       | JSON files remain as transient cache         |

### §15.3 — Consolidation Analysis

| Component                    | Action       | Reason                               |
| ---------------------------- | ------------ | ------------------------------------ |
| `Sync-YouTube` (PS)          | Keep wrapper | PowerShell wraps C# CLI              |
| `Sync-LastFm` (PS)           | Keep wrapper | PowerShell wraps C# CLI              |
| `Get-SyncStatus` (PS)        | Update       | Read from PostgreSQL instead of JSON |
| `Invoke-FiberyMigration.ps1` | Keep         | Direct PSQL for schema deployment    |
| `Register-*SyncTask` (PS)    | Keep         | Windows Task Scheduler setup         |

### §15.4 — Centralization Principle

- All sync logic goes through C# orchestrators
- PowerShell is the CLI wrapper, not the logic layer
- PostgreSQL is the single sink (replaces Google Sheets)
- JSON files are transient cache only

### §15.6 — Complete Mono-Repo Architecture

```
PowerShell (wrapper) ──▶ C# CLI (Spectre) ──▶ Orchestrators
                                                    │
                          ┌─────────────────────────┤
                          ▼                         ▼
                   PostgresService            StateManager
                          │                    (JSON cache)
                          ▼
                   PostgreSQL (canonical)
```

### §15.7 — Mono-Repo Key Rule

All data flows converge on PostgreSQL. No third sink (no Google Sheets, no separate file DB). PowerShell modules that
currently read JSON state files should be updated to read from PostgreSQL where real-time accuracy matters.

---

## §16 — DTO Transformation Pipeline

### §16.4 — Minimization Principle

The DTO→Entity transformation should be the minimum possible mapping. Key metrics:

| Pipeline | Current Steps           | Future Steps      | Reduction |
| -------- | ----------------------- | ----------------- | --------- |
| Last.fm  | 3 (API→DTO→JSON→Sheets) | 2 (API→DTO→DB)    | -33%      |
| YouTube  | 2 (API→DTO→Sheets)      | 1 (API→DTO→JSONB) | -50%      |

### §16.5 — Recommended NormMapper Implementation

**New file:** `csharp/src/Services/ScrobbleNormMapper.cs`

```
Input:  List<Models.Scrobble>
Output: (Artist?, Album?, Track?, Scrobble) quadruple per input row

Logic:
1. For each distinct ArtistName → UpsertArtistAsync (lookup by name, insert if missing)
2. For each distinct ArtistName+AlbumName → UpsertAlbumAsync
3. For each distinct ArtistName+AlbumName+TrackName → UpsertTrackAsync
4. For each PlayedAt+resolved TrackId → UpsertScrobbleAsync
```

This is the correct 3NF normalization that was previously deferred by using flat Google Sheets rows.

**Type map — Last.fm (minimal transforms):**

| Hqub.Field          | Models.Scrobble | Data.Entities.\*     | Transform                       |
| ------------------- | --------------- | -------------------- | ------------------------------- |
| `Track.Name`        | `TrackName`     | `Track.Title`        | None ✅                         |
| `Track.Artist.Name` | `ArtistName`    | `Artist.Name`        | None ✅                         |
| `Track.Album.Name`  | `AlbumName`     | `Album.Title`        | None ✅                         |
| `Track.Date`        | `PlayedAt`      | `Scrobble.Timestamp` | `DateTime?`→`DateTimeOffset` ⚠️ |
| —                   | —               | `Artist.Id`, FKs     | Generated/Resolved ✅           |

**Type map — YouTube (minimal transforms):**

| YouTube.Field             | Models.YouTubeVideo | Data.Entities.FiberyEntity      | Transform   |
| ------------------------- | ------------------- | ------------------------------- | ----------- |
| `Snippet.Title`           | `Title`             | `RawData → .Title` (JSON)       | None ✅     |
| `Snippet.Description`     | `Description`       | `RawData → .Description` (JSON) | None ✅     |
| `ContentDetails.Duration` | `Duration`          | `RawData → .Duration` (JSON)    | None ✅     |
| `Id`                      | `VideoId`           | `FiberyId = VideoId`            | Direct ✅   |
| —                         | —                   | `EntityType = youtube_video`    | Constant ✅ |

**Verification checklist:**

- [ ] 4 string fields pass through unchanged for Last.fm
- [ ] Only `DateTime?→DateTimeOffset` cast needed for Last.fm
- [ ] 6 fields serialized as-is to JSONB for YouTube
- [ ] `VideoId` becomes `FiberyId` directly for YouTube
- [ ] FK resolution (ArtistId, AlbumId, TrackId) via DB lookup
- [ ] No data loss, no reformatting in YouTube pipeline

