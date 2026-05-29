# Skills Learning Checklist

> Modular, tickable, ADHD-friendly learning path for the Scripts monorepo.
> Each item is 5–30 minutes. Do them in any order within a track.

---

## Track A: Google Sheets Pipeline (Legacy)

- [ ] Read `GoogleSheetsService.cs` — identify the main public methods and their return types — read `csharp/src/Services/Sync/GoogleSheetsService.cs` (15 min)
- [ ] Trace how `GoogleSheetsContext` holds the Sheets API service object and spreadsheet ID — read `csharp/src/Services/Sync/GoogleSheetsContext.cs` (10 min)
- [ ] Understand `SpreadsheetBootstrapper` — find where it creates or validates the target spreadsheet on first run — read `csharp/src/Services/Sync/SpreadsheetBootstrapper.cs` (15 min)
- [ ] Identify every method in `SheetMetadataService` that reads or writes tab names and column headers — read `csharp/src/Services/Sync/SheetMetadataService.cs` (10 min)
- [ ] Trace `SheetRowService` — find the method that appends rows and understand the data shape it expects — read `csharp/src/Services/Sync/SheetRowService.cs` (15 min)
- [ ] Read `SheetFormattingService` — identify which formatting operations (colors, bold, freeze rows) are applied — read `csharp/src/Services/Sync/SheetFormattingService.cs` (10 min)
- [ ] Understand `GoogleAuth` — trace the OAuth 2.0 flow from credential file to authenticated `SheetsService` — read `csharp/src/Core/Auth/GoogleAuth.cs` (15 min)
- [ ] Compare `GoogleAuth` token refresh logic with the EF Core pipeline's credential handling — read `csharp/src/Core/Auth/GoogleAuth.cs` vs `csharp/src/Data/ScriptsDbContext.cs` (10 min)
- [ ] Read `SyncAllCommand` — identify which sheets/tabs it syncs and in what order — read `csharp/src/CLI/Sync/SyncAllCommand.cs` (15 min)
- [ ] Trace the full sync path: CLI command → SheetsService → Google Sheets API — map call chain across `SyncAllCommand.cs`, `GoogleSheetsService.cs`, `GoogleSheetsContext.cs` (20 min)
- [ ] Explain why this pipeline is considered "legacy" — identify what the EF Core pipeline replaces and what is retained — read `AGENTS.md` section 1 and `csharp/src/CLI/Sync/SyncAllCommand.cs` (10 min)

**Track A total:** 11 items

---

## Track B: EF Core 10 Pipeline (Current)

### B1: EF Fundamentals (9 items)

- [ ] Read `ScriptsDbContext.cs` — list every `DbSet<T>` property and its entity type — read `csharp/src/Data/ScriptsDbContext.cs` (15 min)
- [ ] Identify the `OnModelCreating` override — find all entity configurations registered via `ApplyConfigurationsFromAssembly` — read `csharp/src/Data/ScriptsDbContext.cs` (10 min)
- [ ] Read `ScriptsDbContextFactory.cs` — understand how the connection string is resolved at design time (for EF CLI tools) — read `csharp/src/Data/ScriptsDbContextFactory.cs` (10 min)
- [ ] Trace `Variables.cs` — find where `$env:PGCONNSTR` is read and how it flows into the DbContext — read `csharp/src/Core/Variables.cs` (10 min)
- [ ] Verify the Npgsql provider registration — find `UseNpgsql()` call and confirm EF Core 10 + Npgsql 10 versions in `Directory.Packages.props` (10 min)
- [ ] Identify all `IEntityTypeConfiguration<T>` classes in `csharp/src/Data/Configurations/` — list each file and which entity it configures (15 min)
- [ ] Read one entity configuration file end-to-end — understand table name, key, column types, JSONB mapping — read any file in `csharp/src/Data/Configurations/` (15 min)
- [ ] Compare `SaveChangesAsync` usage patterns — find all call sites and verify no loops (should use `ExecuteUpdate`/`ExecuteDelete`) — grep for `SaveChanges` across `csharp/src/Data/` (15 min)
- [ ] Verify global usings and file-scoped namespaces are used in all Data project files — spot-check 3 random files in `csharp/src/Data/` (5 min)

### B2: Entity Design (12 items)

