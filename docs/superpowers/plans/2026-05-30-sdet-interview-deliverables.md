# SDET Interview Deliverables — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce four employer-facing deliverables from the Scripts repo's 172-commit, 18-month evolution: a README, an interview question bank, an MCQ quality-enforcement subagent pass, and a modular learning checklist.

**Architecture:** The repo evolved through 5 distinct phases from a flat PowerShell+Python script collection into a multi-language monorepo with EF Core 10, Docker PostgreSQL 18, 170+ TUnit tests, and a 4-tier modularization plan. Each deliverable maps to a different interview preparation axis.

**Tech Stack:** C# (.NET 10, EF Core 10, TUnit, Spectre.Console), Python (uv, pyright), PowerShell 7+, PostgreSQL 18, Docker Compose, Google Sheets API, Last.fm/YouTube/Discogs/MusicBrainz APIs, Azure (Translation, Document Intelligence), OCI Object Storage.

---

## Repo Evolution Map (Analysis Summary)

### Phase 1: Script Origins (Jul–Dec 2024) — 47 commits
- **Root:** `Bearmancer/Powershell` — flat `.ps1` + `.py` files
- **Domain:** Audio processing (SACD→FLAC→MP3), video editing (Whisper AI, GIF creation), torrent utilities
- **Key commits:** `c32900f` (first music script), `c275d98` (migrate audio to Python), `a6a2134` (Gemini CLI in Go)
- **Complexity:** Single-folder, no project structure, hardcoded paths, no tests

### Phase 2: C# Introduction + Monorepo Reorg (Nov–Dec 2025) — 34 commits
- **Turning point:** `be4fe57` (reorganize into `csharp/`, `powershell/`, `python/`)
- **C# birth:** `8db62d6` (Initialize C# scripting project), `584ac8e` (CLI + Google Sheets + state management)
- **Integrations:** Last.fm, YouTube, Discogs, MusicBrainz, Mail.tm
- **CLI:** Spectre.Console `CommandApp` with DI
- **Complexity spike:** From 0 to ~60 C# files, external API orchestration, Google OAuth 2.0

### Phase 3: Sync Engine + Polish (Jan–May 2026) — 15 commits
- **Features:** Last.fm/YouTube playlist sync, change detection, caching
- **Python hardening:** `b356249` (strict type checking with `ty`)
- **Reader service:** Azure Document Intelligence OCR, Playwright browser extraction, PDF/ePub parsing
- **Language services:** Azure Translation, Lingua language detection

### Phase 4: EF Core 10 Migration (May 2026) — 41 commits (highest month)
- **Scale:** `f48ed3f` → `e29ccab` — 17 sequential TDD phases
- **Entities:** 10 domain entities (Artist, Album, Track, Scrobble, Video, ExecutionLog, FailedTask, FiberyEntity, SourceRecord, ReleaseProgress)
- **Migrations:** 6 EF Core migrations with compiled model
- **Repositories:** 5 repos with interfaces + `RepositoryResilienceFactory`
- **Guards:** `Ef11ForbiddenPatternsTests.cs` — regex-based build guards
- **Testcontainers:** `DatabaseTestFixture` with real PostgreSQL
- **Result:** 170 tests, 100% pass rate, `TreatWarningsAsErrors=true`

### Phase 5: Modularization (Planned) — T2–T4
- **T2:** 8-project CPM split (Core, Data, Services.Language, Services.Music, Orchestrators, Reader, CLI, Tests)
- **T3:** Domain isolation + naming refactor
- **T4:** Integration hardening, DI wiring, security audit, OCI deployment

### Complexity Growth Metrics
| Metric | Phase 1 | Phase 2 | Phase 3 | Phase 4 |
|--------|---------|---------|---------|---------|
| C# files | 0 | ~60 | ~100 | 151 source + 75 test |
| Languages | PS1, PY | +C# | +C# | C# (primary) |
| Tests | 0 | ~10 | ~30 | 170 |
| External APIs | 0 | 5 | 7 | 7 |
| DB tables | 0 | 0 | 0 | 10 |
| Commits/month | 6 avg | 17 | 5 | 41 |

