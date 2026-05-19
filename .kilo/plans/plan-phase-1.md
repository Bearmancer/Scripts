# Phase 1: Test Infrastructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish and verify the baseline C# test environment.

**Architecture:** Use PowerShell scripts in `.kilo/tests` to run `dotnet restore`, `dotnet build`, and `dotnet test` with strict error actions and exit code checks.

**Tech Stack:** PowerShell, dotnet SDK

---

### Task 1.1: Verify test project builds

**Files:**
- Create: `.kilo/tests/VerifyTestBuild.ps1`

- [ ] **Step 1: Write the PowerShell script to verify test build**

Create `.kilo/tests/VerifyTestBuild.ps1` with the following content:
```powershell
$ErrorActionPreference = 'Stop'
Set-Location "C:\Users\Lance\Dev\Scripts"
dotnet restore "csharp/src/Tests/CSharpScripts.Tests.csproj"
dotnet build "csharp/src/Tests/CSharpScripts.Tests.csproj" --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "BUILD_FAILED"
}
Write-Output "BUILD_PASS"
```

- [ ] **Step 2: Run the script to verify it passes**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\VerifyTestBuild.ps1`
Expected: `BUILD_PASS` output and exit code 0.

- [ ] **Step 3: Commit**

```bash
git add .kilo/tests/VerifyTestBuild.ps1
git commit -m "test: add script to verify test project builds"
```

---

### Task 1.2: Run all existing tests, confirm 100% pass

**Files:**
- Create: `.kilo/tests/RunTests.ps1`

- [ ] **Step 1: Write the PowerShell script to run tests**

Create `.kilo/tests/RunTests.ps1` with the following content:
```powershell
$ErrorActionPreference = 'Stop'
Set-Location "C:\Users\Lance\Dev\Scripts"
dotnet test "csharp/src/Tests/CSharpScripts.Tests.csproj" --verbosity normal
if ($LASTEXITCODE -ne 0) {
    throw "TESTS_FAILED"
}
Write-Output "ALL_TESTS_PASS"
```

- [ ] **Step 2: Run the script to confirm all existing tests pass**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\RunTests.ps1`
Expected: `ALL_TESTS_PASS` output.

- [ ] **Step 3: Commit**

```bash
git add .kilo/tests/RunTests.ps1
git commit -m "test: add script to run all tests and assert success"
```
