# T2-08: Scripts.Tests Project Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create the `Scripts.Tests` project at `csharp/tests/Scripts.Tests/`, rename from `CSharpScripts.Tests`, add `InternalsVisibleTo` to all library projects, configure `OutputType=Exe` for TUnit test runner, and update solution references.

**Architecture:** `Scripts.Tests` is the test project that references ALL other projects in the solution. It uses TUnit + FluentAssertions + Testcontainers. Each library project exposes its internals to `Scripts.Tests` via `[assembly: InternalsVisibleTo("Scripts.Tests")]` in each project's `Properties/AssemblyInfo.cs`. The test project has `OutputType=Exe` because TUnit requires an executable entry point. The `Scripts.slnx` solution file must include this project and remove any reference to the old `CSharpScripts.Tests` project.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- [ ] T2-00 through T2-07 are signed off — all 7 projects exist and compile
- [ ] CPM is active — `Directory.Packages.props` lists `TUnit`, `FluentAssertions`, `Testcontainers.PostgreSql`
- [ ] `/home/lance/Scripts/csharp/tests\Scripts.Tests\` directory must be created
- [ ] Existing test files from prior phases exist at `csharp/tests/Scripts.Tests/` (if different location, migration is handled here)

---

## Task 1 — Preflight: verify state and backup

### Step 1 — Log current state

```powershell
Write-Host "STATE: Verifying tests directory and checking for existing test projects"
Write-Host "REASON: Must not overwrite without backup (Zero-Presumption Rule 9)"

$testsDir  = '/home/lance/Scripts/csharp/tests'
$testsProjDir = '/home/lance/Scripts/csharp/tests\Scripts.Tests'
$testsProj = Join-Path $testsProjDir 'Scripts.Tests.csproj'
$ts        = Get-Date -Format 'yyyyMMdd_HHmmss'

if (-not (Test-Path $testsDir)) {
    New-Item -ItemType Directory -Path $testsDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $testsDir)) { throw "Failed to create $testsDir" }
    Write-Host "OUTCOME: Created directory $testsDir"
}

if (-not (Test-Path $testsProjDir)) {
    New-Item -ItemType Directory -Path $testsProjDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $testsProjDir)) { throw "Failed to create $testsProjDir" }
    Write-Host "OUTCOME: Created directory $testsProjDir"
} else {
    Write-Host "OUTCOME: Directory $testsProjDir already exists"
}

if (Test-Path $testsProj) {
    $bak = "$testsProj.bak.$ts"
    Copy-Item $testsProj $bak -ErrorAction Stop
    if (-not (Test-Path $bak)) { throw "Backup of Scripts.Tests.csproj failed" }
    Write-Host "OUTCOME: Backed up existing Scripts.Tests.csproj → $bak"
}
```

---

## Task 2 — TDD RED: Write failing tests

### Step 2 — Write tests

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\ScriptsTestsProjectTests.cs`

