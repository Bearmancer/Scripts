# Research Consolidation Mapping

**Date:** 2026-05-25  
**Total Files Scanned:** 31 research files  
**Consolidated Files Created:** 7  
**Consolidation Status:** ✅ COMPLETE

---

## Consolidation Summary

All 31 research files have been analyzed and organized into 7 consolidated concern-based documents. This mapping shows which original files contributed to each consolidated document.

---

## Consolidated Files & Source Mapping

### 1. ENTITY-DESIGN-consolidated.md
**Primary Concern:** Entity definitions, properties, and refactoring

**Source Files:**
- `20260522-t1-02-entity-refactoring-research.md` (Entity inventory, Mbid audit, Track metadata, legacy int IDs, Video/FailedTask discrepancies)
- `20260522-t1-04-entity-configs-research.md` (Configuration gaps, index recommendations)
- `angle-3-jsondocument.md` (JsonDocument mapping conflicts in EF Core 10)

**Key Topics:**
- Mbid property removal (Artist, Album, Track)
- Track metadata audit
- Legacy int ID migration strategy
- Video and FailedTask entity discrepancies
- JsonDocument mapping resolution
- Configuration gaps and recommendations

---

### 2. DBCONTEXT-CONFIGURATION-consolidated.md
**Primary Concern:** DbContext setup, entity configurations, and model compilation

**Source Files:**
- `20260522-t1-03-dbcontext-config-research.md` (DbContext feature checklist, gap analysis)
- `20260522-t1-04-entity-configs-research.md` (Configuration inventory, gaps, recommendations)
- `angle-2-compiled-model.md` (Compiled model lock and runtime configuration)
- `angle-4-pendingmodelchanges.md` (PendingModelChangesWarning in EF Core 9+)

**Key Topics:**
- DbContext NoTracking default
- ApplyConfigurationsFromAssembly pattern
- SourceRecord unmapped entity
- PostgreSQL extensions registration
- Configuration file inventory and gaps
- Compiled model lifecycle
- PendingModelChangesWarning workflow

---

### 3. MIGRATIONS-EXTENSIONS-consolidated.md
**Primary Concern:** Database migrations and PostgreSQL extensions

**Source Files:**
- `20260522-t1-05-migrations-research.md` (Migration status, extensions, NuGet versions, migration command)

**Key Topics:**
- Migration status (none exist yet)
- PostgreSQL extensions (unaccent, pg_trgm)
- Functional indexes
- NuGet package versions
- Migration command for monolithic structure
- Blockers and prerequisites

---

### 4. DATA-ACCESS-REPOSITORIES-consolidated.md
**Primary Concern:** Data access layer, repository pattern, and sync services

**Source Files:**
- `20260522-t1-06-repositories-research.md` (Current data access, repository pattern recommendation, interface contracts)
- `20260522-t1-09-sync-service-research.md` (Duplicate LastFmService analysis, ILike usage, ExecuteUpdate/ExecuteDelete opportunities)

**Key Topics:**
- PostgresService current implementation
- Duplicate LastFmService files
- ILike/EF.Functions.Like usage
- Repository pattern recommendation
- Interface contracts for 7 repositories
- DI registration
- Mutation strategy (ExecuteUpdateAsync, ExecuteDeleteAsync)

---

### 5. STATE-MANAGEMENT-consolidated.md
**Primary Concern:** State management, caching, and file-based persistence

**Source Files:**
- `20260522-t1-07-state-manager-research.md` (StateManager duplicate analysis, usage reference, migration plan)
- `20260522-t1-08-release-cache-research.md` (ReleaseProgressCache architecture, entity design, configuration)

**Key Topics:**
- StateManager duplicate analysis (Core vs Infrastructure)
- StateManager usage reference
- Target directory and namespace
- Migration plan (5 phases)
- ReleaseProgressCache dual caching system
- Entity design for release progress
- Configuration and DbContext addition

---

### 6. TESTING-INFRASTRUCTURE-consolidated.md
**Primary Concern:** Integration testing, test infrastructure, and database isolation

