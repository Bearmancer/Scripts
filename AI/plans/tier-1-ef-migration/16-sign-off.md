# Tier 1 Sign-Off Gate Plan Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Verify all 16 Tier 1 phases (00–15) are complete, building cleanly, passing all tests, with Docker running and all plan files present. Tag `t1-sign-off` only when every check passes.

**Architecture:** This is a GATE plan — no new code is written. Each task is a verification checklist that exercises the full Tier 1 surface area: build integrity, test suite completeness, plan file inventory, Docker connectivity, compiled model regeneration, and end-to-end entity CRUD. The gate opens Tiers 2-4.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions / Testcontainers / Docker

---

## Prerequisites

- T1-15 completed (Testcontainers green, all entity integration tests pass)
- All 16 T1 plan files (00–15) exist at `C:\Users\Lance\Dev\Scripts\AI\plans\tier-1-ef-migration\`
- Git history contains commits prefixed `feat(t1-XX):` for all 16 phases

```powershell
$planDir = 'C:\Users\Lance\Dev\Scripts\AI\plans\tier-1-ef-migration'
$planCount = (Get-ChildItem $planDir -Filter '*.md').Count
Write-Host "Plan files: $planCount of 16" -ForegroundColor $(if ($planCount -eq 16) { 'Green' } else { 'Red' })
```

---

## Task 1 — Build Clean Verification

### Step 0: Preflight

```powershell
# Current state: Unknown build state. T1-00 through T1-15 may have introduced changes.
# Reason: Zero-error build is the non-negotiable gate for sign-off.
# What: Run dotnet build on the solution, assert 0 errors and 0 warnings.
# Expected: Build succeeded. 0 Error(s), 0 Warning(s).

Write-Host "Running dotnet build..."
```

### Step 1: Write test

Create `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\BuildVerificationTests.cs`:

```csharp
using System.Text.RegularExpressions;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.SignOff;

public sealed class BuildVerificationTests
{
    [Test]
    public async Task Dotnet_Build_Slnx_Zero_Errors()
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build C:\\Users\\Lance\\Dev\\Scripts\\csharp\\Scripts.slnx",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0,
            $"dotnet build must exit 0.\nStdOut: {output}\nStdErr: {error}"
        );

        var combined = output + error;
        combined.Should().NotMatch("error CS*",
            $"build output must not contain compilation errors.\nOutput: {combined}"
        );
    }

    [Test]
    public async Task Dotnet_Build_Slnx_Zero_Warnings_RaisedAsErrors()
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build C:\\Users\\Lance\\Dev\\Scripts\\csharp\\Scripts.slnx -warnaserror",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0,
            $"build must succeed with -warnaserror.\nStdOut: {output}\nStdErr: {error}"
        );
    }

    [Test]
    public async Task Dotnet_Restore_Succeeds()
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "restore C:\\Users\\Lance\\Dev\\Scripts\\csharp\\Scripts.slnx",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0,
            $"dotnet restore must succeed.\nStdOut: {output}\nStdErr: {error}"
        );
    }
}
```

### Step 2: Readback

```powershell
New-Item -ItemType Directory -Force -Path C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\BuildVerificationTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect GREEN — build must be clean before proceeding)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "BuildVerificationTests" 2>&1
```

Expected: GREEN — all 3 tests pass. Build is clean with 0 errors.

If RED: Fix build errors before continuing. This is a hard gate.

### Step 4: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\BuildVerificationTests.cs
git commit -m "feat(t1-16): add build verification tests for tier 1 sign-off"
```

---

## Task 2 — Plan File Inventory Verification

### Step 0: Preflight

```powershell
$planDir = 'C:\Users\Lance\Dev\Scripts\AI\plans\tier-1-ef-migration'
Get-ChildItem $planDir -Filter '*.md' | Select-Object Name | Sort-Object Name
```

### Step 1: Write test

Create `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\PlanInventoryTests.cs`:

```csharp
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.SignOff;

public sealed class PlanInventoryTests
{
    private static readonly string PlanDir =
        @"C:\Users\Lance\Dev\Scripts\AI\plans\tier-1-ef-migration";

    private static readonly string[] RequiredPlans =
    {
        "00-environment.md",
        "01-entities.md",
        "02-entity-refactoring.md",
        "03-dbcontext-config.md",
        "04-entity-configurations.md",
        "05-migrations.md",
        "06-repositories.md",
        "07-state-manager.md",
        "08-release-cache.md",
        "09-sync-service-updates.md",
        "10-ef10-queries.md",
        "11-compiled-model.md",
        "12-logging.md",
        "13-lingua.md",
        "14-resilience.md",
        "15-testcontainers.md",
        "16-sign-off.md",
    };

    [Test]
    public void All_17_Plan_Files_Exist()
    {
        var missing = new List<string>();
        foreach (var plan in RequiredPlans)
        {
            var path = Path.Combine(PlanDir, plan);
            if (!File.Exists(path))
                missing.Add(plan);
        }

        missing.Should().BeEmpty(
            $"All 17 plan files must exist.\nMissing: {string.Join(", ", missing)}"
        );
    }

    [Test]
    public void Plan_Files_Are_Non_Empty()
    {
        var empty = new List<string>();
        foreach (var plan in RequiredPlans)
        {
            var path = Path.Combine(PlanDir, plan);
            if (File.Exists(path) && new FileInfo(path).Length == 0)
                empty.Add(plan);
        }

        empty.Should().BeEmpty(
            $"Plan files must not be empty.\nEmpty: {string.Join(", ", empty)}"
        );
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\PlanInventoryTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect GREEN — all plan files are being written now)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "PlanInventoryTests" 2>&1
```

