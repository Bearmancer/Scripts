# Consolidation Action Plan: Execution Steps

**Date:** 2026-05-25  
**Objective:** Eliminate sprawl, resolve contradictions, homogenize guidance  
**Estimated Effort:** 6-8 hours total (P0: 2-3h, P1: 3-4h, P2: 1-2h)

---

## Phase 1: P0 — Critical (2-3 hours)

### Action 1.1: Create Spec Directory Structure

**Files to Create:**
```
.kiro/specs/ef-core-10-migration-continuation/
├── .config.kiro
├── requirements.md
├── design.md
├── tasks.md
└── research/
    ├── ENTITY-DESIGN-consolidated.md
    ├── DBCONTEXT-CONFIGURATION-consolidated.md
    ├── MIGRATIONS-EXTENSIONS-consolidated.md
    ├── DATA-ACCESS-REPOSITORIES-consolidated.md
    ├── STATE-MANAGEMENT-consolidated.md
    ├── TESTING-INFRASTRUCTURE-consolidated.md
    ├── ADVANCED-FEATURES-consolidated.md
    ├── RESEARCH-CONSOLIDATION-MAPPING.md
    └── README.md
```

**Step 1.1.1: Create `.config.kiro`**

```json
{
  "specType": "feature",
  "workflowType": "requirements-first",
  "featureName": "ef-core-10-migration-continuation",
  "createdAt": "2026-05-25T00:00:00Z",
  "lastUpdated": "2026-05-25T00:00:00Z",
  "description": "Migrate monolithic C# project to PostgreSQL (EF Core 10) + 8-project modular solution + hardening across 4 tiers (45 sequenced phases)"
}
```

**Step 1.1.2: Create `requirements.md` (Stub)**

```markdown
# EF Core 10 Migration Continuation — Requirements

## Overview

Migrate the Scripts repository from a monolithic architecture to a modular, PostgreSQL-backed system using EF Core 10.

## Tiers

### Tier 1: EF Core 10 Foundation (Phases 00-16)
- Establish PostgreSQL connectivity
- Extract and configure entities
- Create repository pattern
- Migrate state management
- Implement testing infrastructure

### Tier 2: Modularization (Phases 00-10)
- Split into 8 projects (Core, Data, Language, Music, Orchestrators, Reader, CLI, Tests)
- Implement CPM (Central Package Management)
- Wire dependencies

### Tier 3: Domain Isolation (Phases 00-07)
- Isolate Reader, Music, Language, Sync domains
- Refactor naming conventions
- Migrate to DateTimeOffset

### Tier 4: Hardening (Phases 00-08)
- DI container wiring
- E2E testing
- Security audit
- OCI deployment

## Success Criteria

- 150+ tests passing (100% pass rate)
- All 4 tiers complete and signed off
- Zero build warnings
- Full E2E workflow validated

## See Also

- [Tier Plans](../../plans/INDEX.md)
- [Research](research/README.md)
```

**Step 1.1.3: Create `design.md` (Stub)**

```markdown
# EF Core 10 Migration Continuation — Design

## Architecture

### Current State
- Monolithic C# project
- JSON file-based state management
- CSV-based caching
- Google Sheets integration

### Target State
- 8 modular projects (Core, Data, Language, Music, Orchestrators, Reader, CLI, Tests)
- PostgreSQL 18 database (EF Core 10)
- Centralized state management via EF Core
- OCI Object Storage for backups

### Migration Path

```
Tier 1: EF Foundation
  ↓
Tier 2: Modularization
  ↓
Tier 3: Domain Isolation
  ↓
Tier 4: Hardening
```

## Key Design Decisions

1. **Database**: PostgreSQL 18 (Docker Compose locally, OCI in production)
2. **ORM**: EF Core 10 LTS (Nov 2025 – Nov 2028)
3. **Testing**: TUnit + FluentAssertions + Testcontainers
4. **Logging**: Serilog (CompactJsonFormatter → `~/.cache/logs/scripts/`)
5. **Resilience**: Polly v8 (retry + circuit breaker)
6. **Language Detection**: SearchPioneer.Lingua v1.0.5 (replaces NTextCat)

## See Also

- [Tier Plans](../../plans/INDEX.md)
- [Research](research/README.md)
```