---

## Deliverable 1: Employer README (`docs/employer-readme.md`)

### Purpose
A README.md that employers can read to understand the repo's architecture, the skills it demonstrates, and the engineering decisions made. Distinguishes components without requiring separate repos.

### Structure
- [ ] **Step 1: Write header + one-paragraph summary**
  - Project name, languages, purpose
  - "Multi-language automation monorepo: C# (.NET 10) orchestration + Python toolkit + PowerShell utilities"
  - Mention: PostgreSQL 18, Docker Compose, 170+ tests, EF Core 10

- [ ] **Step 2: Write Architecture section**
  - ASCII diagram showing dependency flow: `CLI → Orchestrators → Data → Core`
  - Explain why single repo with `Scripts.slnx` (shared entities, cross-domain queries, unified test suite)
  - Old pipeline vs new pipeline diagram from AGENTS.md

- [ ] **Step 3: Write Component Breakdown**
  - `csharp/src/` — 6 logical domains (Core, Data, Services, Orchestrators, Reader, CLI)
  - `python/toolkit/` — Audio, video, filesystem, Last.fm utilities
  - `powershell/ScriptsToolkit/` — Azure setup, data scripts, profile
  - `AI/` — Plan-driven development with AI agents

- [ ] **Step 4: Write Key Technical Decisions section**
  - Why EF Core 10 over Dapper/ADO.NET
  - Why TUnit over xUnit/NUnit
  - Why Spectre.Console for CLI
  - Why `uv` over `pip`
  - Why local PostgreSQL over cloud-first

- [ ] **Step 5: Write Skills Demonstrated matrix**
  - Table: Skill Category → Specific Evidence → File Reference
  - Categories: API Integration, Database Design, Testing Strategy, CLI Architecture, Resilience Patterns, Multi-language Engineering

- [ ] **Step 6: Write Getting Started section**
  - Prerequisites, setup commands, how to run tests

---

## Deliverable 2: Interview Question Bank (`docs/interview-question-bank.md`)

### Purpose
Comprehensive question bank for SDET interviews. Three sections: MCQs, long-form, debug scenarios. All tailored for first-job SDET level.

### Section A: MCQ Battery (50 questions)

- [ ] **Step 7: Generate MCQs — Category 1: C# / .NET Fundamentals (10 questions)**
  - Topics: async/await, nullable reference types, DI, primary constructors, file-scoped namespaces, global usings, LINQ, IDisposable, `IAsyncDisposable`, `CancellationToken`
  - Quality rule: All 4 options must be plausible. No option should be obviously wrong. Distractors should reflect common misconceptions.
  - Example format:
    ```
    Q: In this repo's `ScriptsDbContext`, why is `IDbContextFactory<T>` injected
       instead of `DbContext` directly?
    A) DbContext is deprecated in EF Core 10
    B) DbContext is not thread-safe; factory creates isolated instances per operation
    C) IDbContextFactory enables lazy loading
    D) Factory pattern is required for compiled models
    ```
    Correct: B. Distractors: A is plausible (version confusion), C is a real EF concept, D references a real feature.

- [ ] **Step 8: Generate MCQs — Category 2: Database / EF Core (10 questions)**
  - Topics: migrations, JSONB columns, entity configurations, `ExecuteUpdateAsync` vs `SaveChanges`, compiled models, `NoTracking`, repository pattern, connection string handling, `TIMESTAMPTZ` vs `TIMESTAMP`, 3NF normalization
  - Include EF10 vs EF11 guard pattern questions