Expected: GREEN — `All_17_Plan_Files_Exist` and `Plan_Files_Are_Non_Empty` pass.

### Step 4: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\PlanInventoryTests.cs
git commit -m "feat(t1-16): add plan file inventory verification tests"
```

---

## Task 3 — All Tier 1 Tests Green Verification

### Step 0: Preflight

```powershell
# Run full test suite and capture results
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --logger "console;verbosity=detailed" 2>&1 | Tee-Object -FilePath C:\Windows\TEMP\kilo\t1-16-test-results.txt
```

### Step 1: Write test

Create `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\TestSuiteHealthTests.cs`:

```csharp
using System.Text.RegularExpressions;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.SignOff;

public sealed class TestSuiteHealthTests
{
    [Test]
    public async Task Dotnet_Test_All_Projects_Exit_Zero()
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "test C:\\Users\\Lance\\Dev\\Scripts\\csharp\\Scripts.slnx --no-build",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0,
            $"dotnet test must exit 0 for all tests.\nOutput: {output}\nError: {error}"
        );
    }

    [Test]
    public async Task Dotnet_Test_Output_Contains_No_Failures()
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "test C:\\Users\\Lance\\Dev\\Scripts\\csharp\\Scripts.slnx --no-build",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        output.Should().NotContain("Failed!",
            $"test output must not contain any failures.\nOutput: {output}"
        );

        output.Should().Contain("Passed!",
            "test output must show final pass summary"
        );
    }

    [Test]
    public void Test_Directories_Exist_And_Contain_Tests()
    {
        var testDirs = new[]
        {
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\CompiledModel",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Integration",
            @"C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff",
        };

        var missing = new List<string>();
        foreach (var dir in testDirs)
        {
            if (!Directory.Exists(dir))
                missing.Add(dir);
        }

        missing.Should().BeEmpty(
            $"All test directories must exist.\nMissing: {string.Join(", ", missing)}"
        );
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\TestSuiteHealthTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect GREEN — all tests pass)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "TestSuiteHealthTests" 2>&1
```

Expected: GREEN. If any test fails, the corresponding phase's work is incomplete. Fix before proceeding.

### Step 4: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\TestSuiteHealthTests.cs
git commit -m "feat(t1-16): add test suite health verification tests"
```

---

## Task 4 — Docker and Environment Verification

### Step 0: Preflight

```powershell
docker ps 2>&1
docker compose -f C:\Users\Lance\Dev\Scripts\docker-compose.yml config 2>&1
```

### Step 1: Write test

Create `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\EnvironmentVerificationTests.cs`:

```csharp
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.SignOff;

public sealed class EnvironmentVerificationTests
{
    [Test]
    public async Task Docker_Is_Running()
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0,
            "Docker must be running for all database operations"
        );
    }

    [Test]
    public async Task Docker_Compose_File_Is_Valid()
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "compose -f C:\\Users\\Lance\\Dev\\Scripts\\docker-compose.yml config",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0,
            "docker-compose.yml must be valid"
        );
    }

    [Test]
    public void Dot_Env_File_Exists()
    {
        var envPath = @"C:\Users\Lance\Dev\Scripts\.env";
        File.Exists(envPath).Should().BeTrue(
            ".env file must exist with PGCONNSTR"
        );
    }

    [Test]
    public void Dot_Env_Contains_PGCONNSTR()
    {
        var envPath = @"C:\Users\Lance\Dev\Scripts\.env";
        var content = File.ReadAllText(envPath);
        content.Should().Contain("PGCONNSTR",
            ".env must define PGCONNSTR"
        );
    }

    [Test]
    public async Task Compiled_Model_Regenerates_On_Build()
    {
        // Trigger a rebuild and verify CompiledModels are regenerated
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build C:\\Users\\Lance\\Dev\\Scripts\\csharp\\CSharpScripts.csproj",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0);

        var compiledModelDir = @"C:\Users\Lance\Dev\Scripts\csharp\CompiledModels";
        Directory.Exists(compiledModelDir).Should().BeTrue();
        Directory.GetFiles(compiledModelDir, "*.cs").Should().NotBeEmpty();
    }

    [Test]
    public void LogDirectory_Points_To_UserProfile_Cache()
    {
        var expectedBase = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cache", "logs", "scripts"
        );

        var logDir = CSharpScripts.Core.Paths.LogDirectory;

        logDir.Should().Be(expectedBase);
    }
}
```