**Step 1.1.4: Create `tasks.md` (Stub)**

```markdown
# EF Core 10 Migration Continuation — Task List

## Overview

45 sequenced tasks across 4 tiers. Each task follows TDD: RED → GREEN → REFACTOR.

## Tier 1: EF Foundation (16 tasks)

See [Tier 1 Plan](../../plans/tier-1-ef-migration/INDEX.md)

- [ ] T1-00: Environment Preflight
- [ ] T1-01: Entity Extraction
- [ ] T1-02: Entity Refactoring
- [ ] T1-03: DbContext Configuration
- [ ] T1-04: Entity Configurations
- [ ] T1-05: Database Migrations
- [ ] T1-06: Repository Pattern
- [ ] T1-07: State Manager Migration
- [ ] T1-08: Release Cache Migration
- [ ] T1-09: Sync Service Updates
- [ ] T1-10: EF10 Query Upgrades
- [ ] T1-11: Compiled Model
- [ ] T1-12: Logging Relocation
- [ ] T1-13: Lingua Migration
- [ ] T1-14: Resilience Policies
- [ ] T1-15: Testcontainers
- [ ] T1-16: Sign-off

## Tier 2: Modularization (11 tasks)

See [Tier 2 Plan](../../plans/tier-2-cpm-split/INDEX.md)

- [ ] T2-00: CPM Foundation
- [ ] T2-01: Scripts.Core
- [ ] T2-02: Scripts.Data
- [ ] T2-03: Scripts.Services.Language
- [ ] T2-04: Scripts.Services.Music
- [ ] T2-05: Scripts.Orchestrators
- [ ] T2-06: Scripts.Reader
- [ ] T2-07: Scripts.CLI
- [ ] T2-08: Scripts.Tests
- [ ] T2-09: Duplicate Cleanup
- [ ] T2-10: Sign-off

## Tier 3: Domain Isolation (8 tasks)

See [Tier 3 Plan](../../plans/tier-3-domain/INDEX.md)

- [ ] T3-00: Reader Domain Isolation
- [ ] T3-01: Music Domain Isolation
- [ ] T3-02: Language Domain Isolation
- [ ] T3-03: Sync Domain Isolation
- [ ] T3-04: Naming Refactor
- [ ] T3-05: DateTimeOffset Migration
- [ ] T3-06: Inspection Fixes Logic
- [ ] T3-07: Sign-off

## Tier 4: Hardening (9 tasks)

See [Tier 4 Plan](../../plans/tier-4-hardening/INDEX.md)

- [ ] T4-00: DI Container Wiring
- [ ] T4-01: E2E Testing
- [ ] T4-02: Inspection Fixes Structural
- [ ] T4-03: Reader Directory Restructure
- [ ] T4-04: Security Audit
- [ ] T4-05: Tooling Cleanup
- [ ] T4-06: Documentation
- [ ] T4-07: OCI Deployment
- [ ] T4-08: Sign-off

## See Also

- [Tier Plans](../../plans/INDEX.md)
- [Research](research/README.md)
```

**Step 1.1.5: Copy Research Files**

```powershell
# Copy consolidated research files to spec directory
$sourceDir = "C:\Users\Lance\Dev\Scripts\AI\research"
$targetDir = "C:\Users\Lance\Dev\Scripts\.kiro\specs\ef-core-10-migration-continuation\research"

New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

Get-ChildItem -Path $sourceDir -Filter "*-consolidated.md" | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $targetDir -Force
}

Copy-Item -Path "$sourceDir\RESEARCH-CONSOLIDATION-MAPPING.md" -Destination $targetDir -Force
Copy-Item -Path "$sourceDir\README.md" -Destination $targetDir -Force
```

---

### Action 1.2: Create CURRENT_STATUS.md

**File:** `c:\Users\Lance\Dev\Scripts\AI\plans\CURRENT_STATUS.md`