- [ ] Read `Artist.cs` — identify properties, nullable annotations, and any JSONB `Metadata` column — read `csharp/src/Data/Entities/Artist.cs` (10 min)
- [ ] Read `Album.cs` — find FK to Artist, `ReleaseDate` type (`DateOnly`), and any navigation properties — read `csharp/src/Data/Entities/Album.cs` (10 min)
- [ ] Read `Track.cs` — find FKs to both Album and Artist, `DurationSeconds` nullability — read `csharp/src/Data/Entities/Track.cs` (10 min)
- [ ] Read `Scrobble.cs` — find FK to Track, `ScrobbledAt` type (`DateTimeOffset`), and `Platform` field — read `csharp/src/Data/Entities/Scrobble.cs` (10 min)
- [ ] Read `Video.cs` — identify all columns including JSONB `Metadata` and `SyncedAt` — read `csharp/src/Data/Entities/Video.cs` (10 min)
- [ ] Read `ExecutionLog.cs` — find `Timestamp`, `SessionId`, `Payload` (JSONB), and `ExitCode` — read `csharp/src/Data/Entities/ExecutionLog.cs` (10 min)
- [ ] Read `FailedTask.cs` — identify the UUID primary key, `TaskName`, `ErrorMessage`, `Timestamp` — read `csharp/src/Data/Entities/FailedTask.cs` (10 min)
- [ ] Read `FiberyEntity.cs` — find `FiberyId` (varchar 255), `EntityType`, and `RawData` (JSONB) — read `csharp/src/Data/Entities/FiberyEntity.cs` (10 min)
- [ ] Read `SourceRecord.cs` — identify `SourceId`, `EntityType`, and `RawData` (JSONB) — read `csharp/src/Data/Entities/SourceRecord.cs` (10 min)
- [ ] Read `ReleaseProgress.cs` — understand its purpose relative to the music domain tables — read `csharp/src/Data/Entities/ReleaseProgress.cs` (10 min)
- [ ] Compare all entity PK strategies — identify which use `int` vs `UUID` vs `long` and why — read all entity files in `csharp/src/Data/Entities/` (15 min)
- [ ] Verify every entity uses primary constructors and file-scoped namespaces — spot-check 3 entities (5 min)

### B3: Migrations (9 items)

- [ ] List all migration files in `csharp/src/Data/Migrations/` — count them and note their timestamps (5 min)
- [ ] Read the first migration — identify which tables it creates and what columns — read the earliest migration file in `csharp/src/Data/Migrations/` (15 min)
- [ ] Read the snapshot file (`ScriptsDbContextModelSnapshot.cs`) — find the current model shape at a glance — read `csharp/src/Data/Migrations/ScriptsDbContextModelSnapshot.cs` (15 min)
- [ ] Identify which migration adds JSONB columns — search migration files for `jsonb` (10 min)
- [ ] Identify which migration adds indexes — search migration files for `HasIndex` or `CreateIndex` (10 min)
- [ ] Trace a migration that adds a foreign key — find `AddForeignKey` and understand the relationship it creates (10 min)
- [ ] Verify the `dotnet ef` commands in AGENTS.md match the project structure — read `AGENTS.md` section 4 "EF Core Migrations" (5 min)
- [ ] Compare the migration snapshot with the actual entity files — confirm all entities are represented (15 min)
- [ ] Understand rollback — find `Down` methods in migrations and verify they reverse `Up` correctly — read 2 migration files (10 min)

### B4: Repositories (7 items)

- [ ] List all repository interface files in `csharp/src/Data/Repositories/` — identify each contract (5 min)
- [ ] Read `IRepository<T>` base interface — find CRUD method signatures — read the base interface file in `csharp/src/Data/Repositories/` (10 min)
- [ ] Read one concrete repository implementation — trace how it uses `ScriptsDbContext` — read any `*Repository.cs` file (15 min)
- [ ] Identify `RepositoryResilienceFactory` — understand how it wraps repository calls with Polly retry policies — read `csharp/src/Data/Repositories/RepositoryResilienceFactory.cs` (15 min)
- [ ] Compare repository patterns: find if any use `ExecuteUpdateAsync`/`ExecuteDeleteAsync` vs `SaveChangesAsync` loops — grep across repository files (10 min)
- [ ] Verify all repositories are registered in DI — find the service registration (likely in `Program.cs` or a DI extension) — read `csharp/src/Program.cs` (10 min)
- [ ] Explain the dependency flow: CLI → Repository → DbContext → PostgreSQL — trace one full call path (15 min)

