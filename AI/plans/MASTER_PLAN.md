# Scripts Repository: Master Plan

> Single source of truth for navigating the Scripts repo and tracking pending work. Start here, then follow the links below to dive into any subsystem or pending task.

**Last updated**: 2026-06-05
**Status**: Pending work captured across 6 phases (A through F). See [Pending Work](#pending-work). Phase F captures the post-commit code review audit of commit `8941eeb` (cascade-gate override) and is the highest priority — it fixes critical regressions in the tier-1 deliverables that are already shipped.

---

## Overview

The Scripts repo is a multi-language toolkit for personal automation and media library management. It bundles a .NET 10 Library (`csharp/`) for syncing Last.fm scrobbles, enriching music metadata, and reading articles; a Python toolkit (`python/`) for audio, video, and filesystem helpers; a PowerShell module (`powershell/`) for Azure and shell utilities; persistent JSON and PostgreSQL state (`state/`); and a knowledge base (`AI/`) with system audits, database schemas, and planning docs.

The audience is the repo owner (Lance) and any AI agent that needs to orient quickly. Treat this document as the entry point. Every other README in the repo is a sub-index with deeper detail. When in doubt, follow the link, don't duplicate the content here.

---

## Repository Structure

```
Scripts/
|-- AI/                                 # Knowledge base: audits, schemas, plans
|   |-- artifacts/                      # Install scripts + MCP sync utility
|   |-- plans/                          # Long-form planning docs (THIS FILE LIVES HERE)
|   |   `-- adr/                        # Architecture Decision Records
|   `-- references/                     # System audit, DB schemas, fibery archive
|
|-- csharp/                             # .NET 10 Library: data layer, services, reader
|   |-- Scripts.slnx                    # Solution file
|   |-- src/                            # Data (EF Core), Services, Reader, Core, CLI
|   |-- tests/                          # TUnit test suite (Scripts.Tests)
|   `-- CompiledModels/                 # EF Core precompiled query models
|
|-- powershell/                         # Windows pwsh toolkit
|   |-- Install-Env.ps1                 # Windows dev env installer (winget + VS Code)
|   |-- ScriptsToolkit/                 # PowerShell module (Azure, comment stripping)
|   `-- Microsoft.PowerShell_profile.ps1
|
|-- python/                             # Python toolkit (uv-managed)
|   |-- toolkit/                        # audio, video, filesystem, lastfm, pristine modules
|   |-- pyproject.toml                  # Package manifest
|   `-- uv.lock                         # Resolved lockfile
|
|-- state/                              # Persistent runtime state (gitignored data)
|   |-- lastfm/                         # Last.fm sync state
|   |-- youtube/                        # YouTube playlist snapshots + sync state
|   |-- postgres/                       # Local Postgres data dir (Docker volume)
|   `-- pristine/                       # Untouched backup of original state files
|
|-- subagents/                          # Reserved for future subagent definitions (currently empty)
|
|-- docker-compose.yml                  # Postgres 18 service for local dev
|-- global.json                         # SDK pin (10.0.300) -- see [A1](#a1-sdk-pin)
|-- .env                                # Local environment variables (gitignored)
|-- .env.example                        # Template for .env -- see [A2](#a2-docker-environment-cleanup)
`-- .gitignore / .gitattributes         # Standard ignore + line-ending config
```

---

## Quick Navigation

| Subsystem | Entry Point | Language | Purpose |
|-----------|-------------|----------|---------|
| Last.fm + YouTube sync | `csharp/src/Program.cs` | C# | CLI entry, Spectre.Console commands |
| Music enrichment | `csharp/src/Services/Music/` | C# | MusicBrainz + Discogs lookups, scoring |
| Article reader | `csharp/src/Reader/` | C# | Browser session, OCR, EPUB export |
| EF Core data layer | `csharp/src/Data/ScriptsDbContext.cs` | C# | Postgres-backed entity store, 3-schema model |
| Test suite | `csharp/tests/Scripts.Tests/` | C# (TUnit) | 80+ test files, sign-off, guards |
| Python toolkit | `python/toolkit/cli.py` | Python | Audio/video/filesystem CLI |
| PowerShell module | `powershell/ScriptsToolkit/ScriptsToolkit.psd1` | PowerShell | Azure quick setup, comment cleanup |
| Local Postgres | `docker-compose.yml` | YAML | Postgres 18 for C# data layer |
| YouTube state | `state/youtube/sync.json` | JSON | Sync checkpoints, playlist snapshots |
| System audit | `AI/references/system_inventory.md` | Markdown | Full SDK/CLI/MCP inventory (27 KB) |
| Database schemas | `AI/references/schema_mapping.md` | Markdown | Postgres schemas for YouTube/Fibery/Last.fm |
| Install scripts | `AI/artifacts/install_env.sh`, `powershell/Install-Env.ps1` | Bash + pwsh | Cross-platform dev env setup |

---

## Subsystems

### csharp/ .NET 10 Library + CLI

The largest subsystem. A Library that ships a Spectre.Console CLI for syncing Last.fm scrobbles to Postgres, enriching music metadata via MusicBrainz and Discogs, translating notes with Azure, reading articles from web and local files (PDF/EPUB/image OCR), and exporting to EPUB.

- **Solution**: `csharp/Scripts.slnx`
- **Namespace**: `Scripts.*` (rename from `CSharpScripts.*` is complete; see [Refactor Master Plan](#refactor-master-plan))
- **Key folders**:
  - `src/CLI/` . top-level commands (sync, music, clean, mail, read)
  - `src/Services/` . business logic (Music, Sync, Language, Cloud)
  - `src/Reader/` . article extraction, OCR providers, EPUB output
  - `src/Data/` . EF Core `ScriptsDbContext`, repositories, migrations, configurations
  - `src/Core/` . auth (Azure), secrets, shared infrastructure
  - `src/Orchestrators/` . `ScrobbleSyncOrchestrator`, `YouTubePlaylistOrchestrator`
- **Tests**: `csharp/tests/Scripts.Tests/` runs TUnit with Postgres fixtures, sign-off suites, and EF Core guard tests
- **Entry command**: `dotnet run --project csharp/src` (or `csharp/Scripts.csproj`)

### python/ . Python toolkit

A uv-managed Python package providing helpers for audio metadata (`toolkit/audio.py`), video frame extraction (`toolkit/video.py`), filesystem operations (`toolkit/filesystem.py`), Last.fm client (`toolkit/lastfm.py`), and pristine backup comparison (`toolkit/pristine.py`).

- **Manifest**: `python/pyproject.toml`
- **Lockfile**: `python/uv.lock` (use `uv sync` to install)
- **CLI entry**: `python/toolkit/cli.py`
- **Package manager**: `uv` (modern pip/poetry replacement)

### powershell/ . PowerShell module + installers

Windows-native tooling. The `ScriptsToolkit` PowerShell module bundles Azure quick setup, code comment stripping, and data helpers. The root `Install-Env.ps1` provisions the full Windows dev environment via winget and VS Code extensions.

- **Module manifest**: `powershell/ScriptsToolkit/ScriptsToolkit.psd1`
- **Env installer**: `powershell/Install-Env.ps1`
- **Profile**: `powershell/Microsoft.PowerShell_profile.ps1`

### state/ . Persistent runtime data

Gitignored runtime state. Each script writes its checkpoints here so that re-runs are idempotent. The `pristine/` subdirectory holds untouched backups of original state files for diffing.

- **Last.fm**: sync timestamps, processed scrobble IDs
- **YouTube**: `sync.json` (sync state) + `playlists/` (per-playlist snapshots) + `deleted/` (removed playlists)
- **Postgres**: Docker volume data (managed by `docker-compose.yml`)
- **Pristine**: `auth.json` and other originals, never modified

### subagents/ . Reserved

Placeholder for future AI subagent definitions. Currently empty. Refer to `~/.agents/skills/` (outside this repo) for the canonical skill set used by opencode and related agents.

### AI/ . Knowledge base

Curated reference material. Four active areas:

- **artifacts/**: install scripts (`install_env.sh`, `install_rust_cli_tools_windows10.ps1`) and the MCP config sync utility (`sync_mcp.py`)
- **plans/**: long-form planning docs (this file) and Architecture Decision Records under `plans/adr/`
- **references/**: system inventory, database schemas, and the **fibery-archive/** (47 historical docs migrated from the Fibery workspace; read-only reference for past investigations)

---

## Pending Work

All active work captured from this chat session. Items are sequenced by dependency (what blocks what) and grouped into 6 phases. See [Execution Order](#execution-order--dependencies) for the full graph and [Parallelization](#parallelization-opportunities) for tasks that can run concurrently.

### Summary

| Phase | Scope | Items | Blocked By |
|-------|-------|-------|------------|
| [A. Infrastructure Fixes](#a-infrastructure-fixes) | SDK pin, Docker, test compilation, logging relocation, Lingua migration | 6 | nothing (entry point) |
| [B. EF Core 3-Schema Migration](#b-ef-core-3-schema-migration) | Entity config, migration, compiled models, validation | 4 | A complete |
| [C. Azure Identity Fixes](#c-azure-identity-fixes) | DI registration, scope fix, error string, cleanup | 4 | A1 (SDK pin) |
| [D. Documentation](#d-documentation) | MASTER_PLAN update, ADR | 2 | independent (anytime) |
| [E. Verification](#e-verification) | Azure CLI, full build/test | 2 | B + C complete, A3, A4, F2 |
| [F. Post-Commit Code Review Audit](#f-post-commit-code-review-audit) | Critical bugs (C1-C6), infra regressions (I1-I5), meta (M1-M6) from commit `8941eeb` audit | 3 | nothing (highest priority — fixes regressions in already-shipped tier-1 work) |
| [G. Wire Up EF Layer](#g-wire-up-ef-layer) | Audit repository usage, wire repositories to consumer code, update DbContextRegistration | 3 | B complete |

**Entity-to-Schema Mapping** (confirmed for phase B):

| Schema | Entities | Count |
|--------|----------|-------|
| `youtube` | Video | 1 |
| `music` | Album, Artist, Track, Scrobble, ReleaseProgress | 5 |
| `fibery` | FiberyEntity | 1 |
| `public` | ExecutionLog, FailedTask, SourceRecord (cross-cutting) | 3 |
| **Total** | | **10** |

---

### A. Infrastructure Fixes

Must be done first. Blocks all build/test work and the EF Core migration.

#### A1. SDK Pin

- **What**: Create `global.json` at repo root to pin the .NET SDK to 10.0.300 (no SDK 11 preview).
- **File**: `global.json` (new)
- **Content**:
  ```json
  {
    "sdk": {
      "version": "10.0.300",
      "rollForward": "latestPatch"
    }
  }
  ```
- **Blocks**: All build/test work
- **Verify**: `dotnet --version` shows `10.0.3xx` (any 10.0.3 patch)
- **Effort**: Quick (5 min)

#### A2. Docker Environment Cleanup

- **What**: Stop Azure env var pollution in the Postgres container and provide a safe template for new clones.
- **Files**:
  - `docker-compose.yml` (edit) -- remove `env_file: .env`; keep only `environment:` block with Postgres vars
  - `.env.example` (new) -- template with PG + Azure + external API placeholders only
- **Keep**: `./state/postgres/18:/var/lib/postgresql` bind mount (already correct)
- **Verify**: `docker-compose up -d postgres` succeeds; `docker exec postgres psql -U postgres -c "SELECT 1"` returns `1`; no `AZURE_*` vars in `docker exec postgres env`
- **Effort**: Quick (15 min)

#### A3. Test Compilation Fix

- **What**: Resolve 56 `IDE0370` analyzer errors from SDK 11 preview flagging stale `[SuppressMessage]` attributes.
- **File**: `csharp/tests/Scripts.Tests/Scripts.Tests.csproj` (or equivalent)
- **Options** (pick one):
  - Add `<NoWarn>IDE0370</NoWarn>` to test csproj
  - Remove stale `[SuppressMessage]` attributes across test files
- **Verify**: `dotnet build csharp/tests/Scripts.Tests/Scripts.Tests.csproj` passes with 0 errors
- **Effort**: Quick (15 min)

#### A4. PlanInventoryTests Fix

- **What**: Test currently asserts 17 specific plan files exist; all 17 are deleted. The test is stale.
- **File**: `csharp/tests/Scripts.Tests/SignOff/PlanInventoryTests.cs`
- **Options** (pick one):
  - Delete `PlanInventoryTests.cs` (consolidation is final)
  - Rewrite to assert that exactly one plan file exists: `AI/plans/MASTER_PLAN.md`
- **Verify**: Test passes (or is removed from the run)
- **Effort**: Short (30 min)

#### A5. Logging Relocation

- **What**: Relocate the log directory from `$ProjectRoot/logs/` to `%USERPROFILE%\.cache\logs\scripts\`, add `Ben.Demystifier` for cleaned stack traces, remove `ServiceType.Sheets` from the enum, and ensure the log directory is created automatically.
- **Files**:
  - `csharp/src/Core/Paths.cs` (edit) — change `Paths.LogDirectory` from `Path.Combine(ProjectRoot, "logs")` to `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "logs", "scripts")`
  - `csharp/src/Core/Log.cs` (edit) — add `Directory.CreateDirectory` in `Log` static constructor
  - `csharp/src/Core/Log.cs` (edit) — add `Ben.Demystifier` NuGet and call `.Demystify()` on exceptions in `Log.Error(Exception, ...)` and `Log.Fatal(Exception, ...)`
  - `csharp/src/Core/ServiceType.cs` (edit) — remove `Sheets` from the enum and its logger initialization
  - `csharp/tests/Scripts.Tests/Logging/LogDirectoryTests.cs` (new) — verify log directory path
- **Source**: Old tier plan T1-12 (`AI/plans/tier-1-ef-migration/12-logging.md`), status: Not Started
- **Verify**: `dotnet build csharp/Scripts.csproj` passes; log files appear in `%USERPROFILE%\.cache\logs\scripts\`; no `ServiceType.Sheets` references remain
- **Effort**: Short (1 hour)

#### A6. Lingua Migration

- **What**: Replace NTextCat with `SearchPioneer.Lingua v1.0.5` in `LanguageIdentifier.cs`, removing the dependency on a missing `Core14.profile.xml` file and enabling self-contained language detection for 79 languages.
- **Files**:
  - `csharp/src/Services/Language/LanguageIdentifier.cs` (rewrite) — use Lingua's fluent API: `LanguageDetectorBuilder.FromAllLanguages().WithPreloadedLanguageModels().Build()`
  - `csharp/Scripts.csproj` (edit) — remove `<Compile Remove>` line for `LanguageIdentifier.cs`; add `SearchPioneer.Lingua` package
- **Public API contract preserved**: `Detect → string?`, `IsEnglish → bool`, `RequiresTranslation → bool`. `Language.Unknown` replaces NTextCat's null return.
- **Source**: Old tier plan T1-13 (`AI/plans/tier-1-ef-migration/13-lingua.md`), status: Not Started
- **Verify**: `dotnet build csharp/Scripts.csproj` passes; `grep -c "using Lingua" csharp/src/Services/Language/LanguageIdentifier.cs` returns 1
- **Effort**: Short (1 hour)
---

### B. EF Core 3-Schema Migration

The main work. Move 10 entities from `public` into 3 domain schemas (youtube, music, fibery). Cross-cutting tables stay in `public`. Single DbContext, compiled models, data-preserving `ALTER TABLE ... SET SCHEMA` migration.

#### B1. Entity Configuration Updates

- **What**: Add `ToTable(name, schema)` to 7 entity configurations in `csharp/src/Data/Configuration/*.cs`.
- **Files** (7 to edit, 3 unchanged):
  - `youtube` schema: `VideoConfiguration.cs` -> `videos`
  - `music` schema: `AlbumConfiguration.cs`, `ArtistConfiguration.cs`, `TrackConfiguration.cs`, `ScrobbleConfiguration.cs`, `ReleaseProgressConfiguration.cs` -> `albums`, `artists`, `tracks`, `scrobbles`, `release_progress`
  - `fibery` schema: `FiberyEntityConfiguration.cs` -> `fibery_entities`
  - `public` schema: `ExecutionLogConfiguration.cs`, `FailedTaskConfiguration.cs`, `SourceRecordConfiguration.cs` -- no change (default schema)
- **Pattern**:
  ```csharp
  builder.ToTable(name: "videos", schema: "youtube");
  ```
- **Verify**: `dotnet build csharp/Scripts.csproj` passes (0 errors)
- **Effort**: Short (1 hour)

#### B2. Migration Generation

- **What**: Generate a migration that physically moves tables between schemas without dropping data.
- **Command**:
  ```bash
  dotnet ef migrations add SchemaMigration --project csharp --startup-project csharp
  ```
- **Post-generation edit**: The auto-generated `Up()` will use `DROP TABLE` + `CREATE TABLE`. Manually replace with `ALTER TABLE ... SET SCHEMA` for data preservation:
  ```csharp
  migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS youtube;");
  migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS music;");
  migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS fibery;");

  migrationBuilder.Sql("ALTER TABLE public.videos SET SCHEMA youtube;");
  migrationBuilder.Sql("ALTER TABLE public.albums SET SCHEMA music;");
  // ... (5 more music tables)
  migrationBuilder.Sql("ALTER TABLE public.fibery_entities SET SCHEMA fibery;");
  ```
  Mirror in `Down()` with `SET SCHEMA public` and `DROP SCHEMA IF EXISTS`.
- **Verify**: Migration file exists; SQL preview shows `ALTER TABLE ... SET SCHEMA` (not `DROP`/`CREATE`)
- **Effort**: Short (30 min)

#### B3. Compiled Model Regeneration

- **What**: Regenerate EF Core precompiled models so they reference the new schemas.
- **Command**:
  ```bash
  dotnet ef dbcontext optimize --project csharp --startup-project csharp
  ```
- **Must be last** -- depends on final entity config from B1
- **Verify**: `csharp/CompiledModels/*.cs` updated; `grep "schema: \"music\""` returns matches
- **Effort**: Quick (5 min)

#### B4. Data Validation

- **What**: After `dotnet ef database update`, verify schema placement and row counts.
- **Commands**:
  ```bash
  docker exec postgres psql -U lance -d pg_db -c "SELECT schemaname, tablename FROM pg_tables WHERE schemaname IN ('youtube', 'music', 'fibery', 'public') ORDER BY schemaname, tablename;"
  ```
- **Pass conditions**:
  - 10 tables in correct schemas (1 youtube, 5 music, 1 fibery, 3 public)
  - Row counts match pre-migration counts (data preserved)
- **Effort**: Quick (15 min)

---

### C. Azure Identity Fixes

Eliminate inline `new DefaultAzureCredential()` calls, fix scope mismatch, correct stale error strings, and follow Microsoft best practice (no `DefaultAzureCredential` in production).

#### C1. DI Registration

- **What**: Register a single `TokenCredential` in DI; remove the 4 inline instances.
- **Files**:
  - `csharp/src/Core/Auth/AzureCredentialRegistration.cs` (new) -- factory that returns `ManagedIdentityCredential` in production, `ChainedTokenCredential` (VS Code -> Azure CLI -> PowerShell) in dev
  - `csharp/src/Program.cs` (edit) -- call `services.AddAzureCredentials()`; remove old `AzureCredentialManager.EnsureCredentials()` probe
- **Verify**: `dotnet build` passes; only one `TokenCredential` registration in DI
- **Effort**: Short (2 hours)

#### C2. Fix Scope Mismatch

- **What**: `AzureCredentialManager` currently probes `cognitiveservices.azure.com/.default` scope, but `CloudUsageService` needs `management.azure.com/.default` scope.
- **Files**: `csharp/src/Core/Auth/AzureCredentialManager.cs`
- **Options** (pick one):
  - Remove the probe entirely (Option A) -- let services fail lazily with clear errors
  - Probe multiple scopes in the validation pass (Option B) -- iterate both scopes
- **Recommendation**: Option A (remove probe). The DI-registered credential from C1 is sufficient.
- **Verify**: `dotnet build` passes; no `EnsureCredentials()` call from `Program.cs`
- **Effort**: Quick (30 min)

#### C3. Fix Stale Error String

- **What**: `MusicTranslateCommand.cs` line 25 and 28 reference `AZURE_TRANSLATOR_KEY`, but the code actually reads `AZURE_TRANSLATOR_ENDPOINT`.
- **File**: `csharp/src/CLI/Music/MusicTranslateCommand.cs`
- **Change**: Replace `"AZURE_TRANSLATOR_KEY is not set"` -> `"AZURE_TRANSLATOR_ENDPOINT is not set"` (both occurrences)
- **Verify**: `dotnet build` passes; grep confirms no remaining `AZURE_TRANSLATOR_KEY` references
- **Effort**: Quick (15 min)

#### C4. Cleanup Inline `DefaultAzureCredential`

- **What**: Remove all remaining inline `new DefaultAzureCredential()` instances.
- **Files** (4):
  - `csharp/src/Services/Language/AzureTranslationService.cs` -- convert from `static` to instance, inject `TokenCredential`
  - `csharp/src/Services/Cloud/CloudUsageService.cs` -- inject `TokenCredential`
  - `csharp/src/Reader/Ocr/AzureDocumentIntelligenceOcrProvider.cs` -- inject `TokenCredential`
  - `csharp/src/CLI/Music/MusicTranslateCommand.cs` -- update after C3
- **Note**: `AzureTranslationService` is currently `static`; converting to instance-based requires updating all callers to inject the service.
- **Verify**: `grep -r "new DefaultAzureCredential" csharp/src/` returns no matches
- **Effort**: Short (1 hour)

---

### D. Documentation

Capture architecture decisions and keep this file authoritative.

#### D1. Update MASTER_PLAN.md

- **What**: This file. Add the 3-schema migration plan (done), Azure Identity fixes (done), remove stale CLI references, add execution order with dependencies.
- **Status**: Completed in this revision
- **Effort**: Done

#### D2. Create ADR

- **What**: Document the 3-schema architecture decision.
- **File**: `AI/plans/adr/0001-ef-core-3-schema-architecture.md` (new)
- **Include**: Context, decision, consequences (positive/negative), alternatives considered (3 separate databases, 3 DbContexts, single schema)
- **Verify**: File exists, follows MADR or lightweight template
- **Effort**: Short (1 hour)

---

### E. Verification

End-to-end checks before declaring phase B + C complete.

#### E1. Azure CLI

- **What**: Confirm `az login` works and the active subscription is correct.
- **Commands**:
  ```bash
  az login
  az account show
  ```
- **Pass conditions**: `az account show` returns a non-empty `id`, `tenantId`, and `name` matching the expected subscription
- **Verification result (2026-06-05)**:
  - `az version` -> `azure-cli: 2.86.0`, `account extension: 0.2.5`
  - `az account show` exit 0; subscription `id: <subscription-id>`, `name: Azure`, `tenantId: <tenant-id>`, `user: <user-email>`, `state: Enabled`
  - `az account get-access-token` exit 0; returned a valid Bearer token (expires `2026-06-06 00:01:16 UTC`). End-to-end CLI path is confirmed working on this machine.
- **Effort**: Quick (5 min)
---

#### E2. Full Build & Test

- **What**: End-to-end verification that the entire solution compiles and all tests pass after Phases A–C complete.
- **Commands**:
  ```bash
  dotnet build csharp/Scripts.slnx
  dotnet test csharp/Scripts.slnx
  ```
- **Pass conditions**: `dotnet build` returns 0 errors; `dotnet test` passes all non-skipped tests
- **Blocked by**: A3, A4, B4, C2, C3, C4, F2 (PostgresFixture fixes must land first so the full test suite can run)
- **Effort**: Quick (10 min)

## Traceability: Old Tier Plans → MASTER_PLAN

The repository previously contained 47 tier plan files across 4 tiers (T1–T4) in `AI/plans/tier-{1,2,3,4}-*/`. These were created, deleted, and re-created multiple times. The current MASTER_PLAN consolidates all pending work into a single file. Below is the item-level mapping.

### Tier 1 (EF Core Migration — 17 phases)

| Old Plan | Description | MASTER_PLAN Item | Status | Notes |
|----------|-------------|------------------|--------|-------|
| T1-00 | Environment Foundation | A1 (SDK Pin) + A2 (Docker) | ✅ Done | Docker, PGCONNSTR, CanConnectAsync verified |
| T1-01 | Entity Extraction | — | ✅ Done | 10 entities in `Data/Entities/` |
| T1-02 | Entity Refactoring | — | ✅ Done | MBID removal tests pass |
| T1-03 | DbContext Config | — | ✅ Done | NoTracking, ApplyConfigurationsFromAssembly |
| T1-04 | Entity Configurations | B1 (schema support) | ✅ Done (base) | 10 configs exist; B1 adds `ToTable(name, schema)` |
| T1-05 | Database Migrations | B2 (schema migration) | ✅ Done (base) | 6 migrations exist; B2 adds schema migration |
| T1-06 | Repository Pattern | — | ✅ Done | 5 repos + interfaces + ResilienceFactory |
| T1-07 | State Manager Migration | — | ✅ Done | Single StateManager remains |
| T1-08 | Release Cache Migration | — | ✅ Done | ReleaseProgressService wired, CSV cache deleted |
| T1-09 | Sync Service Updates | — | ✅ Done | LastFmService has IDbContextFactory |
| T1-10 | EF10 Query Upgrades | — | ✅ Done | Guard tests written and passing |
| T1-11 | Compiled Model | — | ✅ Done | CompiledModelTests pass (but C2: bypassed in CI) |
| T1-12 | Logging Relocation | **A5** | ✅ Code in place | Commits `f8cbd19` + `e29ccab`; `Paths.LogDirectory` correct; `ServiceType.Sheets` removed from Core enum (but `Infrastructure/Logger.cs:319` still has Sheets — see F1.C1 note) |
| T1-13 | Lingua Migration | **A6** | ✅ Code in place | Commit `1e3aea9`; `LanguageIdentifier.cs` uses Lingua; duplicate `using Lingua;` at lines 1+3 (F1.C4) |
| T1-14 | Resilience Policies | F1.C1 (audit fix) | ⚠️ No-Op | Marked done but `EnableRetryOnFailure` never wired; F1 fixes |
| T1-15 | Testcontainers | — | ✅ Done | DatabaseTestFixture uses local Postgres |
| T1-16 | Sign-off Gate | E2 (full build/test) | ❌ Blocked | Blocked on T1-12, T1-13 |

### Tiers 2–4 (Deferred — Not in Scope)

| Tier | Focus | Status | Why Deferred |
|------|-------|--------|--------------|
| T2 | Modularization (8-project split) | 🔒 Blocked | Requires T1 sign-off first |
| T3 | Domain Isolation, DateTimeOffset | 🔒 Blocked | Requires T2 sign-off first |
| T4 | DI, Integration, Security | 🔒 Blocked | Requires T3 sign-off first |

Tiers 2–4 are architectural changes that should only begin after the core EF Core + Azure Identity work (Phases A–F) is complete and verified. They are intentionally excluded from this MASTER_PLAN.

**Provenance**: Traceability built from git history (commits `e29ccab`, `1562e16`) and `CURRENT_STATUS.md` (recovered from `e29ccab:AI/plans/CURRENT_STATUS.md`).
---


### F. Post-Commit Code Review Audit

Findings from a post-commit code review of the cascade-gate override in commit `8941eeb` ("feat(t1-16): EF10 compiled model regeneration + all 47 plans marked done in INDEX.md (cascade-gate override per user directive)"). Phase F is **P0 priority** because C1 and C2 are silent regressions in tier-1 deliverables that were marked complete by the override. Run F1 before any of A-E so the audit fixes are part of the rebuild, not a follow-up.

#### F1. Critical Bugs (C1-C6)

| ID | File:Line | Issue | Fix |
|----|-----------|-------|-----|
| C1 | `csharp/src/Data/DbContextRegistration.cs:13` and `csharp/src/Data/ScriptsDbContextFactory.cs:13` | T1-14 retry policy is a no-op. Both call sites use bare `opts.UseNpgsql(connStr)` with no `EnableRetryOnFailure`. Commit `c09170d` claimed Polly v8 `ResiliencePipeline` retry was added, but it was never wired. **Note**: `csharp/src/Infrastructure/Resilience.cs` (197 lines) is NOT dead — it has 30+ active call sites in `GoogleSheetsService.cs`. Do NOT delete it. Instead, migrate `GoogleSheetsService` to `Core.Resilience` (Polly v8) before removing. | Add `opts.UseNpgsql(connStr, npg => npg.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null))` to both sites. Migrate `GoogleSheetsService.cs` from `Infrastructure.Resilience` to `Core.Resilience`. Only then delete `Infrastructure/Resilience.cs`. Verify `RetryExhaustedException` lands in `csharp/src/Core/Resilience.cs` per the original plan. |
| C2 | `csharp/tests/Scripts.Tests/GlobalSetup.cs:14` | T1-16 compiled model is bypassed in tests via `SCRIPTS_NO_COMPILED_MODEL=1`. Root cause: EF Core 10.0.8 upstream TOCTOU race. Means t1-16 deliverable is unverifiable in CI. | Either fix the upstream race (file EF Core issue, pin to a non-buggy version) or move the `SCRIPTS_NO_COMPILED_MODEL` toggle to a runtime config so CI can opt in. Add a test that fails with a clear error when the env var is set. |
| C3 | `csharp/src/Data/ScriptsDbContext.cs:23,33` | `Console.WriteLine` debug diagnostics in production code. | Remove the two debug lines. If diagnostics are needed, inject and use `ILogger`. |
| C4 | `csharp/src/Services/Language/LanguageIdentifier.cs:1,3` | `using Lingua;` declared twice. | Delete the duplicate on line 3. |
| C5 | Semver comparison in version-check code | `string.Compare` is used for semver comparison, which gives wrong results (e.g., `"1.9.0" > "1.10.0"` is `true` lexicographically). | Replace with a proper semver parser. `System.Version` is insufficient; use a small helper that compares numeric segments. |
| C6 | `csharp/tests/Scripts.Tests/.../LinguaPackageReferenceTests.cs:94-114` | Tests hit `api.nuget.org` during test runs. Breaks offline CI and makes the suite non-deterministic. | Mock the NuGet API or move these to an integration test category excluded from the default run. |

**Verify F1**: `grep -rn "EnableRetryOnFailure" csharp/src/Data/` shows 2 matches; `grep -rn "Console.WriteLine" csharp/src/Data/ScriptsDbContext.cs` returns nothing; `grep -c "using Lingua" csharp/src/Services/Language/LanguageIdentifier.cs` returns 1; no `new DefaultAzureCredential` outside DI (cross-ref C1 from Phase C).

**Effort**: Short (3 hours)

#### F2. Infrastructure Regressions (I1-I5)

| ID | File | Issue | Fix |
|----|------|-------|-----|
| I1 | `csharp/tests/Scripts.Tests/Infrastructure/PostgresFixture.cs` | Concurrency model is not thread-safe. Multiple parallel test classes share a single fixture instance without synchronization. | Add a `SemaphoreSlim` around fixture initialization, or switch to `IAsyncLifetime` per test class. |
| I2 | `csharp/tests/Scripts.Tests/Infrastructure/PostgresFixture.cs` | `DisposeAsync` is not idempotent. Calling it twice (e.g., from a teardown hook + a using statement) throws. | Add a `_disposed` guard or use `Interlocked.Exchange` on a dispose flag. |
| I3 | `csharp/tests/Scripts.Tests/Infrastructure/` | Three layers of concurrency control (xUnit `[Collection]`, Polly retry, custom semaphore). Redundant and confusing. | Pick one. Recommended: xUnit `[CollectionDefinition]` for serial PG access; remove the custom semaphore and Polly retry. |
| I4 | `csharp/tests/Scripts.Tests/Infrastructure/TestDbInitializer.cs` | `TRUNCATE` statements hardcode table names that no longer match after the 3-schema migration. | Use a `pg_tables` query to enumerate and truncate, or read table names from the DbContext model. |
| I5 | (same as C2) | t1-16 compiled model unverifiable in CI. | (see C2 fix) |

**Verify F2**: Test suite runs in parallel with no fixture-related flakes; `TRUNCATE` succeeds after the B-schema migration lands.

**Effort**: Short (4 hours)

#### F3. Meta / Process Issues (M1-M6)

| ID | Issue | Fix |
|----|-------|-----|
| M1 | `[DBG]` debug markers left in source. | Add a `Debug.WriteLine` analyzer rule (or grep-based CI check) to fail the build on `[DBG]` markers. |
| M2 | `catch (Exception)` (catch-all) used in places where a more specific type or `when` filter would be appropriate. | Audit all `catch` blocks; replace catch-alls with specific types or add `when` filters. Consider an analyzer rule. |
| M3 | Plan/code drift: `IsoCode6391` vs `IsoCode6393` — the plan referenced one, the code implements the other (or vice versa). | Reconcile. Update the plan to match the code (or fix the code to match the plan). Document the choice. |
| M4 | Commit messages are misleading. Commit `8941eeb` claims "47 plans marked done" via cascade-gate override, masking that the underlying tier-1 deliverables (T1-14, T1-16) have critical bugs. | Adopt a commit message policy: subject + body with explicit link to the plan/issue. Reject cascade-gate overrides; require each tier-1 deliverable to pass code review before the corresponding checkbox flips. |
| M5 | Test refactor moved tests to require a live Postgres connection, even tests that don't need one (e.g., pure unit tests). | Split tests into `[Trait("Category", "Unit")]` and `[Trait("Category", "Integration")]`. Default run = unit only. Integration opt-in via env var. |
| M6 | Commit `cbb4a62` re-introduces the compiled-model debugging branch that commit `8941eeb` was supposed to remove. | Audit commits between `cbb4a62` and `8941eeb`; either revert `cbb4a62` or document why the debugging branch needs to stay. |

**Verify F3**: CI fails on `[DBG]` markers; commit message lint passes; integration tests are gated by category; no `catch (Exception)` in non-top-level catch blocks (analyzer rule).

**Effort**: Medium (1 day)

---

### G. Wire Up EF Layer

The EF layer is currently 'dead' — entities exist but are not wired to consumer code. This phase revives the repository pattern so that the 3-schema migration (Phase B) actually has consumers.

#### G1. Audit Repository Usage

- **What**: Determine which of the 5 repositories (AlbumRepository, ArtistRepository, TrackRepository, ScrobbleRepository, FiberyEntityRepository) are actually called by consumer code (CLI commands, services, orchestrators).
- **Deliverable**: `csharp/docs/repository-usage-audit.md` — matrix of repository × consumer, with call-count and last-used-commit.
- **Effort**: Short (2 hours)

#### G2. Wire Repositories to Consumer Code

- **What**: Update CLI commands and services to use the repository interfaces instead of direct `ScriptsDbContext` access.
- **Files**: `csharp/src/CLI/`, `csharp/src/Services/`, `csharp/src/Orchestrators/` — any file that currently calls `context.Albums.AddAsync()` etc. directly.
- **Pattern**: Inject `IAlbumRepository` (etc.) via constructor; replace direct `context.SaveChangesAsync()` with repository method calls.
- **Verify**: `grep -rn "context\.Albums\." csharp/src/` returns no matches (all album operations go through the repository).
- **Effort**: Short (4-8 hours)

#### G3. Update DbContextRegistration

- **What**: Register all 5 repositories in DI via `csharp/src/Data/DbContextRegistration.cs`.
- **Pattern**:
  ```csharp
  services.AddScoped<IAlbumRepository, AlbumRepository>();
  // ... (4 more)
  ```
- **Verify**: `dotnet build csharp/Scripts.csproj` passes; `dotnet test csharp/Scripts.slnx` passes.
- **Effort**: Quick (30 min)

---

**Note on the working-tree state of this file**: `AI/plans/MASTER_PLAN.md` is currently untracked (`git status` shows `?? AI/plans/MASTER_PLAN.md`). It must be `git add`ed and committed before the next planning session treats it as authoritative. The plan-introduces-adr D2 is similarly uncommitted (directory `AI/plans/adr/` is missing from the working tree).

## Execution Order & Dependencies

Strict dependency graph. Tasks within the same wave can run in parallel.

```
Wave 0 (no blockers)
  A1, A2, A3, A4, A5, A6, F1  # F1 is P0 audit; A5/A6 are old T1-12/T1-13 carryovers; A6 must complete BEFORE F1.C4 (both touch LanguageIdentifier.cs)