```markdown
# EF Core 10 Migration — Current Status

**Last Updated:** 2026-05-25 (Auto-generated)  
**Status:** 60% Complete (Tier 1 Foundation Phase)

---

## Test Summary

| Metric | Value |
|--------|-------|
| Total Tests | 131 |
| Passing | 78 (60%) |
| Failing | 53 (40%) |
| Target | 150+ (100%) |

### Passing Tests by Category

- DbContext Configuration: 5 tests
- Entity Definitions: 8 tests
- Entity Refactoring: 3 tests
- Environment Setup: 8 tests
- EF11 Guards: 1 test
- Repositories (unit): 44 tests
- Other: 9 tests

### Failing Tests by Category

- Entity Configuration Integration: 12 tests
- Repository Integration: 15 tests
- Testcontainers Lifecycle: 20 tests
- Other: 6 tests

---

## Tier Progress

| Tier | Phase | Status | Notes |
|------|-------|--------|-------|
| T1 | 00-16 | 🟡 60% | Foundation phase; compiled model out of sync |
| T2 | 00-10 | 🔒 Blocked | Waiting for T1 sign-off |
| T3 | 00-07 | 🔒 Blocked | Waiting for T2 sign-off |
| T4 | 00-08 | 🔒 Blocked | Waiting for T3 sign-off |

---

## Blockers (P0)

1. **Compiled Model Out of Sync**
   - Issue: PendingModelChangesWarning on all integration tests
   - Impact: 53 tests failing
   - Resolution: Regenerate compiled model after entity config changes

2. **Testcontainers Lifecycle**
   - Issue: DatabaseTestFixture has design flaws
   - Impact: 20 tests failing
   - Resolution: Redesign fixture with proper async/await handling

---

## High Priority (P1)

1. StateManager migration to Data/State layer
2. Release cache migration from CSV to EF Core
3. Sync service EF10 pattern updates
4. XDG logging relocation
5. Lingua language detection migration

---

## Medium Priority (P2)

1. Polly resilience policy integration
2. Build warning cleanup (71 warnings)
3. Code quality improvements

---

## Next Immediate Action

**Regenerate compiled model:**
```powershell
cd C:\Users\Lance\Dev\Scripts\csharp
dotnet ef dbcontext optimize --project src/Data/Scripts.Data.csproj --startup-project src/CLI/Scripts.CLI.csproj
```

Then re-run tests to verify compiled model sync.

---

## See Also

- [Tier Plans](INDEX.md)
- [Consolidation Audit](../CONSOLIDATION_AUDIT_REPORT.md)
- [Research](../research/README.md)
```

---

### Action 1.3: Update AGENTS.md

**File:** `c:\Users\Lance\Dev\Scripts\AGENTS.md`

**Change §10 "Plan Navigation":**

From:
```
## 10. Plan Navigation

AI/plans/INDEX.md                  ← Start here for plan status and CPM graph
```

To:
```
## 10. Plan Navigation

AI/plans/INDEX.md                  ← Start here for plan status and CPM graph
AI/plans/CURRENT_STATUS.md         ← Current test counts, blockers, next action
.kiro/specs/ef-core-10-migration-continuation/  ← Kiro spec wrapper (requirements, design, tasks)
```

**Add new section after §10:**

```
## 11. Current Status

**Last Updated:** 2026-05-25  
**Test Status:** 78 passing, 53 failing (60% pass rate)  
**Tier 1 Progress:** 60% complete  
**Blocker:** Compiled model out of sync

See `AI/plans/CURRENT_STATUS.md` for detailed status, blockers, and next action.
```

---

## Phase 2: P1 — High Priority (3-4 hours)

### Action 2.1: Link Research to Tier Plans

**For each tier plan file (45 total):**

Add section after "Prerequisites":

```markdown
## Research Reference

This phase is informed by the following research documents:

- [ENTITY-DESIGN-consolidated.md](../../research/ENTITY-DESIGN-consolidated.md) — Entity definitions and refactoring
- [DBCONTEXT-CONFIGURATION-consolidated.md](../../research/DBCONTEXT-CONFIGURATION-consolidated.md) — DbContext setup
- [MIGRATIONS-EXTENSIONS-consolidated.md](../../research/MIGRATIONS-EXTENSIONS-consolidated.md) — Database migrations
- [DATA-ACCESS-REPOSITORIES-consolidated.md](../../research/DATA-ACCESS-REPOSITORIES-consolidated.md) — Repository pattern
- [STATE-MANAGEMENT-consolidated.md](../../research/STATE-MANAGEMENT-consolidated.md) — State management
- [TESTING-INFRASTRUCTURE-consolidated.md](../../research/TESTING-INFRASTRUCTURE-consolidated.md) — Testing infrastructure
- [ADVANCED-FEATURES-consolidated.md](../../research/ADVANCED-FEATURES-consolidated.md) — Advanced EF Core features
```

