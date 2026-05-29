# Scripts: A Personal Automation Monorepo

A multi-language automation platform that ingests data from six external APIs (Last.fm, YouTube, Discogs, MusicBrainz, Azure Translation, Azure Document Intelligence), persists it to a local PostgreSQL 18 database via EF Core 10, and backs it up to OCI Object Storage. The project has evolved over 18 months and 172 commits from a CSV-and-spreadsheet pipeline into a modular, test-driven .NET 10 solution with 170 passing tests, automated guard rails, and infrastructure-as-code. It serves as both a practical tool and a proving ground for modern software engineering practices: strict separation of concerns, dependency injection, resilience patterns, and plan-driven development with AI-assisted orchestration.

---

## Architecture

```
                    +--------------------------+
                    |         CLI (entry)      |
                    |  Spectre.Console + DI    |
                    |  Scripts.CLI.csproj      |
                    +------+-----+-----+------+
                           |     |     |
              +------------+     |     +------------+
              v                  v                  v
     +------------------+  +------------+  +------------------+
     |  Orchestrators   |  |  Reader    |  | Services         |
     |  Last.fm, YT,    |  |  Playwright|  | Language + Music |
     |  Sheets (legacy) |  |  AngleSharp|  | Translation,     |
     +--------+---------+  |  PdfPig   |  | Discogs, MBrainz |
              |             +-----+-----+  +--------+---------+
              v                   |                 |
     +--------+-------------------+-----------------+
     |                   Data                        |
     |   EF Core 10, Npgsql 10, CsvHelper            |
     |   10 entities, 6 migrations, 5 repositories   |
     +--------------------+--------------------------+
                          |
                          v
     +--------------------+--------------------------+
     |                   Core                        |
     |   Serilog, Polly, Google.Apis.Auth             |
     |   RepositoryResilienceFactory, Ben.Demystifier |
     +-----------------------------------------------+

     Dependency flow: CLI -> Orchestrators -> Data -> Core
                    CLI -> Reader        -> Core
                    CLI -> Services      -> Core
```

All dependencies point inward. Core has zero references to sibling or parent projects.

---

## Component Breakdown

### C# (.NET 10) -- `csharp/`

The primary codebase. Contains six projects organized by domain responsibility:

| Project | Path | Purpose |
|---------|------|---------|
| Core | `csharp/src/Core/Scripts.Core.csproj` | Cross-cutting concerns: logging, resilience, authentication |
| Data | `csharp/src/Data/Scripts.Data.csproj` | EF Core DbContext, entities, migrations, repository layer |
| Services.Language | `csharp/src/Services/Language/Scripts.Services.Language.csproj` | Azure Translation, Lingua language detection |
| Services.Music | `csharp/src/Services/Music/Scripts.Services.Music.csproj` | MetaBrainz, Discogs API clients |
| Orchestrators | `csharp/src/Orchestrators/Scripts.Orchestrators.csproj` | Last.fm, YouTube, Google Sheets (legacy) |
| Reader | `csharp/src/Reader/Scripts.Reader.csproj` | Playwright, AngleSharp, PdfPig, OCR |
| CLI | `csharp/src/CLI/Scripts.CLI.csproj` | Composition root, Spectre.Console commands, DI wiring |

Solution file: `csharp/Scripts.slnx`
Global build settings: `csharp/Directory.Build.props` (TargetFramework, LangVersion, GlobalUsings, CPM)

### Python 3.12+ -- `python/`

Utility toolkit for audio/video processing, filesystem operations, and a Last.fm API client. All scripts managed with `uv` and type-checked with `ty` (strict mode). No `pip` or `venv` -- `uv` handles dependency resolution and virtual environment management in a single tool.

### PowerShell 7+ -- `powershell/`

Azure environment provisioning scripts, data transformation utilities, and profile customization. All scripts use `-ErrorAction Stop` and absolute paths. No silent suppression.

### AI-Assisted Development -- `AI/`

Plan-driven development artifacts generated and maintained by Kilo/Claude agents:

- `AI/plans/INDEX.md` -- Master plan index with status tracking
- `AI/plans/CURRENT_STATUS.md` -- Live test counts, blockers, next actions
- `AI/plans/tier-1-ef-migration/` through `tier-4-hardening/` -- 4-tier migration plan
- `AGENTS.md` -- Single source of truth for agent context, conventions, and navigation

---

## Key Technical Decisions

