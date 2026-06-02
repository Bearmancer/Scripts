# EF Core 10 Migration Research — Consolidated Index

**Date:** 2026-05-25  
**Status:** ✅ Consolidation Complete  
**Original Files:** 31 → **8 Consolidated + 1 Mapping**

---

## Quick Navigation

### By Concern Area

| Concern | File | Phases | Key Topics |
|---------|------|--------|-----------|
| **Entity Design** | `ENTITY-DESIGN-consolidated.md` | T1-02, T1-04 | Mbid removal, JsonDocument mapping, configuration gaps |
| **DbContext & Configuration** | `DBCONTEXT-CONFIGURATION-consolidated.md` | T1-03, T1-04 | DbContext setup, SourceRecord mapping, extensions, compiled models |
| **Migrations & Extensions** | `MIGRATIONS-EXTENSIONS-consolidated.md` | T1-05 | Migration status, PostgreSQL extensions, functional indexes |
| **Data Access & Repositories** | `DATA-ACCESS-REPOSITORIES-consolidated.md` | T1-06, T1-09 | Repository pattern, 7 repository pairs, mutation strategies |
| **State Management & Caching** | `STATE-MANAGEMENT-consolidated.md` | T1-07, T1-08 | StateManager migration, ReleaseProgressCache entity design |
| **Testing Infrastructure** | `TESTING-INFRASTRUCTURE-consolidated.md` | T1-15 | Integration testing, DatabaseFixture, TUnit patterns, native DB testing |
| **Advanced Features** | `ADVANCED-FEATURES-consolidated.md` | T1-10 to T1-14 | EF10 patterns, compiled models, logging, Lingua, resilience |

---

## Cross-Reference

**RESEARCH-CONSOLIDATION-MAPPING.md** — Complete mapping showing which original files contributed to each consolidated document.

---

## Implementation Sequence

1. **Start with Entity Design** — Remove Mbid properties, resolve JsonDocument mapping
2. **Then DbContext & Configuration** — Add SourceRecord, register extensions, fix gaps
3. **Then Migrations & Extensions** — Generate initial migration with extensions
4. **Then Data Access & Repositories** — Create 7 repository pairs, delete duplicates
5. **Then State Management** — Migrate StateManager to Data/State/, design ReleaseProgress entity
6. **Then Testing Infrastructure** — Create test project, DatabaseFixture, TUnit patterns
7. **Finally Advanced Features** — Compiled models, logging, Lingua, resilience policies

---

## Key Findings Summary

### Critical Issues (P0)
- SourceRecord entity unmapped — needs configuration
- JsonDocument mapping conflict — remove `mb.Ignore<JsonDocument>()`
- PostgreSQL extensions not registered (unaccent, pg_trgm)
- Duplicate LastFmService files — delete legacy version
- No test project exists

### High Priority (P1)
- 3 Mbid properties to remove (zero external references)
- Missing indexes on Platform, SessionId, TaskName, EntityType
- Missing column types on UploadDate, ReleaseDate, SyncedAt
- 7 repository pairs to create
- DB retry policy missing from DbContext registration

### Medium Priority (P2)
- Compiled models not yet generated
- Logging path needs migration
- Lingua migration from NTextCat
- Infrastructure Resilience duplicate to delete

---

## File Locations

All consolidated files are in:
```
C:\Users\Lance\Dev\Scripts\.kiro\specs\ef-core-10-migration-continuation\research\
```

**Consolidated Files:**
- ENTITY-DESIGN-consolidated.md
- DBCONTEXT-CONFIGURATION-consolidated.md
- MIGRATIONS-EXTENSIONS-consolidated.md
- DATA-ACCESS-REPOSITORIES-consolidated.md
- STATE-MANAGEMENT-consolidated.md
- TESTING-INFRASTRUCTURE-consolidated.md
- ADVANCED-FEATURES-consolidated.md

**Reference:**
- RESEARCH-CONSOLIDATION-MAPPING.md
- README.md (this file)

---

## Usage Tips

1. **For implementation planning:** Start with the concern area most relevant to your current task
2. **For cross-referencing:** Use RESEARCH-CONSOLIDATION-MAPPING.md to find which original research contributed to each topic
3. **For detailed investigation:** Each consolidated file is self-contained with all relevant information, file paths, and recommendations
4. **For task creation:** Use the "Summary of Required Changes" sections in each consolidated file as the basis for implementation tasks

---

## Consolidation Statistics

| Metric | Value |
|--------|-------|
| Original research files | 31 |
| Consolidated files | 7 |
| Mapping document | 1 |
| Total files remaining | 9 |
| Reduction | 71% fewer files |
| Phases covered | T1-02 through T1-15 |
| Concern areas | 7 |

---

## Next Steps

1. Review consolidated files for accuracy and completeness
2. Use as basis for implementation planning and task creation
3. Execute phases in recommended sequence
4. Reference RESEARCH-CONSOLIDATION-MAPPING.md for detailed source attribution
