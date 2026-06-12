# YouTube Pipeline Rebuild Plan

## TL;DR
> Rebuild the YT pipeline from a clean slate with strict separation of concerns: raw JSON snapshots, separate translation files, a manifest/index, PostgreSQL current tables, PostgreSQL history tables, and explicit sync/run history.
>
> **Deliverables**
> - Clean purge of existing local YT state
> - pgtui connectivity config for PostgreSQL browsing
> - Raw-only JSON cache by playlist ID + name
> - Separate translation sidecar files for full-playlist translation output
> - Manifest/index for routing, rename handling, and replay planning
> - PostgreSQL current, history, and run-history tables
> - Backup / PITR / restore workflow for PostgreSQL
> - Offline rebuild validation with zero YouTube API calls
>
> **Estimated Effort**: Large
> **Parallel Execution**: YES - 5 waves
> **Critical Path**: Purge/reset → raw split → translation sidecar → manifest → PGSQL current/history → rebuild/backup → final QA

---

## Context

### Original Request
User wants a final comprehensive TDD plan for the current YouTube pipeline, with micro-steps and every step gated by failing and passing commands. The pipeline must restart from scratch after purging current state, minimize Google/YouTube API calls, keep raw JSON pure, keep translations separate, and store history in PostgreSQL.

### Confirmed Decisions
- Current state should be purged; no incremental migration from the old cache.
- Raw cache stores **raw API JSON only**.
- Translations live in a **separate translation file** for the full playlist.
- PostgreSQL should contain **all data**, including deleted/history information.
- Deleted playlists/videos get **separate history tables** in PostgreSQL.
- Sync history is separate from playlist/video history.
- `sync.json` is only for **current run cursor/progress**.
- Playlist filenames should be cleaner and include both ID and name.
- Translation should not rerun unnecessarily; invalidation must be explicit.
- pgtui needs a PostgreSQL connection config for verification.

### Research Findings
- YouTube API quota must be minimized with ETag / conditional fetch / incremental playlist sync.
- Raw JSON and translation outputs must not be mixed in one file.
- PostgreSQL should be the durable owner of current state, domain history, and run history.
- Backup strategy should use PostgreSQL base backups + WAL/PITR; native incrementals are optional if complexity is justified.

---

## Work Objectives

### Core Objective
Rebuild the YT pipeline so it can start from a clean state, fetch all playlists, cache raw JSON safely, translate separately, materialize PostgreSQL current/history tables, and restore from backups without re-calling the YouTube API unnecessarily.

### Concrete Deliverables
- Cleaned local YT state with new directory structure
- pgtui config pointing at local PostgreSQL
- Raw DTOs and raw-only JSON persistence
- Translation DTOs and separate translation-file persistence
- Manifest/index file keyed by playlist ID
- PostgreSQL schema for current tables, history tables, and run history
- Sync/run cursor file (`sync.json`) with no durable snapshot burden
- Backup and restore scripts/workflow
- Offline rebuild test proving zero YouTube calls

### Definition of Done
- [ ] Raw cache contains only raw API JSON, no derived translation fields
- [ ] Translation output is stored in separate files
- [ ] Manifest resolves playlist ID → raw/translation/deleted paths
- [ ] PostgreSQL stores current state, deleted history, and sync/run history separately
- [ ] A fresh PostgreSQL rebuild succeeds from local files without calling YouTube
- [ ] Backup / restore drill passes
- [ ] pgtui connects to the database using the configured URI

### Must Have
- Raw JSON remains immutable once written
- Separate translation file for each playlist
- Separate history tables for deleted playlists/videos
- Separate run-history table for sync executions
- Change detection must not rerun translation unless inputs changed
- No YouTube API calls during offline PostgreSQL rebuild

### Must NOT Have
- Raw + translated data mixed in the same raw file
- `sync.json` storing durable history snapshots
- Manifest acting as a second business database
- Translation cache becoming the source of truth
- Filename-only identity for playlists
- Full translation reruns when raw inputs are unchanged

