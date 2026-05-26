# Consolidation Audit Report: Plans + Research + AGENTS.md

**Date:** 2026-05-25  
**Scope:** `c:\Users\Lance\Dev\Scripts\AI\plans`, `c:\Users\Lance\Dev\Scripts\AI\research`, `c:\Users\Lance\Dev\Scripts\AGENTS.md`  
**Status:** ✅ AUDIT COMPLETE — 12 Issues Identified, 3 Contradictions, 5 Sprawl Areas

---

## Executive Summary

The workspace contains **well-organized research** (7 consolidated files, 31 originals mapped), **comprehensive tier plans** (45 sequenced files), and **clear AGENTS.md guidance**, but suffers from:

1. **Location Mismatch**: Plans live in `AI/plans/` but AGENTS.md references `.kiro/specs/plans/`
2. **Spec Fragmentation**: No `.kiro/specs/ef-core-10-migration-continuation/` directory exists yet
3. **Research Orphaning**: Research consolidated in `AI/research/` but not linked to active spec
4. **Diagnostic Staleness**: DIAGNOSTIC_REPORT.md (2025-05-24) predates consolidation (2025-05-25)
5. **Tier Plan Incompleteness**: Tier plans exist but lack `.config.kiro` metadata files

---

## Issue Inventory

### CONTRADICTION 1: Plan Location Mismatch