- [ ] **Step 9: Generate MCQs — Category 3: Testing Strategy (10 questions)**
  - Topics: TUnit attributes, guard tests, Testcontainers, reflection-based assertions, test isolation, `EnsureDeletedAsync` vs `EnsureCreatedAsync`, integration vs unit, mock vs real DB, architectural boundary tests, `TreatWarningsAsErrors`
  - Include questions about the EF11 forbidden pattern tests

- [ ] **Step 10: Generate MCQs — Category 4: API Integration & Resilience (10 questions)**
  - Topics: OAuth 2.0 flow, rate limiting, retry policies (Polly), `Task.WhenAll` concurrency, thread-safety of DbContext, cancellation tokens, HTTP 429 handling, exponential backoff, API pagination, incremental sync

- [ ] **Step 11: Generate MCQs — Category 5: DevOps & Infrastructure (10 questions)**
  - Topics: Docker Compose, PostgreSQL health checks, `.env` secrets management, `git worktree`, CI/CD concepts, `gitleaks`, OCI Object Storage, WAL backup, connection string security, `PGCONNSTR`

### Section B: Long-Form Questions (10 questions)

- [ ] **Step 12: Write long-form questions with evaluation rubrics**
  - Each question: scenario + what to evaluate + strong/weak answer indicators
  - Topics:
    1. Walk through the repo's architecture from CLI entry point to database write
    2. Explain the EF Core migration strategy and why T1→T2→T3→T4 is sequential
    3. Describe how you'd add a new entity (Artist → Album → Track pipeline)
    4. Compare the old Google Sheets pipeline vs new PostgreSQL pipeline
    5. Explain the guard test pattern (Ef11ForbiddenPatternsTests) and why it exists
    6. Describe the Python toolkit's role and how it complements the C# system
    7. Walk through error handling in a sync operation (Last.fm or YouTube)
    8. Explain the `RepositoryResilienceFactory` and retry policies
    9. How would you debug a failing EF Core migration?
    10. Describe the state management system (StateManager + JSON files → EF)

### Section C: Debug Scenarios (8 scenarios)

- [ ] **Step 13: Write debug scenarios with step-by-step solutions**
  - Each scenario: bug description + code snippet + expected behavior + debugging steps + root cause + fix
  - Scenarios:
    1. `DbContext` concurrent access throws `InvalidOperationException` during YouTube sync
    2. Migration fails with "relation already exists" after partial apply
    3. `JsonContains` query returns no results despite data existing in JSONB column
    4. Testcontainers test passes locally but fails in CI (port conflict)
    5. `LastFmService` silently drops tracks due to `CancellationToken` premature cancellation
    6. EF Core compiled model out of date after adding new entity property
    7. `RepositoryResilienceFactory` retry loop causes duplicate scrobbles
    8. `StateTransition` JSON deserialization fails after schema change

---

## Deliverable 3: MCQ Quality Enforcement Subagent

### Purpose
Validate that all MCQs meet quality standards. No "throwaway" options.

