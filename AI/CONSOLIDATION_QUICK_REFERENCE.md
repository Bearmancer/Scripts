# Consolidation Quick Reference Card

**Date:** 2026-05-25  
**Purpose:** One-page reference for consolidation effort  
**Audience:** Anyone executing the consolidation plan

---

## The Problem (In 30 Seconds)

| Issue | Impact | Fix |
|-------|--------|-----|
| Plans in `AI/plans/` but AGENTS.md references wrong location | Confusion about where to find plans | Update AGENTS.md §10 |
| Spec directory doesn't exist | Research mapping references non-existent directory | Create `.kiro/specs/ef-core-10-migration-continuation/` |
| 4 diagnostic files with no clear ownership | Status tracking fragmented | Consolidate into `CURRENT_STATUS.md` |
| 31 original research files not archived | Clutter in `AI/research/` | Archive to `AI/research/archive/` |
| Research and plans in separate silos | No cross-references | Add research references to tier plans |

---

## The Solution (In 3 Phases)

### Phase 1: P0 — Critical (2-3 hours)

```powershell
# 1. Create spec directory structure
mkdir C:\Users\Lance\Dev\Scripts\.kiro\specs\ef-core-10-migration-continuation
mkdir C:\Users\Lance\Dev\Scripts\.kiro\specs\ef-core-10-migration-continuation\research

# 2. Create .config.kiro (see CONSOLIDATION_ACTION_PLAN.md for content)
# 3. Create requirements.md, design.md, tasks.md stubs
# 4. Copy research files to spec/research/
# 5. Create CURRENT_STATUS.md
# 6. Update AGENTS.md §10 and add §11
```

### Phase 2: P1 — High Priority (3-4 hours)

```powershell
# 1. Add research references to all 45 tier plans
# 2. Create AI/research/archive/
# 3. Move 31 original research files to archive
# 4. Create AI/plans/archive/
# 5. Move old diagnostic files to archive
# 6. Standardize headers on all 45 tier plans
```

### Phase 3: P2 — Medium Priority (1-2 hours)

```powershell
# 1. Create CROSS_REFERENCE_INDEX.md
# 2. Verify tier plan completeness
# 3. Document any missing sections
```

---

## Key Files to Create/Modify

### Create (New Files)

| File | Purpose | Phase |
|------|---------|-------|
| `.kiro/specs/ef-core-10-migration-continuation/.config.kiro` | Spec metadata | P0 |
| `.kiro/specs/ef-core-10-migration-continuation/requirements.md` | Requirements stub | P0 |
| `.kiro/specs/ef-core-10-migration-continuation/design.md` | Design stub | P0 |
| `.kiro/specs/ef-core-10-migration-continuation/tasks.md` | Tasks stub | P0 |
| `AI/plans/CURRENT_STATUS.md` | Consolidated status | P0 |
| `AI/CROSS_REFERENCE_INDEX.md` | Cross-reference map | P2 |

### Modify (Existing Files)

| File | Change | Phase |
|------|--------|-------|
| `AGENTS.md` | Update §10, add §11 | P0 |
| `AI/plans/tier-*/` (45 files) | Add research references | P1 |
| `AI/plans/tier-*/` (45 files) | Standardize headers | P1 |
| `AI/research/README.md` | Add archive reference | P1 |

### Archive (Move to archive/)

| File | Destination | Phase |
|------|-------------|-------|
| `AI/research/20260*.md` (31 files) | `AI/research/archive/` | P1 |
| `AI/research/angle-*.md` (5 files) | `AI/research/archive/` | P1 |
| `AI/plans/DIAGNOSTIC_REPORT.md` | `AI/plans/archive/` | P1 |
| `AI/plans/DEBUGGING_SUMMARY.md` | `AI/plans/archive/` | P1 |
| `AI/plans/MIGRATION_TO_NATIVE_TESTING.md` | `AI/plans/archive/` | P1 |
| `AI/plans/QUICK_WINS_IMPLEMENTATION.md` | `AI/plans/archive/` | P1 |

---

## Execution Checklist

### Phase 1: P0 (2-3 hours)

- [ ] Create `.kiro/specs/ef-core-10-migration-continuation/` directory
- [ ] Create `.config.kiro` (copy from CONSOLIDATION_ACTION_PLAN.md)
- [ ] Create `requirements.md` stub (copy from CONSOLIDATION_ACTION_PLAN.md)
- [ ] Create `design.md` stub (copy from CONSOLIDATION_ACTION_PLAN.md)
- [ ] Create `tasks.md` stub (copy from CONSOLIDATION_ACTION_PLAN.md)
- [ ] Copy 7 consolidated research files to `spec/research/`
- [ ] Copy `RESEARCH-CONSOLIDATION-MAPPING.md` to `spec/research/`
- [ ] Copy `README.md` to `spec/research/`
- [ ] Create `AI/plans/CURRENT_STATUS.md` (copy from CONSOLIDATION_ACTION_PLAN.md)
- [ ] Update `AGENTS.md` §10 (change reference to `AI/plans/`)
- [ ] Add `AGENTS.md` §11 (add "Current Status" section)

### Phase 2: P1 (3-4 hours)