### B5: Testing (12 items)

- [ ] Read `DatabaseTestFixture.cs` — understand how it spins up a PostgreSQL container for tests — read `csharp/tests/Scripts.Tests/DatabaseTestFixture.cs` (15 min)
- [ ] Read `DatabaseTestBase.cs` — find how each test gets a clean database/schema — read `csharp/tests/Scripts.Tests/DatabaseTestBase.cs` (15 min)
- [ ] Identify the test framework — verify TUnit + FluentAssertions are used (not xUnit/NUnit) — read `csharp/tests/Scripts.Tests/Scripts.Tests.csproj` (5 min)
- [ ] Read entity tests for `Artist` — find CRUD operation coverage — read `csharp/tests/Scripts.Tests/Entities/ArtistTests.cs` (15 min)
- [ ] Read entity tests for `Scrobble` — find timestamp and platform filter tests — read `csharp/tests/Scripts.Tests/Entities/ScrobbleTests.cs` (15 min)
- [ ] Read configuration tests — verify entity configurations produce correct SQL schemas — read config test files in `csharp/tests/Scripts.Tests/` (15 min)
- [ ] Read `Ef11ForbiddenPatternsTests.cs` — understand how it guards against EF11-only API usage — read `csharp/tests/Scripts.Tests/Guards/Ef11ForbiddenPatternsTests.cs` (10 min)
- [ ] Read compiled model tests — verify the EF Core compiled model is consistent — read compiled model test files in `csharp/tests/Scripts.Tests/` (10 min)
- [ ] Read sign-off tests — understand what "sign-off" means for each tier — read sign-off test files in `csharp/tests/Scripts.Tests/` (10 min)
- [ ] Run the full test suite — verify 155 passing, 0 failing — run `dotnet test csharp/Scripts.slnx` (10 min)
- [ ] Identify test count per category — count entity tests, config tests, guard tests, sign-off tests (10 min)
- [ ] Verify Testcontainers setup — find the Docker image used and port mapping — read the fixture setup code (10 min)

### B6: Resilience & Logging (5 items)

- [ ] Read `Resilience.cs` — identify Polly policies (retry, circuit breaker, timeout) — read `csharp/src/Core/Resilience.cs` (15 min)
- [ ] Read `Log.cs` — understand the Serilog static logger setup — read `csharp/src/Core/Log.cs` (10 min)
- [ ] Verify log output format — find `CompactJsonFormatter` config and confirm log directory path — read `csharp/src/Core/Log.cs` and `AGENTS.md` section 8 (10 min)
- [ ] Trace how `Ben.Demystifier` is integrated — find the enricher registration — read `csharp/src/Core/Log.cs` (5 min)
- [ ] Compare resilience patterns in Core vs Repository layer — read `csharp/src/Core/Resilience.cs` vs `RepositoryResilienceFactory.cs` (15 min)

### B7: CLI Architecture (6 items)

- [ ] Read `Program.cs` — identify the composition root: DI registration, Spectre.Console setup — read `csharp/src/Program.cs` (15 min)
- [ ] Read `SpectreTypeRegistrar.cs` — understand how it bridges `IServiceCollection` with Spectre's `ITypeRegistrar` — read `csharp/src/CLI/SpectreTypeRegistrar.cs` (10 min)
- [ ] List all command files in `csharp/src/CLI/` — identify each CLI command and its purpose (10 min)
- [ ] Trace one command end-to-end: CLI args → service call → database → output — pick any command file and follow the chain (20 min)
- [ ] Verify `--help` output — run `dotnet run --project csharp/src/CLI/Scripts.CLI.csproj -- --help` and read the output (5 min)
- [ ] Identify how commands handle errors — find try/catch or Spectre error handling patterns in 2 command files (10 min)

**Track B total:** 60 items (B1: 9, B2: 12, B3: 9, B4: 7, B5: 12, B6: 5, B7: 6)

---

## Track C: Python Toolkit