**Source Files:**
- `20260522-t1-15-testcontainers-research.md` (Test project gaps, DatabaseFixture design, TUnit patterns)
- `20260525-deep-dive-transaction-rollback-safety.md` (Concurrency locking, transaction safety)
- `20260525-efcore-async-thread-safety.md` (Async/await safety, thread-safety, DbContext pooling)
- `20260525-integration-testing-parallelization-optimizations.md` (Compiled model optimization, NpgsqlDataSource, command timeouts)
- `20260525-native-database-testing-research.md` (Native database testing vs Testcontainers, transactional isolation)
- `20260525-npgsql-connection-pooling-nuances.md` (Connection pooling, await yielding, DbContext pooling)
- `angle-1-testcontainers.md` (Testcontainers lifecycle, shared container pattern)
- `angle-5-test-isolation.md` (Transaction rollback vs Respawn, parallel test isolation)

**Key Topics:**
- Test project structure and gaps
- Native database testing strategy (recommended)
- Testcontainers alternative
- DatabaseFixture design and implementation
- Assembly-level setup
- Concurrency and thread-safety
- Async/await safety
- Npgsql connection pooling
- TUnit test patterns
- Success criteria

---

### 7. ADVANCED-FEATURES-consolidated.md
**Primary Concern:** Advanced EF Core features, logging, language detection, and resilience

**Source Files:**
- `20260522-t1-10-ef10-queries-research.md` (EF10 query patterns, JSONB inventory)
- `20260522-t1-11-compiled-model-research.md` (Compiled model generation, MSBuild properties)
- `20260522-t1-12-logging-research.md` (Log path migration, Ben.Demystifier integration)
- `20260522-t1-13-lingua-research.md` (NTextCat to Lingua migration)
- `20260522-t1-14-resilience-research.md` (Polly v8 status, circuit breaker, DB retry policy, Infrastructure duplicate)

**Key Topics:**
- EF10 query patterns (no EF11-only patterns found)
- JSONB column inventory
- Compiled models generation and auto-detection
- Logging path migration to `%USERPROFILE%\.cache\logs\scripts`
- Ben.Demystifier integration
- Lingua language detection migration
- Polly v8 resilience patterns
- DB retry policy gaps
- Infrastructure Resilience duplicate

---

## Additional Research Files (Not Consolidated)

These files provide context and analysis but are not directly consolidated into the 7 main documents:

| File | Purpose | Status |
|------|---------|--------|
| `20260522-tier2-plan-phase2-verification.md` | Tier 2 plan verification and namespace drift fixes | Reference only |
| `20260523-gcp-credits-llm-assessment-research.md` | GCP credits and LLM assessment | Reference only |
| `20260523-oci-postgres-backup-and-config-research.md` | OCI PostgreSQL backup and configuration | Reference only |
| `20260525-efcore10-jsondocument-nullreference-research.md` | JsonDocument NullReferenceException details | Included in ENTITY-DESIGN |
| `20260525-efcore10-pending-model-changes-research.md` | PendingModelChangesWarning details | Included in DBCONTEXT-CONFIGURATION |
| `state_inventory_and_failure_patterns.md` | System inventory and recurring failure patterns | Reference only |
| `CONSOLIDATED_RESEARCH.md` | Tier 1 phases 02-06 consolidated research | Reference only |

---

## File Organization by Concern

### Entity Design & Refactoring
- **Consolidated File:** `ENTITY-DESIGN-consolidated.md`
- **Phases:** T1-02, T1-04 (partial)
- **Key Decisions:** Mbid removal, JsonDocument mapping, configuration gaps

### DbContext & Configuration
- **Consolidated File:** `DBCONTEXT-CONFIGURATION-consolidated.md`
- **Phases:** T1-03, T1-04 (partial)
- **Key Decisions:** SourceRecord mapping, extension registration, compiled models

### Migrations & Extensions
- **Consolidated File:** `MIGRATIONS-EXTENSIONS-consolidated.md`
- **Phases:** T1-05
- **Key Decisions:** Extension registration, functional indexes, migration command

### Data Access & Repositories
- **Consolidated File:** `DATA-ACCESS-REPOSITORIES-consolidated.md`
- **Phases:** T1-06, T1-09
- **Key Decisions:** Repository pattern, 7 repository pairs, mutation strategy

### State Management & Caching
- **Consolidated File:** `STATE-MANAGEMENT-consolidated.md`
- **Phases:** T1-07, T1-08
- **Key Decisions:** StateManager migration, ReleaseProgressCache entity design

