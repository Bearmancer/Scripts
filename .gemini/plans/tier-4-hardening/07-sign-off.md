# Tier 4 Sign-Off Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete release-ready verification — all tests green, build clean, Gitleaks clean, documentation accurate — then tag `t4-sign-off` and publish a CLI release artifact.

**Architecture:** This is a gate plan, not an implementation plan. No new production code is written. The plan runs the full verification matrix (build → test → security → docs), captures evidence, and creates the git tag only when every gate passes. If any gate fails, the problem is escalated to the relevant Tier 4 phase plan for remediation before re-running the gate.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions / Gitleaks / PowerShell

---

## Pre-flight

- [ ] **Step 0: Pre-flight validation**

```powershell
Get-Command pwsh     -ErrorAction Stop
Get-Command dotnet   -ErrorAction Stop
Get-Command git      -ErrorAction Stop
Get-Command gitleaks -ErrorAction Stop

Write-Host ".NET: $(dotnet --version)"
Write-Host "git:  $(git --version)"
Write-Host "gitleaks: $(gitleaks version)"

# Confirm we are on the expected branch
$branch = git -C 'C:\Users\Lance\Dev\Scripts' branch --show-current
Write-Host "Branch: $branch"

# Load .env so PGCONNSTR is available
Get-Content 'C:\Users\Lance\Dev\Scripts\.env' | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.+)$') {
        [System.Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
    }
}
if (-not $env:PGCONNSTR) { throw 'PGCONNSTR not set after loading .env' }

# Restore
dotnet restore 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' -ErrorAction Stop
```

---

## Task 1: Write sign-off gate tests

**Files:**
- Create: `csharp/tests/Scripts.Tests/SignOffTests/SignOffGateTests.cs`

- [ ] **Step 1: Write sign-off gate tests**