### EF Core 10 over Dapper or raw SQL

EF Core 10 provides compile-time model validation, migration management, LINQ-to-SQL translation, and first-class JSONB support via Npgsql. The trade-off is heavier dependency weight, but the payoff is a schema-as-code workflow where migrations in `csharp/src/Data/Migrations/` serve as auditable, version-controlled schema history. Guard tests (`Ef11ForbiddenPatternsTests.cs`) prevent accidental introduction of EF11-only APIs that would break the LTS target.

### TUnit over xUnit/NUnit

TUnit is a modern test framework built for .NET 10 with source-generated test discovery, eliminating runtime reflection overhead. Combined with FluentAssertions, it produces expressive, readable test assertions. The framework's design aligns with the project's commitment to current tooling.

### Spectre.Console over raw Console.WriteLine

Spectre.Console provides a DI-compatible CLI framework with rich output formatting, progress bars, and interactive prompts. It replaces ad-hoc argument parsing with a declarative command model (`Spectre.Console.Cli`), making the CLI composable and testable.

### uv over pip/venv/poetry

`uv` is a single-binary Python package manager and virtual environment tool written in Rust. It replaces the fragmented `pip` + `venv` + `poetry` stack with deterministic dependency resolution and lockfile support, reducing setup from three tools to one.

### Local PostgreSQL 18 over cloud-managed databases

Running PostgreSQL 18 via Docker Compose (`docker-compose.yml`) gives full control over versioning, configuration, and backup strategy. The database is backed up weekly to OCI Object Storage via `pg_dump` with daily WAL archiving. This eliminates cloud costs during development while maintaining a production-grade backup/restore workflow.

---

## Skills Demonstrated

| Skill | Evidence | File/Location |
|-------|----------|---------------|
| Database design | 10 EF Core entities in 3NF with JSONB columns, 6 migrations | `csharp/src/Data/Entities/*.cs`, `csharp/src/Data/Migrations/` |
| Repository pattern | 5 typed repositories with async CRUD operations | `csharp/src/Data/Repositories/` |
| Dependency injection | DI container wired in CLI composition root | `csharp/src/CLI/Program.cs` |
| Resilience engineering | Polly retry policies via factory pattern | `csharp/src/Core/Resilience/RepositoryResilienceFactory.cs` |
| Structured logging | Serilog CompactJsonFormatter, demystified stack traces | `csharp/src/Core/Logging/` |
| API integration | 6 external API clients (Last.fm, YouTube, Discogs, MusicBrainz, Azure Translation, Azure Doc Intelligence) | `csharp/src/Orchestrators/`, `csharp/src/Services/` |
| Test-driven development | 170 tests, 100% pass rate, TUnit + FluentAssertions | `csharp/tests/Scripts.Tests/` |
| Build guard rails | Regex-based guard tests preventing EF11 API usage | `csharp/tests/Scripts.Tests/Guards/Ef11ForbiddenPatternsTests.cs` |
| Integration testing | Testcontainers for PostgreSQL in CI/test environments | `csharp/tests/Scripts.Tests/` |
| Web scraping | Playwright + AngleSharp + PdfPig for document parsing | `csharp/src/Reader/` |
| Multi-language development | C# 13, Python 3.12+, PowerShell 7+ in one repo | Root structure |
| Python type safety | Strict typing with `ty` across all Python modules | `python/` |
| Infrastructure-as-code | Docker Compose for PostgreSQL 18, OCI backup pipeline | `docker-compose.yml` |
| Security hygiene | .env secrets, gitleaks pre-push audit, no hardcoded credentials | `AGENTS.md`, `.env` |
| Software evolution | 5-phase architecture migration over 18 months, 172 commits | Git history |
| AI-assisted development | Plan-driven TDD with Kilo/Claude agents, 4-tier migration plan | `AI/plans/INDEX.md` |
| CLI design | Spectre.Console with DI, help text, interactive prompts | `csharp/src/CLI/` |
| Schema versioning | EF Core migrations as auditable schema history | `csharp/src/Data/Migrations/` |
| Build configuration | Central Package Management, TreatWarningsAsErrors, GlobalUsings | `csharp/Directory.Build.props`, `csharp/Directory.Packages.props` |

---

## Evolution Timeline

### Phase 1: Script Collection (Month 1-3)
Standalone PowerShell and Python scripts for individual tasks. No shared infrastructure. Data lived in local CSV files.