**Mapping by Tier:**

| Tier | Relevant Research |
|------|-------------------|
| T1-00 to T1-04 | ENTITY-DESIGN, DBCONTEXT-CONFIGURATION |
| T1-05 | MIGRATIONS-EXTENSIONS |
| T1-06, T1-09 | DATA-ACCESS-REPOSITORIES |
| T1-07, T1-08 | STATE-MANAGEMENT |
| T1-10 to T1-14 | ADVANCED-FEATURES |
| T1-15 | TESTING-INFRASTRUCTURE |
| T2-00 to T2-10 | All (reference as needed) |
| T3-00 to T3-07 | All (reference as needed) |
| T4-00 to T4-08 | All (reference as needed) |

---

### Action 2.2: Archive Original Research Files

**Step 2.2.1: Create archive directory**

```powershell
New-Item -ItemType Directory -Path "C:\Users\Lance\Dev\Scripts\AI\research\archive" -Force | Out-Null
```

**Step 2.2.2: Move original 31 research files to archive**

```powershell
$archiveDir = "C:\Users\Lance\Dev\Scripts\AI\research\archive"

Get-ChildItem -Path "C:\Users\Lance\Dev\Scripts\AI\research" -Filter "20260*.md" | ForEach-Object {
    Move-Item -Path $_.FullName -Destination $archiveDir -Force
}

Get-ChildItem -Path "C:\Users\Lance\Dev\Scripts\AI\research" -Filter "angle-*.md" | ForEach-Object {
    Move-Item -Path $_.FullName -Destination $archiveDir -Force
}

Get-ChildItem -Path "C:\Users\Lance\Dev\Scripts\AI\research" -Filter "state_*.md" | ForEach-Object {
    Move-Item -Path $_.FullName -Destination $archiveDir -Force
}

Get-ChildItem -Path "C:\Users\Lance\Dev\Scripts\AI\research" -Filter "CONSOLIDATED_*.md" | ForEach-Object {
    Move-Item -Path $_.FullName -Destination $archiveDir -Force
}
```

**Step 2.2.3: Update research README.md**

Add section:

```markdown
## Archive

Original 31 research files have been consolidated into 7 concern-based documents and archived in `archive/` for reference.

See [RESEARCH-CONSOLIDATION-MAPPING.md](RESEARCH-CONSOLIDATION-MAPPING.md) for mapping of original files to consolidated documents.
```

---

### Action 2.3: Archive Old Diagnostic Files

**Files to archive:**
- `AI/plans/DIAGNOSTIC_REPORT.md`
- `AI/plans/DEBUGGING_SUMMARY.md`
- `AI/plans/MIGRATION_TO_NATIVE_TESTING.md`
- `AI/plans/QUICK_WINS_IMPLEMENTATION.md`

**Step 2.3.1: Create archive directory**

```powershell
New-Item -ItemType Directory -Path "C:\Users\Lance\Dev\Scripts\AI\plans\archive" -Force | Out-Null
```

**Step 2.3.2: Move files to archive**

```powershell
$archiveDir = "C:\Users\Lance\Dev\Scripts\AI\plans\archive"

@(
    "DIAGNOSTIC_REPORT.md",
    "DEBUGGING_SUMMARY.md",
    "MIGRATION_TO_NATIVE_TESTING.md",
    "QUICK_WINS_IMPLEMENTATION.md"
) | ForEach-Object {
    $path = "C:\Users\Lance\Dev\Scripts\AI\plans\$_"
    if (Test-Path $path) {
        Move-Item -Path $path -Destination $archiveDir -Force
    }
}
```

---

### Action 2.4: Standardize Tier Plan Headers

**For each tier plan file (45 total):**

Ensure header follows this format:

```markdown
# T{N}-{NN}: {Phase Name}

> **For agentic workers:** Use `superpowers:subagent-driven-development` to implement this plan task-by-task.

**Goal:** {One-sentence goal}

**Architecture:** {Brief architecture description}

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

{Prerequisites}

---

## Research Reference

{Research references}

---

## Task 1 — {Task Name}

{Task content}
```