```csharp
// csharp/tests/Scripts.Tests/SignOffTests/SignOffGateTests.cs
using System.Diagnostics;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.SignOffTests;

public class SignOffGateTests
{
    private static (int ExitCode, string StdOut, string StdErr) RunCommand(
        string fileName, string arguments, string workingDir)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory      = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute       = false
        };
        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    [Test]
    public void FinalBuild_IsSuccessful_ZeroErrors_ZeroWarnings()
    {
        var (exit, stdout, _) = RunCommand(
            "dotnet", "build C:\\Users\\Lance\\Dev\\Scripts\\csharp\\Scripts.slnx",
            @"C:\Users\Lance\Dev\Scripts");

        exit.Should().Be(0, $"build failed:\n{stdout}");
        stdout.Should().NotContain(" error ",
            $"build must have 0 errors:\n{stdout}");
        stdout.Should().NotContain(" warning ",
            $"build must have 0 warnings (TreatWarningsAsErrors=true):\n{stdout}");
    }

    [Test]
    public void FinalTestRun_AllTestsPass()
    {
        var (exit, stdout, stderr) = RunCommand(
            "dotnet",
            "test C:\\Users\\Lance\\Dev\\Scripts\\csharp\\Scripts.slnx --logger console;verbosity=normal",
            @"C:\Users\Lance\Dev\Scripts");

        exit.Should().Be(0, $"tests failed:\n{stderr}");
        stdout.Should().NotContain("Failed",
            $"all tests must pass:\n{stdout}");
        stdout.Should().NotContain("Error",
            $"no test errors:\n{stdout}");
    }

    [Test]
    public void Gitleaks_FindsNoSecrets()
    {
        var (exit, stdout, _) = RunCommand(
            "gitleaks",
            "detect --no-git --source C:\\Users\\Lance\\Dev\\Scripts",
            @"C:\Users\Lance\Dev\Scripts");

        exit.Should().Be(0, $"Gitleaks found secrets:\n{stdout}");
    }

    [Test]
    public void AllTier4PlanFiles_Exist()
    {
        var tier4Root = @"C:\Users\Lance\Dev\Scripts\.gemini\plans\tier-4-hardening";
        var expectedFiles = new[]
        {
            "00-di-wiring.md",
            "01-e2e-testing.md",
            "02-inspection-structural.md",
            "03-reader-restructure.md",
            "04-security-audit.md",
            "05-tooling.md",
            "06-documentation.md",
            "07-sign-off.md"
        };

        foreach (var f in expectedFiles)
        {
            File.Exists(Path.Combine(tier4Root, f)).Should().BeTrue(
                $"{f} must exist in tier-4-hardening/");
        }
    }

    [Test]
    public void IndexMd_Tier4_AllPhasesComplete()
    {
        var content = File.ReadAllText(
            @"C:\Users\Lance\Dev\Scripts\.gemini\plans\INDEX.md");
        var tier4Section = content.Substring(
            content.IndexOf("### Tier 4", StringComparison.Ordinal));
        var rows = tier4Section.Split('\n')
            .TakeWhile(l => !l.StartsWith("###") || l.StartsWith("### Tier 4"))
            .Where(l => l.Contains(".md"));

        foreach (var row in rows)
        {
            row.Should().Contain('✅',
                $"all Tier 4 phase rows must be marked ✅: '{row.Trim()}'");
        }
    }

    [Test]
    public void ServiceRegistration_Exists()
    {
        File.Exists(@"C:\Users\Lance\Dev\Scripts\csharp\src\CLI\ServiceRegistration.cs")
            .Should().BeTrue("ServiceRegistration.cs must exist (T4-00)");
    }

    [Test]
    public void NoMailFiles_InCliDirectory()
    {
        var mailFiles = Directory.GetFiles(
            @"C:\Users\Lance\Dev\Scripts\csharp\src\CLI",
            "*Mail*", SearchOption.AllDirectories)
            .Where(f => !f.Contains(".bak."))
            .ToList();

        mailFiles.Should().BeEmpty("Mail command was removed in T4-05");
    }

    [Test]
    public void ReadmeMd_HasQuickStart()
    {
        var readme = File.ReadAllText(@"C:\Users\Lance\Dev\Scripts\README.md");
        readme.Should().Contain("Quick Start");
        readme.Should().Contain("PGCONNSTR");
        readme.Should().Contain("dotnet build");
        readme.Should().Contain("dotnet test");
    }
}
```

- [ ] **Step 2: Read-back**

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOffTests\SignOffGateTests.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run gate tests — all must be GREEN**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "SignOffGateTests" `
    --logger "console;verbosity=detailed" 2>&1
```

**If any test fails:** Stop. Go back to the relevant Tier 4 phase plan and remediate. Do not proceed to Task 2 until all 9 gate tests pass.

---

## Task 2: Full test suite — final run

- [ ] **Step 1: Run all tests**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --logger "trx;LogFileName=C:\Users\Lance\Dev\Scripts\csharp\tests\t4-sign-off.trx" `
    --logger "console;verbosity=normal" `
    2>&1 | Tee-Object -Variable testOut
$testOut | Write-Host
if ($LASTEXITCODE -ne 0) { throw "Test suite failed — do not tag" }
```

- [ ] **Step 2: Verify TRX file was written**

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\t4-sign-off.trx' | Should -Be $true
Write-Host "TRX test result file written"
```

- [ ] **Step 3: Check for any failed tests in TRX**

```powershell
$trx = [xml](Get-Content 'C:\Users\Lance\Dev\Scripts\csharp\tests\t4-sign-off.trx' -Encoding UTF8)
$failed = $trx.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -eq 'Failed' }
$failed.Count | Should -Be 0 `
    "Failed tests: $($failed | ForEach-Object { $_.testName } | Out-String)"