---

## Verification Strategy

### Test Decision
- **Infrastructure exists**: YES
- **Automated tests**: YES
- **Framework**: TUnit + FluentAssertions + PowerShell assertions + PostgreSQL CLI checks
- **Workflow**: Red → Green → Refactor for every micro-step

### QA Policy
Every task must include an executable failing gate and a passing gate.

- **Local / computer checks**: PowerShell `.ps1`
- **C# behavior checks**: `.cs` tests via `dotnet test`
- **PostgreSQL checks**: terminal commands (`psql`, `pg_basebackup`, `pg_dump`, restore drill scripts)
- **Interactive DB smoke test**: `pgtui` after config is fixed

### Evidence Policy
Save evidence under `.omo/evidence/youtube-pipeline/` with names like:
- `task-01-red.txt`
- `task-01-green.txt`
- `task-08-restore-drill.txt`

---

## Execution Strategy

### Wave 0 — Tooling and clean-slate setup
1. Purge current YT local state.
2. Configure pgtui to point at PostgreSQL.
3. Freeze naming and identity rules before implementation.

### Wave 1 — Raw cache contract
1. Split raw DTOs from derived DTOs.
2. Make raw JSON persistence translation-free.
3. Validate raw-only file output.

### Wave 2 — Translation and manifest
1. Add separate translation files.
2. Add translation invalidation rules.
3. Add manifest/index and rename/delete routing.

### Wave 3 — PostgreSQL schema and history
1. Add current tables.
2. Add playlist/video history tables.
3. Add sync/run history tables.

### Wave 4 — Sync behavior and change detection
1. Wire ETag / incremental sync.
2. Ensure rename/delete/update semantics do not force unnecessary translation.
3. Ensure user-playlist-only scope is enforced.

### Wave 5 — Backup and rebuild
1. Add backup / restore scripts.
2. Verify PostgreSQL PITR / restore drill.
3. Verify offline rebuild from local files with zero API calls.

### Final Verification Wave
1. Plan compliance audit
2. Code quality review
3. Real end-to-end QA
4. Scope fidelity and backup integrity review

---

## TODOs

- [x] 1. Purge the current YT cache and configure pgtui

  **What to do**:
  - Remove the current local YT state so the rebuild starts from zero.
  - Update `C:\Users\Lance\AppData\Roaming\pgtui\config.toml` so `dbs` contains the local PostgreSQL URI from `.env`.
  - Verify `pgtui` opens without the missing-`dbs` error.

  **Failing gate**:
  - `pgtui` currently fails with the missing `dbs` array message.
  - `pwsh -File .\powershell\tests\Assert-YoutubeStateClean.ps1` fails while the old YT state still exists.

  **Passing gate**:
  - `pgtui` starts and sees the configured PostgreSQL connection.
  - `pwsh -File .\powershell\tests\Assert-YoutubeStateClean.ps1` passes after state purge.

  **Must NOT do**:
  - Do not preserve old mixed cache files.
  - Do not keep raw/derived hybrid data in the old structure.

  **Recommended Agent Profile**:
  - **Category**: quick
  - Reason: small environment/tooling step with a single output.

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Blocks**: all later YT work
  - **Blocked By**: none

  **Acceptance Criteria**:
  - `pgtui` launches successfully.
  - YT cache purge is confirmed by an executable check, not by manual inspection.

  **QA Scenarios**:
  - Happy path: `pgtui` launches with the configured db.
  - Failure path: invalid URI or missing `dbs` produces the expected error.

