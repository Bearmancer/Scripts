# Reader Domain Isolation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Verify and enforce that the Reader domain references only Core, with no dependencies on Data, Orchestrators, or Services.

**Architecture:** Scripts.Reader is a standalone library for web scraping, PDF parsing, and OCR operations. It depends solely on Scripts.Core for shared utilities and abstractions. Any cross-cutting concern that Reader currently imports from Data or Orchestrators must be extracted into a Core interface and injected.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Pre-flight Checks

```powershell
# Verify toolchain
if (-not (Get-Command pwsh -ErrorAction SilentlyContinue)) { throw "pwsh not found" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "dotnet SDK not found" }
dotnet --version | Select-String "^10\." || throw ".NET 10 SDK not found"

# Verify solution builds before starting
dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --no-restore
# Expected: Build succeeded. 0 Error(s).
```

---

## Task 1 — TDD RED: Write domain isolation tests

**Current State:** Tests do not yet assert Reader dependency constraints.
**Reason:** We need a failing test to drive the isolation work.
**What:** Add two tests to `Scripts.Tests` — one for `.csproj` reference constraints, one for namespace correctness.
**Expected Outcome:** `dotnet test` reports 0 passed, 2 failed on the new tests.

### Step 1.1 — Create test file

```powershell
$testFile = "C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\T3\T300_ReaderDomainTests.cs"

# Verify tests project exists
Test-Path "C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Scripts.Tests.csproj" -ErrorAction Stop | Should -Be $true

# Create T3 directory
New-Item -ItemType Directory -Path "C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\T3" -Force -ErrorAction Stop
Test-Path "C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\T3" -ErrorAction Stop | Should -Be $true
```

Create file `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\T3\T300_ReaderDomainTests.cs`:

```csharp
using System.IO;
using System.Linq;
using FluentAssertions;
using TUnit.Core;

namespace CSharpScripts.Tests.T3;

public class T300_ReaderDomainTests
{
    private const string ReaderCsproj =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Scripts.Reader.csproj";

    private const string ReaderSrcDir =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\Reader";

    [Test]
    public void ReaderDomain_HasNoDependencies_OnDataOrOrchestrators()
    {
        File.Exists(ReaderCsproj).Should().BeTrue(
            "because Scripts.Reader.csproj must exist at the expected path");

        var content = File.ReadAllText(ReaderCsproj);

        content.Should().NotContain("Scripts.Data",
            "because Reader must not reference the Data layer");
        content.Should().NotContain("Scripts.Orchestrators",
            "because Reader must not reference Orchestrators");
        content.Should().NotContain("Scripts.Services",
            "because Reader must not reference any Services project directly");
    }

    [Test]
    public void ReaderDomain_AllFiles_HaveCorrectNamespace()
    {
        Directory.Exists(ReaderSrcDir).Should().BeTrue(
            $"because Reader source directory must exist at {ReaderSrcDir}");

        var readerFiles = Directory
            .GetFiles(ReaderSrcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        readerFiles.Should().NotBeEmpty(
            "because the Reader project must contain at least one .cs file");

        foreach (var file in readerFiles)
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("namespace "))
                continue;

            content.Should().Contain("namespace CSharpScripts.Reader",
                $"because {Path.GetFileName(file)} has a wrong namespace — expected CSharpScripts.Reader.*");
        }
    }
}
```

### Step 1.2 — Run tests to confirm RED

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "FullyQualifiedName~T300_ReaderDomainTests" `
    2>&1 | Tee-Object -Variable testOutput

$testOutput | Select-String "Failed" | Should -Not -BeNullOrEmpty
```

Expected output fragment:
```
Failed! - Failed: 1, Passed: 0, Skipped: 0
```
(or both pass if Reader is already clean — proceed to GREEN confirmation)

---

## Task 2 — Inspect current Reader dependencies

**Current State:** Unknown whether Reader has illegal imports.
**Reason:** Must identify what needs to be fixed before editing source.
**What:** Grep for disallowed namespace imports in Reader `.cs` files.
**Expected Outcome:** A list of files that import from `CSharpScripts.Data` or `CSharpScripts.Orchestrators`.

