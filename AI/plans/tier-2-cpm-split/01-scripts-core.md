# T2-01: Scripts.Core Project Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create `Scripts.Core.csproj` at `csharp/src/Core/`, register it in `Scripts.slnx`, and confirm it compiles with zero project references.

**Architecture:** `Scripts.Core` is the innermost layer of the dependency graph — it must reference NO other project in this solution. It owns logging bootstrapping (Serilog), resilience primitives (Polly), Google Auth abstractions, and shared utilities (extensions, paths). Every other project depends on Core directly or transitively.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- [ ] T2-00 (CPM Foundation) is signed off — `Directory.Build.props` and `Directory.Packages.props` exist
- [ ] `dotnet build csharp/Scripts.slnx` exits 0 before this task begins
- [ ] `/home/lance/Scripts/csharp/src\Core\` directory exists (create if absent)

---

## Task 1 — Verify directory and back up any existing csproj

### Step 1 — Log current state

```powershell
Write-Host "STATE: Verifying src/Core directory and any existing Scripts.Core.csproj"
Write-Host "REASON: Must not overwrite without backup (Zero-Presumption Rule 9)"

$coreDir  = '/home/lance/Scripts/csharp/src\Core'
$coreProj = Join-Path $coreDir 'Scripts.Core.csproj'
$ts       = Get-Date -Format 'yyyyMMdd_HHmmss'

if (-not (Test-Path $coreDir)) {
    New-Item -ItemType Directory -Path $coreDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $coreDir)) { throw "Failed to create $coreDir" }
    Write-Host "OUTCOME: Created directory $coreDir"
} else {
    Write-Host "OUTCOME: Directory $coreDir already exists"
}

if (Test-Path $coreProj) {
    $bak = "$coreProj.bak.$ts"
    Copy-Item $coreProj $bak -ErrorAction Stop
    if (-not (Test-Path $bak)) { throw "Backup of Scripts.Core.csproj failed" }
    Write-Host "OUTCOME: Backed up existing Scripts.Core.csproj → $bak"
}
```

---

## Task 2 — TDD RED: Write failing tests

### Step 2 — Write tests

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\ScriptsCoreProjectTests.cs`

```csharp
using System.IO;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests;

public class ScriptsCoreProjectTests
{
    private const string CoreCsproj =
        @"/home/lance/Scripts/csharp/src\Core\Scripts.Core.csproj";

    private const string SlnxPath =
        @"/home/lance/Scripts/csharp/Scripts.slnx";

    private const string AssemblyInfoPath =
        @"/home/lance/Scripts/csharp/src\Core\Properties\AssemblyInfo.cs";

    [Test]
    public void ScriptsCore_CsprojFile_Exists()
    {
        File.Exists(CoreCsproj).Should().BeTrue(
            "Scripts.Core.csproj must exist at csharp/src/Core/");
    }

    [Test]
    public void ScriptsCore_HasNoProjectReferences()
    {
        File.Exists(CoreCsproj).Should().BeTrue();
        var content = File.ReadAllText(CoreCsproj);
        content.Should().NotContain("<ProjectReference",
            "Scripts.Core must not reference any other project — it is the innermost layer");
    }

    [Test]
    public void ScriptsCore_IsRegistered_InSolutionFile()
    {
        File.Exists(SlnxPath).Should().BeTrue();
        var content = File.ReadAllText(SlnxPath);
        content.Should().Contain("Scripts.Core.csproj",
            "Scripts.Core.csproj must be listed in Scripts.slnx");
    }

    [Test]
    public void ScriptsCore_AssemblyInfo_HasInternalsVisibleTo()
    {
        File.Exists(AssemblyInfoPath).Should().BeTrue(
            "Properties/AssemblyInfo.cs must exist in Scripts.Core");
        var content = File.ReadAllText(AssemblyInfoPath);
        content.Should().Contain("InternalsVisibleTo",
            "Core must expose internals to Scripts.Tests");
        content.Should().Contain("Scripts.Tests",
            "InternalsVisibleTo must target Scripts.Tests");
    }

    [Test]
    public void ScriptsCore_CompilesIndependently()
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            Arguments              = @"build /home/lance/Scripts/csharp/src\Core\Scripts.Core.csproj",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.WaitForExit();
        var stderr = proc.StandardError.ReadToEnd();
        proc.ExitCode.Should().Be(0, $"Scripts.Core.csproj did not compile. stderr: {stderr}");
    }
}
```

### Step 3 — Run tests RED

```powershell
$result = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsCoreProjectTests" `
    --no-build 2>&1
Write-Host $result
# Expected: FAILED — ScriptsCore_CsprojFile_Exists, ScriptsCore_IsRegistered_InSolutionFile, etc.
```

---

## Task 3 — GREEN: Create Scripts.Core.csproj

### Step 4 — Write the project file

File: `/home/lance/Scripts/csharp/src\Core\Scripts.Core.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Serilog" />
    <PackageReference Include="Serilog.Sinks.Console" />
    <PackageReference Include="Serilog.Sinks.File" />
    <PackageReference Include="Serilog.Enrichers.Process" />
    <PackageReference Include="Serilog.Enrichers.Thread" />
    <PackageReference Include="Serilog.Formatting.Compact" />
    <PackageReference Include="Polly" />
    <PackageReference Include="Polly.RateLimiting" />
    <PackageReference Include="System.Threading.RateLimiting" />
    <PackageReference Include="Google.Apis.Auth" />
    <PackageReference Include="Ben.Demystifier" />
  </ItemGroup>