- [ ] 2. Split raw DTOs from derived DTOs

  **What to do**:
  - Create raw-only DTOs for YouTube playlist/video JSON.
  - Create separate derived DTOs for translated output.
  - Ensure raw serialization cannot emit translated/display fields.

  **Failing gate**:
  - `dotnet test csharp/tests/Scripts.Tests/Scripts.Tests.csproj --filter FullyQualifiedName~YouTubeRawDtoContractTests` fails because raw DTO serialization still leaks derived fields.

  **Passing gate**:
  - Same command passes after raw DTOs are separate and raw serialization is pure.

  **Must NOT do**:
  - Do not serialize `TranslatedTitle`, `TranslatedDescription`, or display-only properties into raw cache.

  **Recommended Agent Profile**:
  - **Category**: unspecified-high
  - Reason: touches domain model, serializer behavior, and test boundaries.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: translation and manifest tasks
  - **Blocked By**: task 1

  **Acceptance Criteria**:
  - Raw cache output contains only raw fields.
  - Derived fields are present only in derived DTOs or later layers.

  **QA Scenarios**:
  - Happy path: raw DTO file deserializes to raw-only structure.
  - Failure path: a raw DTO containing derived fields fails the contract test.

- [ ] 3. Build separate translation files for each playlist

  **What to do**:
  - Add translation-file writers for full playlist translation output.
  - Persist translations to a dedicated directory, separate from raw snapshots.
  - Make translation invalidation depend on raw source input and translator version.

  **Failing gate**:
  - `dotnet test csharp/tests/Scripts.Tests/Scripts.Tests.csproj --filter FullyQualifiedName~YouTubeTranslationCacheTests` fails because translation output is still mixed with raw cache or reruns unnecessarily.

  **Passing gate**:
  - The same command passes once translation output is isolated and invalidation is explicit.

  **Must NOT do**:
  - Do not treat translation presence as freshness.
  - Do not rerun translation when raw inputs are unchanged.

  **Recommended Agent Profile**:
  - **Category**: deep
  - Reason: translation invalidation and cache boundaries are architecture-critical.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2
  - **Blocked By**: task 2
  - **Blocks**: manifest and PGSQL materialization

  **Acceptance Criteria**:
  - Each playlist has a separate translation artifact.
  - Translation reruns only when source inputs change.

  **QA Scenarios**:
  - Happy path: translation file produced for a changed playlist.
  - Failure path: unchanged raw input does not retrigger translation.

- [ ] 4. Add manifest / index and clean filename scheme

  **What to do**:
  - Create `state/youtube/manifest.json` as the routing/index layer.
  - Use a filename scheme that combines ID and name cleanly, e.g. `PL123__My Favorites.json`.
  - Make the manifest map playlist ID to raw path, translation path, deleted path, hashes, and timestamps.

  **Failing gate**:
  - `pwsh -File .\powershell\tests\Assert-YoutubeManifest.ps1` fails while no manifest exists or while filenames collide.

  **Passing gate**:
  - The same command passes after the manifest is authoritative and the filename scheme is collision-safe.

  **Must NOT do**:
  - Do not let filenames be the only identity.
  - Do not let the manifest become a second business DB.

  **Recommended Agent Profile**:
  - **Category**: unspecified-high
  - Reason: touches routing, identity, delete behavior, and rename semantics.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2
  - **Blocked By**: task 2
  - **Blocks**: tasks 5, 6, 7

  **Acceptance Criteria**:
  - Manifest resolves active, deleted, raw, and translation artifacts.
  - Duplicate playlist names no longer collide.

  **QA Scenarios**:
  - Happy path: two playlists with the same name can coexist using distinct IDs.
  - Failure path: missing manifest entry causes the routing assertion to fail.

