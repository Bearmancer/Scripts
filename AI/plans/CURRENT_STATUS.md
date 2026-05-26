# EF Core 10 Migration — Current Status

**Last Updated:** 2026-05-27
**Status:** ~65% Complete (Tier 1 EF work in progress, modularization tracked separately)
**Next Action:** T1-07 (StateManager — file I/O remains, no EF migration yet)

---

## Test Summary

| Metric      | Value      | Target |
| ----------- | ---------- | ------ |
| Total Tests | 136        | 250+   |
| Passing     | 136 (100%) | 100%   |
| Failing     | 0          | 0      |
| Pass Rate   | 100%       | 100%   |

---

## Two Concurrent Tracks

The work has two independent concerns that must not be conflated:

| Track                   | Scope                                   | Status        | Gate           |
| ----------------------- | --------------------------------------- | ------------- | -------------- |
| **T1 — EF Core**        | Database layer only (monolith)          | 🟡 In Progress | T1-16 sign-off |
| **T2 — Modularization** | 8-project split, CPM, namespace cleanup | 🔒 Blocked     | T1 sign-off    |

**Key rule:** T1 plans must not contain modularization steps. Modularization is T2.
The duplicate `Core/` vs `Infrastructure/` classes are a T2 concern — they exist intentionally
during T1 because the split hasn't happened yet.

---

## Tier Progress

| Tier | Phases | Status        | Progress | Notes                                          |
| ---- | ------ | ------------- | -------- | ---------------------------------------------- |
| T1   | 00–16  | 🟡 In Progress | ~65%     | 00–06, 11, 14, 15 done. 07–10, 12, 13 pending. |
| T2   | 00–10  | 🔒 Blocked     | 0%       | Waiting for T1 sign-off                        |
| T3   | 00–07  | 🔒 Blocked     | 0%       | Waiting for T2 sign-off                        |
| T4   | 00–08  | 🔒 Blocked     | 0%       | Waiting for T3 sign-off                        |

---

## T1 Phase Status (EF Core only)

| Phase | Task                  | Status        | Notes                                                                                                                      |
| ----- | --------------------- | ------------- | -------------------------------------------------------------------------------------------------------------------------- |
| T1-00 | Environment preflight | ✅ Done        | Docker, PGCONNSTR, CanConnectAsync                                                                                         |
| T1-01 | Entity extraction     | ✅ Done        | 8 entities in Data/Entities/                                                                                               |
| T1-02 | Entity refactoring    | ✅ Done        | MBID removal tests pass                                                                                                    |
| T1-03 | DbContext config      | ✅ Done        | NoTracking, ApplyConfigurationsFromAssembly                                                                                |
| T1-04 | Entity configurations | ✅ Done        | 10 configs in Data/Configuration/                                                                                          |
| T1-05 | Migrations            | ✅ Done        | 6 migrations, snapshot current                                                                                             |
| T1-06 | Repositories          | ✅ Done        | 5 repos + interfaces + ResilienceFactory                                                                                   |
| T1-07 | StateManager → EF     | ❌ Not started | StateManager.cs still pure file I/O                                                                                        |
| T1-08 | Release cache → EF    | 🟡 Partial     | ReleaseProgressService exists; MusicSearchCommand still calls old ReleaseProgressCache (live .cs file in Core/Persistence) |
| T1-09 | Sync service EF10     | ❌ Not started | LastFmService has no IDbContextFactory                                                                                     |
| T1-10 | EF10 query guards     | ❌ Not started | Guard tests not yet written                                                                                                |
| T1-11 | Compiled model        | ✅ Done        | CompiledModelTests pass                                                                                                    |
| T1-12 | Logging relocation    | ❌ Not started | LogDirectory still points to ProjectRoot/logs                                                                              |
| T1-13 | Lingua migration      | ❌ Not started | LanguageIdentifier.cs excluded from build                                                                                  |
| T1-14 | Resilience policies   | ✅ Done        | EnableRetryOnFailure + RepositoryResilienceFactory                                                                         |
| T1-15 | Testcontainers        | ✅ Done        | DatabaseTestFixture uses local Postgres                                                                                    |
| T1-16 | Sign-off              | ❌ Blocked     | Waiting on 07–10, 12, 13                                                                                                   |

---

## T1-08 Broken State (Critical)

`MusicSearchCommand.cs` still calls `ReleaseProgressCache.Delete/Load/AppendTrack`.
The live class `Core/Persistence/ReleaseProgressCache.cs` still exists (CSV-based).
`ReleaseProgressService.cs` (EF-backed) exists but is not wired into MusicSearchCommand.

