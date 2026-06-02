# T2-10: Tier 2 Sign-Off Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Perform final Tier 2 verification — full solution build, all tests green, CPM compliance validation, duplicate-free verification, dependency graph audit with no circular references, and git tag creation.

**Architecture:** This is the sign-off gate for Tier 2. Every automated check that was used during development is re-run in sequence. If any check fails, Tier 2 is not signed off and the previous phase must be revisited. The dependency graph is verified programmatically by parsing all `.csproj` files and checking for circular `ProjectReference` chains. A git tag `t2-sign-off` is applied only after all checks pass.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- [ ] T2-00 through T2-09 are signed off — all phases complete, no known issues
- [ ] Docker Desktop is running (for integration tests)
- [ ] `$env:PGCONNSTR` is set (for EF Core / integration tests)
- [ ] Working directory: `/home/lance/Scripts`

---

## Task 1 — TDD RED: Write sign-off verification tests

### Step 1 — Write the test file

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Tier2SignOffTests.cs`

```csharp
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests;

public class Tier2SignOffTests
{
    private const string CsharpRoot =
        @"/home/lance/Scripts/csharp";

    private const string SlnxPath =
        @"/home/lance/Scripts/csharp/Scripts.slnx";

    /// <summary>
    /// Every .csproj in the solution must have zero inline Version= attributes.
    /// All versions must come from Directory.Packages.props via CPM.
    /// </summary>
    [Test]
    public void AllProjectsUseCpm_NoInlineVersions()
    {
        var csprojFiles = Directory.GetFiles(CsharpRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
            .ToList();

        csprojFiles.Should().NotBeEmpty("At least one .csproj must exist");

        var violations = new List<string>();
        foreach (var file in csprojFiles)
        {
            var content = File.ReadAllText(file);
            if (System.Text.RegularExpressions.Regex.IsMatch(content, @"PackageReference.+Version="""))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty(
            $"CPM violation: these .csproj files still have inline Version=: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// Parse all .csproj ProjectReference elements and detect any circular dependency chain.
    /// </summary>
    [Test]
    public void DependencyGraph_HasNoCircularReferences()
    {
        var csprojFiles = Directory.GetFiles(CsharpRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
            .ToList();

        var adjacency = new Dictionary<string, List<string>>();

        foreach (var file in csprojFiles)
        {
            var projectName = Path.GetFileNameWithoutExtension(file);
            var content = File.ReadAllText(file);

            var xdoc = XDocument.Parse(content);
            var projectRefs = xdoc.Descendants("ProjectReference")
                .Select(pr => Path.GetFileNameWithoutExtension(pr.Attribute("Include")?.Value ?? ""))
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            adjacency[projectName] = projectRefs;
        }

        // DFS cycle detection
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        bool HasCycle(string node)
        {
            visited.Add(node);
            recursionStack.Add(node);

            if (adjacency.TryGetValue(node, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        if (HasCycle(neighbor))
                            return true;
                    }
                    else if (recursionStack.Contains(neighbor))
                    {
                        return true; // back edge = cycle
                    }
                }
            }

            recursionStack.Remove(node);
            return false;
        }

        foreach (var node in adjacency.Keys)
        {
            if (!visited.Contains(node))
            {
                if (HasCycle(node))
                {
                    Assert.Fail("Circular dependency detected in the project graph. " +
                        "Check ProjectReference chains across all .csproj files.");
                }
            }
        }
    }

    /// <summary>
    /// The solution file must reference exactly these 8 projects.
    /// </summary>
    [Test]
    public void SolutionFile_Contains_AllEightProjects()
    {
        File.Exists(SlnxPath).Should().BeTrue();

        var content = File.ReadAllText(SlnxPath);

        var expectedProjects = new[]
        {
            "Scripts.Core.csproj",
            "Scripts.Data.csproj",
            "Scripts.Services.Language.csproj",
            "Scripts.Services.Music.csproj",
            "Scripts.Orchestrators.csproj",
            "Scripts.Reader.csproj",
            "Scripts.CLI.csproj",
            "Scripts.Tests.csproj",
        };

        foreach (var expected in expectedProjects)
        {
            content.Should().Contain(expected,
                $"Scripts.slnx must reference {expected}");
        }
    }

    /// <summary>
    /// Full solution must build with zero errors.
    /// </summary>
    [Test]
    public void FullSolution_BuildsSuccessfully()
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            Arguments              = @"build /home/lance/Scripts/csharp/Scripts.slnx",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.WaitForExit();
        var stderr = proc.StandardError.ReadToEnd();
        proc.ExitCode.Should().Be(0,
            $"Full solution build failed. stderr: {stderr}");
    }

    /// <summary>
    /// No .cs filename should appear in more than one src/ project directory.
    /// </summary>
    [Test]
    public void NoDuplicateCsFiles_AcrossSrcProjects()
    {
        var srcRoot = Path.Combine(CsharpRoot, "src");
        var allCsFiles = Directory.GetFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
            .ToList();

        var duplicates = allCsFiles
            .GroupBy(f => Path.GetFileName(f))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} → {string.Join(", ", g.Select(l => Path.GetDirectoryName(l)))}")
            .ToList();

        duplicates.Should().BeEmpty(
            $"Duplicate .cs filenames detected:\n{string.Join("\n", duplicates)}");
    }
}
```

### Step 2 — Run tests RED (they may pass if already clean, but verify)

```powershell
$result = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~Tier2SignOffTests" `
    --no-build 2>&1
Write-Host $result
# Expected: Any failure here indicates a sign-off blocker
```

---

## Task 2 — GREEN: Full solution build

### Step 3 — dotnet restore + dotnet build

```powershell
Write-Host "STATE: Running full solution dotnet restore"
Write-Host "REASON: Tier 2 sign-off requires clean restore and build"

$restoreOutput = dotnet restore '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $restoreOutput
if ($LASTEXITCODE -ne 0) {
    Write-Host "BLOCKER: dotnet restore failed — Tier 2 cannot be signed off"
    throw "dotnet restore failed with exit code $LASTEXITCODE"
}
Write-Host "OUTCOME: dotnet restore OK"

Write-Host "STATE: Running full solution dotnet build"
$buildOutput = dotnet build '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) {
    Write-Host "BLOCKER: dotnet build failed — Tier 2 cannot be signed off"
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

# Verify output contains "Build succeeded." and "0 Error(s)"
if ($buildOutput -notmatch 'Build succeeded') {
    throw "Build output does not contain 'Build succeeded'"
}
Write-Host "OUTCOME: dotnet build OK — 0 errors"
```

---

## Task 3 — GREEN: Full solution test suite

### Step 4 — dotnet test

```powershell
Write-Host "STATE: Running full solution test suite"
Write-Host "REASON: All tests must pass for Tier 2 sign-off"

$testOutput = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) {
    Write-Host "BLOCKER: dotnet test failed — Tier 2 cannot be signed off"
    throw "dotnet test failed with exit code $LASTEXITCODE"
}

# Verify the output shows all passed
if ($testOutput -match 'Failed!') {
    throw "Tests contain failures — Tier 2 cannot be signed off"
}
Write-Host "OUTCOME: All tests passed"
```

---

## Task 4 — GREEN: CLI --help smoke test

### Step 5 — dotnet run -- --help

```powershell
Write-Host "STATE: Running CLI smoke test (dotnet run -- --help)"
Write-Host "REASON: Verify the compiled CLI executable is functional"

$helpOutput = dotnet run --project '/home/lance/Scripts/csharp/src\CLI\Scripts.CLI.csproj' -- --help 2>&1
Write-Host $helpOutput
if ($LASTEXITCODE -ne 0) {
    Write-Host "BLOCKER: dotnet run -- --help failed — Tier 2 cannot be signed off"
    throw "dotnet run -- --help failed with exit code $LASTEXITCODE"
}

if ($helpOutput -notmatch 'tools') {
    throw "CLI --help output does not contain application name 'tools'"
}
if ($helpOutput -notmatch 'sync') {
    throw "CLI --help output does not contain 'sync' command branch"
}
if ($helpOutput -notmatch 'music') {
    throw "CLI --help output does not contain 'music' command branch"
}
Write-Host "OUTCOME: CLI --help smoke test OK"
```

---

## Task 5 — REFACTOR: Run Tier2SignOffTests to GREEN

### Step 6 — Run sign-off test suite

```powershell
Write-Host "STATE: Running Tier2SignOffTests to confirm all gates pass"

$signOffOutput = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~Tier2SignOffTests" 2>&1
Write-Host $signOffOutput
if ($LASTEXITCODE -ne 0) {
    throw "Tier2SignOffTests failed — sign-off blocked"
}

# Verify specific test names passed
if ($signOffOutput -match 'Failed') {
    throw "One or more Tier2SignOffTests failed"
}
Write-Host "OUTCOME: All 5 Tier2SignOffTests PASSED"
```

---

## Task 6 — GREEN: CPM compliance final scan

### Step 7 — Manual scan for inline versions

```powershell
Write-Host "STATE: Running final CPM compliance scan"
Write-Host "REASON: Zero-tolerance for inline Version= in any .csproj"

$csprojFiles = Get-ChildItem -Path '/home/lance/Scripts/csharp' -Filter '*.csproj' -Recurse |
    Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' }

$violations = @()
foreach ($file in $csprojFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    if ($content -match 'PackageReference.+Version="') {
        $violations += $file.Name
    }
}

if ($violations.Count -gt 0) {
    $msg = "CPM VIOLATION: Inline Version= found in: " + ($violations -join ', ')
    Write-Host "BLOCKER: $msg"
    throw $msg
}
Write-Host "OUTCOME: CPM compliance verified — zero inline Version= attributes"
```

---

## Task 7 — GREEN: No duplicate .cs files scan

### Step 8 — Scan for duplicate filenames

```powershell
Write-Host "STATE: Scanning for duplicate .cs filenames across src/ projects"
Write-Host "REASON: Each type must have exactly one authoritative location"

$srcRoot = '/home/lance/Scripts/csharp/src'
$allCsFiles = Get-ChildItem -Path $srcRoot -Filter '*.cs' -Recurse |
    Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' }

$duplicates = $allCsFiles |
    Group-Object { $_.Name } |
    Where-Object { $_.Count -gt 1 }

if ($duplicates) {
    $dupReport = ($duplicates | ForEach-Object {
        $_.Name + " → " + ($_.Group | ForEach-Object { $_.DirectoryName }) -join ', '
    }) -join "`n"
    Write-Host "BLOCKER: Duplicate .cs files detected:`n$dupReport"
    throw "Duplicate .cs files detected — Tier 2 sign-off blocked"
}
Write-Host "OUTCOME: No duplicate .cs files detected"
```

---

## Task 8 — GREEN: Dependency graph audit

### Step 9 — Verify expected dependency structure

```powershell
Write-Host "STATE: Auditing dependency graph structure"
Write-Host "REASON: Dependency graph must follow the specified hierarchy"

$projects = @{
    'Scripts.Core'      = @()                                                                           # No deps
    'Scripts.Data'      = @('Scripts.Core')                                                             # Core only
    'Scripts.Services.Language' = @('Scripts.Core')                                                     # Core only
    'Scripts.Services.Music'    = @('Scripts.Core')                                                     # Core only
    'Scripts.Orchestrators'     = @('Scripts.Data', 'Scripts.Services.Language', 'Scripts.Services.Music')  # Data + Services
    'Scripts.Reader'            = @('Scripts.Core')                                                     # Core only
    'Scripts.CLI'               = @('Scripts.Core', 'Scripts.Data',                                     # All libraries
                                     'Scripts.Services.Language', 'Scripts.Services.Music',
                                     'Scripts.Orchestrators', 'Scripts.Reader')
    'Scripts.Tests'             = @('Scripts.Core', 'Scripts.Data',                                     # Everything
                                     'Scripts.Services.Language', 'Scripts.Services.Music',
                                     'Scripts.Orchestrators', 'Scripts.Reader', 'Scripts.CLI')
}

$srcDir = '/home/lance/Scripts/csharp/src'
$projMap = @{
    'Scripts.Core'                = Join-Path $srcDir 'Core\Scripts.Core.csproj'
    'Scripts.Data'                = Join-Path $srcDir 'Data\Scripts.Data.csproj'
    'Scripts.Services.Language'   = Join-Path $srcDir 'Services\Language\Scripts.Services.Language.csproj'
    'Scripts.Services.Music'      = Join-Path $srcDir 'Services\Music\Scripts.Services.Music.csproj'
    'Scripts.Orchestrators'       = Join-Path $srcDir 'Orchestrators\Scripts.Orchestrators.csproj'
    'Scripts.Reader'              = Join-Path $srcDir 'Reader\Scripts.Reader.csproj'
    'Scripts.CLI'                 = Join-Path $srcDir 'CLI\Scripts.CLI.csproj'
    'Scripts.Tests'               = '/home/lance/Scripts/csharp/tests\Scripts.Tests\Scripts.Tests.csproj'
}

$errors = @()
foreach ($projectName in $projects.Keys) {
    $projPath = $projMap[$projectName]
    if (-not (Test-Path $projPath)) {
        $errors += "Missing: $projectName at $projPath"
        continue
    }

    $content = Get-Content $projPath -Raw -Encoding UTF8
    $expectedRefs = $projects[$projectName]

    # Check each expected reference is present
    foreach ($expected in $expectedRefs) {
        if ($content -notmatch [regex]::Escape("$expected.csproj")) {
            $errors += "$projectName is missing expected reference to $expected"
        }
    }

    # Check for forbidden references
    $allProjects = @('Scripts.Core', 'Scripts.Data',
                     'Scripts.Services.Language', 'Scripts.Services.Music',
                     'Scripts.Orchestrators', 'Scripts.Reader',
                     'Scripts.CLI', 'Scripts.Tests')

    foreach ($other in $allProjects) {
        if ($other -eq $projectName) { continue }
        if ($other -in $expectedRefs) { continue }

        if ($content -match [regex]::Escape("$other.csproj")) {
            $errors += "$projectName has UNEXPECTED reference to $other (not in its dependency spec)"
        }
    }
}

if ($errors.Count -gt 0) {
    $errorReport = $errors -join "`n  "
    Write-Host "BLOCKER: Dependency graph violations:`n  $errorReport"
    throw "Dependency graph audit failed"
}
Write-Host "OUTCOME: Dependency graph audit passed"
```

---

## Task 9 — Git tag: t2-sign-off

### Step 10 — Apply git tag (only if all prior steps passed)

```powershell
Write-Host "STATE: All sign-off checks passed. Applying git tag t2-sign-off."
Write-Host "REASON: Marks the exact commit where Tier 2 was signed off"

$existingTag = git -C '/home/lance/Scripts' tag -l 't2-sign-off' 2>&1
if ($existingTag) {
    Write-Host "OUTCOME: Tag t2-sign-off already exists at:"
    git -C '/home/lance/Scripts' log -1 --oneline 't2-sign-off'
    Write-Host "WHAT: Deleting old tag and re-applying at current HEAD"
    git -C '/home/lance/Scripts' tag -d 't2-sign-off'
}

git -C '/home/lance/Scripts' tag -a 't2-sign-off' -m "Tier 2 sign-off: CPM + 8-project modularization complete"
if ($LASTEXITCODE -ne 0) { throw "git tag creation failed" }

# Verify tag exists
$tagVerify = git -C '/home/lance/Scripts' tag -l 't2-sign-off'
if ($tagVerify -ne 't2-sign-off') { throw "Tag t2-sign-off not found after creation" }
Write-Host "OUTCOME: Git tag t2-sign-off applied at HEAD"
```

---

## Task 10 — Commit sign-off tests

```powershell
git -C '/home/lance/Scripts' add `
    'csharp/tests/Scripts.Tests/Tier2SignOffTests.cs'

git -C '/home/lance/Scripts' commit `
    -m "feat(t2-10): Tier 2 sign-off — full build, all tests green, CPM compliance, dependency audit, no duplicates"
```

---

## Sign-off Criteria

- [ ] `dotnet restore csharp/Scripts.slnx` exits 0
- [ ] `dotnet build csharp/Scripts.slnx` exits 0 with `Build succeeded.` and `0 Error(s)`
- [ ] `dotnet test csharp/Scripts.slnx` exits 0 — all tests GREEN, `0 failed`
- [ ] `dotnet run --project csharp/src/CLI/Scripts.CLI.csproj -- --help` exits 0 and outputs `tools`, `sync`, `music`
- [ ] Zero `.csproj` files contain `PackageReference ... Version="` (CPM compliance)
- [ ] Zero duplicate `.cs` filenames across `csharp/src/` projects
- [ ] Dependency graph audit: no missing refs, no unexpected refs, no circular refs
- [ ] All 5 `Tier2SignOffTests` pass:
  - `AllProjectsUseCpm_NoInlineVersions`
  - `DependencyGraph_HasNoCircularReferences`
  - `SolutionFile_Contains_AllEightProjects`
  - `FullSolution_BuildsSuccessfully`
  - `NoDuplicateCsFiles_AcrossSrcProjects`
- [ ] Git tag `t2-sign-off` applied at HEAD

---

## Tier 2 Completion Summary

After sign-off, the solution state should be:

```
csharp/
├── Directory.Build.props           ← Global MSBuild settings + GlobalUsings + CPM enable
├── Directory.Packages.props        ← All NuGet PackageVersion declarations
├── Scripts.slnx                    ← References all 8 projects
├── src/
│   ├── Core/Scripts.Core.csproj                ← Serilog, Polly, Google.Apis.Auth, Ben.Demystifier
│   ├── Data/Scripts.Data.csproj                ← EF Core 10, Npgsql 10, CsvHelper
│   ├── Services/Language/Scripts.Services.Language.csproj  ← Azure Translation, Lingua, RestSharp
│   ├── Services/Music/Scripts.Services.Music.csproj        ← MusicBrainz, Discogs, Mapperly
│   ├── Orchestrators/Scripts.Orchestrators.csproj         ← Last.fm, YouTube, Sheets
│   ├── Reader/Scripts.Reader.csproj                       ← Playwright, AngleSharp, PdfPig, OCR
│   └── CLI/Scripts.CLI.csproj                             ← Spectre.Console, Composition Root, Exe
└── tests/
    └── Scripts.Tests/Scripts.Tests.csproj    ← TUnit, FluentAssertions, Testcontainers
```

**Dependency Flow (verified):**
```
CLI → Orchestrators → Data → Core
CLI → Reader → Core
CLI → Language → Core
CLI → Music → Core
Tests → [all projects]
Core → (nothing)
```