- [ ] 5. Demote `sync.json` to cursor-only and add run history

  **What to do**:
  - Reduce `sync.json` to current cursor/progress state only.
  - Add PostgreSQL `sync_runs` table for durable execution history.
  - Make daily run history queryable without relying on local file state.

  **Failing gate**:
  - `dotnet test csharp/tests/Scripts.Tests/Scripts.Tests.csproj --filter FullyQualifiedName~YouTubeSyncRunTests` fails because run history is not yet separated from cursor state.

  **Passing gate**:
  - The same command passes after cursor and durable run history are split.

  **Must NOT do**:
  - Do not store playlist snapshots in `sync.json`.
  - Do not use sync logs as a business history substitute.

  **Recommended Agent Profile**:
  - **Category**: unspecified-high
  - Reason: operational history design plus persistence boundaries.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3
  - **Blocked By**: task 4
  - **Blocks**: PGSQL history and rebuild tasks

  **Acceptance Criteria**:
  - `sync.json` can resume a run.
  - `sync_runs` stores execution history independently.

  **QA Scenarios**:
  - Happy path: interrupted run resumes from cursor.
  - Failure path: a missing run-history row does not break cursor resume.

- [ ] 6. Implement PostgreSQL current tables and history tables

  **What to do**:
  - Add current tables for playlists, videos, and playlist-video relationships.
  - Add history tables for deleted playlists and deleted videos.
  - Keep sync/run history separate from playlist/video history.

  **Failing gate**:
  - `psql "$env:PGCONNSTR" -c "\dt youtube.*"` fails to show the required tables before schema work.

  **Passing gate**:
  - The same command shows the current tables, history tables, and run-history table after schema work.

  **Must NOT do**:
  - Do not mix raw JSON storage with history tables.
  - Do not collapse playlist history into sync history.

  **Recommended Agent Profile**:
  - **Category**: deep
  - Reason: schema design, migrations, and history semantics are load-bearing.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3
  - **Blocked By**: tasks 2, 3, 4, 5
  - **Blocks**: change detection and rebuild tasks

  **Acceptance Criteria**:
  - Current state tables are queryable.
  - Deleted entities have explicit history rows.
  - Run history is separately queryable.

  **QA Scenarios**:
  - Happy path: insert/update/delete produces current + history rows.
  - Failure path: schema assertions fail when a required table is missing.

- [ ] 7. Wire rename, delete, and change-detection behavior

  **What to do**:
  - Use playlist ID as the stable identity across rename and delete events.
  - Make change detection use upstream ETag / source hash and translation version.
  - Ensure translation does not rerun unless source data or translator version changes.

  **Failing gate**:
  - `dotnet test csharp/tests/Scripts.Tests/Scripts.Tests.csproj --filter FullyQualifiedName~YouTubeChangeDetectionTests` fails because rename/delete/change behavior still causes unnecessary retranslation or identity confusion.

  **Passing gate**:
  - The same command passes once rename/delete/change are identity-safe and translation reruns only when necessary.

  **Must NOT do**:
  - Do not use title alone as identity.
  - Do not use translation existence as freshness.

  **Recommended Agent Profile**:
  - **Category**: ultrabrain
  - Reason: change detection and identity rules have subtle failure modes.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 4
  - **Blocked By**: tasks 3, 4, 6
  - **Blocks**: backup and rebuild tasks

  **Acceptance Criteria**:
  - Rename does not create a new playlist identity.
  - Delete moves items to history/archive instead of losing them.
  - Translation does not rerun when source is unchanged.

  **QA Scenarios**:
  - Happy path: title-only rename updates the same playlist identity.
  - Failure path: duplicate-title collision fails if ID routing is broken.

- [ ] 8. Add PostgreSQL backup, WAL, and restore workflow

  **What to do**:
  - Add a base-backup workflow.
  - Add WAL archive / PITR restore workflow.
  - If incremental backup is added, make it explicit and test restore-chain behavior.

  **Failing gate**:
  - `pwsh -File .\powershell\tests\Assert-PgBackupDrill.ps1` fails until backup artifacts and restore steps exist.

  **Passing gate**:
  - The same command passes once backup, WAL/PITR, and restore-drill scripts succeed.

  **Must NOT do**:
  - Do not treat backup as a substitute for history tables.
  - Do not let incremental backup complexity hide restore failure.

  **Recommended Agent Profile**:
  - **Category**: unspecified-high
  - Reason: operational backup/restore workflow spans DB and shell tooling.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 5
  - **Blocked By**: task 6
  - **Blocks**: final rebuild validation

  **Acceptance Criteria**:
  - Backup artifacts are created.
  - Restore drill succeeds.
  - WAL/PITR path is documented and verified.

  **QA Scenarios**:
  - Happy path: restore to a known good point succeeds.
  - Failure path: missing WAL segment causes the drill to fail as expected.

