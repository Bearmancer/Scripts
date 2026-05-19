# Phase 2: Repo Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Assert that IDE directories `.vscode` and `.idea` are clean and deleted from the repository.

**Architecture:** PowerShell script assertions.

**Tech Stack:** PowerShell

---

### Task 2.1: Assert `.vscode/` absent from root

**Files:**
- Create: `.kilo/tests/AssertNoVscode.ps1`

- [ ] **Step 1: Write the PowerShell assertion script**

Create `.kilo/tests/AssertNoVscode.ps1` with the following content:
```powershell
$ErrorActionPreference = 'Stop'
if (Test-Path "C:\Users\Lance\Dev\Scripts\.vscode") {
    throw ".vscode/ directory exists!"
}
Write-Output "PASS"
```

- [ ] **Step 2: Run the script to verify it passes**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\AssertNoVscode.ps1`
Expected: `PASS`

- [ ] **Step 3: Commit**

```bash
git add .kilo/tests/AssertNoVscode.ps1
git commit -m "test: assert .vscode/ absent from root"
```

---

### Task 2.2: Assert `.idea/` absent from root

**Files:**
- Create: `.kilo/tests/AssertNoIdea.ps1`

- [ ] **Step 1: Write the PowerShell assertion script**

Create `.kilo/tests/AssertNoIdea.ps1` with the following content:
```powershell
$ErrorActionPreference = 'Stop'
if (Test-Path "C:\Users\Lance\Dev\Scripts\.idea") {
    throw ".idea/ directory exists!"
}
Write-Output "PASS"
```

- [ ] **Step 2: Run the script to verify it passes**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\AssertNoIdea.ps1`
Expected: `PASS`

- [ ] **Step 3: Commit**

```bash
git add .kilo/tests/AssertNoIdea.ps1
git commit -m "test: assert .idea/ absent from root"
```
