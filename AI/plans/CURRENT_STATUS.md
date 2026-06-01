# EF Core 10 Migration — Current Status

**Last Updated:** 2026-05-27
**Status:** ~80% Complete (Tier 1 EF work in progress; modularization begins only after T1 sign-off)
**Next Action:** T1-12 (Logging relocation)

---

## Test Summary

| Metric      | Value                                              | Target |
| ----------- | -------------------------------------------------- | ------ |
| Total Tests | 215                                                | 250+   |
| Passing     | 199 (92.6%)                                        | 100%   |
| Failing     | 16 (T1-13 Lingua, not started)                     | 0      |
| Pass Rate   | 92.6% (target 100% — T1-13 Lingua pending)         | 100%   |

---

## EF-First Sequencing

The work is intentionally sequential. Tier 1 is the only active workstream right now; Tier 2 modularization starts only after T1 sign-off.

| Track                   | Scope                                   | Status        | Gate           |
| ----------------------- | --------------------------------------- | ------------- | -------------- |
| **T1 — EF Core**        | Database layer only (monolith)          | 🟡 In Progress | T1-16 sign-off |
| **T2 — Modularization** | 8-project split, CPM, namespace cleanup (post-T1) | 🔒 Blocked     | T1 sign-off    |

**Key rule:** T1 plans must not contain modularization steps. Modularization is T2 and remains blocked until T1 sign-off.
The duplicate `Core/` vs `Infrastructure/` classes are a T2 concern — they exist intentionally
during T1 because the split hasn't happened yet.

---

## Tier Progress

| Tier | Phases | Status        | Progress | Notes                                          |
| ---- | ------ | ------------- | -------- | ---------------------------------------------- |
| T1   | 00–16  | 🟡 In Progress | ~80%     | 00–11, 14, 15 done. 12, 13 pending. |
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
| T1-07 | StateManager → EF     | ✅ Done        | Single StateManager remains in Data/State; full suite verified 155 passing                                                |
| T1-08 | Release cache → EF    | ✅ Done        | ReleaseProgressService wired in MusicSearchCommand, CSV cache deleted                                                      |
| T1-09 | Sync service EF10     | ✅ Done        | LastFmService has IDbContextFactory, PostgresService deleted                                                               |
| T1-10 | EF10 query guards     | ✅ Done        | Guard tests written and passing                                                                                            |
| T1-11 | Compiled model        | ✅ Done        | CompiledModelTests pass                                                                                                    |
| T1-12 | Logging relocation    | ❌ Not started | LogDirectory still points to ProjectRoot/logs                                                                              |
| T1-13 | Lingua migration      | ❌ Not started | LanguageIdentifier.cs excluded from build                                                                                  |
| T1-14 | Resilience policies   | ✅ Done        | EnableRetryOnFailure + RepositoryResilienceFactory                                                                         |
| T1-15 | Testcontainers        | ✅ Done        | DatabaseTestFixture uses local Postgres                                                                                    |
| T1-16 | Sign-off              | ❌ Blocked     | Waiting on 12, 13                                                                                                   |

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
- `src/Core/Persistence/` directory — now contains only backup files.

---

## EF First, Then Modularization

### The Problem

T1 (EF Core) and T2 (modularization) are sequential in the plan, but the codebase
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
T1 is the only active tier right now. T2 begins only after T1 sign-off, then T3 and T4 follow in order.
These are **T2 concerns** — they will be resolved when Infrastructure files move to their
proper projects with correct visibility modifiers. Do not fix them in T1.

---

## See Also

- [Plan Index](INDEX.md)
- [Tier 1 Plans](tier-1-ef-migration/)
- [AGENTS.md](../../AGENTS.md)