```csharp
using System.IO;
using System.Linq;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests;

public class ScriptsTestsProjectTests
{
    private const string TestsCsproj =
        @"/home/lance/Scripts/csharp/tests\Scripts.Tests\Scripts.Tests.csproj";

    private const string CoreAssemblyInfo =
        @"/home/lance/Scripts/csharp/src\Core\Properties\AssemblyInfo.cs";

    private const string DataAssemblyInfo =
        @"/home/lance/Scripts/csharp/src\Data\Properties\AssemblyInfo.cs";

    private const string LanguageAssemblyInfo =
        @"/home/lance/Scripts/csharp/src\Services\Language\Properties\AssemblyInfo.cs";

    private const string MusicAssemblyInfo =
        @"/home/lance/Scripts/csharp/src\Services\Music\Properties\AssemblyInfo.cs";

    private const string OrchestratorsAssemblyInfo =
        @"/home/lance/Scripts/csharp/src\Orchestrators\Properties\AssemblyInfo.cs";

    private const string ReaderAssemblyInfo =
        @"/home/lance/Scripts/csharp/src\Reader\Properties\AssemblyInfo.cs";

    private const string ClicAssemblyInfo =
        @"/home/lance/Scripts/csharp/src\CLI\Properties\AssemblyInfo.cs";

    private const string SlnxPath =
        @"/home/lance/Scripts/csharp/Scripts.slnx";

    [Test]
    public void ScriptsTests_CsprojFile_Exists()
    {
        File.Exists(TestsCsproj).Should().BeTrue(
            "Scripts.Tests.csproj must exist at csharp/tests/Scripts.Tests/");
    }

    [Test]
    public void ScriptsTests_References_AllProjects()
    {
        File.Exists(TestsCsproj).Should().BeTrue();
        var content = File.ReadAllText(TestsCsproj);

        var expectedRefs = new[]
        {
            "Scripts.Core.csproj",
            "Scripts.Data.csproj",
            "Scripts.Services.Language.csproj",
            "Scripts.Services.Music.csproj",
            "Scripts.Orchestrators.csproj",
            "Scripts.Reader.csproj",
            "Scripts.CLI.csproj",
        };

        foreach (var expected in expectedRefs)
        {
            content.Should().Contain(expected,
                $"Scripts.Tests must reference {expected}");
        }
    }

    [Test]
    public void ScriptsTests_HasTUnitAndFluentAssertions()
    {
        File.Exists(TestsCsproj).Should().BeTrue();
        var content = File.ReadAllText(TestsCsproj);

        content.Should().Contain("TUnit",
            "TUnit PackageReference must be present");
        content.Should().Contain("FluentAssertions",
            "FluentAssertions PackageReference must be present");
        content.Should().Contain("Testcontainers.PostgreSql",
            "Testcontainers.PostgreSql PackageReference must be present");
    }

    [Test]
    public void ScriptsTests_HasOutputTypeExe()
    {
        File.Exists(TestsCsproj).Should().BeTrue();
        var content = File.ReadAllText(TestsCsproj);
        content.Should().Contain("<OutputType>Exe</OutputType>",
            "Scripts.Tests.csproj must have OutputType=Exe for the TUnit test runner");
    }

    [Test]
    public void ScriptsTests_HasNoInlineVersions()
    {
        File.Exists(TestsCsproj).Should().BeTrue();
        var content = File.ReadAllText(TestsCsproj);
        content.Should().NotMatchRegex(@"PackageReference.+Version=""",
            "Scripts.Tests.csproj must not contain inline Version= (CPM violation)");
    }

    [Test]
    public void ScriptsTests_InternalsVisibleTo_ConfiguredIn_AllLibraryProjects()
    {
        var assemblyInfoFiles = new[]
        {
            CoreAssemblyInfo,
            DataAssemblyInfo,
            LanguageAssemblyInfo,
            MusicAssemblyInfo,
            OrchestratorsAssemblyInfo,
            ReaderAssemblyInfo,
            ClicAssemblyInfo,
        };

        foreach (var infoFile in assemblyInfoFiles)
        {
            File.Exists(infoFile).Should().BeTrue(
                $"AssemblyInfo.cs must exist at {infoFile}");

            var content = File.ReadAllText(infoFile);
            content.Should().Contain("InternalsVisibleTo",
                $"{infoFile} must contain InternalsVisibleTo attribute");
            content.Should().Contain("Scripts.Tests",
                $"{infoFile} must expose internals to Scripts.Tests");
        }
    }

    [Test]
    public void ScriptsTests_IsRegistered_InSolutionFile()
    {
        File.Exists(SlnxPath).Should().BeTrue();
        var content = File.ReadAllText(SlnxPath);
        content.Should().Contain("Scripts.Tests.csproj",
            "Scripts.Tests.csproj must be listed in Scripts.slnx");
    }

    [Test]
    public void ScriptsTests_CompilesAndRunsTests()
    {
        File.Exists(TestsCsproj).Should().BeTrue();
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            Arguments              = @"test /home/lance/Scripts/csharp/tests\Scripts.Tests\Scripts.Tests.csproj",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.WaitForExit();
        var stderr = proc.StandardError.ReadToEnd();
        proc.ExitCode.Should().Be(0, $"Scripts.Tests project did not compile/test successfully. stderr: {stderr}");
    }
}
```

### Step 3 — Run tests RED

```powershell
$result = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsTestsProjectTests" `
    --no-build 2>&1