```powershell
$readerDir = "C:\Users\Lance\Dev\Scripts\csharp\src\Reader"

Write-Host "=== Files importing CSharpScripts.Data ==="
Get-ChildItem $readerDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "CSharpScripts\.Data" |
    Select-Object -ExpandProperty Path

Write-Host "=== Files importing CSharpScripts.Orchestrators ==="
Get-ChildItem $readerDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "CSharpScripts\.Orchestrators" |
    Select-Object -ExpandProperty Path

Write-Host "=== Files importing CSharpScripts.Services ==="
Get-ChildItem $readerDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "CSharpScripts\.Services" |
    Select-Object -ExpandProperty Path
```

---

## Task 3 — GREEN: Remove illegal Reader → Data/Orchestrators references

> Only perform this task if Task 2 found violations. If Reader is already clean, skip to Task 4.

**Current State:** One or more Reader files import from Data or Orchestrators.
**Reason:** The dependency violates the inward-only flow: Reader → Core.
**What:** For each violation, extract the shared type into `Scripts.Core` and update Reader to use the Core version.
**Expected Outcome:** No Reader file imports from Data or Orchestrators.

### Step 3.1 — Back up affected files

For each affected file (replace `<FileName>` with the actual filename):

```powershell
$src = "C:\Users\Lance\Dev\Scripts\csharp\src\Reader\<FileName>.cs"
$bak = "$src.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $src -Destination $bak -ErrorAction Stop
Test-Path $bak -ErrorAction Stop | Should -Be $true
Write-Host "Backed up: $bak"
```

### Step 3.2 — Extract shared interface to Core

If a Reader file calls a method from `CSharpScripts.Data` (e.g., a repository), create an interface in Core:

Create file `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Abstractions\IDocumentStore.cs`:

```csharp
namespace CSharpScripts.Core.Abstractions;

/// <summary>
/// Abstraction for persisting parsed document results.
/// Implemented in Scripts.Data; consumed by Scripts.Reader via DI.
/// </summary>
public interface IDocumentStore
{
    Task SaveAsync(string key, string content, CancellationToken ct = default);
}
```

Verify file created:
```powershell
Test-Path "C:\Users\Lance\Dev\Scripts\csharp\src\Core\Abstractions\IDocumentStore.cs" -ErrorAction Stop
# Expected: True
```

### Step 3.3 — Update Reader file to use interface

Replace the direct `CSharpScripts.Data` import with `CSharpScripts.Core.Abstractions`:

```powershell
# In each violating Reader .cs file, replace the illegal using directive
$file = "C:\Users\Lance\Dev\Scripts\csharp\src\Reader\<FileName>.cs"
$content = Get-Content $file -Raw -Encoding UTF8
$updated = $content -replace "using CSharpScripts\.Data[^;]*;", "using CSharpScripts.Core.Abstractions;"
Set-Content -Path $file -Value $updated -Encoding UTF8 -ErrorAction Stop

# Verify the illegal import is gone
$check = Get-Content $file -Raw -Encoding UTF8
$check | Should -Not -Match "CSharpScripts\.Data"
Write-Host "Cleaned: $file"
```

### Step 3.4 — Remove project reference from .csproj

```powershell
$csproj = "C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Scripts.Reader.csproj"
$bak    = "$csproj.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $csproj -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true

$xml = [xml](Get-Content $csproj -Encoding UTF8)
$refs = $xml.Project.ItemGroup.ProjectReference |
    Where-Object { $_.Include -like "*Scripts.Data*" -or $_.Include -like "*Scripts.Orchestrators*" }

foreach ($ref in $refs) {
    $ref.ParentNode.RemoveChild($ref) | Out-Null
}

$xml.Save($csproj)
Test-Path $csproj | Should -Be $true
Write-Host "Updated .csproj: $csproj"
```

---

## Task 4 — GREEN: Fix namespace violations

> Only perform this task if Task 1's namespace test fails.

