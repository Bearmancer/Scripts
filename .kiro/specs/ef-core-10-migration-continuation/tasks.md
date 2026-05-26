# EF Core 10 Migration Continuation — Task List

**Date:** 2026-05-25  
**Status:** ✅ Spec Created  
**Total Tasks:** 45 (across 4 tiers)

---

## Tier 1: EF Foundation (16 tasks)

See [Tier 1 Plan](../../plans/tier-1-ef-migration/INDEX.md)

- [x] T1-00: Environment Preflight
- [x] T1-01: Entity Extraction
- [-] T1-02: Entity Refactoring
- [~] T1-03: DbContext Configuration
- [~] T1-04: Entity Configurations
- [~] T1-05: Database Migrations
- [~] T1-06: Repository Pattern
- [~] T1-07: State Manager Migration
- [~] T1-08: Release Cache Migration
- [~] T1-09: Sync Service Updates
- [~] T1-10: EF10 Query Upgrades
- [~] T1-11: Compiled Model
- [~] T1-12: Logging Relocation
- [~] T1-13: Lingua Migration
- [~] T1-14: Resilience Policies
- [~] T1-15: Testcontainers
- [~] T1-16: Sign-off

**Success Criteria:**
- 150+ tests passing (100% pass rate)
- All 16 phases complete
- Zero build warnings
- Full E2E workflow validated

---

## Tier 2: Modularization (11 tasks)

See [Tier 2 Plan](../../plans/tier-2-cpm-split/INDEX.md)

- [~] T2-00: CPM Foundation
- [~] T2-01: Scripts.Core
- [~] T2-02: Scripts.Data
- [~] T2-03: Scripts.Services.Language
- [~] T2-04: Scripts.Services.Music
- [~] T2-05: Scripts.Orchestrators
- [~] T2-06: Scripts.Reader
- [~] T2-07: Scripts.CLI
- [~] T2-08: Scripts.Tests
- [~] T2-09: Duplicate Cleanup
- [~] T2-10: Sign-off

**Success Criteria:**
- 200+ tests passing (100% pass rate)
- All 8 projects created and wired
- Full solution builds clean
- Dependency flow correct (inward only)

---

## Tier 3: Domain Isolation (8 tasks)

See [Tier 3 Plan](../../plans/tier-3-domain/INDEX.md)

- [~] T3-00: Reader Domain Isolation
- [~] T3-01: Music Domain Isolation
- [~] T3-02: Language Domain Isolation
- [~] T3-03: Sync Domain Isolation
- [~] T3-04: Naming Refactor
- [~] T3-05: DateTimeOffset Migration
- [~] T3-06: Inspection Fixes Logic
- [~] T3-07: Sign-off

**Success Criteria:**
- All domain boundaries verified
- No cross-domain dependencies
- Naming conventions consistent
- All tests passing

---

## Tier 4: Hardening (9 tasks)

See [Tier 4 Plan](../../plans/tier-4-hardening/INDEX.md)

- [~] T4-00: DI Container Wiring
- [~] T4-01: E2E Testing
- [~] T4-02: Inspection Fixes Structural
- [~] T4-03: Reader Directory Restructure
- [~] T4-04: Security Audit
- [~] T4-05: Tooling Cleanup
- [~] T4-06: Documentation
- [~] T4-07: OCI Deployment
- [~] T4-08: Sign-off

**Success Criteria:**
- All 4 tiers complete and signed off
- 250+ tests passing (100% pass rate)
- Zero build warnings
- Zero security issues
- Release-ready verification complete

---

## Overall Success Criteria

- ✅ 250+ tests passing (100% pass rate)
- ✅ All 4 tiers complete and signed off
- ✅ Zero build warnings
- ✅ Full E2E workflow validated
- ✅ Zero security issues
- ✅ Release-ready verification complete

---

## See Also

- [Tier Plans](../../plans/INDEX.md)
- [Research](research/README.md)
- [Requirements](requirements.md)
- [Design](design.md)