Write-Host "All tests passed. Total: $($trx.TestRun.Results.UnitTestResult.Count)"
```

---

## Task 3: Build clean verification

- [ ] **Step 1: Clean build from scratch**

```powershell
dotnet clean C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1 | Out-Null
dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx -ErrorAction Stop 2>&1 | Out-Null
$build = dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
$build | Write-Host
$errors   = $build | Where-Object { $_ -match ' error ' }
$warnings = $build | Where-Object { $_ -match ' warning ' }
$errors.Count   | Should -Be 0 "Build errors found: $errors"
$warnings.Count | Should -Be 0 "Build warnings found: $warnings"
Write-Host "Build: 0 errors, 0 warnings ✅"
```

---

## Task 4: Security gate

- [ ] **Step 1: Final Gitleaks scan**

```powershell
$glResult = gitleaks detect --no-git --source 'C:\Users\Lance\Dev\Scripts' 2>&1
$glResult | Write-Host
if ($LASTEXITCODE -ne 0) { throw "Gitleaks: secrets detected — do not tag. Remediate in T4-04." }
Write-Host "Gitleaks: CLEAN ✅"
```

---

## Task 5: Publish CLI artifact

- [ ] **Step 1: Publish release build**

```powershell
$publishOut = 'C:\Users\Lance\Dev\Scripts\csharp\publish'
if (Test-Path $publishOut) {
    Remove-Item $publishOut -Recurse -Force -ErrorAction Stop
}

dotnet publish `
    'C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Scripts.CLI.csproj' `
    -c Release `
    -o $publishOut `
    2>&1 | Tee-Object -Variable pubOut
$pubOut | Write-Host
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
```

- [ ] **Step 2: Verify publish output**

```powershell
$publishOut = 'C:\Users\Lance\Dev\Scripts\csharp\publish'
Test-Path $publishOut | Should -Be $true

# Check that at least one executable was produced
$exes = Get-ChildItem $publishOut -Filter '*.exe'
$exes.Count | Should -BeGreaterThan 0 "Expected at least one .exe in publish output"
$exes | ForEach-Object { Write-Host "Published: $($_.Name) ($([math]::Round($_.Length / 1MB, 2)) MB)" }
```

---

## Task 6: Tag and push

- [ ] **Step 1: Commit sign-off test file**

```powershell
git -C 'C:\Users\Lance\Dev\Scripts' add `
    csharp/tests/Scripts.Tests/SignOffTests/ `
    csharp/tests/t4-sign-off.trx
git -C 'C:\Users\Lance\Dev\Scripts' commit `
    -m "feat(t4-07): sign-off gate — all tests green, build clean, gitleaks clean"
```

- [ ] **Step 2: Tag release**

```powershell
git -C 'C:\Users\Lance\Dev\Scripts' tag t4-sign-off
Write-Host "Tagged: t4-sign-off"
```

- [ ] **Step 3: Push tag**

```powershell
git -C 'C:\Users\Lance\Dev\Scripts' push origin t4-sign-off
if ($LASTEXITCODE -ne 0) { throw "Push failed" }
Write-Host "Tag pushed: t4-sign-off ✅"
```

---

## Acceptance Criteria (every item must be checked before tagging)

- [ ] `FinalBuild_IsSuccessful_ZeroErrors_ZeroWarnings` — PASS
- [ ] `FinalTestRun_AllTestsPass` — PASS
- [ ] `Gitleaks_FindsNoSecrets` — PASS
- [ ] `AllTier4PlanFiles_Exist` — PASS
- [ ] `IndexMd_Tier4_AllPhasesComplete` — PASS
- [ ] `ServiceRegistration_Exists` — PASS
- [ ] `NoMailFiles_InCliDirectory` — PASS
- [ ] `ReadmeMd_HasQuickStart` — PASS
- [ ] TRX file produced with 0 failed tests
- [ ] `dotnet publish` produces at least one `.exe` in `csharp/publish/`
- [ ] `git tag t4-sign-off` created and pushed to origin