### Phase 2: Google Sheets Integration (Month 4-6)
Introduced C# with .NET 8. Pipeline: API -> .NET Object -> JSON -> CSV -> Google Sheets. First OAuth 2.0 integration via `Google.Apis.Auth`. Serilog logging added.

### Phase 3: Database Foundation (Month 7-10)
Migrated to .NET 10. Introduced PostgreSQL 18 via Docker Compose. EF Core 10 with initial entity model. TUnit test framework adopted. 100+ tests written.

### Phase 4: Architecture Modularization (Month 11-14)
Split monolithic project into 6 domain-specific projects. Implemented strict inward dependency flow. Central Package Management via `Directory.Packages.props`. Spectre.Console CLI. Polly resilience layer. Guard tests introduced.

### Phase 5: Hardening and AI Integration (Month 15-18)
OCI Object Storage backup pipeline. Testcontainers for integration tests. AI-assisted plan-driven development with Kilo/Claude agents. 4-tier migration plan. 170 tests at 100% pass rate. Python toolkit expanded with strict typing.

---

## Getting Started

### Prerequisites

- .NET 10 SDK (`dotnet --version` should return `10.0.x`)
- Python 3.12+ with `uv` (`uv --version`)
- PowerShell 7+ (`pwsh --version`)
- Docker Desktop (running)

### Setup

```powershell
# 1. Start PostgreSQL 18
docker compose up -d

# 2. Load environment variables
Get-Content .env | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.+)$') {
        [System.Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
    }
}

# 3. Restore and build
dotnet restore csharp/Scripts.slnx
dotnet build   csharp/Scripts.slnx

# 4. Apply database migrations
dotnet ef database update `
    --project csharp/src/Data/Scripts.Data.csproj `
    --startup-project csharp/src/CLI/Scripts.CLI.csproj
```

### Run Tests

```powershell
dotnet test csharp/Scripts.slnx
```

Expected: 170 tests, 0 failures.

### Run CLI

```powershell
dotnet run --project csharp/src/CLI/Scripts.CLI.csproj -- --help
```

---

## Test Suite Highlights

### Guard Tests -- `Ef11ForbiddenPatternsTests.cs`

A set of regex-based build guard tests that scan all `.cs` files in the solution for patterns that are only available in EF Core 11 (e.g., `MaxByAsync`, `MinByAsync`, `EF.Functions.JsonPathExists`). If any EF11-only API is detected, the build fails. This enforces the EF Core 10 LTS target without relying on manual code review.

```csharp
// Example: If this pattern appears anywhere in src/, the test fails
// EF11 (forbidden): context.Scrobbles.MaxByAsync(s => s.ScrobbledAt)
// EF10 (required):  context.Scrobbles.OrderByDescending(s => s.ScrobbledAt).FirstOrDefaultAsync()
```

### Testcontainers

Integration tests spin up a real PostgreSQL 18 container, apply migrations, and run queries against a live database. This catches provider-specific SQL translation issues that in-memory providers miss.

### Reflection-Based Assertions

Some tests use reflection to verify architectural invariants at runtime -- for example, ensuring that no repository class directly references an API client, or that all entities have required configuration in the DbContext.

### Test Metrics

| Metric | Value |
|--------|-------|
| Total tests | 170 |
| Pass rate | 100% |
| Framework | TUnit |
| Assertion library | FluentAssertions |
| Integration tests | Testcontainers (PostgreSQL 18) |
| Guard tests | Regex-based EF11 API detection |

---

## Security Practices

- All secrets stored in `.env` (never committed)
- Pre-push audit via `gitleaks detect --no-git`
- `TreatWarningsAsErrors=true` in `Directory.Build.props`
- No hardcoded connection strings, API keys, or tokens in source
- Connection string loaded from `$env:PGCONNSTR` at runtime
- Logs written to `%USERPROFILE%\.cache\logs\scripts\` with no secret leakage

---

## Repository Metadata

| Field | Value |
|-------|-------|
| Commits | 172 |
| Duration | 18 months |
| Languages | C# (.NET 10), Python 3.12+, PowerShell 7+ |
| Database | PostgreSQL 18 |
| Test count | 170 |
| Pass rate | 100% |
| External APIs | 6 |
| EF Core entities | 10 |
| EF Core migrations | 6 |
| Repositories | 5 |
| Domain projects | 6 + 1 CLI |
