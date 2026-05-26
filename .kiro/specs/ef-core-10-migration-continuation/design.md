# EF Core 10 Migration Continuation — Design

**Date:** 2026-05-25  
**Status:** ✅ Spec Created

---

## Architecture Overview

### Current State

```
Monolithic Architecture:
  API → .NET Object → JSON → CSV → Google Sheets
  (Single project, file-based state, CSV caching)
```

### Target State

```
Modular Architecture:
  API → .NET 10 Service → PostgreSQL 18 → OCI Object Storage
  (8 projects, EF Core 10, database-backed state, weekly pg_dump + daily WAL)
```

---

## Technology Stack

| Layer              | Technology                                                |
| ------------------ | --------------------------------------------------------- |
| Language           | C# 14 (.NET 10 SDK)                                       |
| ORM                | EF Core 10 LTS (Nov 2025 – Nov 2028)                      |
| Database           | PostgreSQL 18 (Docker Compose locally, OCI in production) |
| Driver             | Npgsql 10                                                 |
| Testing            | TUnit + FluentAssertions                                  |
| Logging            | Serilog (CompactJsonFormatter → `~/.cache/logs/scripts/`) |
| Resilience         | Polly v8 (retry + circuit breaker)                        |
| Language Detection | SearchPioneer.Lingua v1.0.5 (replaces NTextCat)           |
| CLI                | Spectre.Console + Spectre.Console.Cli                     |
| Auth               | Google OAuth 2.0 (`Google.Apis.Auth`)                     |

---

## Project Structure (Target State)

```
csharp/
├── Directory.Build.props (Global: TargetFramework, LangVersion, GlobalUsings, CPM)
├── Directory.Packages.props (All NuGet versions)
├── Scripts.slnx (Solution file)
│
├── src/
│   ├── Core/ (Serilog, Polly, Google.Apis.Auth)
│   ├── Data/ (EF Core, Npgsql, CsvHelper; includes State/)
│   ├── Services/
│   │   ├── Language/ (Azure Translation, RestSharp, Lingua)
│   │   └── Music/ (MetaBrainz, Discogs)
│   ├── Orchestrators/ (Last.fm, YouTube, Sheets)
│   ├── Reader/ (Playwright, AngleSharp, PdfPig, OCR)
│   └── CLI/ (Spectre.Console; composition root; Program.cs)
│
└── tests/
    └── Scripts.Tests/ (TUnit, FluentAssertions, Testcontainers)
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

## Database Schema (3NF)

### Music Domain

| Table       | Key Columns                                                                                        |
| ----------- | -------------------------------------------------------------------------------------------------- |
| `artists`   | `Id INT PK`, `Name TEXT`, `Metadata JSONB`                                                         |
| `albums`    | `Id INT PK`, `ArtistId INT FK`, `Title TEXT`, `ReleaseDate DATE`                                   |
| `tracks`    | `Id INT PK`, `AlbumId INT FK`, `ArtistId INT FK`, `Title TEXT`, `DurationSeconds INT?`             |
| `scrobbles` | `Id BIGINT PK`, `TrackId INT FK`, `ScrobbledAt TIMESTAMPTZ`, `Platform VARCHAR(50)`                |
| `videos`    | `Id INT PK`, `Url TEXT`, `Title TEXT`, `UploadDate DATE`, `SyncedAt TIMESTAMPTZ`, `Metadata JSONB` |

### Management Domain

| Table             | Key Columns                                                                             |
| ----------------- | --------------------------------------------------------------------------------------- |
| `execution_logs`  | `Id INT PK`, `Timestamp TIMESTAMPTZ`, `SessionId TEXT`, `Payload JSONB`, `ExitCode INT` |
| `failed_tasks`    | `Id UUID PK`, `TaskName TEXT`, `ErrorMessage TEXT`, `Timestamp TIMESTAMPTZ`             |
| `fibery_entities` | `Id UUID PK`, `FiberyId VARCHAR(255)`, `EntityType VARCHAR(100)`, `RawData JSONB`       |
| `source_records`  | `Id UUID PK`, `SourceId TEXT`, `EntityType TEXT`, `RawData JSONB`                       |

---

## EF Core 10 Design Decisions

### ✅ Patterns to Use

- **Primary Constructors** — C# 14 feature
- **File-Scoped Namespaces** — Cleaner code
- **Global Usings** — Reduced boilerplate
- **ExecuteUpdateAsync / ExecuteDeleteAsync** — Preferred over SaveChanges loops
- **NoTracking Default** — DbContext configured with AsNoTracking()
- **Explicit Configuration** — ApplyConfigurationsFromAssembly pattern
- **DateOnly Native Mapping** — Npgsql-native → `albums.release_date`
- **Complex Types for JSONB** — Column mapping for structured data

### ❌ Patterns to Avoid

- **EF11-Only Features** — MaxByAsync, MinByAsync, JsonPathExists
- **Dapper** — Use EF Core directly
- **Legacy Repository Pattern** — Use thin wrappers or EF Core directly
- **SaveChanges Loops** — Use ExecuteUpdate/ExecuteDelete instead

### EF10 Equivalents for EF11 Features

| EF11-Only        | EF10 Replacement                                            |
| ---------------- | ----------------------------------------------------------- |
| `MaxByAsync`     | `OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync()` |
| `MinByAsync`     | `OrderBy(x => x.Timestamp).FirstOrDefaultAsync()`           |
| `JsonPathExists` | `JsonContains()` / `@>` Npgsql operator                     |

---

## Migration Path

```
Tier 1: EF Foundation
  ├─ Environment setup (Docker, connection string)
  ├─ Entity extraction and configuration
  ├─ Repository pattern implementation
  ├─ State management migration
  ├─ Testing infrastructure
  └─ Sign-off (150+ tests passing)
         ↓
Tier 2: Modularization
  ├─ CPM setup (Directory.Build.props, Directory.Packages.props)
  ├─ Project extraction (8 projects)
  ├─ Dependency wiring
  └─ Sign-off (200+ tests passing)
         ↓
Tier 3: Domain Isolation
  ├─ Domain boundary verification
  ├─ Naming refactor
  ├─ DateTimeOffset migration
  └─ Sign-off (all domains isolated)
         ↓
Tier 4: Hardening
  ├─ DI container wiring
  ├─ E2E testing
  ├─ Security audit
  ├─ OCI deployment
  └─ Sign-off (release-ready)
```

---

## Key Design Principles

1. **TDD First** — No production code without a failing test
2. **Absolute Zero Presumption** — Verify all tooling, paths, exit codes
3. **Strict Granularity** — One miniscule addition per task
4. **Inward Dependencies** — No circular dependencies
5. **Explicit Configuration** — No magic, all configuration explicit
6. **Resilience by Default** — Polly policies on all external calls
7. **Logging Everywhere** — Serilog with structured logging
8. **Security First** — No secrets in code, Gitleaks audit before push

---

## See Also

- [Tier Plans](../../plans/INDEX.md)
- [Research](research/README.md)
- [Requirements](requirements.md)
