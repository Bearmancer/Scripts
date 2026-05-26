# Tooling Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove all Mail command stubs from the CLI project, replace Black with Ruff in `python/pyproject.toml`, and verify Rider SWEA (Solution-Wide Error Analysis) would be clean by building with zero warnings.

**Architecture:** Mail command removal is deletion-with-backup: each file gets a `.bak.YYYYMMDD_HHmmss` copy before deletion, followed by `Test-Path` on both original (must be gone) and backup (must exist). Python tooling is a `pyproject.toml` edit — remove Black, add Ruff with the canonical config block, then run `uv run ruff check` to confirm zero lint errors.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions / Python uv / Ruff

---

## Pre-flight

- [ ] **Step 0: Pre-flight validation**

```powershell
Get-Command pwsh   -ErrorAction Stop
Get-Command dotnet -ErrorAction Stop
Get-Command git    -ErrorAction Stop
Get-Command uv     -ErrorAction Stop

dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx -ErrorAction Stop

# Inventory all Mail-related files
Get-ChildItem 'C:\Users\Lance\Dev\Scripts\csharp\src\CLI' -Recurse -Filter '*Mail*' |
    Select-Object FullName
```

---

## Task 1: Write failing Mail-removal tests

**Files:**
- Create: `csharp/tests/Scripts.Tests/ToolingTests/MailRemovalTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// csharp/tests/Scripts.Tests/ToolingTests/MailRemovalTests.cs
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.ToolingTests;

public class MailRemovalTests
{
    private const string CliRoot = @"C:\Users\Lance\Dev\Scripts\csharp\src\CLI";

    [Test]
    public void MailCommand_DoesNotExist_InCliDirectory()
    {
        var mailFiles = Directory.GetFiles(CliRoot, "*Mail*", SearchOption.AllDirectories)
            .Where(f => !f.Contains(".bak."))  // exclude backup files
            .ToList();

        mailFiles.Should().BeEmpty(
            $"Mail command was removed — these files must not exist: {string.Join(", ", mailFiles.Select(Path.GetFileName))}");
    }

    [Test]
    public void ProgramCs_DoesNotRegister_MailCommand()
    {
        var programPath = Path.Combine(CliRoot, "Program.cs");
        File.Exists(programPath).Should().BeTrue();

        var content = File.ReadAllText(programPath);
        content.Should().NotContain("MailCommand",
            "Program.cs must not reference MailCommand after removal");
        content.Should().NotContain("AddCommand<Mail",
            "Program.cs must not register any Mail command variant");
    }
}
```

- [ ] **Step 2: Read-back**

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ToolingTests\MailRemovalTests.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run — confirm RED**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "MailRemovalTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: tests fail — Mail files still exist in CLI.

- [ ] **Step 3.5: State assessment**

Run this to confirm exactly which files need deletion:
```powershell
Get-ChildItem 'C:\Users\Lance\Dev\Scripts\csharp\src\CLI' -Recurse -Filter '*Mail*' |
    Select-Object FullName, Length
```

---

## Task 2: Delete Mail command files

- [ ] **Step 1: Backup and delete each Mail file**

```powershell
$mailFiles = Get-ChildItem 'C:\Users\Lance\Dev\Scripts\csharp\src\CLI' `
    -Recurse -Filter '*Mail*' |
    Where-Object { $_.Name -notlike '*.bak.*' }

foreach ($file in $mailFiles) {
    $bak = $file.FullName + '.bak.' + (Get-Date -Format 'yyyyMMdd_HHmmss')

    # Log: State → Reason → What → Expected
    Write-Host "State: $($file.FullName) exists"
    Write-Host "Reason: Mail command removed in T4-05"
    Write-Host "What: Backup to $bak then delete"
    Write-Host "Expected: original gone, backup present"

    Copy-Item $file.FullName $bak -ErrorAction Stop
    Test-Path $bak | Should -Be $true

    Remove-Item $file.FullName -Force -ErrorAction Stop
    Test-Path $file.FullName | Should -Be $false

    Write-Host "OK: deleted $($file.Name)"
}
```

- [ ] **Step 2: Remove Mail registration from Program.cs**

Open `csharp/src/CLI/Program.cs` and delete any line that registers a Mail command, e.g.:
```csharp
// DELETE lines like:
config.AddCommand<MailCommand>("mail");
```

Do not remove any other command registrations.

- [ ] **Step 3: Build — confirm no compile errors**

```powershell
dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1 | Tee-Object -Variable b
$b | Where-Object { $_ -match ' error ' } | Should -BeNullOrEmpty
Write-Host "Build clean after Mail removal"
```

- [ ] **Step 4: Run MailRemovalTests — confirm GREEN**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "MailRemovalTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: both tests PASS.

- [ ] **Step 5: Commit Mail removal**

```powershell
git -C C:\Users\Lance\Dev\Scripts add `
    csharp/src/CLI/ `
    csharp/tests/Scripts.Tests/ToolingTests/MailRemovalTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t4-05a): remove Mail command stubs from CLI"
```

---

## Task 3: Write failing Python tooling tests

**Files:**
- Create: `csharp/tests/Scripts.Tests/ToolingTests/PythonToolingTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// csharp/tests/Scripts.Tests/ToolingTests/PythonToolingTests.cs
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.ToolingTests;

public class PythonToolingTests
{
    private const string PyprojectPath = @"C:\Users\Lance\Dev\Scripts\python\pyproject.toml";

    [Test]
    public void PyprojectToml_Exists()
    {
        File.Exists(PyprojectPath).Should().BeTrue(
            "python/pyproject.toml must exist");
    }

