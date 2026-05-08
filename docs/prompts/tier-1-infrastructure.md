# Tier 1 — Infrastructure Prompt

> **Plan:** `.kilo/plans/cpm.md` | **Active Task:** `.kilo/prompt/active-task.md`
> **Tasks:** T02 (Directory Normalization), T03 (AGENTS.md Rewrite)
> **Parallel Group:** B — Sequential (T02 → T03)

---

## T02 — Directory Normalization

### Objective

Delete empty root stubs, move skills, remove old plans. `.kilo/` becomes the sole orchestration directory.

### Actions

1. **Delete 6 empty root directories:**
    - `knowledge/`, `prompt/`, `references/`, `research/`, `rules/`, `skills/`
    - Use `Remove-Item` — they're empty stubs

2. **Delete absorbed files:**
    - `.kilo/plans/consolidation-plan.md` — fully absorbed into cpm.md

3. **Verify `.kilo/` structure:**
    ```
    .kilo/
    ├── plans/cpm.md
    ├── rules/standards.md
    ├── knowledge/
    ├── prompt/
    │   ├── active-task.md
    │   ├── tier-0-foundation.md
    │   ├── tier-1-infrastructure.md
    │   ├── tier-2-migration.md
    │   └── tier-3-polish.md
    ├── logs/
    └── Fibery Export/
    ```

### Win Gate

```
Test-Path .kilo/plans/cpm.md
```

Returns `True`. Empty root stubs no longer exist.

---

## T03 — AGENTS.md Rewrite

### Objective

Update `AGENTS.md` to reflect new consolidated structure. Remove stale references.

### Actions

1. **Update directory map** to show:
    - `.kilo/knowledge/verification/` for task verification records
    - `.kilo/prompt/active-task.md` as the active task tracker
    - `.kilo/prompt/tier-*-*.md` for tiered execution prompts

2. **Remove:**
    - All `.clinerules/` references
    - `execution-prompt.md` reference → replace with tiered prompt references
    - Any hook-related sections

3. **Add:**
    - MCP section noting Docker MCP Gateway as canonical MCP manager

### Win Gate

```
Select-String ".clinerules" AGENTS.md
```

Returns 0 matches.