Write-Host $result
# Expected: FAILED — ScriptsTests_CsprojFile_Exists and all others fail because project does not exist yet
```

---

## Task 3 — GREEN: Create Scripts.Tests.csproj

### Step 4 — Write the project file

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Scripts.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Core\Scripts.Core.csproj" />
    <ProjectReference Include="..\..\src\Data\Scripts.Data.csproj" />
    <ProjectReference Include="..\..\src\Services\Language\Scripts.Services.Language.csproj" />
    <ProjectReference Include="..\..\src\Services\Music\Scripts.Services.Music.csproj" />
    <ProjectReference Include="..\..\src\Orchestrators\Scripts.Orchestrators.csproj" />
    <ProjectReference Include="..\..\src\Reader\Scripts.Reader.csproj" />
    <ProjectReference Include="..\..\src\CLI\Scripts.CLI.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="TUnit" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Testcontainers.PostgreSql" />
  </ItemGroup>
</Project>
```

### Step 5 — Verify the project file

```powershell
$testsProj = '/home/lance/Scripts/csharp/tests\Scripts.Tests\Scripts.Tests.csproj'
if (-not (Test-Path $testsProj)) { throw "Scripts.Tests.csproj was not created" }

$content = Get-Content $testsProj -Raw -Encoding UTF8

$requiredRefs = @(
    'Scripts.Core.csproj',
    'Scripts.Data.csproj',
    'Scripts.Services.Language.csproj',
    'Scripts.Services.Music.csproj',
    'Scripts.Orchestrators.csproj',
    'Scripts.Reader.csproj',
    'Scripts.CLI.csproj'
)
foreach ($ref in $requiredRefs) {
    if ($content -notmatch [regex]::Escape($ref)) {
        throw "Scripts.Tests.csproj must reference $ref"
    }
}

if ($content -notmatch '<OutputType>Exe</OutputType>') {
    throw "Scripts.Tests.csproj must have OutputType=Exe for TUnit"
}
if ($content -notmatch 'TUnit') {
    throw "Scripts.Tests.csproj must contain TUnit PackageReference"
}
if ($content -notmatch 'FluentAssertions') {
    throw "Scripts.Tests.csproj must contain FluentAssertions PackageReference"
}
if ($content -notmatch 'Testcontainers\.PostgreSql') {
    throw "Scripts.Tests.csproj must contain Testcontainers.PostgreSql PackageReference"
}
if ($content -match 'PackageReference.+Version="') {
    throw "Scripts.Tests.csproj must not contain inline Version= (CPM violation)"
}
Write-Host "OUTCOME: Scripts.Tests.csproj verified OK"
```

---

## Task 4 — GREEN: Verify InternalsVisibleTo in all library projects

### Step 6 — Verify all AssemblyInfo.cs files have InternalsVisibleTo("Scripts.Tests")

```powershell
Write-Host "STATE: Verifying InternalsVisibleTo in all 7 library projects"
Write-Host "REASON: Tests project must be able to access internal types for testing"

$assemblyInfoPaths = @(
    '/home/lance/Scripts/csharp/src\Core\Properties\AssemblyInfo.cs',
    '/home/lance/Scripts/csharp/src\Data\Properties\AssemblyInfo.cs',
    '/home/lance/Scripts/csharp/src\Services\Language\Properties\AssemblyInfo.cs',
    '/home/lance/Scripts/csharp/src\Services\Music\Properties\AssemblyInfo.cs',
    '/home/lance/Scripts/csharp/src\Orchestrators\Properties\AssemblyInfo.cs',
    '/home/lance/Scripts/csharp/src\Reader\Properties\AssemblyInfo.cs',
    '/home/lance/Scripts/csharp/src\CLI\Properties\AssemblyInfo.cs'
)

foreach ($infoPath in $assemblyInfoPaths) {
    if (-not (Test-Path $infoPath)) {
        throw "AssemblyInfo.cs missing at $infoPath — each project must have one"
    }

    $content = Get-Content $infoPath -Raw -Encoding UTF8

    if ($content -notmatch 'InternalsVisibleTo') {
        throw "$infoPath is missing InternalsVisibleTo attribute"
    }
    if ($content -notmatch 'Scripts\.Tests') {
        throw "$infoPath does not expose internals to Scripts.Tests"
    }
    Write-Host "OUTCOME: $infoPath — InternalsVisibleTo verified"
}
Write-Host "OUTCOME: All 7 projects have correct InternalsVisibleTo"
```

---

## Task 5 — GREEN: Register Scripts.Tests in Scripts.slnx

### Step 7 — Remove old CSharpScripts.Tests from solution