    [Test]
    public void PyprojectToml_HasRuff_Section()
    {
        var content = File.ReadAllText(PyprojectPath);
        content.Should().Contain("[tool.ruff]",
            "pyproject.toml must have a [tool.ruff] section");
    }

    [Test]
    public void PyprojectToml_HasRuffLint_Section()
    {
        var content = File.ReadAllText(PyprojectPath);
        content.Should().Contain("[tool.ruff.lint]",
            "pyproject.toml must have a [tool.ruff.lint] section");
    }

    [Test]
    public void PyprojectToml_DoesNotContain_Black()
    {
        var content = File.ReadAllText(PyprojectPath);
        content.Should().NotContain("[tool.black]",
            "Black has been replaced by Ruff — [tool.black] must not exist");
        content.Should().NotContain("\"black\"",
            "Black dependency must be removed from pyproject.toml");
    }

    [Test]
    public void PyprojectToml_RuffLineLength_Is120()
    {
        var content = File.ReadAllText(PyprojectPath);
        content.Should().Contain("line-length = 120",
            "Ruff line-length must be set to 120");
    }

    [Test]
    public void PyprojectToml_RuffLint_HasRequiredSelectors()
    {
        var content = File.ReadAllText(PyprojectPath);
        content.Should().Contain("\"E\"",  "Ruff must select E rules");
        content.Should().Contain("\"W\"",  "Ruff must select W rules");
        content.Should().Contain("\"F\"",  "Ruff must select F rules");
        content.Should().Contain("\"I\"",  "Ruff must select I rules (isort)");
        content.Should().Contain("\"B\"",  "Ruff must select B rules (bugbear)");
        content.Should().Contain("\"UP\"", "Ruff must select UP rules (pyupgrade)");
    }
}
```

- [ ] **Step 2: Read-back**

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ToolingTests\PythonToolingTests.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run — confirm RED**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "PythonToolingTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: fail — `pyproject.toml` has Black or lacks Ruff config.

---

## Task 4: Update python/pyproject.toml

**Files:**
- Modify: `python/pyproject.toml`

- [ ] **Step 1: Backup pyproject.toml**

```powershell
$pyproject = 'C:\Users\Lance\Dev\Scripts\python\pyproject.toml'
$bak = $pyproject + '.bak.' + (Get-Date -Format 'yyyyMMdd_HHmmss')
Copy-Item $pyproject $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true
Write-Host "Backed up to $bak"
```

- [ ] **Step 2: Remove Black from pyproject.toml**

Remove any of the following if present:
- The line `"black"` in `[project.optional-dependencies]` or `[tool.poetry.dev-dependencies]`
- The entire `[tool.black]` section and its keys

- [ ] **Step 3: Add Ruff configuration to pyproject.toml**

Add the following block (after `[build-system]` or at end of file):

```toml
[tool.ruff]
line-length = 120

[tool.ruff.lint]
select = ["E", "W", "F", "I", "B", "C4", "UP"]
ignore = []
```

- [ ] **Step 4: Add ruff as a dev dependency (if not already present)**

```powershell
uv add --project 'C:\Users\Lance\Dev\Scripts\python' ruff --dev 2>&1
if ($LASTEXITCODE -ne 0) { throw "uv add ruff failed" }
```

- [ ] **Step 5: Run PythonToolingTests — confirm GREEN**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "PythonToolingTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: all 6 tests PASS.

---

## Task 5: Run ruff lint — zero errors

- [ ] **Step 1: Run ruff check on Python source**

```powershell
$ruffOut = uv run --project 'C:\Users\Lance\Dev\Scripts\python' `
    ruff check 'C:\Users\Lance\Dev\Scripts\python' 2>&1
$ruffOut | Write-Host
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Ruff found issues — fix them before committing"
    # Fix auto-fixable issues:
    uv run --project 'C:\Users\Lance\Dev\Scripts\python' `
        ruff check --fix 'C:\Users\Lance\Dev\Scripts\python' 2>&1
}
```

- [ ] **Step 2: Re-run ruff check — confirm zero errors**

```powershell
uv run --project 'C:\Users\Lance\Dev\Scripts\python' `
    ruff check 'C:\Users\Lance\Dev\Scripts\python' 2>&1
if ($LASTEXITCODE -ne 0) { throw "ruff still reports errors — fix manually" }
Write-Host "ruff: 0 errors"
```

- [ ] **Step 3: Final build + tests — no regressions**

```powershell
dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1 | Tee-Object -Variable b
$b | Where-Object { $_ -match ' error ' } | Should -BeNullOrEmpty

dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --logger "console;verbosity=normal" 2>&1
```

- [ ] **Step 4: Commit**

```powershell
git -C C:\Users\Lance\Dev\Scripts add `
    python/pyproject.toml `
    python/uv.lock `
    csharp/tests/Scripts.Tests/ToolingTests/
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t4-05b): replace Black with Ruff in python/pyproject.toml; ruff check passes 0 errors"
```

---

## Acceptance Criteria

- [ ] No `*Mail*` files in `csharp/src/CLI/` (excluding `.bak.*`)
- [ ] `Program.cs` contains no `MailCommand` or `AddCommand<Mail` references
- [ ] `python/pyproject.toml` has `[tool.ruff]` with `line-length = 120`
- [ ] `python/pyproject.toml` has `[tool.ruff.lint]` with `select = ["E", "W", "F", "I", "B", "C4", "UP"]`
- [ ] `python/pyproject.toml` contains no `[tool.black]` and no `"black"` dependency
- [ ] `uv run ruff check python/` exits with code `0`
- [ ] All `MailRemovalTests` and `PythonToolingTests` pass
- [ ] `dotnet build csharp/Scripts.slnx` → `0 Error(s). 0 Warning(s).`