---

## Phase 3: P2 — Medium Priority (1-2 hours)

### Action 3.1: Create Cross-Reference Index

**File:** `c:\Users\Lance\Dev\Scripts\AI\CROSS_REFERENCE_INDEX.md`

```markdown
# Cross-Reference Index

**Date:** 2026-05-25  
**Purpose:** Map relationships between tier plans, research files, and AGENTS.md sections

---

## Tier Plans → Research Files

### Tier 1: EF Foundation

| Phase | Plan | Research |
|-------|------|----------|
| T1-00 | Environment Preflight | (None) |
| T1-01 | Entity Extraction | ENTITY-DESIGN |
| T1-02 | Entity Refactoring | ENTITY-DESIGN |
| T1-03 | DbContext Configuration | DBCONTEXT-CONFIGURATION |
| T1-04 | Entity Configurations | ENTITY-DESIGN, DBCONTEXT-CONFIGURATION |
| T1-05 | Database Migrations | MIGRATIONS-EXTENSIONS |
| T1-06 | Repository Pattern | DATA-ACCESS-REPOSITORIES |
| T1-07 | State Manager Migration | STATE-MANAGEMENT |
| T1-08 | Release Cache Migration | STATE-MANAGEMENT |
| T1-09 | Sync Service Updates | DATA-ACCESS-REPOSITORIES |
| T1-10 | EF10 Query Upgrades | ADVANCED-FEATURES |
| T1-11 | Compiled Model | ADVANCED-FEATURES |
| T1-12 | Logging Relocation | ADVANCED-FEATURES |
| T1-13 | Lingua Migration | ADVANCED-FEATURES |
| T1-14 | Resilience Policies | ADVANCED-FEATURES |
| T1-15 | Testcontainers | TESTING-INFRASTRUCTURE |
| T1-16 | Sign-off | (All) |

### Tier 2: Modularization

| Phase | Plan | Research |
|-------|------|----------|
| T2-00 | CPM Foundation | (None) |
| T2-01 to T2-08 | Project Extraction | (All as reference) |
| T2-09 | Duplicate Cleanup | (All as reference) |
| T2-10 | Sign-off | (All) |

### Tier 3: Domain Isolation

| Phase | Plan | Research |
|-------|------|----------|
| T3-00 to T3-06 | Domain Isolation | (All as reference) |
| T3-07 | Sign-off | (All) |

### Tier 4: Hardening

| Phase | Plan | Research |
|-------|------|----------|
| T4-00 to T4-07 | Hardening | (All as reference) |
| T4-08 | Sign-off | (All) |

---

## AGENTS.md Sections → Tier Plans

| AGENTS.md Section | Relevant Tier Plans |
|-------------------|-------------------|
| §2 Key Technologies | All tiers (reference) |
| §3 Environment Setup | T1-00 |
| §4 Building & Running | T1-00, T2-00, T4-00 |
| §5 C# Project Structure | T2-00 to T2-10 |
| §6 Database Schema | T1-01 to T1-05 |
| §7 EF Core 10 Notes | T1-03 to T1-14 |
| §8 Development Conventions | All tiers (reference) |
| §9 Absolute Zero Presumption | All tiers (reference) |
| §10 Plan Navigation | All tiers (reference) |

---

## Blockers → Responsible Tier

| Blocker | Tier | Phase | Status |
|---------|------|-------|--------|
| Compiled model out of sync | T1 | 11 | 🟡 In Progress |
| Testcontainers lifecycle | T1 | 15 | 🟡 In Progress |
| StateManager migration | T1 | 07 | ❌ Not Started |
| Release cache migration | T1 | 08 | ❌ Not Started |
| Sync service updates | T1 | 09 | ❌ Not Started |

---

## Research Files → Tier Plans

### ENTITY-DESIGN-consolidated.md

Used by:
- T1-01: Entity Extraction
- T1-02: Entity Refactoring
- T1-04: Entity Configurations

### DBCONTEXT-CONFIGURATION-consolidated.md

Used by:
- T1-03: DbContext Configuration
- T1-04: Entity Configurations

### MIGRATIONS-EXTENSIONS-consolidated.md

Used by:
- T1-05: Database Migrations

### DATA-ACCESS-REPOSITORIES-consolidated.md

Used by:
- T1-06: Repository Pattern
- T1-09: Sync Service Updates

### STATE-MANAGEMENT-consolidated.md

Used by:
- T1-07: State Manager Migration
- T1-08: Release Cache Migration

### TESTING-INFRASTRUCTURE-consolidated.md

Used by:
- T1-15: Testcontainers

### ADVANCED-FEATURES-consolidated.md

Used by:
- T1-10: EF10 Query Upgrades
- T1-11: Compiled Model
- T1-12: Logging Relocation
- T1-13: Lingua Migration
- T1-14: Resilience Policies

---

## See Also

- [Tier Plans](plans/INDEX.md)
- [Research](research/README.md)
- [Current Status](plans/CURRENT_STATUS.md)
- [Consolidation Audit](CONSOLIDATION_AUDIT_REPORT.md)
```

