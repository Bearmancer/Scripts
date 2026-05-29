# T2-07: Scripts.CLI Project Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create `Scripts.CLI.csproj` at `csharp/src/CLI/` with `OutputType=Exe`, `AssemblyName=tools`, referencing all 6 library projects, and move `Program.cs` into `src/CLI/`.

**Architecture:** `Scripts.CLI` is the composition root — the outermost layer that wires together all library projects. It references `Scripts.Core`, `Scripts.Data`, `Scripts.Services.Language`, `Scripts.Services.Music`, `Scripts.Orchestrators`, and `Scripts.Reader`. It depends on `Spectre.Console` and `Spectre.Console.Cli` for the CLI framework. `Program.cs` is moved from `csharp/src/Program.cs` to `csharp/src/CLI/Program.cs` so it compiles within the CLI project. The `OutputType=Exe` makes this the runnable entry point; `PublishSingleFile=true` with `SelfContained=false` produces a single-file executable that depends on the .NET runtime being installed.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Logging & Resilience Context

### Logging Configuration

**Current:** Logs written to `<project_root>/logs/` (e.g., `C:\Users\Lance\Dev\Scripts\logs\`)

**Target:** `%USERPROFILE%\.cache\logs\scripts\` (per AGENTS.md)

**Changes Required:**

**File:** `csharp/src/Core/Paths.cs`

```csharp
public static readonly string LogDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".cache", "logs", "scripts"
);
```

**File:** `csharp/src/Core/Log.cs`

Add Ben.Demystifier integration for stack trace clarity:

```csharp
public static void Error(Exception ex, string messageTemplate, params object?[] args) =>
    ActiveLogger.Error(exception: ex.Demystify(), messageTemplate: messageTemplate, propertyValues: args);
```

**File:** `csharp/CSharpScripts.csproj`

Add `<PackageReference Include="Ben.Demystifier" />` to Directory.Packages.props (already listed).

### Log Format

- **File format:** `yyyy-MM-dd_HH-mm-ss.json` (Serilog CompactJsonFormatter)
- **Console output:** Human-readable Serilog template
- **Stack traces:** Demystified via Ben.Demystifier
- **Directory creation:** Automatic via `Directory.CreateDirectory(Paths.LogDirectory)` in Log static constructor

### Resilience Policies

**File:** `csharp/src/Core/Resilience.cs` (Polly v8)

Provides complete resilience pipeline:
- **Circuit breaker:** 50% failure ratio, 3-min window, 30-sec break
- **Rate limiter:** Last.fm only, 1 permit/sec
- **Retry:** 10 attempts, exponential backoff, jitter
- **Timeout:** Per-service (30s-120s)

**Gap:** No EF Core retry strategy. Both `DbContextRegistration.cs` and `ScriptsDbContextFactory.cs` must add:

```csharp
npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
    maxRetryCount: 3,
    maxRetryDelay: TimeSpan.FromSeconds(30),
    errorCodesToAdd: null
)
```

### Compiled Models (Optional Performance Optimization)

EF Core 10 supports compiled models for startup performance:

**Enable in `Directory.Build.props`:**
```xml
<EFOptimizeContext>true</EFOptimizeContext>
<EFScaffoldModelStage>build</EFScaffoldModelStage>
```

**Add to `.csproj`:**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Tasks" />
```

**Generate:**
```powershell
dotnet ef dbcontext optimize --project csharp/src/Data/Scripts.Data.csproj --output-dir CompiledModels
```

**Note:** EF9+ auto-detects compiled models — no `.UseModel()` call needed. When `OnModelCreating` changes, regenerate the compiled model and add a new migration.

---

## Prerequisites