```powershell
Write-Host "STATE: Removing old CSharpScripts.Tests project from Scripts.slnx if present"
Write-Host "REASON: Old project reference must be removed to prevent duplicate references during dotnet build"

$slnx       = '/home/lance/Scripts/csharp/Scripts.slnx'
$oldProject = 'tests\CSharpScripts.Tests\CSharpScripts.Tests.csproj'

if ((Get-Content $slnx -Raw) -match [regex]::Escape($oldProject)) {
    $ts  = Get-Date -Format 'yyyyMMdd_HHmmss'
    $bak = "$slnx.bak.$ts"
    Copy-Item $slnx $bak -ErrorAction Stop
    if (-not (Test-Path $bak)) { throw "Backup of Scripts.slnx failed" }

    dotnet sln remove $slnx $oldProject -ErrorAction Stop 2>&1 | Tee-Object -Variable removeOutput
    Write-Host $removeOutput
    if ($LASTEXITCODE -ne 0) { throw "dotnet sln remove failed for old CSharpScripts.Tests" }

    $slnContent = Get-Content $slnx -Raw -Encoding UTF8
    if ($slnContent -match [regex]::Escape($oldProject)) {
        throw "Old CSharpScripts.Tests still present in Scripts.slnx after removal"
    }
    Write-Host "OUTCOME: Removed old CSharpScripts.Tests from solution"
} else {
    Write-Host "OUTCOME: Old CSharpScripts.Tests not found in solution — no removal needed"
}
```

### Step 8 — Add new Scripts.Tests to solution

```powershell
Write-Host "STATE: Adding Scripts.Tests.csproj to Scripts.slnx"

$slnx = '/home/lance/Scripts/csharp/Scripts.slnx'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$slnx.bak.$ts"
Copy-Item $slnx $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of Scripts.slnx failed" }

dotnet sln '/home/lance/Scripts/csharp/Scripts.slnx' `
    add '/home/lance/Scripts/csharp/tests\Scripts.Tests\Scripts.Tests.csproj' `
    2>&1 | Tee-Object -Variable slnOutput
Write-Host $slnOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet sln add failed for Scripts.Tests.csproj" }

$slnContent = Get-Content $slnx -Raw -Encoding UTF8
if ($slnContent -notmatch 'Scripts\.Tests\.csproj') {
    throw "Scripts.Tests.csproj not found in Scripts.slnx after dotnet sln add"
}
Write-Host "OUTCOME: Scripts.Tests.csproj registered in solution"
```

---

## Task 6 — GREEN: Full solution restore, build, and test

### Step 9 — Restore, build, and run all tests

```powershell
Write-Host "STATE: Running dotnet restore, build, and test for full solution"
Write-Host "REASON: Verify entire dependency graph compiles and all tests pass"

$restoreOutput = dotnet restore '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $restoreOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed for full solution" }

$buildOutput = dotnet build '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for full solution" }

$testOutput = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet test failed for full solution" }

# Expected:
# Build succeeded.
# 0 Error(s)
# All tests passed: N total, N passed, 0 failed, 0 skipped
```

---

## Task 7 — Commit

```powershell
git -C '/home/lance/Scripts' add `
    'csharp/tests/Scripts.Tests/Scripts.Tests.csproj' `
    'csharp/tests/Scripts.Tests/ScriptsTestsProjectTests.cs' `
    'csharp/Scripts.slnx'

git -C '/home/lance/Scripts' commit `
    -m "feat(t2-08): add Scripts.Tests.csproj with TUnit, InternalsVisibleTo in all 7 library projects, OutputType=Exe"
```

---

## Sign-off Criteria

- [ ] `csharp/tests/Scripts.Tests/Scripts.Tests.csproj` exists with `OutputType=Exe`
- [ ] References all 7 projects: Core, Data, Services.Language, Services.Music, Orchestrators, Reader, CLI
- [ ] Contains `TUnit`, `FluentAssertions`, and `Testcontainers.PostgreSql` PackageReferences
- [ ] Zero inline `Version=` attributes (CPM compliant)
- [ ] All 7 library projects have `Properties/AssemblyInfo.cs` with `[assembly: InternalsVisibleTo("Scripts.Tests")]`
- [ ] `Scripts.slnx` references `Scripts.Tests.csproj`
- [ ] `dotnet build csharp/Scripts.slnx` — full solution build exits 0
- [ ] `dotnet test csharp/Scripts.slnx` — all tests GREEN, exit code 0