**Current State:** One or more Reader `.cs` files use an incorrect namespace.
**Reason:** Namespace must match `CSharpScripts.Reader.*` for consistency.
**What:** For each file with the wrong namespace, back it up and update the namespace declaration.
**Expected Outcome:** All Reader files use `CSharpScripts.Reader.*`.

```powershell
$readerDir = "C:\Users\Lance\Dev\Scripts\csharp\src\Reader"

Get-ChildItem $readerDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    ForEach-Object {
        $file    = $_.FullName
        $content = Get-Content $file -Raw -Encoding UTF8

        if ($content -match "namespace " -and $content -notmatch "namespace CSharpScripts\.Reader") {
            $bak = "$file.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
            Copy-Item -Path $file -Destination $bak -ErrorAction Stop
            Test-Path $bak | Should -Be $true

            # Extract current namespace
            $current = ([regex]"namespace\s+([\w\.]+)").Match($content).Groups[1].Value
            Write-Host "Fixing namespace in $($_.Name): $current → CSharpScripts.Reader"

            $fixed = $content -replace "namespace\s+$([regex]::Escape($current))", "namespace CSharpScripts.Reader"
            Set-Content -Path $file -Value $fixed -Encoding UTF8 -ErrorAction Stop

            # Verify
            $verify = Get-Content $file -Raw -Encoding UTF8
            $verify | Should -Match "namespace CSharpScripts\.Reader"
        }
    }
```

---

## Task 5 — Rebuild and run tests

**Current State:** Source changes applied.
**Reason:** Confirm compilation and tests pass.
**What:** Restore, build, test.
**Expected Outcome:** 0 errors, all tests pass.

```powershell
dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx -ErrorAction Stop
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --no-restore -ErrorAction Stop

$buildOutput = dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --no-restore 2>&1
$buildOutput | Select-String "Error\(s\)" | ForEach-Object {
    $_ | Should -Match "0 Error\(s\)"
}
```

```powershell
$testOutput = dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "FullyQualifiedName~T300_ReaderDomainTests" 2>&1
$testOutput | Select-String "Failed:" | ForEach-Object {
    $_ | Should -Match "Failed: 0"
}
Write-Host "T300 tests: GREEN"
```

Expected output:
```
Test Run Successful.
Tests: 2 (2 passed)
```

---

## Task 6 — REFACTOR: Clean up and commit

**Current State:** Tests green, source clean.
**Reason:** Commit isolation work as a discrete unit.
**What:** Stage all changes, verify no secrets, commit.
**Expected Outcome:** One commit with message `feat(t3-00): isolate Reader domain`.

```powershell
Set-Location C:\Users\Lance\Dev\Scripts -ErrorAction Stop

# Security check — never commit secrets
gitleaks detect --no-git 2>&1 | Select-String "leaks found" | ForEach-Object {
    throw "Gitleaks found secrets — abort commit"
}

git add csharp/src/Reader/ csharp/tests/Scripts.Tests/T3/T300_ReaderDomainTests.cs 2>&1
git add csharp/src/Core/Abstractions/ 2>&1  # only if new abstractions were added

git status 2>&1 | Write-Host

git commit -m "feat(t3-00): isolate Reader domain — remove Data/Orchestrators refs, fix namespaces" `
    -ErrorAction Stop 2>&1 | Tee-Object -Variable commitOut

$commitOut | Select-String "feat\(t3-00\)" | Should -Not -BeNullOrEmpty
Write-Host "Committed: t3-00"
```

---

## Completion Criteria

| Check | Command | Expected |
|-------|---------|----------|
| Build clean | `dotnet build csharp/Scripts.slnx` | `0 Error(s)` |
| Tests pass | `dotnet test --filter T300` | `Failed: 0` |
| No Data ref | `grep -r "Scripts.Data" csharp/src/Reader/` | No output |
| No Orchestrators ref | `grep -r "Scripts.Orchestrators" csharp/src/Reader/` | No output |
| Namespace correct | `grep -rL "CSharpScripts.Reader" csharp/src/Reader/**/*.cs` | No output |
| Commit present | `git log --oneline -1` | `feat(t3-00)` |