Wave 1 (depends on A1 -- SDK pin)
  C1, C2, C3, C4

Wave 2 (depends on Wave 0 complete)
  B1

Wave 3 (depends on B1)
  B2  # C2 moved to Wave 1 (was duplicated here erroneously)

Wave 4 (depends on B2)
  B3

Wave 5 (depends on B3 + Wave 1)
  B4, E1

Wave 6 (independent, can run anytime after Wave 0)
  D1 (done), D2, F2, F3, E2  # F2 must complete BEFORE E2 (PostgresFixture fixes needed for test suite)

Wave 7 (depends on B4 complete — Phase G: wire EF layer to consumers)
  G1 (audit)  # must complete before G2

Wave 8 (depends on G1)
  G2 (wire repositories)  # must complete before G3

Wave 9 (depends on G2)
  G3 (DI registration)
```

### Blocking Matrix

| Task | Blocks | Blocked By |
|------|--------|------------|
| A1 | A3, A4, B*, C*, E2 | nothing |
| A2 | B4 (data validation needs running Postgres) | nothing |
| A3 | E2 | A1 |
| A4 | E2 | A1 |
| A5 | nothing | nothing (independent; touches `Core/Paths.cs`, `Core/Log.cs`, `Core/ServiceType.cs`) |
| A6 | nothing | nothing (independent; touches `Services/Language/LanguageIdentifier.cs`, csproj) |
| B1 | B2, B3 | A1 |
| B2 | B3, B4 | B1 |
| B3 | B4 | B2 |
| B4 | E2 | A2, B3 |
| C1 | C4, E1 | A1 |
| C2 | E2 | A1 (and ideally C1) |
| C3 | E2 | A1 |
| C4 | E1, E2 | C1 |
| D1 | nothing | nothing (done) |
| D2 | nothing | nothing (independent) |
| E1 | nothing | C1, C4 |
| E2 | nothing | A3, A4, B4, C2, C3, C4, F2 |
| F1 | E1, E2 (E2 should re-run after C1 fix lands) | nothing (P0; run in Wave 0) |
| F2 | E2 (full test suite) | F1 (fix PostgresFixture before re-running full suite) |
| F3 | nothing | nothing (process work, no code coupling) |
|| G1 | nothing | B4 (schema migration must complete first) |
|| G2 | G3 | G1 (audit must complete before wiring) |
|| G3 | E2 (DI registration affects test suite) | G2 (wiring must complete before registration) |

---

## Parallelization Opportunities

These task pairs (or groups) can run concurrently because they touch independent files and have no data dependency.

| Wave | Group A | Group B | Reason |
|------|---------|---------|--------|
| Wave 0 | A1 (SDK pin) | A2 (Docker) | Different files; no shared state |
| Wave 0 | A3 (test compile) | A4 (PlanInventoryTests) | A3 fixes the csproj, A4 fixes a single test file |
| Wave 0 | A5 (Logging Relocation) | A6 (Lingua Migration) | A5 touches `Core/Paths.cs`, `Core/Log.cs`, `Core/ServiceType.cs`; A6 touches `Services/Language/LanguageIdentifier.cs`. No overlap. |
| Wave 0 | A5, A6 | A1-A4 | A5/A6 touch `Core/` and `Services/`; A1-A4 touch `global.json`, `docker-compose.yml`, `.env.example`, test csproj. No overlap. |
| Wave 1 | C1 (DI registration, new file) | C3 (error string fix) | Different files; C3 doesn't need C1 |
| Wave 1 | C1, C2, C3, C4 | B1 | C touches `Core/Auth`, `Services/`, `CLI/`; B touches `Data/Configuration/`. No overlap. |
| Wave 6 | D2 (ADR) | anything in Waves 0-5 | Pure documentation work, no code coupling |
| Wave 0 | F1.C1-C3, F1.C5-C6 (critical bug audit) | A1-A6 (infrastructure fixes) | F1.C1 touches `csharp/src/Data/`, F1.C5 touches version-check code, F1.C6 touches tests. A1-A6 touch `global.json`, `docker-compose.yml`, `.env.example`, test csproj, `Core/`, `Services/`. **EXCEPTION**: F1.C4 and A6 both touch `LanguageIdentifier.cs` — A6 must complete first (sequenced in Wave 0). |
| Wave 0 | F1.C5 (semver) | F1.C6 (NuGet test network) | Both are small, independent test/code changes. |
| Wave 6 | F2 (test infra) | D2 (ADR) | F2 touches test infrastructure; D2 is a markdown file. No coupling. |
| Wave 6 | F3 (process) | D2 (ADR) | F3 is commit-message/CI-config work; D2 is a markdown file. |
| Wave 7 | G1 (audit repositories) | (nothing — solo task) | G1 touches `csharp/src/` read-only; no concurrent work needed |
| Wave 8 | G2 (wire repositories) | D2 (ADR) | G2 touches `csharp/src/CLI/`, `Services/`, `Orchestrators/`; D2 is a markdown file. No coupling. |
| Wave 9 | G3 (DI registration) | D2 (ADR) | G3 touches `csharp/src/Data/DbContextRegistration.cs`; D2 is a markdown file. No coupling. |

**Explicitly sequential** (do NOT parallelize):

- A1 then A3/A4 -- SDK must be pinned before test code can be analyzed correctly
- B1 then B2 then B3 then B4 -- each migration step depends on the previous
- C1 then C4 -- C4's caller updates assume C1's `TokenCredential` exists in DI

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Migration fails partway through | Low | High | Apply to fresh database first; have `Down()` rollback script ready; capture row counts before/after |
| Compiled models become stale | Medium | Medium | Regenerate after every schema change (B3 must follow B2 immediately) |
| `AzureTranslationService` is `static` and used widely | High | Medium | Audit all callers before C4; update each `static` usage to inject the service |
| Azure credentials break in CI | Low | High | Test locally with `az login` first; use managed identity in CI |
| Test compilation still fails after A1 | Low | High | Fall back to `<NoWarn>IDE0370</NoWarn>` in test csproj |
| PlanInventoryTests still references deleted files | Low | Low | Delete or rewrite to assert single `MASTER_PLAN.md` exists |
| F1 regressions in already-shipped tier-1 work go unnoticed (C1, C2 cascade) | **High** | **High** | F1 is P0 in Wave 0. Run F1 grep/build checks as a pre-merge gate; do not let cascade-gate overrides skip code review (M4). |
| Compiled model is bypassed in CI (C2 / I5) so T1-16 ships unverified | High | High | Either pin EF Core to a version without the TOCTOU race or move the env var to an explicit opt-in with a failing test. |
| `PostgresFixture` not thread-safe (I1) causes flaky CI | Medium | High | F2 makes `DisposeAsync` idempotent and adds a `SemaphoreSlim`; gate the full test suite on a 3x local run before merging. |
| Commit `8941eeb` cascade-gate override masks future regressions (M4) | Medium | High | Reject cascade-gate overrides; require each tier-1 deliverable to pass code review before the corresponding checkbox flips. Document the policy in D2. |
| `MASTER_PLAN.md` itself is untracked and could be lost (working-tree state) | Low | High | `git add AI/plans/MASTER_PLAN.md` and commit before next planning session. Add a `git status` check to the pre-merge gate. |

---

## Success Criteria

Phase A done when:

- [ ] `dotnet --version` returns `10.0.3xx`
- [ ] `docker exec postgres env | grep AZURE` returns nothing
- [ ] `.env.example` exists
- [ ] `dotnet build csharp/tests/Scripts.Tests/Scripts.Tests.csproj` passes (0 errors)
- [ ] `PlanInventoryTests` passes (or is deleted)
- [ ] `Paths.LogDirectory` resolves to `%USERPROFILE%\.cache\logs\scripts\` (A5)
- [ ] `grep -c "using Lingua" csharp/src/Services/Language/LanguageIdentifier.cs` returns 1 (A6)
- [ ] No `ServiceType.Sheets` references remain (A5)

Phase B done when:

- [ ] 7 entity configurations updated with `ToTable(name, schema)` (3 cross-cutting unchanged)
- [ ] Migration uses `ALTER TABLE ... SET SCHEMA` (not `DROP`/`CREATE`)
- [ ] Compiled models regenerated and reference new schemas
- [ ] Row counts preserved after migration
- [ ] `SELECT schemaname, tablename FROM pg_tables WHERE schemaname IN ('youtube', 'music', 'fibery', 'public');` shows 10 tables in correct schemas

Phase C done when:

- [ ] Single `TokenCredential` registered in DI (no inline `new DefaultAzureCredential()`)
- [ ] All Azure services use constructor-injected `TokenCredential`
- [ ] `AZURE_TRANSLATOR_KEY` references replaced with `AZURE_TRANSLATOR_ENDPOINT`
- [ ] `AzureCredentialManager.EnsureCredentials()` removed or scope-agnostic
- [ ] Production uses `ManagedIdentityCredential`, dev uses `ChainedTokenCredential`

Phase D done when:

- [ ] `AI/plans/MASTER_PLAN.md` reflects current architecture
- [ ] `AI/plans/adr/0001-ef-core-3-schema-architecture.md` exists

Phase E done when:

- [ ] `az login` succeeds; `az account show` returns expected subscription
- [ ] `dotnet build csharp/Scripts.slnx` succeeds (0 errors)
- [ ] `dotnet test csharp/Scripts.slnx` passes
- [ ] All guard tests (EF11GuardTests, Ef11ForbiddenPatternsTests, Ef10ReplacementPatternTests) pass

Phase F done when (P0 — audit, do first):

- [ ] C1: `grep -rn "EnableRetryOnFailure" csharp/src/Data/` shows matches in BOTH `DbContextRegistration.cs` AND `ScriptsDbContextFactory.cs`
- [ ] C1: `GoogleSheetsService.cs` migrated from `Infrastructure.Resilience` to `Core.Resilience`; `csharp/src/Infrastructure/Resilience.cs` deleted; `csharp/src/Core/Resilience.cs` contains `RetryExhaustedException` and is the sole resilience helper
- [ ] C2: `SCRIPTS_NO_COMPILED_MODEL` is no longer set in `GlobalSetup.cs`; a test exists that fails if the env var is set without an explicit opt-in
- [ ] C3: `grep -n "Console.WriteLine" csharp/src/Data/ScriptsDbContext.cs` returns nothing
- [ ] C4: `grep -c "using Lingua" csharp/src/Services/Language/LanguageIdentifier.cs` returns 1
- [ ] C5: Semver comparison replaced with a numeric-segment helper; unit test covers `1.9.0 < 1.10.0` and `1.10.0 > 1.9.0`
- [ ] C6: `LinguaPackageReferenceTests` is in an integration test category, excluded from the default `dotnet test` run
- [ ] I1-I4: Test suite runs in parallel with no fixture-related flakes; `TRUNCATE` succeeds against the post-migration 3-schema database
- [ ] M1: CI fails on `[DBG]` markers (analyzer or grep check)
- [ ] M2: No bare `catch (Exception)` outside top-level handlers (analyzer rule)
- [ ] M3: `IsoCode6391` vs `IsoCode6393` plan/code drift resolved and documented
- [ ] M4: Commit `8941eeb` re-audited; cascade-gate override policy documented and enforced
- [ ] M5: Tests split by `[Trait("Category", "Unit")]` and `[Trait("Category", "Integration")]`; default run = unit only
- [ ] M6: Commits between `cbb4a62` and `8941eeb` audited; either `cbb4a62` reverted or debugging branch justified in writing
- [ ] `AI/plans/MASTER_PLAN.md` is committed to git (currently `?? AI/plans/MASTER_PLAN.md`)

Phase G done when:

- [ ] `csharp/docs/repository-usage-audit.md` exists with repository × consumer matrix
- [ ] `grep -rn "context\.Albums\." csharp/src/` returns no matches (all operations go through repositories)
- [ ] All 5 repositories registered in `DbContextRegistration.cs`
- [ ] `dotnet build csharp/Scripts.csproj` passes
- [ ] `dotnet test csharp/Scripts.slnx` passes
---

## Refactor Master Plan

The C# namespace refactor (`CSharpScripts.*` -> `Scripts.*`), access modifier enforcement, and path consolidation were completed before this work began. The plan is preserved at:

- **`AI/plans/atlas-planning-handoff.md`** (1007 lines) -- detailed 3-schema migration + Azure remediation handoff
- **`AI/plans/planning-handoff-summary.md`** (169 lines) -- high-level summary of decisions

If you need to see the original refactor scope, the historical `csharp/plans/refactor_plan.md` was deleted in this revision (it was a different scope: namespace rename only).

---

## Installation Order

Three install scripts cover the cross-platform dev environment. Run them in this order on a fresh machine:

| Order | Script | Platform | What it does |
|-------|--------|----------|--------------|
| 1 | `powershell/Install-Env.ps1` | Windows (pwsh) | winget packages, VS Code extensions, symlinks |
| 2 | `~/Desktop/Install-RustCliTools.ps1` | Windows (pwsh) | 41 Rust CLI/TUI tools via cargo (single source of truth) |
| 3 | `AI/artifacts/install_env.sh` | Linux/WSL2 (bash) | SDKs (Go, pwsh), CLI tools, shell config |

For the Python toolkit, use `uv sync` inside `python/`. For the C# project, restore with `dotnet restore` and the EF Core tool (`dotnet tool install --global dotnet-ef`).

---

## Architecture Decision Records

ADRs live at `AI/plans/adr/NNNN-short-title.md` using the MADR template (or any lightweight format). The directory does not exist yet; create it when writing the first ADR ([D2](#d2-create-adr)).

When to write an ADR:

- New dependency or framework adoption
- Schema topology change (see ADR 0001, planned)
- Namespace restructuring
- Deployment target or hosting model change
- Any decision that affects 3+ subsystems

---

## Maintenance & Contributing

### Conventions

- **Markdown**: H1 for the file title, H2 for top-level sections, H3 for subsections. Tables for structured data, code blocks for examples. No em or en dashes, no AI slop phrases ("delve", "leverage", "in the world of..."). See existing READMEs for tone.
- **C# code**: follow the conventions in `csharp/Directory.Build.props` and the active guard tests under `csharp/tests/Scripts.Tests/Guards/`. EF Core 10/11 patterns enforced via `EF11GuardTests` and `Ef10ReplacementPatternTests`.
- **Python**: type hints required, ruff-style formatting, `uv` for dependency management.
- **PowerShell**: approved verbs (`Get-`, `Set-`, `Invoke-`), comment-based help on exported functions.
- **Plans**: every plan file lives under `AI/plans/`. This file (`MASTER_PLAN.md`) is the index; ADRs go in `AI/plans/adr/`. No plan files anywhere else in the repo.

### Cleanup

- The `fibery-archive/` under `AI/references/` is a frozen historical snapshot. Don't edit those files; if you need to correct something, write a new doc and link to it.
- The `state/` directory is gitignored runtime data. Never commit changes here.
- Re-audit the system inventory (`AI/references/system_inventory.md`) quarterly. The Rust tool list (`Install-RustCliTools.ps1`) drifts faster than the rest.
- When a plan phase is complete, move its checkboxes from pending to done in the [Success Criteria](#success-criteria) tables above. Don't delete completed sections; keep them as a record.

### Contributing flow

1. Identify the subsystem you want to change.
2. Read the subsystem's README or section in this master plan.
3. If the change is architectural, write or update an ADR in `AI/plans/adr/`.
4. If the change touches the EF Core data layer, update the relevant `Data/Configuration/*.cs` file and consider whether a migration is needed.
5. Run the test suite for the affected subsystem before opening a PR.

---

## Key Files Reference

### Configuration

- `global.json` -- SDK pin (10.0.300) [planned, A1]
- `docker-compose.yml` -- PostgreSQL 18 service
- `.env` -- environment variables (gitignored)
- `.env.example` -- template [planned, A2]
- `csharp/Scripts.csproj` -- main library project
- `csharp/tests/Scripts.Tests/Scripts.Tests.csproj` -- test project
- `csharp/Directory.Build.props` -- shared build properties

### EF Core

- `csharp/src/Data/ScriptsDbContext.cs` -- DbContext
- `csharp/src/Data/Configuration/*.cs` -- entity configurations (10 files; 7 need schema updates)
- `csharp/src/Data/Entities/*.cs` -- entity classes (10 files)
- `csharp/src/Data/Migrations/*.cs` -- migrations (7 existing + 1 new schema migration)
- `csharp/src/Data/Repositories/*.cs` -- repositories (5 files)
- `csharp/CompiledModels/*.cs` -- compiled models (auto-generated)

### Azure

- `csharp/src/Core/Auth/AzureCredentialManager.cs` -- credential validation (to be removed/scope-agnostic in C2)
- `csharp/src/Core/Auth/AzureCredentialRegistration.cs` -- DI registration (to be created in C1)
- `csharp/src/Core/Auth/Secrets.cs` -- environment variable reader
- `csharp/src/Services/Language/AzureTranslationService.cs` -- translation (static -> instance in C4)
- `csharp/src/Services/Cloud/CloudUsageService.cs` -- cost management
- `csharp/src/Reader/Ocr/AzureDocumentIntelligenceOcrProvider.cs` -- OCR
- `csharp/src/CLI/Music/MusicTranslateCommand.cs` -- error message fix in C3

### Tests

- `csharp/tests/Scripts.Tests/SignOff/PlanInventoryTests.cs` -- plan file validation [fix in A4]
- `csharp/tests/Scripts.Tests/Guards/EF11GuardTests.cs` -- EF 11 API guards
- `csharp/tests/Scripts.Tests/Guards/Ef11ForbiddenPatternsTests.cs` -- EF 11 forbidden patterns
- `csharp/tests/Scripts.Tests/Guards/Ef10ReplacementPatternTests.cs` -- EF 10 patterns
- `csharp/tests/Scripts.Tests/DbContext/SchemaMappingTests.cs` -- schema validation (optional, after B3)
