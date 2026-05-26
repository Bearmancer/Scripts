# Specs Consolidation + # Attachment Debug Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Consolidate research files and plans into specs, debug # file attachment issue inside .kiro, and deduplicate dead artifacts using file-organizer skill with TDD approach.

**Architecture:** Use subagent-driven-development to dispatch focused subagents (max 3 at a time), each working on scoped tasks. Apply file-organizer skill for consolidation, writing-plans for plan structuring, and test-driven-development for validation.

**Tech Stack:** Kiro skills (subagent-driven-development, file-organizer, writing-plans, test-driven-development), PowerShell, file system tools.

---

## Task 1: Debug # Attachment Issue Inside .kiro

**Files:**
- Modify: `c:\Users\Lance\Dev\Scripts\.kiro\settings\*.json` (if exists)
- Research: Kiro documentation and config files

- [ ] **Step 1: Research Kiro # attachment feature**

```markdown
Investigate why #File or #Folder doesn't work inside .kiro directory.
Check:
1. Any .gitignore patterns that might exclude .kiro
2. Kiro settings/configuration for file attachment
3. Any workspace-specific exclusions
```

- [ ] **Step 2: Test # attachment from inside .kiro**

```powershell
# From within .kiro directory context, try to reference a file using # prefix
# Document what error or behavior occurs
```

- [ ] **Step 3: Propose fix if root cause identified**

```markdown
Document findings and proposed solution.
```

---

## Task 2: Consolidate Research Files into Specs (File Organizer)

**Files:**
- Research: `c:\Users\Lance\Dev\Scripts\.kiro\specs\research\*.md`
- Target: `c:\Users\Lance\Dev\Scripts\.kiro\specs\ef-core-10-migration-continuation\research\`

- [ ] **Step 1: List all research files in .kiro/specs/research/**

```powershell
Get-ChildItem -Path "c:\Users\Lance\Dev\Scripts\.kiro\specs\research" -Filter "*.md" | Select-Object Name, Length, LastWriteTime
```

- [ ] **Step 2: Identify duplicates or obsolete research files**

```markdown
Categorize research files:
- Current/Active: Still relevant for current work
- Obsolete: Outdated, superseded by newer research
- Duplicate: Similar content in multiple files
```

- [ ] **Step 3: Move relevant research to ef-core-10-migration-continuation/**

```powershell
# Move research files that are still relevant to the active spec
Move-Item -Path "c:\Users\Lance\Dev\Scripts\.kiro\specs\research\20260522-t1-*.md" -Destination "c:\Users\Lance\Dev\Scripts\.kiro\specs\ef-core-10-migration-continuation\research\" -Force
```

- [ ] **Step 4: Archive or delete obsolete files**

```powershell
# After confirmation, archive or delete obsolete research
```

---

## Task 3: Consolidate Plans into Specs Structure

**Files:**
- Source: `c:\Users\Lance\Dev\Scripts\.kiro\specs\plans\tier-*-*`
- Target: Already in specs/plans - verify structure

- [ ] **Step 1: Verify current plans structure in .kiro/specs/plans/**

```powershell
Get-ChildItem -Path "c:\Users\Lance\Dev\Scripts\.kiro\specs\plans" -Directory | Select-Object Name
```

- [ ] **Step 2: Cross-reference with AGENTS.md expected location (AI/plans/)**

```markdown
AGENTS.md references "AI/plans/INDEX.md" but this directory doesn't exist.
Decision: Keep plans in .kiro/specs/plans/ (current location) or note discrepancy.
```

- [ ] **Step 3: Ensure tier plans follow writing-plans skill format**

```markdown
Each tier plan should have:
- Proper header with required sub-skill reference
- TDD-style task structure
- Checkbox syntax for tracking
```

---

## Task 4: TDD Validation - Test Current State

**Files:**
- Test: `csharp/tests/Scripts.Tests/`

- [ ] **Step 1: Run current test suite**

```powershell
cd c:\Users\Lance\Dev\Scripts
dotnet test csharp/Scripts.slnx --no-build --verbosity minimal 2>&1 | Select-String -Pattern "Passed|Failed|Total"
```

- [ ] **Step 2: Document test results**

```markdown
Capture:
- Total tests
- Passed/Failed count
- Failure categories from diagnostic report
```

- [ ] **Step 3: Verify EF Core 10 migration progress**

```markdown
Based on DEBUGGING_SUMMARY.md:
1. Testcontainers Lifecycle - Need verification
2. Compiled Model Lock - Need verification  
3. JsonDocument NullReference - Need verification
4. PendingModelChangesWarning - Need verification
```

---

## Task 5: Deduplicate and Clean Dead Artifacts

**Files:**
- Scan: `c:\Users\Lance\Dev\Scripts\.kiro\specs\`
- Archive: `c:\Users\Lance\Dev\Scripts\.kiro\specs\plans\archive\`

- [ ] **Step 1: Identify duplicate files across specs directories**

```powershell
# Find files with similar names or content
Get-ChildItem -Path "c:\Users\Lance\Dev\Scripts\.kiro\specs" -Recurse -Filter "*.md" | Group-Object Name | Where-Object { $_.Count -gt 1 }
```

- [ ] **Step 2: Identify dead artifacts (empty folders, old backups)**

```powershell
# Find empty directories
Get-ChildItem -Path "c:\Users\Lance\Dev\Scripts\.kiro\specs" -Recurse -Directory | Where-Object { (Get-ChildItem $_.FullName -Force).Count -eq 0 }
```

- [ ] **Step 3: Create archive for deduplicated files**

```powershell
New-Item -ItemType Directory -Path "c:\Users\Lance\Dev\Scripts\.kiro\specs\plans\archive" -Force
```

- [ ] **Step 4: Move duplicates to archive (after confirmation)**

```powershell
# Move with .bak timestamp
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
```

---

## Execution Notes

**Subagent Deployment Strategy:**
- Deploy max 3 subagents at a time using subagent-driven-development
- Give each subagent only 1-2 files to work with at a time
- Use test-driven-development skill for validation steps
- Use file-organizer skill for consolidation tasks

**Priority Order:**
1. Debug # attachment issue (quick investigation)
2. TDD validation - verify current test state
3. Consolidate research files
4. Verify/consolidate plans
5. Deduplicate dead artifacts

**Success Criteria:**
- [ ] # attachment issue documented with root cause
- [ ] Research files consolidated into ef-core-10-migration-continuation
- [ ] Plans structure verified against writing-plans skill
- [ ] Current test state documented (pass/fail counts)
- [ ] Duplicate/obsolete artifacts archived