### Testing Infrastructure
- **Consolidated File:** `TESTING-INFRASTRUCTURE-consolidated.md`
- **Phases:** T1-15, T1-15 (deep dives)
- **Key Decisions:** Native database testing, DatabaseFixture design, TUnit patterns

### Advanced Features
- **Consolidated File:** `ADVANCED-FEATURES-consolidated.md`
- **Phases:** T1-10, T1-11, T1-12, T1-13, T1-14
- **Key Decisions:** EF10 patterns, compiled models, logging, Lingua, resilience

---

## Cross-Reference Index

### By Original File

| Original File | Consolidated Into |
|---|---|
| 20260522-t1-02-entity-refactoring-research.md | ENTITY-DESIGN |
| 20260522-t1-03-dbcontext-config-research.md | DBCONTEXT-CONFIGURATION |
| 20260522-t1-04-entity-configs-research.md | ENTITY-DESIGN, DBCONTEXT-CONFIGURATION |
| 20260522-t1-05-migrations-research.md | MIGRATIONS-EXTENSIONS |
| 20260522-t1-06-repositories-research.md | DATA-ACCESS-REPOSITORIES |
| 20260522-t1-07-state-manager-research.md | STATE-MANAGEMENT |
| 20260522-t1-08-release-cache-research.md | STATE-MANAGEMENT |
| 20260522-t1-09-sync-service-research.md | DATA-ACCESS-REPOSITORIES |
| 20260522-t1-10-ef10-queries-research.md | ADVANCED-FEATURES |
| 20260522-t1-11-compiled-model-research.md | ADVANCED-FEATURES |
| 20260522-t1-12-logging-research.md | ADVANCED-FEATURES |
| 20260522-t1-13-lingua-research.md | ADVANCED-FEATURES |
| 20260522-t1-14-resilience-research.md | ADVANCED-FEATURES |
| 20260522-t1-15-testcontainers-research.md | TESTING-INFRASTRUCTURE |
| 20260522-tier2-plan-phase2-verification.md | (Reference only) |
| 20260523-gcp-credits-llm-assessment-research.md | (Reference only) |
| 20260523-oci-postgres-backup-and-config-research.md | (Reference only) |
| 20260525-deep-dive-transaction-rollback-safety.md | TESTING-INFRASTRUCTURE |
| 20260525-efcore-async-thread-safety.md | TESTING-INFRASTRUCTURE |
| 20260525-efcore10-jsondocument-nullreference-research.md | ENTITY-DESIGN |
| 20260525-efcore10-pending-model-changes-research.md | DBCONTEXT-CONFIGURATION |
| 20260525-integration-testing-parallelization-optimizations.md | TESTING-INFRASTRUCTURE |
| 20260525-native-database-testing-research.md | TESTING-INFRASTRUCTURE |
| 20260525-npgsql-connection-pooling-nuances.md | TESTING-INFRASTRUCTURE |
| angle-1-testcontainers.md | TESTING-INFRASTRUCTURE |
| angle-2-compiled-model.md | DBCONTEXT-CONFIGURATION |
| angle-3-jsondocument.md | ENTITY-DESIGN |
| angle-4-pendingmodelchanges.md | DBCONTEXT-CONFIGURATION |
| angle-5-test-isolation.md | TESTING-INFRASTRUCTURE |
| state_inventory_and_failure_patterns.md | (Reference only) |
| CONSOLIDATED_RESEARCH.md | (Reference only) |

---

## Usage Recommendations

1. **For Entity Design Work:** Start with `ENTITY-DESIGN-consolidated.md`
2. **For DbContext Setup:** Start with `DBCONTEXT-CONFIGURATION-consolidated.md`
3. **For Migrations:** Start with `MIGRATIONS-EXTENSIONS-consolidated.md`
4. **For Data Access:** Start with `DATA-ACCESS-REPOSITORIES-consolidated.md`
5. **For State Management:** Start with `STATE-MANAGEMENT-consolidated.md`
6. **For Testing:** Start with `TESTING-INFRASTRUCTURE-consolidated.md`
7. **For Advanced Features:** Start with `ADVANCED-FEATURES-consolidated.md`

Each consolidated file is self-contained with all relevant information, file paths, and recommendations for its concern area.

---

## Next Steps

1. Review each consolidated file for accuracy and completeness
2. Use the consolidated files as the basis for implementation planning
3. Archive the original 31 research files for reference
4. Create implementation tasks based on the consolidated findings