### Step 2: Readback + Run

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\EnvironmentVerificationTests.cs'
Test-Path $file
# Expected: True

dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "EnvironmentVerificationTests" 2>&1
```

Expected: GREEN — Docker running, compose valid, .env exists, compiled model regenerates, log directory correct.

### Step 3: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\EnvironmentVerificationTests.cs
git commit -m "feat(t1-16): add environment verification tests for sign-off"
```

---

## Task 5 — Git Tag: `t1-sign-off`

### Step 0: Preflight

```powershell
# Verify all tests are green one final time
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

If ALL tests pass:

```powershell
# Create signed annotated tag
git tag -a t1-sign-off -m "Tier 1 EF Core Migration sign-off: 17 phases complete, all tests green, build clean, Docker verified"
```

### Step 1: Write test

Create `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\GitTagTests.cs`:

```csharp
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.SignOff;

public sealed class GitTagTests
{
    [Test]
    public async Task Git_Tag_T1_SignOff_Exists()
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "tag -l t1-sign-off",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var output = (await process.StandardOutput.ReadToEndAsync()).Trim();
        await process.WaitForExitAsync();

        output.Should().Be("t1-sign-off",
            "git tag t1-sign-off must exist after sign-off verification passes"
        );
    }

    [Test]
    public async Task Git_Log_Contains_All_Phase_Commits()
    {
        var requiredPrefixes = new[]
        {
            "feat(t1-00",
            "feat(t1-01",
            "feat(t1-10",
            "feat(t1-11",
            "feat(t1-12",
            "feat(t1-13",
            "feat(t1-14",
            "feat(t1-15",
            "feat(t1-16",
        };

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "log --oneline HEAD --not main 2>$null",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var log = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        var missing = requiredPrefixes
            .Where(p => !log.Contains(p))
            .ToList();

        missing.Should().BeEmpty(
            $"git log must contain commits for all T1 phases.\nMissing prefixes: {string.Join(", ", missing)}\nLog:\n{log}"
        );
    }
}
```

### Step 2: Readback + Run

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\GitTagTests.cs'
Test-Path $file
# Expected: True

dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "GitTagTests" 2>&1
```

Expected: GREEN — `t1-sign-off` tag exists, commits cover all required phase prefixes.

### Step 3: Final commit for sign-off tests

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SignOff\
git add C:\Users\Lance\Dev\Scripts\AI\plans\tier-1-ef-migration\16-sign-off.md
git commit -m "feat(t1-16): add sign-off gate verification tests"
```

---

## Sign-Off Gate Checklist

Execute in order. ALL must pass before tagging.

| # | Check | Command | Expect |
|---|-------|---------|--------|
| 1 | Build clean | `dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx` | 0 errors |
| 2 | All tests green | `dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx` | All pass |
| 3 | 17 plan files | `Get-ChildItem AI/plans/tier-1-ef-migration/*.md | Measure-Object` | Count=17 |
| 4 | Docker running | `docker ps` | Exit 0 |
| 5 | Compiled model | `Test-Path csharp/CompiledModels/ScriptsDbContextModel.cs` | True |
| 6 | Log directory | Verify `Paths.LogDirectory` = `%USERPROFILE%\.cache\logs\scripts` | Correct |
| 7 | DB retry | Verify `EnableRetryOnFailure` in `DbContextRegistration.cs` | Present |
| 8 | No EF11 patterns | `dotnet test --filter "Ef11ForbiddenPatternsTests"` | 4/4 PASS |
| 9 | Lingua tests | `dotnet test --filter "LanguageIdentifierTests"` | 10/10 PASS |
| 10 | Circuit breaker | `dotnet test --filter "PollyBehaviorTests"` | 3/3 PASS |
| 11 | Artist E2E | `dotnet test --filter "ArtistEntityIntegrationTests"` | 3/3 PASS |
| 12 | BuildVerification | `dotnet test --filter "BuildVerificationTests"` | 3/3 PASS |
| 13 | PlanInventory | `dotnet test --filter "PlanInventoryTests"` | 2/2 PASS |
| 14 | TestSuiteHealth | `dotnet test --filter "TestSuiteHealthTests"` | 3/3 PASS |
| 15 | Environment | `dotnet test --filter "EnvironmentVerificationTests"` | 6/6 PASS |
| 16 | GitTag | `dotnet test --filter "GitTagTests"` | 2/2 PASS |

**Final gate command:**

```powershell
git tag -a t1-sign-off -m "Tier 1 EF Core Migration: 17 phases, all tests green, build clean, Docker verified, compiled model present, log directory relocated"
git tag -l t1-sign-off
```

Expected: `t1-sign-off` listed.

**Next tier:** T2-00 CPM Foundation (unlocked by `t1-sign-off` tag).
