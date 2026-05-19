# Phase 14: Final Verification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Perform absolute final validation of build correctness, unit tests, integration tests, and push rewritten changes to the origin remote. Verify .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 stack.

**Architecture:** PowerShell script assertions.

**Tech Stack:** PowerShell, Git, dotnet CLI

---

### Task 14.1: Full solution build

**Files:**
- Create validation script: `.kilo/tests/VerifyFinalBuild.ps1`

- [ ] **Step 1: Write validation script**

Create `.kilo/tests/VerifyFinalBuild.ps1`:
```powershell
$ErrorActionPreference = 'Stop'
Set-Location "C:\Users\Lance\Dev\Scripts"
dotnet restore "csharp/CSharpScripts.csproj"
dotnet build "csharp/CSharpScripts.csproj" --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "FINAL_BUILD_FAILED"
}
Write-Output "BUILD_PASS"
```

- [ ] **Step 2: Run build script**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\VerifyFinalBuild.ps1`
Expected: `BUILD_PASS`

- [ ] **Step 3: Commit**

```bash
git add .kilo/tests/VerifyFinalBuild.ps1
git commit -m "test: add final build verification script"
```

---

### Task 14.2: Full test run with 100% pass rate

**Files:**
- Create validation script: `.kilo/tests/VerifyFinalTests.ps1`

- [ ] **Step 1: Write test runner script**

Create `.kilo/tests/VerifyFinalTests.ps1`:
```powershell
$ErrorActionPreference = 'Stop'
Set-Location "C:\Users\Lance\Dev\Scripts"
dotnet test "csharp/src/Tests/CSharpScripts.Tests.csproj" --verbosity normal
if ($LASTEXITCODE -ne 0) {
    throw "FINAL_TESTS_FAILED"
}
Write-Output "ALL_TESTS_PASS"
```

- [ ] **Step 2: Run test runner script**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\VerifyFinalTests.ps1`
Expected: `ALL_TESTS_PASS`

- [ ] **Step 3: Commit**

```bash
git add .kilo/tests/VerifyFinalTests.ps1
git commit -m "test: add final test suite verification script"
```

---

### Task 14.3: Verify .NET 10 / EF Core 10 stack assertions

- [ ] **Step 1: Assert `dotnet --version` reports 10.x.x**
- [ ] **Step 2: Assert no `net11.0` strings remain in any `.csproj` or `.props` file**
- [ ] **Step 3: Assert all tests pass**

---

### Task 14.4: Final push to GitHub remote

- [ ] **Step 1: Re-add origin remote if it was dropped during history filters**

Run: `git remote add origin https://github.com/Bearmancer/Scripts.git`

- [ ] **Step 2: Force-push rewritten history**

Run: `git push -u origin main --force`
Expected: Push succeeds.
