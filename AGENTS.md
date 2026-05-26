# Scripts Repository — Agent Init Guide

> **This is the single source of truth for repo context, conventions, and navigation.**
> Read this file first. Then open `AI/plans/INDEX.md` to find your place in the plan.

---

## 1. Project Overview

Personal repository for automation scripts, utilities, and lightweight applications.
Multi-lingual: **C# (.NET 10)**, **Python (uv)**, **PowerShell**.
Local PostgreSQL 18 database managed via Docker Compose.

### Architecture (Old → New)

```
Old Pipeline: API → .NET Object → JSON → CSV → Google Sheets
New Pipeline: API → .NET 10 Service → Local PostgreSQL 18 ($PGCONNSTR) → OCI Object Storage (weekly pg_dump + daily WAL backup)
```

Google Sheets is **retained for backward compatibility** during the migration.
It will be deprecated in a future phase after EF Core is fully operational and validated.

---

## 2. Key Technologies

| Layer          | Technology                                          |
| -------------- | --------------------------------------------------- |
| Shell          | PowerShell Core / Windows PowerShell                |
| C#             | .NET 10 SDK, EF Core 10, Npgsql 10                  |
| Python         | Python 3.12+, managed by `uv`                       |
| Database       | PostgreSQL 18 (local Docker) + OCI Object Storage (backup/WAL) |
| Infrastructure | Docker Compose                                      |
| Test           | TUnit + FluentAssertions           |
| CLI            | Spectre.Console + Spectre.Console.Cli               |
| Logging        | Serilog (CompactJsonFormatter → `~/.cache/logs/scripts/`) |
| Auth           | Google OAuth 2.0 (`Google.Apis.Auth`)               |

---

## 3. Environment Setup

### Prerequisites

- Docker Desktop running
- .NET 10 SDK (`dotnet --version` → `10.0.x`)
- PowerShell 7+ (`pwsh --version`)

### Database (PostgreSQL 18 — Docker Compose)
```powershell
# Start database
docker compose up -d

# Stop database
docker compose down

# Check connection
$env:PGCONNSTR   # Must be set — see .env
```

**Connection string** is set via `$env:PGCONNSTR` (defined in `.env`).
**Never hardcode, echo, or log connection strings.**

### Environment Variables

All secrets live in `.env`. Load before EF Core commands:

```powershell
# Load .env into current shell (PowerShell)
Get-Content .env | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.+)$') {
        [System.Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
    }
}
```

---

## 4. Building & Running

### C# (primary language for this plan)

```powershell
# From repo root
dotnet restore csharp/Scripts.slnx
dotnet build   csharp/Scripts.slnx
dotnet test    csharp/Scripts.slnx

# Run CLI (after modularization)
dotnet run --project csharp/src/CLI/Scripts.CLI.csproj -- --help
```

### EF Core Migrations

```powershell
# Generate migration (run from csharp/ dir)
dotnet ef migrations add <MigrationName> --project src/Data/Scripts.Data.csproj --startup-project src/CLI/Scripts.CLI.csproj

# Apply to local database
dotnet ef database update --project src/Data/Scripts.Data.csproj --startup-project src/CLI/Scripts.CLI.csproj
```

### Python

```powershell
cd python
uv run <script.py>
uv add <package>
```

### PowerShell

```powershell
.\powershell\ScriptsToolkit\<script>.ps1
```

---

## 5. C# Project Structure (Target State)

```
csharp/
├── Directory.Build.props          ← Global: TargetFramework, LangVersion, GlobalUsings, CPM
├── Directory.Packages.props       ← All NuGet versions (no Version= in .csproj files)
├── Scripts.slnx                   ← Solution file (references all projects below)
│
├── src/
│   ├── Core/
│   │   └── Scripts.Core.csproj    ← Serilog, Polly, Google.Apis.Auth
│   ├── Data/
│   │   └── Scripts.Data.csproj    ← EF Core, Npgsql, CsvHelper; includes State/
│   ├── Services/
│   │   ├── Language/
│   │   │   └── Scripts.Services.Language.csproj  ← Azure Translation, RestSharp, Lingua
│   │   └── Music/
│   │       └── Scripts.Services.Music.csproj     ← MetaBrainz, Discogs
│   ├── Orchestrators/
│   │   └── Scripts.Orchestrators.csproj   ← Last.fm, YouTube, Sheets (retained)
│   ├── Reader/
│   │   └── Scripts.Reader.csproj   ← Playwright, AngleSharp, PdfPig, OCR
│   └── CLI/
│       └── Scripts.CLI.csproj      ← Spectre.Console; composition root; Program.cs
│
└── tests/
    └── Scripts.Tests/
        └── Scripts.Tests.csproj    ← TUnit, FluentAssertions, Testcontainers
```

### Dependency Flow (Inward Only)

```
CLI → Orchestrators → Data → Core
CLI → Reader        → Core
CLI → Language      → Core
CLI → Music         → Core
Tests → [all]
Core → (nothing)
```

---

## 6. Database Schema (3NF)

### Music Domain

| Table       | Key Columns                                          |
| ----------- | ---------------------------------------------------- |
| `artists`   | `Id INT PK`, `Name TEXT`, `Metadata JSONB`           |
| `albums`    | `Id INT PK`, `ArtistId INT FK`, `Title TEXT`, `ReleaseDate DATE` |
| `tracks`    | `Id INT PK`, `AlbumId INT FK`, `ArtistId INT FK`, `Title TEXT`, `DurationSeconds INT?` |
| `scrobbles` | `Id BIGINT PK`, `TrackId INT FK`, `ScrobbledAt TIMESTAMPTZ`, `Platform VARCHAR(50)` |
| `videos`    | `Id INT PK`, `Url TEXT`, `Title TEXT`, `Description TEXT`, `ChannelName TEXT`, `UploadDate DATE`, `SyncedAt TIMESTAMPTZ`, `Metadata JSONB` |

