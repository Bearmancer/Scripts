# EF Core 10 Migration Continuation — Requirements

**Date:** 2026-05-25  
**Status:** ✅ Spec Created  
**Tiers:** 4 (45 sequenced phases)

---

## Overview

Migrate the Scripts repository from a monolithic architecture to a modular, PostgreSQL-backed system using EF Core 10 LTS (Nov 2025 – Nov 2028).

---

## Tier 1: EF Core 10 Foundation (Phases 00-16)

**Goal:** Establish PostgreSQL connectivity, extract and configure entities, create repository pattern, migrate state management, implement testing infrastructure.

**Key Deliverables:**
- ScriptsDbContext with 9 entity configurations
- Repository pattern with 7 repository pairs
- StateManager migration to Data/State layer
- Release cache migration from CSV to EF Core
- Testcontainers integration testing infrastructure
- 150+ tests passing (100% pass rate)

**Success Criteria:**
- All 16 phases complete
- 150+ tests passing
- Zero build warnings
- Full E2E workflow validated

---

## Tier 2: Modularization (Phases 00-10)

**Goal:** Split monolithic project into 8 modular projects with CPM (Central Package Management).

**Key Deliverables:**
- Scripts.Core (Serilog, Polly, Google.Apis.Auth)
- Scripts.Data (EF Core, Npgsql, CsvHelper)
- Scripts.Services.Language (Azure Translation, Lingua)
- Scripts.Services.Music (MetaBrainz, Discogs)
- Scripts.Orchestrators (Last.fm, YouTube, Sheets)
- Scripts.Reader (Playwright, AngleSharp, PdfPig)
- Scripts.CLI (Spectre.Console, composition root)
- Scripts.Tests (TUnit, FluentAssertions, Testcontainers)

**Success Criteria:**
- All 8 projects created and wired
- 200+ tests passing
- Full solution builds clean
- Dependency flow correct (inward only)

---

## Tier 3: Domain Isolation (Phases 00-07)

**Goal:** Isolate Reader, Music, Language, and Sync domains; refactor naming conventions; migrate to DateTimeOffset.

**Key Deliverables:**
- Reader domain isolated and tested
- Music domain isolated and tested
- Language domain isolated and tested
- Sync/Orchestrators domain isolated and tested
- Entity naming standardized (suffix convention)
- All timestamps migrated to DateTimeOffset

**Success Criteria:**
- All domain boundaries verified
- No cross-domain dependencies
- Naming conventions consistent
- All tests passing

---

## Tier 4: Hardening (Phases 00-08)

**Goal:** Wire DI container, implement E2E testing, security audit, OCI deployment.

**Key Deliverables:**
- DI container fully wired across all projects
- E2E sync workflow tests
- CancellationToken support throughout
- Security audit complete (Gitleaks, secret redaction)
- Reader directory restructured (Extraction/Local/Output/Quality)
- OCI deployment configured
- Final documentation and onboarding guide

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
- [Current Status](../../plans/CURRENT_STATUS.md)