</Project>
```

### Step 5 — Verify the file was written

```powershell
$coreProj = '/home/lance/Scripts/csharp/src\Core\Scripts.Core.csproj'
if (-not (Test-Path $coreProj)) { throw "Scripts.Core.csproj was not created" }

$content = Get-Content $coreProj -Raw -Encoding UTF8
if ($content -match '<ProjectReference') {
    throw "Scripts.Core.csproj must not contain any ProjectReference elements"
}
if ($content -match 'PackageReference.+Version="') {
    throw "Scripts.Core.csproj must not contain inline Version= attributes (CPM violation)"
}
Write-Host "OUTCOME: Scripts.Core.csproj verified — no ProjectReferences, no inline versions"
```

---

## Task 4 — GREEN: Create Properties/AssemblyInfo.cs

### Step 6 — Create AssemblyInfo.cs

```powershell
$propsDir = '/home/lance/Scripts/csharp/src\Core\Properties'
if (-not (Test-Path $propsDir)) {
    New-Item -ItemType Directory -Path $propsDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $propsDir)) { throw "Failed to create $propsDir" }
    Write-Host "OUTCOME: Created Properties directory"
}
```

File: `/home/lance/Scripts/csharp/src\Core\Properties\AssemblyInfo.cs`

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Scripts.Tests")]
```

```powershell
$infoPath = '/home/lance/Scripts/csharp/src\Core\Properties\AssemblyInfo.cs'
if (-not (Test-Path $infoPath)) { throw "AssemblyInfo.cs was not created in Scripts.Core" }

$content = Get-Content $infoPath -Raw -Encoding UTF8
if ($content -notmatch 'InternalsVisibleTo') { throw "InternalsVisibleTo missing from AssemblyInfo.cs" }
if ($content -notmatch 'Scripts\.Tests')    { throw "Scripts.Tests not listed in InternalsVisibleTo" }
Write-Host "OUTCOME: AssemblyInfo.cs verified OK"
```

---

## Task 5 — GREEN: Register Scripts.Core.csproj in Scripts.slnx

### Step 7 — Add project to solution

```powershell
Write-Host "STATE: Adding Scripts.Core.csproj to Scripts.slnx"
Write-Host "REASON: Solution file must reference every project for dotnet build/test to discover it"

$slnx = '/home/lance/Scripts/csharp/Scripts.slnx'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'

# Backup solution file
$bak = "$slnx.bak.$ts"
Copy-Item $slnx $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of Scripts.slnx failed" }

dotnet sln '/home/lance/Scripts/csharp/Scripts.slnx' `
    add '/home/lance/Scripts/csharp/src\Core\Scripts.Core.csproj' `
    2>&1 | Tee-Object -Variable slnOutput
Write-Host $slnOutput

if ($LASTEXITCODE -ne 0) { throw "dotnet sln add failed for Scripts.Core.csproj" }

# Verify registration
$content = Get-Content $slnx -Raw -Encoding UTF8
if ($content -notmatch 'Scripts\.Core\.csproj') {
    throw "Scripts.Core.csproj not found in Scripts.slnx after dotnet sln add"
}
Write-Host "OUTCOME: Scripts.Core.csproj registered in solution"
```

---

## Task 6 — GREEN: Build Scripts.Core

### Step 8 — Restore and build

```powershell
Write-Host "STATE: Running dotnet restore and dotnet build for Scripts.Core"
Write-Host "REASON: Confirm the project compiles in isolation"

$restoreOutput = dotnet restore '/home/lance/Scripts/csharp/src\Core\Scripts.Core.csproj' 2>&1
Write-Host $restoreOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed for Scripts.Core" }

$buildOutput = dotnet build '/home/lance/Scripts/csharp/src\Core\Scripts.Core.csproj' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for Scripts.Core — stderr above" }

# Expected output:
# Build succeeded.
# 0 Error(s)
```

---

## Task 7 — REFACTOR: Run all tests GREEN

### Step 9 — Run project tests

```powershell
$testOutput = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsCoreProjectTests" 2>&1
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) { throw "ScriptsCoreProjectTests failed" }
# Expected: All 5 tests passed
```

---

## Task 8 — Commit

```powershell
git -C '/home/lance/Scripts' add `
    'csharp/src/Core/Scripts.Core.csproj' `
    'csharp/src/Core/Properties/AssemblyInfo.cs' `
    'csharp/tests/Scripts.Tests/ScriptsCoreProjectTests.cs' `
    'csharp/Scripts.slnx'

git -C '/home/lance/Scripts' commit `
    -m "feat(t2-01): add Scripts.Core.csproj with CPM, no project references, InternalsVisibleTo"
```

---

## Sign-off Criteria

- [ ] `csharp/src/Core/Scripts.Core.csproj` exists
- [ ] `Scripts.Core.csproj` contains zero `<ProjectReference>` elements
- [ ] `Scripts.Core.csproj` contains zero inline `Version=` attributes
- [ ] `csharp/src/Core/Properties/AssemblyInfo.cs` exists with `[assembly: InternalsVisibleTo("Scripts.Tests")]`
- [ ] `Scripts.slnx` references `Scripts.Core.csproj`
- [ ] `dotnet build csharp/src/Core/Scripts.Core.csproj` exits 0
- [ ] `ScriptsCoreProjectTests` — all 5 tests GREEN