- [ ] Add research references to all 45 tier plans
- [ ] Create `AI/research/archive/` directory
- [ ] Move 31 original research files to archive
- [ ] Move 5 angle-*.md files to archive
- [ ] Update `AI/research/README.md` (add archive reference)
- [ ] Create `AI/plans/archive/` directory
- [ ] Move `DIAGNOSTIC_REPORT.md` to archive
- [ ] Move `DEBUGGING_SUMMARY.md` to archive
- [ ] Move `MIGRATION_TO_NATIVE_TESTING.md` to archive
- [ ] Move `QUICK_WINS_IMPLEMENTATION.md` to archive
- [ ] Standardize headers on all 45 tier plans

### Phase 3: P2 (1-2 hours)

- [ ] Create `AI/CROSS_REFERENCE_INDEX.md` (copy from CONSOLIDATION_ACTION_PLAN.md)
- [ ] Run tier plan completeness verification script
- [ ] Document any missing sections
- [ ] Verify all cross-references work

---

## Success Criteria

✅ **Consolidation is successful when:**

- [ ] No contradictions between plans, research, and AGENTS.md
- [ ] All research files consolidated (7 + 1 mapping in spec/research/)
- [ ] All diagnostic files consolidated (1 CURRENT_STATUS.md)
- [ ] Spec directory created with metadata and stubs
- [ ] Cross-references established between plans and research
- [ ] All tier plans standardized and complete
- [ ] Zero sprawl (no duplicate files, no orphaned artifacts)
- [ ] All cross-references verified to work

---

## Common Tasks

### Add Research Reference to Tier Plan

**Add this section after "Prerequisites":**

```markdown
## Research Reference

This phase is informed by the following research documents:

- [ENTITY-DESIGN-consolidated.md](../../research/ENTITY-DESIGN-consolidated.md)
- [DBCONTEXT-CONFIGURATION-consolidated.md](../../research/DBCONTEXT-CONFIGURATION-consolidated.md)
- [MIGRATIONS-EXTENSIONS-consolidated.md](../../research/MIGRATIONS-EXTENSIONS-consolidated.md)
- [DATA-ACCESS-REPOSITORIES-consolidated.md](../../research/DATA-ACCESS-REPOSITORIES-consolidated.md)
- [STATE-MANAGEMENT-consolidated.md](../../research/STATE-MANAGEMENT-consolidated.md)
- [TESTING-INFRASTRUCTURE-consolidated.md](../../research/TESTING-INFRASTRUCTURE-consolidated.md)
- [ADVANCED-FEATURES-consolidated.md](../../research/ADVANCED-FEATURES-consolidated.md)
```

### Standardize Tier Plan Header

**Replace existing header with:**

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

### Move Files to Archive

```powershell
# Create archive directory
New-Item -ItemType Directory -Path "C:\Users\Lance\Dev\Scripts\AI\research\archive" -Force | Out-Null

# Move files
Get-ChildItem -Path "C:\Users\Lance\Dev\Scripts\AI\research" -Filter "20260*.md" | ForEach-Object {
    Move-Item -Path $_.FullName -Destination "C:\Users\Lance\Dev\Scripts\AI\research\archive" -Force
}
```

---

## Troubleshooting

### Issue: Cross-references don't work

**Solution:** Verify relative paths are correct
- From tier plan: `../../research/ENTITY-DESIGN-consolidated.md`
- From spec/research: `../../../plans/tier-1-ef-migration/00-environment.md`

### Issue: Spec directory not recognized by Kiro

**Solution:** Verify `.config.kiro` exists and is valid JSON
```powershell
Test-Path "C:\Users\Lance\Dev\Scripts\.kiro\specs\ef-core-10-migration-continuation\.config.kiro"
```

### Issue: Old diagnostic files still referenced

**Solution:** Search for references and update
```powershell
grep -r "DIAGNOSTIC_REPORT" C:\Users\Lance\Dev\Scripts\AI\
```

---

## Timeline

| Phase | Duration | Start | End |
|-------|----------|-------|-----|
| P0 | 2-3 hours | Today | Today |
| P1 | 3-4 hours | Tomorrow | Tomorrow |
| P2 | 1-2 hours | Next day | Next day |
| **Total** | **6-8 hours** | **Today** | **In 3 days** |

---

## Resources

| Document | Purpose |
|----------|---------|
| `CONSOLIDATION_AUDIT_REPORT.md` | Detailed audit of all issues |
| `CONSOLIDATION_ACTION_PLAN.md` | Step-by-step execution plan |
| `CONSOLIDATION_SUMMARY.md` | High-level overview |
| `CONSOLIDATION_QUICK_REFERENCE.md` | This file (one-page reference) |

---

## Key Contacts

For questions about:
- **Audit findings:** See CONSOLIDATION_AUDIT_REPORT.md
- **Execution steps:** See CONSOLIDATION_ACTION_PLAN.md
- **High-level overview:** See CONSOLIDATION_SUMMARY.md
- **Quick reference:** See CONSOLIDATION_QUICK_REFERENCE.md (this file)

---

## Final Notes

- ✅ All file operations use absolute paths
- ✅ Create `.bak.YYYYMMDD_HHmmss` backups before moving files
- ✅ Verify all cross-references work after consolidation
- ✅ Test that Kiro spec workflow can read new spec directory
- ✅ Update this plan as issues are discovered during execution

**Status:** Ready for Execution  
**Next Action:** Start Phase 1 (P0) — Create spec directory structure

---

**Generated:** 2026-05-25  
**Last Updated:** 2026-05-25  
**Version:** 1.0