- [ ] T2-00 through T2-06 are signed off — all 6 library projects exist and compile independently
- [ ] CPM is active — `Directory.Packages.props` lists `Spectre.Console`, `Spectre.Console.Cli`
- [ ] `C:\Users\Lance\Dev\Scripts\csharp\src\CLI\` directory exists (create if absent)
- [ ] `csharp/src/Program.cs` exists (current location of the entry point)

---

## Task 1 — Preflight: verify state and backup

### Step 1 — Log current state

```powershell
Write-Host "STATE: Verifying src/CLI directory, existing Program.cs, and any existing Scripts.CLI.csproj"
Write-Host "REASON: Must not overwrite without backup (Zero-Presumption Rule 9)"

$cliDir  = 'C:\Users\Lance\Dev\Scripts\csharp\src\CLI'
$cliProj = Join-Path $cliDir 'Scripts.CLI.csproj'
$program = 'C:\Users\Lance\Dev\Scripts\csharp\src\Program.cs'
$ts      = Get-Date -Format 'yyyyMMdd_HHmmss'

if (-not (Test-Path $cliDir)) {
    New-Item -ItemType Directory -Path $cliDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $cliDir)) { throw "Failed to create $cliDir" }
    Write-Host "OUTCOME: Created directory $cliDir"
} else {
    Write-Host "OUTCOME: Directory $cliDir already exists"
}

if (Test-Path $cliProj) {
    $bak = "$cliProj.bak.$ts"
    Copy-Item $cliProj $bak -ErrorAction Stop
    if (-not (Test-Path $bak)) { throw "Backup of Scripts.CLI.csproj failed" }
    Write-Host "OUTCOME: Backed up existing Scripts.CLI.csproj → $bak"
}

if (-not (Test-Path $program)) {
    throw "Program.cs does not exist at $program — cannot proceed with move"
}
Write-Host "OUTCOME: Program.cs found at $program"
```

---

## Task 2 — TDD RED: Write failing tests

### Step 2 — Write tests

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ScriptsCliProjectTests.cs`