### Management Domain

| Table            | Key Columns                                       |
| ---------------- | ------------------------------------------------- |
| `execution_logs` | `Id INT PK`, `Timestamp TIMESTAMPTZ`, `SessionId TEXT`, `Payload JSONB`, `ExitCode INT` |
| `failed_tasks`   | `Id UUID PK`, `TaskName TEXT`, `ErrorMessage TEXT`, `Timestamp TIMESTAMPTZ` |
| `fibery_entities`| `Id UUID PK`, `FiberyId VARCHAR(255)`, `EntityType VARCHAR(100)`, `RawData JSONB` |
| `source_records` | `Id UUID PK`, `SourceId TEXT`, `EntityType TEXT`, `RawData JSONB` |

**Schema authority:** Actual entity files in `csharp/src/Data/Entities/*.cs` take precedence over this table. EF Core 10 migrations via `dotnet ef database update`. No manual SQL files.

---

## 7. EF Core 10 — Critical Version Notes

We target **EF Core 10 LTS** (Nov 2025 – Nov 2028) with Npgsql 10. Do NOT use EF11-only features:

| EF11-Only (Do NOT use)        | EF10 Replacement                                          |
| ----------------------------- | --------------------------------------------------------- |
| `MaxByAsync` / `MinByAsync`   | `OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync()` |
| `EF.Functions.JsonPathExists` | `EF.Functions.JsonContains()` / `@>` Npgsql operator      |

**EF10 features available:**
- `LeftJoin` / `RightJoin` operators (Npgsql-native)
- Named query filters (filter by `platform` enum)
- `DateOnly` translations (Npgsql-native → `albums.release_date`)
- Complex Types for JSONB column mapping
- `ExecuteUpdateAsync` / `ExecuteDeleteAsync` — **always prefer over SaveChanges loops**

**Design mandates:**
- Primary Constructors, File-Scoped Namespaces, Global Usings
- `ExecuteUpdate` / `ExecuteDelete` for mutations — never `SaveChanges()` loops
- No Dapper, no legacy Repository pattern (use EF Core directly or via thin wrappers)

---

## 8. Development Conventions

### Security (Non-Negotiable)

- **NEVER** read, log, echo, or hardcode secrets: `$env:*`, `.env`, API tokens, connection strings
- Rely on `.env` files and secure credential stores
- Run Gitleaks audit before pushing: `gitleaks detect --no-git`

### State Management

- Do **not** modify files in `state/` manually — Docker uses it for PostgreSQL persistence
- State files path: `state/postgres/18/docker`

### Logging

- Log directory: `%USERPROFILE%\.cache\logs\scripts\`
- File format: `yyyy-MM-dd_HH-mm-ss.json` (Serilog CompactJsonFormatter)
- Console output: human-readable Serilog template
- Stack traces: demystified via `Ben.Demystifier`

### Code Style

- All commands use PowerShell with `-ErrorAction Stop` (no silent suppression)
- Absolute paths (`C:\Users\Lance\Dev\Scripts\...`) in all scripts
- Explicit `-Encoding UTF8` on all file write operations
- `foreach` loops (not LINQ `.ForEach()`) for mutations

---

## 9. Absolute Zero Presumption Ruleset

Every agent task MUST follow these rules. No exceptions.

1. **No Tooling Presumptions** — Verify `pwsh`, `dotnet`, `git` exist before starting
2. **No Success Presumptions** — Never use `-ErrorAction SilentlyContinue`. Use `-ErrorAction Stop`
3. **No I/O Presumptions** — Every file create/delete/move MUST be followed by a `Test-Path` assertion
4. **No Encoding Presumptions** — Explicitly declare `-Encoding UTF8`
5. **No Exit Code Presumptions** — Capture `2>&1` output and run Regex assertions on it
6. **No Path Presumptions** — Use absolute paths (`C:\Users\Lance\Dev\Scripts\...`)
7. **No State Presumptions** — Log: Current State → Reason → What → Expected Outcome before each mutation
8. **No NuGet Presumptions** — Always `dotnet restore` before build/test
9. **No Deletion Presumptions** — Create `.bak.YYYYMMDD_HHmmss` before any deletion
10. **Strict TDD Granularity** — ONE miniscule addition per task

---

## 10. Plan Navigation

```
AI/plans/INDEX.md                  ← Start here for plan status and CPM graph
AI/plans/CURRENT_STATUS.md         ← Current test counts, blockers, next action
.kiro/specs/ef-core-10-migration-continuation/  ← Kiro spec wrapper (requirements, design, tasks)
AI/plans/tier-1-ef-migration/      ← Database foundation (critical path blocker)
AI/plans/tier-2-cpm-split/         ← CPM + 8-project split (depends on T1 green)
AI/plans/tier-3-domain/            ← Domain isolation + naming (depends on T2 green)
AI/plans/tier-4-hardening/         ← Integration, quality, DI (depends on T3 green)
```

Each tier must reach **sign-off** (all tests green, `dotnet build` clean) before the next tier begins.

---

## 11. Current Status

**Last Updated:** 2026-05-26  
**Test Status:** 136 passing, 0 failing (100% pass rate)  
**Tier 1 Progress:** 60% complete  
**Blocker:** None. Next step: T1-07 (StateManager migration to EF).

See `AI/plans/CURRENT_STATUS.md` for detailed status, blockers, and next action.