Resolution path:
1. Complete T1-07 (StateManager) first
2. Wire MusicSearchCommand to ReleaseProgressService (T1-08 Task 5)
3. Delete Core/Persistence/ReleaseProgressCache.cs after wiring confirmed

---

## Stray Files Cleaned (2026-05-27)

Deleted:
- `src/Data/ScriptsDbContext.cs.bak.20260525_123441`
- `src/Data/Entities/FailedTask.cs.bak.20260523_154336`
- `src/Infrastructure/ReleaseProgressCache.cs.bak.20260527_000236`
- `src/Core/Persistence/ReleaseProgressCache.cs.bak.20260527_000236`
- `src/Hierarchy/MetaBrainz.MusicBrainz.hierarchy.txt`
- `src/Hierarchy/ParkSquare.Discogs.hierarchy.txt`

Remaining known stray:
- `src/Services/PostgresService.cs` — orphan, no callers, has 6 pragma suppresses. Delete in T1-09.
- `src/Core/Persistence/ReleaseProgressCache.cs` — live CSV class, still called by MusicSearchCommand. Delete after T1-08 wiring.
- `src/Core/Persistence/` directory — will be empty after above deletion.

---

## Path of Least Resistance: Two Concurrent Tracks

### The Problem

T1 (EF Core) and T2 (modularization) are sequential in the plan but the codebase
already has partial T2 artifacts: `Core/` and `Infrastructure/` contain duplicate
classes (`Paths`, `Resilience`, `StringExtensions`, `SyncProgress`, `UI/Console`).
These duplicates are intentional — they exist because the monolith hasn't been split yet.

### The Risk

If T1 plans try to delete Infrastructure duplicates, they corrupt the monolith build
before T2 has created the new project boundaries. If T2 plans assume T1 cleaned up
Infrastructure, they'll find nothing to move.

### The Solution: Hard Boundary

**T1 plans must only touch:**
- `src/Data/` (entities, configs, migrations, repositories, DbContext)
- `src/Data/State/` (StateManager)
- `src/Data/Persistence/` (ReleaseProgressService)
- `src/Services/Sync/LastFmService.cs` (IDbContextFactory injection only)
- `src/Core/Resilience.cs` (RetryExhaustedException addition only)
- `src/Core/Log.cs` (Demystifier, ServiceType.Sheets removal)
- `src/Core/Paths.cs` (LogDirectory relocation)
- `src/Services/Language/LanguageIdentifier.cs` (Lingua rewrite)
- Test files in `tests/Scripts.Tests/`

**T1 plans must NOT touch:**
- `src/Infrastructure/` (any file) — T2 owns this
- `src/Core/Persistence/` beyond deleting ReleaseProgressCache after T1-08 wiring
- `src/CLI/` — T2 owns this
- `src/Orchestrators/` — T2 owns this
- `src/Program.cs` — T2 owns this

**T2 plans own:**
- Deleting all `src/Infrastructure/` duplicates
- Moving `src/Core/Console/` → `Scripts.Core` project
- Moving `src/Infrastructure/` → appropriate projects
- Splitting `src/CLI/` into `Scripts.CLI` project
- Splitting `src/Orchestrators/` into `Scripts.Orchestrators` project

### Execution Order (No Change)

T1 → T2 → T3 → T4. Sequential. No parallelism between tiers.
The "two concurrent tracks" framing means: T1 is EF-only, T2 is modularization-only.
They don't run at the same time — they run in sequence with clean handoff.

---

## Next Immediate Actions

1. **T1-07**: Migrate StateManager to `Data/State/` namespace (file already there, namespace already correct — verify tests pass, then delete Core/Persistence duplicate)
2. **T1-08**: Wire MusicSearchCommand to ReleaseProgressService, then delete Core/Persistence/ReleaseProgressCache.cs
3. **T1-09**: Inject IDbContextFactory into LastFmService, delete PostgresService.cs orphan
4. **T1-10**: Write EF11 guard tests (should pass immediately — no EF11 patterns exist)
5. **T1-12**: Relocate LogDirectory, add Ben.Demystifier, remove ServiceType.Sheets
6. **T1-13**: Rewrite LanguageIdentifier with Lingua

---

## Build Warnings (Current)

The monolith has ~35 warnings from `Infrastructure/` files (CA1062, CA1002, CA1063, etc.).
These are **T2 concerns** — they will be resolved when Infrastructure files move to their
proper projects with correct visibility modifiers. Do not fix them in T1.

---

## See Also

- [Plan Index](INDEX.md)
- [Tier 1 Plans](tier-1-ef-migration/)
- [AGENTS.md](../../AGENTS.md)