```csharp
using System.IO;
using System.Linq;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests;

public class ScriptsCliProjectTests
{
    private const string ClicCsproj =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Scripts.CLI.csproj";

    private const string CliProgramCs =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Program.cs";

    private const string OldProgramCs =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\Program.cs";

    private const string AssemblyInfoPath =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Properties\AssemblyInfo.cs";

    [Test]
    public void ScriptsClic_CsprojFile_Exists()
    {
        File.Exists(ClicCsproj).Should().BeTrue(
            "Scripts.CLI.csproj must exist at csharp/src/CLI/");
    }

    [Test]
    public void ScriptsClic_References_AllLibraryProjects()
    {
        File.Exists(ClicCsproj).Should().BeTrue();
        var content = File.ReadAllText(ClicCsproj);

        content.Should().Contain("Scripts.Core.csproj",
            "CLI must reference Scripts.Core");
        content.Should().Contain("Scripts.Data.csproj",
            "CLI must reference Scripts.Data");
        content.Should().Contain("Scripts.Services.Language.csproj",
            "CLI must reference Scripts.Services.Language");
        content.Should().Contain("Scripts.Services.Music.csproj",
            "CLI must reference Scripts.Services.Music");
        content.Should().Contain("Scripts.Orchestrators.csproj",
            "CLI must reference Scripts.Orchestrators");
        content.Should().Contain("Scripts.Reader.csproj",
            "CLI must reference Scripts.Reader");
    }

    [Test]
    public void ScriptsClic_HasOutputTypeExe()
    {
        File.Exists(ClicCsproj).Should().BeTrue();
        var content = File.ReadAllText(ClicCsproj);
        content.Should().Contain("<OutputType>Exe</OutputType>",
            "Scripts.CLI.csproj must have OutputType=Exe as the entry point");
    }

    [Test]
    public void ScriptsClic_HasAssemblyNameTools()
    {
        File.Exists(ClicCsproj).Should().BeTrue();
        var content = File.ReadAllText(ClicCsproj);
        content.Should().Contain("<AssemblyName>tools</AssemblyName>",
            "Scripts.CLI.csproj must have AssemblyName=tools for backward compatibility");
    }

    [Test]
    public void ScriptsClic_ProgramCs_Exists_InCliDirectory()
    {
        File.Exists(CliProgramCs).Should().BeTrue(
            "Program.cs must be located at csharp/src/CLI/Program.cs (moved from csharp/src/Program.cs)");
    }

    [Test]
    public void ScriptsClic_OldProgramCs_NotFound()
    {
        File.Exists(OldProgramCs).Should().BeFalse(
            "Old Program.cs at csharp/src/Program.cs must be deleted after move to csharp/src/CLI/Program.cs");
    }

    [Test]
    public void ScriptsClic_HasSpectrePackages()
    {
        File.Exists(ClicCsproj).Should().BeTrue();
        var content = File.ReadAllText(ClicCsproj);

        content.Should().Contain("Spectre.Console",
            "Spectre.Console PackageReference must be present");
        content.Should().Contain("Spectre.Console.Cli",
            "Spectre.Console.Cli PackageReference must be present");
    }

    [Test]
    public void ScriptsClic_HasNoInlineVersions()
    {
        File.Exists(ClicCsproj).Should().BeTrue();
        var content = File.ReadAllText(ClicCsproj);
        content.Should().NotMatchRegex(@"PackageReference.+Version=""",
            "Scripts.CLI.csproj must not contain inline Version= (CPM violation)");
    }

    [Test]
    public void ScriptsClic_AssemblyInfo_HasInternalsVisibleTo()
    {
        File.Exists(AssemblyInfoPath).Should().BeTrue(
            "Properties/AssemblyInfo.cs must exist in Scripts.CLI");
        var content = File.ReadAllText(AssemblyInfoPath);
        content.Should().Contain("InternalsVisibleTo");
        content.Should().Contain("Scripts.Tests");
    }

    [Test]
    public void ScriptsClic_CompilesAndRunsHelpFlag()
    {
        File.Exists(ClicCsproj).Should().BeTrue();

        // Build first
        var buildPsi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            Arguments              = @"build C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Scripts.CLI.csproj",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var buildProc = System.Diagnostics.Process.Start(buildPsi)!;
        buildProc.WaitForExit();
        buildProc.ExitCode.Should().Be(0, $"Scripts.CLI.csproj did not build. stderr: {buildProc.StandardError.ReadToEnd()}");

        // Then run --help
        var runPsi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            Arguments              = @"run --project C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Scripts.CLI.csproj -- --help",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var runProc = System.Diagnostics.Process.Start(runPsi)!;
        runProc.WaitForExit();
        var stdout = runProc.StandardOutput.ReadToEnd();
        var stderr = runProc.StandardError.ReadToEnd();

        runProc.ExitCode.Should().Be(0,
            $"dotnet run -- --help exited with code {runProc.ExitCode}. stderr: {stderr}");

        stdout.Should().Contain("tools",
            "--help output must contain the application name 'tools'");
    }
}
```

### Step 3 — Run tests RED

```powershell
$result = dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsCliProjectTests" `
    --no-build 2>&1
Write-Host $result
# Expected: FAILED — ScriptsClic_CsprojFile_Exists, ProgramCs_Exists_InCliDirectory, etc.
```

---

## Task 3 — GREEN: Create Scripts.CLI.csproj

### Step 4 — Write the project file

File: `C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Scripts.CLI.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <AssemblyName>tools</AssemblyName>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>false</SelfContained>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Core\Scripts.Core.csproj" />
    <ProjectReference Include="..\Data\Scripts.Data.csproj" />
    <ProjectReference Include="..\Services\Language\Scripts.Services.Language.csproj" />
    <ProjectReference Include="..\Services\Music\Scripts.Services.Music.csproj" />
    <ProjectReference Include="..\Orchestrators\Scripts.Orchestrators.csproj" />
    <ProjectReference Include="..\Reader\Scripts.Reader.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Spectre.Console" />
    <PackageReference Include="Spectre.Console.Cli" />
  </ItemGroup>