- [ ] Read `pyproject.toml` — identify dependencies, Python version, and project metadata — read `python/pyproject.toml` (5 min)
- [ ] List all modules in `python/toolkit/` — identify each script's purpose from its filename (5 min)
- [ ] Read the main entry point module — understand how the toolkit is invoked — read the primary module in `python/toolkit/` (10 min)
- [ ] Trace one toolkit script end-to-end — follow input → processing → output — read any module in `python/toolkit/` (15 min)
- [ ] Identify how `uv` manages dependencies — find `uv.lock` or `pyproject.toml` dependency section — read `python/pyproject.toml` (5 min)
- [ ] Verify the Python version constraint — confirm Python 3.12+ requirement — read `python/pyproject.toml` (5 min)
- [ ] Find any HTTP/API calls in toolkit modules — grep for `requests`, `httpx`, or `urllib` across `python/toolkit/` (10 min)
- [ ] Identify how errors are handled in toolkit scripts — find try/except patterns in 2 modules (10 min)
- [ ] Compare Python toolkit's purpose vs C# CLI — identify overlapping functionality — read `python/toolkit/` and `csharp/src/CLI/` (15 min)
- [ ] Run one toolkit script with `uv run` — verify it executes without errors — run `cd python && uv run <script>.py` (10 min)

**Track C total:** 10 items

---

## Track D: DevOps & Infrastructure

- [ ] Read `docker-compose.yml` — identify the PostgreSQL 18 service, ports, volumes, and env vars — read `docker-compose.yml` (10 min)
- [ ] Verify the PostgreSQL version — confirm it targets PostgreSQL 18 image — read `docker-compose.yml` (5 min)
- [ ] Read `.env` — identify all environment variables defined (do NOT log secrets) — read `.env` (5 min)
- [ ] Explain the `.env` loading pattern in PowerShell — find the `Get-Content .env` snippet in `AGENTS.md` section 3 (5 min)
- [ ] Read `.gitignore` — identify what is excluded (state/, .env, bin/, obj/, logs) — read `.gitignore` (10 min)
- [ ] Read `.gitattributes` — find line-ending rules and binary file markers — read `.gitattributes` (5 min)
- [ ] Read `deploy_oci_postgres.ps1` — understand the OCI backup/WAL deployment script — read `deploy_oci_postgres.ps1` (15 min)
- [ ] Trace the database persistence path — find `state/postgres/18/docker` references and understand Docker volume mapping — read `docker-compose.yml` (10 min)
- [ ] Verify `state/` is gitignored — confirm no database files are tracked — check `.gitignore` for `state/` (5 min)
- [ ] Identify the backup strategy — find references to `pg_dump` and WAL archival in scripts or docs — grep for `pg_dump` or `WAL` across repo (10 min)
- [ ] Run `docker compose up -d` — verify PostgreSQL starts without errors — run the command and check `docker ps` (5 min)

**Track D total:** 11 items

---

## Track E: AI-Assisted Development

- [ ] Read `AGENTS.md` — understand the repo conventions, architecture, and agent ruleset — read `AGENTS.md` (20 min)
- [ ] Read `AI/plans/INDEX.md` — find the current plan status and CPM graph — read `AI/plans/INDEX.md` (10 min)
- [ ] Read `AI/plans/CURRENT_STATUS.md` — identify test counts, blockers, and next action — read `AI/plans/CURRENT_STATUS.md` (10 min)
- [ ] Read one tier plan file — understand the task breakdown format and sign-off criteria — read any file in `AI/plans/tier-1-ef-migration/` (15 min)
- [ ] Identify the "Absolute Zero Presumption Ruleset" — find all 10 rules in `AGENTS.md` section 9 (10 min)
- [ ] Compare tier plan structure with `CURRENT_STATUS.md` — verify they are in sync — read `AI/plans/INDEX.md` and `AI/plans/CURRENT_STATUS.md` (10 min)

**Track E total:** 6 items

---

## Summary

| Track | Items | Est. Time |
|-------|------:|----------:|
| A: Google Sheets Pipeline | 11 | 2h 25m |
| B1: EF Fundamentals | 9 | 1h 45m |
| B2: Entity Design | 12 | 1h 50m |
| B3: Migrations | 9 | 1h 35m |
| B4: Repositories | 7 | 1h 20m |
| B5: Testing | 12 | 2h 10m |
| B6: Resilience & Logging | 5 | 0h 55m |
| B7: CLI Architecture | 6 | 1h 10m |
| C: Python Toolkit | 10 | 1h 30m |
| D: DevOps & Infrastructure | 11 | 1h 40m |
| E: AI-Assisted Development | 6 | 1h 15m |

**Total items:** 98
**Total estimated time:** 17 hours 0 minutes
**Completion tracking:** ___/98 items checked