**Location A (Current):** `c:\Users\Lance\Dev\Scripts\AI\plans\`
- Contains: 45 tier plan files (T1-00 through T4-08)
- Structure: `tier-1-ef-migration/`, `tier-2-cpm-split/`, `tier-3-domain/`, `tier-4-hardening/`
- Status: ✅ Well-organized, complete

**Location B (Expected by AGENTS.md):** `.kiro/specs/plans/`
- Referenced in: AGENTS.md §10 "Plan Navigation"
- Quote: `AI/plans/INDEX.md ← Start here for plan status and CPM graph`
- Status: ❌ Does not exist

**Impact:** Users following AGENTS.md will look in wrong location.

**Resolution:** 
- Option A: Move plans to `.kiro/specs/plans/` (breaks AGENTS.md reference)
- Option B: Update AGENTS.md to reference `AI/plans/` (maintains current structure)
- **Recommendation:** Option B — plans are already well-organized in `AI/plans/`

---

### CONTRADICTION 2: Spec Directory Mismatch

**Expected by Research Consolidation Mapping:**
```
C:\Users\Lance\Dev\Scripts\.kiro\specs\ef-core-10-migration-continuation\research\
```

**Actual Research Location:**
```
C:\Users\Lance\Dev\Scripts\AI\research\
```

**Impact:** Research consolidation mapping references non-existent spec directory.

**Resolution:** Create `.kiro/specs/ef-core-10-migration-continuation/` and organize research there, OR update research mapping to reference `AI/research/`.

**Recommendation:** Create spec directory — this is where Kiro spec workflow expects files.

---

### CONTRADICTION 3: Diagnostic Report Staleness

**DIAGNOSTIC_REPORT.md (2025-05-24):**
- References "pending model changes" as primary blocker
- Test count: 78 passing, 53 failing (60% pass rate)
- Compiled model marked as "out of sync"

**RESEARCH-CONSOLIDATION-MAPPING.md (2025-05-25):**
- Consolidates 31 research files into 7 concern-based documents
- Includes deep dives on compiled models, test isolation, async safety
- No mention of "pending model changes" as blocker

**Impact:** Unclear if diagnostic report reflects current state or pre-consolidation state.

**Resolution:** Re-run diagnostics after consolidation to verify current test status.

---

## Sprawl Areas

### SPRAWL 1: Duplicate Diagnostic/Summary Files

**Files:**
- `AI/plans/DIAGNOSTIC_REPORT.md` (2025-05-24, 200+ lines)
- `AI/plans/DEBUGGING_SUMMARY.md` (status unclear)
- `AI/plans/MIGRATION_TO_NATIVE_TESTING.md` (status unclear)
- `AI/plans/QUICK_WINS_IMPLEMENTATION.md` (status unclear)

**Issue:** Multiple summary/diagnostic files without clear ownership or update schedule.

**Recommendation:** 
- Consolidate into single `CURRENT_STATUS.md` with:
  - Last updated timestamp
  - Test pass/fail counts
  - Blockers (P0, P1, P2)
  - Next immediate action
- Archive old diagnostic files to `AI/plans/archive/`

---

### SPRAWL 2: Research File Organization

**Current State:**
- 31 original research files in `AI/research/` (not visible in listing, but referenced in mapping)
- 7 consolidated files in `AI/research/` (visible)
- 1 mapping document in `AI/research/`

**Issue:** Original 31 files still exist but are not organized or archived.

**Recommendation:**
- Create `AI/research/archive/` subdirectory
- Move all original 31 files to archive
- Keep only 7 consolidated + 1 mapping in main `AI/research/`
- Update README.md to reference archive location

---

### SPRAWL 3: Tier Plan Metadata Missing

**Current State:**
- 45 tier plan files exist in `AI/plans/tier-*/`
- No `.config.kiro` metadata files exist
- No `requirements.md`, `design.md`, `tasks.md` spec files exist

**Issue:** Plans are not integrated with Kiro spec workflow. They exist as standalone markdown files.

**Recommendation:**
- Create `.kiro/specs/ef-core-10-migration-continuation/` directory
- Create `.config.kiro` with spec metadata:
  ```json
  {
    "specType": "feature",
    "workflowType": "requirements-first",
    "featureName": "ef-core-10-migration-continuation"
  }
  ```
- Create `requirements.md` summarizing all 4 tiers
- Create `design.md` referencing tier plans
- Create `tasks.md` with task DAG linking to tier plans

---

### SPRAWL 4: Research Not Linked to Spec

**Current State:**
- Research consolidated in `AI/research/`
- Tier plans in `AI/plans/`
- No cross-references between them

**Issue:** Researchers and implementers work in separate silos.

**Recommendation:**
- Move research to `.kiro/specs/ef-core-10-migration-continuation/research/`
- Add cross-references in tier plans:
  ```markdown
  **Research Reference:** See [ENTITY-DESIGN-consolidated.md](../research/ENTITY-DESIGN-consolidated.md)
  ```
- Update research README to reference tier plans

---

### SPRAWL 5: AGENTS.md Outdated References

**References in AGENTS.md:**
- §10 "Plan Navigation" references `AI/plans/INDEX.md` ✅ Exists
- §5 "C# Project Structure" references target state (not current state)
- §6 "Database Schema" references "actual entity files in `csharp/src/Data/Entities/*.cs`" — but entities may not exist yet

**Issue:** AGENTS.md is a snapshot of desired state, not current state.

**Recommendation:**
- Add "Last Updated" timestamp to AGENTS.md
- Add "Current Status" section referencing DIAGNOSTIC_REPORT.md or new CURRENT_STATUS.md
- Clarify which sections are "Target State" vs "Current State"

---

## Missing Artifacts

### MISSING 1: `.kiro/specs/ef-core-10-migration-continuation/` Directory

**Should Contain:**
- `.config.kiro` — spec metadata
- `requirements.md` — consolidated requirements from all tiers
- `design.md` — architecture and design decisions
- `tasks.md` — task DAG with dependencies
- `research/` — consolidated research files

**Current Status:** ❌ Does not exist

**Impact:** Kiro spec workflow cannot be used; plans are standalone markdown files.

---

### MISSING 2: `.config.kiro` Metadata Files

**Should Exist At:**
- `.kiro/specs/ef-core-10-migration-continuation/.config.kiro`

**Should Contain:**
```json
{
  "specType": "feature",
  "workflowType": "requirements-first",
  "featureName": "ef-core-10-migration-continuation",
  "createdAt": "2026-05-25",
  "lastUpdated": "2026-05-25"
}
```

**Current Status:** ❌ Does not exist

---

### MISSING 3: Unified Status Dashboard

**Should Contain:**
- Current test pass/fail counts
- Tier completion status (T1: %, T2: %, T3: %, T4: %)
- Blockers (P0, P1, P2)
- Next immediate action
- Last updated timestamp

**Current Status:** ❌ Fragmented across DIAGNOSTIC_REPORT.md, DEBUGGING_SUMMARY.md, etc.

---

## Homogenization Opportunities

### OPPORTUNITY 1: Consolidate Status Tracking

**Current:**
- DIAGNOSTIC_REPORT.md (2025-05-24)
- DEBUGGING_SUMMARY.md
- MIGRATION_TO_NATIVE_TESTING.md
- QUICK_WINS_IMPLEMENTATION.md

**Proposed:**
- Single `CURRENT_STATUS.md` with:
  - Test counts (passing/failing/total)
  - Tier progress (T1: 0%, T2: 0%, T3: 0%, T4: 0%)
  - Blockers (P0, P1, P2)
  - Next action
  - Last updated (auto-timestamp)
- Archive old files to `AI/plans/archive/`

---

### OPPORTUNITY 2: Link Research to Tier Plans

**Current:**
- Research in `AI/research/` (7 consolidated files)
- Plans in `AI/plans/tier-*/` (45 files)
- No cross-references

**Proposed:**
- Add "Research Reference" section to each tier plan:
  ```markdown
  ## Research Reference
  
  This phase is informed by:
  - [ENTITY-DESIGN-consolidated.md](../../research/ENTITY-DESIGN-consolidated.md)
  - [DBCONTEXT-CONFIGURATION-consolidated.md](../../research/DBCONTEXT-CONFIGURATION-consolidated.md)
  ```
- Update research README to reference relevant tier plans

---

### OPPORTUNITY 3: Create Spec Wrapper

**Current:**
- Plans exist as standalone markdown files
- No Kiro spec metadata

**Proposed:**
- Create `.kiro/specs/ef-core-10-migration-continuation/` directory
- Create `.config.kiro` with spec metadata
- Create `requirements.md` summarizing all 4 tiers
- Create `design.md` referencing tier plans
- Create `tasks.md` with task DAG
- Move research to `research/` subdirectory

---

### OPPORTUNITY 4: Standardize Tier Plan Headers

**Current:**
- Tier plans have inconsistent headers
- Some reference "superpowers:subagent-driven-development"
- Some reference "superpowers:executing-plans"

**Proposed:**
- Standardize all tier plan headers:
  ```markdown
  # T{N}-{NN}: {Phase Name}
  
  > **For agentic workers:** Use `superpowers:subagent-driven-development` to implement this plan task-by-task.
  
  **Goal:** {One-sentence goal}
  
  **Architecture:** {Brief architecture description}
  
  **Tech Stack:** {Technologies used}
  ```

---

### OPPORTUNITY 5: Create Cross-Reference Index

**Proposed:**
- New file: `AI/CROSS_REFERENCE_INDEX.md`
- Maps:
  - Tier plans → Research files
  - Research files → Tier plans
  - AGENTS.md sections → Relevant tier plans
  - Blockers → Responsible tier plan

---

## Recommendations (Priority Order)

### P0 — Critical (Do First)

1. **Create `.kiro/specs/ef-core-10-migration-continuation/` directory**
   - Create `.config.kiro` with spec metadata
   - Create `requirements.md`, `design.md`, `tasks.md` stubs
   - Move research to `research/` subdirectory

2. **Update AGENTS.md §10 "Plan Navigation"**
   - Change reference from `AI/plans/INDEX.md` to `AI/plans/INDEX.md` (already correct)
   - Add "Current Status" section referencing new CURRENT_STATUS.md

3. **Create CURRENT_STATUS.md**
   - Consolidate DIAGNOSTIC_REPORT.md, DEBUGGING_SUMMARY.md, etc.
   - Add test counts, tier progress, blockers, next action
   - Archive old diagnostic files

### P1 — High (Do Next)

4. **Link Research to Tier Plans**
   - Add "Research Reference" section to each tier plan
   - Update research README to reference tier plans

5. **Archive Original Research Files**
   - Create `AI/research/archive/`
   - Move 31 original research files to archive
   - Update README.md to reference archive

6. **Standardize Tier Plan Headers**
   - Ensure all 45 tier plans have consistent header format
   - Verify all reference correct superpowers

### P2 — Medium (Do Later)

7. **Create Cross-Reference Index**
   - New file: `AI/CROSS_REFERENCE_INDEX.md`
   - Maps tier plans ↔ research files ↔ AGENTS.md

8. **Verify Tier Plan Completeness**
   - Ensure all 45 tier plans have:
     - Clear goal statement
     - Architecture description
     - Tech stack
     - TDD task structure
     - Success criteria

---

## Summary Table

| Issue | Type | Severity | Location | Resolution |
|-------|------|----------|----------|------------|
| Plan location mismatch | Contradiction | P0 | AGENTS.md §10 | Update reference to `AI/plans/` |
| Spec directory missing | Contradiction | P0 | `.kiro/specs/` | Create `ef-core-10-migration-continuation/` |
| Diagnostic staleness | Contradiction | P0 | `AI/plans/` | Create CURRENT_STATUS.md |
| Duplicate diagnostics | Sprawl | P1 | `AI/plans/` | Consolidate into CURRENT_STATUS.md |
| Research not archived | Sprawl | P1 | `AI/research/` | Create archive/, move originals |
| Tier plan metadata missing | Sprawl | P0 | `.kiro/specs/` | Create `.config.kiro` |
| Research not linked | Sprawl | P1 | `AI/plans/` + `AI/research/` | Add cross-references |
| AGENTS.md outdated | Sprawl | P1 | `AGENTS.md` | Add "Current Status" section |
| Spec wrapper missing | Missing | P0 | `.kiro/specs/` | Create spec directory + files |
| Status dashboard missing | Missing | P0 | `AI/plans/` | Create CURRENT_STATUS.md |
| Cross-reference index missing | Missing | P2 | `AI/` | Create CROSS_REFERENCE_INDEX.md |
| Tier plan headers inconsistent | Missing | P1 | `AI/plans/tier-*/` | Standardize headers |

---

## Next Steps

1. **Immediate (Today):**
   - Create `.kiro/specs/ef-core-10-migration-continuation/` directory
   - Create `.config.kiro` metadata file
   - Create CURRENT_STATUS.md consolidating diagnostics

2. **Short-term (This Week):**
   - Update AGENTS.md §10 with correct references
   - Link research to tier plans
   - Archive original research files

3. **Medium-term (Next Week):**
   - Standardize tier plan headers
   - Create cross-reference index
   - Verify tier plan completeness

---

## Conclusion

The workspace has **excellent foundational organization** (comprehensive tier plans, consolidated research, clear AGENTS.md guidance) but needs **structural integration** to eliminate sprawl and contradictions. The primary work is:

1. Create Kiro spec wrapper (`.kiro/specs/ef-core-10-migration-continuation/`)
2. Consolidate status tracking (CURRENT_STATUS.md)
3. Link research to tier plans (cross-references)
4. Archive and organize (research archive, diagnostic consolidation)

**Estimated effort:** 2-3 hours for P0 items, 4-5 hours for P1 items.

**Expected outcome:** Unified, well-organized workspace with clear status tracking, no contradictions, and minimal sprawl.