</Project>
```

### Step 5 — Verify the project file

```powershell
$cliProj = 'C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Scripts.CLI.csproj'
if (-not (Test-Path $cliProj)) { throw "Scripts.CLI.csproj was not created" }

$content = Get-Content $cliProj -Raw -Encoding UTF8

$requiredRefs = @(
    'Scripts.Core.csproj',
    'Scripts.Data.csproj',
    'Scripts.Services.Language.csproj',
    'Scripts.Services.Music.csproj',
    'Scripts.Orchestrators.csproj',
    'Scripts.Reader.csproj'
)
foreach ($ref in $requiredRefs) {
    if ($content -notmatch [regex]::Escape($ref)) {
        throw "Scripts.CLI.csproj must reference $ref"
    }
}

if ($content -notmatch '<OutputType>Exe</OutputType>') {
    throw "Scripts.CLI.csproj must have OutputType=Exe"
}
if ($content -notmatch '<AssemblyName>tools</AssemblyName>') {
    throw "Scripts.CLI.csproj must have AssemblyName=tools"
}
if ($content -notmatch '<PublishSingleFile>true</PublishSingleFile>') {
    throw "Scripts.CLI.csproj must have PublishSingleFile=true"
}
if ($content -match 'PackageReference.+Version="') {
    throw "Scripts.CLI.csproj must not contain inline Version= (CPM violation)"
}
Write-Host "OUTCOME: Scripts.CLI.csproj verified OK"
```

---

## Task 4 — GREEN: Move Program.cs to src/CLI/

### Step 6 — Move Program.cs with verification

```powershell
Write-Host "STATE: Moving Program.cs from csharp/src/ to csharp/src/CLI/"
Write-Host "REASON: Program.cs must be inside the CLI project directory for compilation"
Write-Host "WHAT: Move csharp/src/Program.cs → csharp/src/CLI/Program.cs"

$srcFile  = 'C:\Users\Lance\Dev\Scripts\csharp\src\Program.cs'
$destFile = 'C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Program.cs'
$ts = Get-Date -Format 'yyyyMMdd_HHmmss'

# Backup source before moving
$bak = "$srcFile.bak.$ts"
Copy-Item $srcFile $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of Program.cs failed" }
Write-Host "OUTCOME: Backed up Program.cs → $bak"

# Move (copy then delete source)
Move-Item $srcFile $destFile -ErrorAction Stop

# Verify destination exists
if (-not (Test-Path $destFile)) { throw "Program.cs was not moved to $destFile" }
Write-Host "OUTCOME: Program.cs moved to $destFile"

# Verify source is gone
if (Test-Path $srcFile) { throw "Old Program.cs still exists at $srcFile — move incomplete" }
Write-Host "OUTCOME: Old Program.cs deleted from $srcFile"
```

---

## Task 5 — GREEN: Create Properties/AssemblyInfo.cs and register in solution

### Step 7 — Create AssemblyInfo.cs

```powershell
$propsDir = 'C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Properties'
if (-not (Test-Path $propsDir)) {
    New-Item -ItemType Directory -Path $propsDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $propsDir)) { throw "Failed to create $propsDir" }
    Write-Host "OUTCOME: Created Properties directory"
}
```

File: `C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Properties\AssemblyInfo.cs`

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Scripts.Tests")]
```

```powershell
$infoPath = 'C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Properties\AssemblyInfo.cs'
if (-not (Test-Path $infoPath)) { throw "AssemblyInfo.cs was not created in Scripts.CLI" }

$content = Get-Content $infoPath -Raw -Encoding UTF8
if ($content -notmatch 'InternalsVisibleTo') { throw "InternalsVisibleTo missing from AssemblyInfo.cs" }
if ($content -notmatch 'Scripts\.Tests')    { throw "Scripts.Tests not listed in InternalsVisibleTo" }
Write-Host "OUTCOME: AssemblyInfo.cs verified OK"
```