- [ ] 9. Prove offline rebuild from local files with zero YouTube calls

  **What to do**:
  - Rebuild PostgreSQL from local raw files and translation files.
  - Assert that the rebuild does not call YouTube.
  - Verify history, current state, and run history are restored.

  **Failing gate**:
  - `dotnet test csharp/tests/Scripts.Tests/Scripts.Tests.csproj --filter FullyQualifiedName~YouTubeOfflineRebuildTests` fails until rebuild uses local files only.

  **Passing gate**:
  - The same command passes when rebuild completes without any YouTube API calls.

  **Must NOT do**:
  - Do not make the rebuild depend on fresh Google data.
  - Do not require manual database edits.

  **Recommended Agent Profile**:
  - **Category**: deep
  - Reason: end-to-end rebuild validation is the core system behavior.

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Blocked By**: tasks 2 through 8

  **Acceptance Criteria**:
  - Rebuild works offline.
  - No YouTube API calls occur during rebuild.
  - Current/history/run tables are restored.

  **QA Scenarios**:
  - Happy path: PGSQL rebuilds from local files only.
  - Failure path: network-disabled environment still passes rebuild validation.

---

## Final Verification Wave

- [ ] F1. Plan compliance audit — `oracle`

  **What to do**:
  - Verify every must-have is represented by a task and an executable command.
  - Verify raw/derived/manifest/PGSQL/run-history boundaries are clear.
  - Verify no task smuggles in hidden assumptions.

  **Pass condition**:
  - `VERDICT: APPROVE`

- [ ] F2. Code quality review — `unspecified-high`

  **What to do**:
  - Run `dotnet test`, build, and lint/checks.
  - Review for raw/derived leakage, accidental mixed responsibilities, and stale translation rules.

  **Pass condition**:
  - Build, tests, and schema checks pass cleanly.

- [ ] F3. Real end-to-end QA — `unspecified-high`

  **What to do**:
  - Execute the full sync path.
  - Verify pgtui connectivity.
  - Verify current + history + backup + rebuild flows.

  **Pass condition**:
  - Full pipeline works on a clean start.

- [ ] F4. Scope fidelity and backup integrity — `deep`

  **What to do**:
  - Confirm no task drifted beyond the approved architecture.
  - Confirm backup/restore and history coverage match the plan.

  **Pass condition**:
  - No unplanned scope, no missing backup coverage, no hidden coupled responsibilities.

---

## Success Criteria

### Verification Commands
```powershell
pwsh -File .\powershell\tests\Assert-YoutubeStateClean.ps1  # expected: fails before purge, passes after purge
dotnet test csharp/tests/Scripts.Tests/Scripts.Tests.csproj    # expected: all YT tests pass when implementation is complete
psql "$env:PGCONNSTR" -c "\dt youtube.*"                    # expected: required current/history/run tables visible
pgtui                                                         # expected: starts after config is fixed
```

### Final Checklist
- [ ] Raw JSON cache is raw-only
- [ ] Translation output is separate
- [ ] Manifest/index is explicit and collision-safe
- [ ] Sync history is separate from playlist/video history
- [ ] PostgreSQL stores current state and history separately
- [ ] Backup / restore drill passes
- [ ] Offline rebuild requires zero YouTube API calls
- [ ] pgtui connects successfully