- [ ] **Step 14: Define MCQ quality rules**
  - Rule 1: Every option must be a plausible answer to someone with partial knowledge
  - Rule 2: No option should be obviously absurd or use opposite phrasing
  - Rule 3: Correct answer must not always be the longest/most detailed
  - Rule 4: Correct answer position must be randomized (not always B)
  - Rule 5: Each question must target a specific concept from the actual codebase
  - Rule 6: Distractors should reflect real misconceptions (e.g., "EF Core 11 has MaxByAsync" is a good distractor because it's true but wrong for this repo)
  - Rule 7: Difficulty calibrated for SDET entry-level (0–2 years experience)

- [ ] **Step 15: Dispatch subagent to validate MCQs**
  - Task: Read `docs/interview-question-bank.md` Section A
  - For each question, rate each option's plausibility on a 1–5 scale
  - Flag any question where a distractor scores < 2 (implausible)
  - Flag any question where correct answer is always the longest
  - Flag any question where correct answer position is not randomized
  - Output: list of questions to revise, with specific fix suggestions

- [ ] **Step 16: Revise flagged MCQs**
  - Apply subagent suggestions
  - Re-run validation on revised questions

---

## Deliverable 4: Skills Learning Checklist (`docs/skills-learning-checklist.md`)

### Purpose
ADD-maximalist, OCD-friendly modular checklist. Every item is small, tickable, and has a time estimate. Two tracks: Google Sheets pipeline (legacy) and EF Core pipeline (current).

### Track A: Google Sheets Pipeline (Legacy Understanding)

- [ ] **Step 17: Create Track A checklist — Google OAuth 2.0 setup**
  - `[ ]` Read Google.Apis.Auth NuGet package docs (15 min)
  - `[ ]` Understand OAuth 2.0 installed app flow (20 min)
  - `[ ]` Trace `GoogleCredentialService.cs` auth flow (15 min)
  - `[ ]` Trace `GoogleSheetsService.cs` sheet creation (20 min)
  - `[ ]` Understand `SpreadsheetBootstrapper.cs` initialization (15 min)
  - `[ ]` Understand `SheetMetadataService.cs` metadata retrieval (15 min)
  - `[ ]` Understand `SheetRowService.cs` row operations (15 min)
  - `[ ]` Understand `SheetFormattingService.cs` cell formatting (10 min)
  - `[ ]` Trace full sync flow: `SyncAllCommand` → `ScrobbleSyncOrchestrator` → `GoogleSheetsService` (30 min)
  - `[ ]` Understand CSV export path and desktop output (10 min)
  - `[ ]` Understand why Google Sheets is being deprecated in favor of PostgreSQL (10 min)

### Track B: EF Core 10 Pipeline (Current)

- [ ] **Step 18: Create Track B checklist — EF Core fundamentals**
  - `[ ]` Read EF Core 10 overview docs (30 min)
  - `[ ]` Understand `DbContext` lifecycle and `IDbContextFactory` (20 min)
  - `[ ]` Read `ScriptsDbContext.cs` — all DbSets, `OnModelCreating`, `NoTracking` (20 min)
  - `[ ]` Read `ScriptsDbContextFactory.cs` — design-time factory (10 min)
  - `[ ]` Understand `ApplyConfigurationsFromAssembly` pattern (15 min)
  - `[ ]` Read one entity configuration (e.g., `AlbumConfiguration.cs`) (10 min)
  - `[ ]` Understand JSONB column mapping with `HasColumnType("jsonb")` (15 min)
  - `[ ]` Understand `ComplexType` for nested objects (15 min)
  - `[ ]` Read `Variables.cs` — connection string constant (5 min)

- [ ] **Step 19: Create Track B checklist — Entity design**
  - `[ ]` Read `Artist.cs` entity (5 min)
  - `[ ]` Read `Album.cs` entity + FK to Artist (5 min)
  - `[ ]` Read `Track.cs` entity + FKs to Album and Artist (5 min)
  - `[ ]` Read `Scrobble.cs` entity + FK to Track (5 min)
  - `[ ]` Read `Video.cs` entity (5 min)
  - `[ ]` Read `ExecutionLog.cs` entity (5 min)
  - `[ ]` Read `FailedTask.cs` entity (5 min)
  - `[ ]` Read `FiberyEntity.cs` entity (5 min)
  - `[ ]` Read `SourceRecord.cs` entity (5 min)
  - `[ ]` Read `ReleaseProgress.cs` entity (5 min)
  - `[ ]` Understand 3NF normalization in this schema (15 min)
  - `[ ]` Understand `TIMESTAMPTZ` vs `TIMESTAMP` choice (10 min)

- [ ] **Step 20: Create Track B checklist — Migrations**
  - `[ ]` Read `InitialCreate.cs` migration (10 min)
  - `[ ]` Read `InitialEntities.cs` migration (10 min)
  - `[ ]` Read `AddSourceRecord.cs` migration (5 min)
  - `[ ]` Read `AddDomainEntities.cs` migration (10 min)
  - `[ ]` Read `FixJsonDocumentModel.cs` migration (10 min)
  - `[ ]` Read `AddReleaseProgress.cs` migration (5 min)
  - `[ ]` Understand `ScriptsDbContextModelSnapshot.cs` (15 min)
  - `[ ]` Understand `dotnet ef migrations add` workflow (10 min)
  - `[ ]` Understand `dotnet ef database update` workflow (10 min)

- [ ] **Step 21: Create Track B checklist — Repositories**
  - `[ ]` Read `IAlbumRepository.cs` interface (5 min)
  - `[ ]` Read `AlbumRepository.cs` implementation (10 min)
  - `[ ]` Read `RepositoryResilienceFactory.cs` — Polly retry wrapping (15 min)
  - `[ ]` Read `RepositoryRegistration.cs` — DI registration (5 min)
  - `[ ]` Understand thin repository pattern vs raw DbContext usage (10 min)
  - `[ ]` Read `ScrobbleRepository.cs` — query patterns (10 min)
  - `[ ]` Read `VideoRepository.cs` — JSONB queries (10 min)

- [ ] **Step 22: Create Track B checklist — Testing**
  - `[ ]` Read `DatabaseTestFixture.cs` — Testcontainers setup (15 min)
  - `[ ]` Read `DatabaseTestBase.cs` — base class for DB tests (10 min)
  - `[ ]` Read one entity test (e.g., `AlbumEntityTests.cs`) (10 min)
  - `[ ]` Read one configuration test (e.g., `AlbumConfigurationTests.cs`) (10 min)
  - `[ ]` Read `Ef11ForbiddenPatternsTests.cs` — guard tests (15 min)
  - `[ ]` Read `Ef10ReplacementPatternTests.cs` — replacement validation (10 min)
  - `[ ]` Read `EditorConfigEf10RulesTests.cs` — editor config enforcement (10 min)
  - `[ ]` Read `CompiledModelTests.cs` — compiled model verification (10 min)
  - `[ ]` Read `BuildVerificationTests.cs` — sign-off tests (10 min)
  - `[ ]` Read `ConnectionStringTests.cs` — env var validation (5 min)
  - `[ ]` Read `DockerEnvironmentTests.cs` — preflight checks (5 min)
  - `[ ]` Read `MigrationTests.cs` — migration state verification (10 min)

- [ ] **Step 23: Create Track B checklist — Resilience & Logging**
  - `[ ]` Read `Resilience.cs` — Polly policies (15 min)
  - `[ ]` Read `Log.cs` — Serilog configuration (10 min)
  - `[ ]` Understand `Ben.Demystifier` stack traces (5 min)
  - `[ ]` Read `Serilog` + `CompactJsonFormatter` log output format (10 min)
  - `[ ]` Understand log directory: `~/.cache/logs/scripts/` (5 min)

- [ ] **Step 24: Create Track B checklist — CLI Architecture**
  - `[ ]` Read `Program.cs` — Spectre.Console `CommandApp` setup (10 min)
  - `[ ]` Read `SpectreTypeRegistrar.cs` — DI integration (10 min)
  - `[ ]` Read `SyncAllCommand.cs` — command routing (10 min)
  - `[ ]` Read `SyncLastFmCommand.cs` — single-service sync (10 min)
  - `[ ]` Read `MusicSearchCommand.cs` — search + cache (10 min)
  - `[ ]` Understand `BaseAsyncCommand.cs` — shared command logic (10 min)

### Track C: Python Toolkit

- [ ] **Step 25: Create Track C checklist — Python modern tooling**
  - `[ ]` Read `pyproject.toml` — `uv` config, `ty` strict rules (10 min)
  - `[ ]` Read `toolkit/__init__.py` — module exports (5 min)
  - `[ ]` Read `toolkit/audio.py` — FFmpeg wrapper (10 min)
  - `[ ]` Read `toolkit/video.py` — video processing (10 min)
  - `[ ]` Read `toolkit/filesystem.py` — file operations (10 min)
  - `[ ]` Read `toolkit/lastfm.py` — Last.fm API client (10 min)
  - `[ ]` Read `toolkit/cli.py` — argparse CLI (10 min)
  - `[ ]` Read `toolkit/exceptions.py` — custom exceptions (5 min)
  - `[ ]` Read `toolkit/types.py` — type definitions (5 min)
  - `[ ]` Understand `uv` vs `pip` dependency management (10 min)

### Track D: DevOps & Infrastructure

- [ ] **Step 26: Create Track D checklist — Docker + PostgreSQL**
  - `[ ]` Read `docker-compose.yml` — PostgreSQL 18 service (5 min)
  - `[ ]` Understand `PGCONNSTR` environment variable (5 min)
  - `[ ]` Understand `.env` file secrets management (5 min)
  - `[ ]` Understand Docker volumes for persistence (5 min)
  - `[ ]` Understand `pg_isready` health check (5 min)
  - `[ ]` Read `deploy_oci_postgres.ps1` — OCI deployment (10 min)

- [ ] **Step 27: Create Track D checklist — Git & CI**
  - `[ ]` Understand `git worktree` usage in this repo (10 min)
  - `[ ]` Read `.gitignore` — tracked vs ignored patterns (5 min)
  - `[ ]` Read `.gitattributes` — line ending enforcement (5 min)
  - `[ ]` Understand `gitleaks detect --no-git` security audit (5 min)
  - `[ ]` Understand branch topology (main + feature branches + merge commits) (10 min)

### Track E: AI-Assisted Development

- [ ] **Step 28: Create Track E checklist — Plan-driven development**
  - `[ ]` Read `AGENTS.md` — agent conventions (10 min)
  - `[ ]` Read `AI/plans/INDEX.md` — plan navigation (5 min)
  - `[ ]` Read `AI/plans/CURRENT_STATUS.md` — current state (5 min)
  - `[ ]` Understand 4-tier migration plan structure (10 min)
  - `[ ]` Understand Absolute Zero Presumption Ruleset (10 min)
  - `[ ]` Read one tier plan file (e.g., `tier-1-ef-migration/01-entities.md`) (10 min)

---

## Task Execution Order

| Task | Deliverable | Depends On | Estimated Effort |
|------|------------|------------|-----------------|
| Steps 1–6 | Employer README | None | 45 min |
| Steps 7–11 | MCQ Battery | None | 90 min |
| Steps 12–13 | Long-form + Debug | Steps 7–11 (for topic coverage) | 60 min |
| Steps 14–16 | MCQ Quality Enforcement | Steps 7–13 | 30 min (subagent) |
| Steps 17–28 | Learning Checklist | None | 45 min |
| Final | Review all deliverables | All above | 20 min |

**Total estimated effort: ~4.5 hours**

---

## File Structure

```
docs/
├── employer-readme.md              ← Deliverable 1
├── interview-question-bank.md      ← Deliverable 2 (MCQs + long-form + debug)
├── skills-learning-checklist.md    ← Deliverable 4 (5 tracks, ~80 tickable items)
└── superpowers/
    └── plans/
        └── 2026-05-30-sdet-interview-deliverables.md  ← This plan
```

---

## Self-Review Checklist

1. **Spec coverage:** All 4 user requests covered (evolution map, README, question bank, learning checklist)
2. **Placeholder scan:** No TBD/TODO — all steps have concrete content
3. **Type consistency:** File paths match actual repo structure
4. **MCQ quality:** Enforced via dedicated subagent pass (Steps 14–16)
5. **ADD-friendly:** Learning checklist has ~80 items, each 5–30 min, independently tickable
6. **SDET calibration:** All questions target 0–2 year experience level