### Step 8 — Register in Scripts.slnx

```powershell
Write-Host "STATE: Adding Scripts.CLI.csproj to Scripts.slnx"

$slnx = 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$slnx.bak.$ts"
Copy-Item $slnx $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of Scripts.slnx failed" }

dotnet sln 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' `
    add 'C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Scripts.CLI.csproj' `
    2>&1 | Tee-Object -Variable slnOutput
Write-Host $slnOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet sln add failed for Scripts.CLI.csproj" }

$slnContent = Get-Content $slnx -Raw -Encoding UTF8
if ($slnContent -notmatch 'Scripts\.CLI\.csproj') {
    throw "Scripts.CLI.csproj not found in Scripts.slnx after dotnet sln add"
}
Write-Host "OUTCOME: Scripts.CLI.csproj registered in solution"
```

---

## Task 6 — GREEN: Full solution build

### Step 9 — Restore and build full solution

```powershell
Write-Host "STATE: Running dotnet restore and dotnet build for the full solution"
Write-Host "REASON: CLI references all projects — full solution build validates the entire dependency graph"

$restoreOutput = dotnet restore 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' 2>&1
Write-Host $restoreOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed for full solution" }

$buildOutput = dotnet build 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for full solution" }

# Expected:
# Build succeeded.
# 0 Error(s)
```

---

## Task 7 — REFACTOR: Run all tests GREEN

### Step 10 — Run all project tests

```powershell
$testOutput = dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' 2>&1
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) { throw "Full test suite failed" }

# Also run the --help verification manually
$helpOutput = dotnet run --project 'C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Scripts.CLI.csproj' -- --help 2>&1
Write-Host $helpOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet run -- --help failed with exit code $LASTEXITCODE" }
if ($helpOutput -notmatch 'tools') { throw "--help output does not contain 'tools' application name" }
Write-Host "OUTCOME: All tests passed, --help flag confirmed"
```

---

## Task 8 — Commit

```powershell
git -C 'C:\Users\Lance\Dev\Scripts' add `
    'csharp/src/CLI/Scripts.CLI.csproj' `
    'csharp/src/CLI/Program.cs' `
    'csharp/src/CLI/Properties/AssemblyInfo.cs' `
    'csharp/tests/Scripts.Tests/ScriptsCliProjectTests.cs' `
    'csharp/Scripts.slnx'

git -C 'C:\Users\Lance\Dev\Scripts' add 'csharp/src/Program.cs' 2>$null

git -C 'C:\Users\Lance\Dev\Scripts' commit `
    -m "feat(t2-07): add Scripts.CLI.csproj with OutputType=Exe, AssemblyName=tools, move Program.cs to src/CLI"
```

---

## Sign-off Criteria

- [ ] `csharp/src/CLI/Scripts.CLI.csproj` exists with `OutputType=Exe`, `AssemblyName=tools`, `PublishSingleFile=true`, `SelfContained=false`
- [ ] References all 6 library projects: Core, Data, Services.Language, Services.Music, Orchestrators, Reader
- [ ] `csharp/src/CLI/Program.cs` exists (moved from old location)
- [ ] `csharp/src/Program.cs` does NOT exist (deleted after move)
- [ ] `Spectre.Console` and `Spectre.Console.Cli` PackageReferences present
- [ ] Zero inline `Version=` attributes (CPM compliant)
- [ ] `csharp/src/CLI/Properties/AssemblyInfo.cs` has `[assembly: InternalsVisibleTo("Scripts.Tests")]`
- [ ] `Scripts.slnx` references `Scripts.CLI.csproj`
- [ ] `dotnet build csharp/Scripts.slnx` — full solution build exits 0
- [ ] `dotnet run --project csharp/src/CLI/Scripts.CLI.csproj -- --help` exits 0 and outputs "tools"
- [ ] All tests in solution pass: `dotnet test csharp/Scripts.slnx` exits 0