---

### Action 3.2: Verify Tier Plan Completeness

**Checklist for each tier plan:**

- [ ] Header includes goal, architecture, tech stack
- [ ] Prerequisites section exists
- [ ] Research reference section exists
- [ ] TDD task structure (RED → GREEN → REFACTOR)
- [ ] Success criteria defined
- [ ] All 7-step TDD loop documented

**Script to verify:**

```powershell
$tierDir = "C:\Users\Lance\Dev\Scripts\AI\plans"

Get-ChildItem -Path "$tierDir\tier-*" -Filter "*.md" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    
    $checks = @{
        "Goal" = $content -match "^## Goal"
        "Architecture" = $content -match "^## Architecture"
        "Tech Stack" = $content -match "^## Tech Stack"
        "Prerequisites" = $content -match "^## Prerequisites"
        "Research Reference" = $content -match "^## Research Reference"
        "TDD Loop" = $content -match "Step 1: Write the failing test"
        "Success Criteria" = $content -match "^## Success Criteria"
    }
    
    $missing = $checks.GetEnumerator() | Where-Object { -not $_.Value } | Select-Object -ExpandProperty Name
    
    if ($missing) {
        Write-Host "❌ $($_.Name): Missing $($missing -join ', ')"
    } else {
        Write-Host "✅ $($_.Name)"
    }
}
```

---

## Execution Checklist

### Phase 1: P0 (2-3 hours)

- [ ] Create `.kiro/specs/ef-core-10-migration-continuation/` directory
- [ ] Create `.config.kiro` metadata file
- [ ] Create `requirements.md` stub
- [ ] Create `design.md` stub
- [ ] Create `tasks.md` stub
- [ ] Copy research files to spec directory
- [ ] Create `CURRENT_STATUS.md`
- [ ] Update `AGENTS.md` §10 and add §11

### Phase 2: P1 (3-4 hours)

- [ ] Add research references to all 45 tier plans
- [ ] Create `AI/research/archive/` directory
- [ ] Move 31 original research files to archive
- [ ] Update research `README.md`
- [ ] Create `AI/plans/archive/` directory
- [ ] Move old diagnostic files to archive
- [ ] Standardize headers on all 45 tier plans

### Phase 3: P2 (1-2 hours)

- [ ] Create `CROSS_REFERENCE_INDEX.md`
- [ ] Verify tier plan completeness (run script)
- [ ] Document any missing sections

---

## Success Criteria

- [ ] No contradictions between plans, research, and AGENTS.md
- [ ] All research files consolidated and archived
- [ ] All diagnostic files consolidated into CURRENT_STATUS.md
- [ ] Spec directory created with metadata and stubs
- [ ] Cross-references established between plans and research
- [ ] All tier plans standardized and complete
- [ ] Zero sprawl (no duplicate files, no orphaned artifacts)

---

## Estimated Timeline

| Phase | Duration | Effort |
|-------|----------|--------|
| P0 | 2-3 hours | High (structural changes) |
| P1 | 3-4 hours | Medium (cross-references, archiving) |
| P2 | 1-2 hours | Low (documentation, verification) |
| **Total** | **6-8 hours** | **Medium** |

---

## Notes

- All file operations should use absolute paths
- Create `.bak.YYYYMMDD_HHmmss` backups before moving files
- Verify all cross-references work after consolidation
- Test that Kiro spec workflow can read new spec directory
- Update this plan as issues are discovered during execution
