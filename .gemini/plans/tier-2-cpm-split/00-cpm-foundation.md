# T2-00: CPM Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create `Directory.Build.props` and `Directory.Packages.props`, enable Central Package Management, and strip all inline `Version=` attributes from every `.csproj` in the solution.

**Architecture:** CPM centralises all NuGet version declarations in a single `Directory.Packages.props` at the C# root. `Directory.Build.props` applies global SDK settings (framework, language version, nullability, global usings) to every project without repetition. After this task every `.csproj` references packages by name only — no `Version=` attribute anywhere.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- [ ] Docker Desktop running (`docker ps` succeeds)
- [ ] .NET 10 SDK installed (`dotnet --version` → `10.0.x`)
- [ ] PowerShell 7+ (`pwsh --version`)
- [ ] Repo root: `C:\Users\Lance\Dev\Scripts`
- [ ] C# root: `C:\Users\Lance\Dev\Scripts\csharp`

---

## Task 1 — Backup existing props files (Zero-Presumption Rule 9)

### Step 1 — Log current state

```powershell
Write-Host "STATE: Checking for existing Directory.Build.props and Directory.Packages.props"
Write-Host "REASON: Must backup before overwriting (Zero-Presumption Rule 9)"

$buildProps  = 'C:\Users\Lance\Dev\Scripts\csharp\Directory.Build.props'
$pkgProps    = 'C:\Users\Lance\Dev\Scripts\csharp\Directory.Packages.props'
$ts          = Get-Date -Format 'yyyyMMdd_HHmmss'

if (Test-Path $buildProps) {
    $bak = "$buildProps.bak.$ts"
    Copy-Item $buildProps $bak -ErrorAction Stop
    if (-not (Test-Path $bak)) { throw "Backup of Directory.Build.props failed" }
    Write-Host "OUTCOME: Backed up Directory.Build.props → $bak"
} else {
    Write-Host "OUTCOME: Directory.Build.props does not exist — no backup needed"
}

if (Test-Path $pkgProps) {
    $bak = "$pkgProps.bak.$ts"
    Copy-Item $pkgProps $bak -ErrorAction Stop
    if (-not (Test-Path $bak)) { throw "Backup of Directory.Packages.props failed" }
    Write-Host "OUTCOME: Backed up Directory.Packages.props → $bak"
} else {
    Write-Host "OUTCOME: Directory.Packages.props does not exist — no backup needed"
}
```

---

## Task 2 — TDD RED: Write failing tests

### Step 2 — Write tests (they will fail because the files don't exist yet)

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\CpmFoundationTests.cs`

```csharp
using System.IO;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests;

public class CpmFoundationTests
{
    private const string CsharpRoot = @"C:\Users\Lance\Dev\Scripts\csharp";

    [Test]
    public void DirectoryPackagesProps_Exists()
    {
        File.Exists(Path.Combine(CsharpRoot, "Directory.Packages.props"))
            .Should().BeTrue("Directory.Packages.props must exist at the C# root for CPM to work");
    }

    [Test]
    public void ManagePackageVersionsCentrally_IsEnabled()
    {
        var buildPropsPath = Path.Combine(CsharpRoot, "Directory.Build.props");
        File.Exists(buildPropsPath).Should().BeTrue("Directory.Build.props must exist");

        var content = File.ReadAllText(buildPropsPath);
        content.Should().Contain(
            "<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>",
            "CPM must be explicitly enabled in Directory.Build.props");
    }

    [Test]
    public void CsProjFiles_HaveNo_InlineVersions()
    {
        var csprojFiles = Directory.GetFiles(CsharpRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
            .ToList();

        csprojFiles.Should().NotBeEmpty("There must be at least one .csproj in the solution");

        var violations = new List<string>();
        foreach (var file in csprojFiles)
        {
            var content = File.ReadAllText(file);
            // A space before 'Version=' distinguishes package version attributes from SDK version
            if (System.Text.RegularExpressions.Regex.IsMatch(content, @"PackageReference.+Version="""))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty(
            $"These .csproj files still contain inline PackageReference Version= attributes: {string.Join(", ", violations)}");
    }
}
```

### Step 3 — Run tests and confirm RED

```powershell
$result = dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "FullyQualifiedName~CpmFoundationTests" `
    --no-build 2>&1

Write-Host $result
# Expected: FAILED — DirectoryPackagesProps_Exists, ManagePackageVersionsCentrally_IsEnabled, CsProjFiles_HaveNo_InlineVersions
```

---

## Task 3 — GREEN: Create Directory.Build.props

### Step 4 — Write Directory.Build.props

File: `C:\Users\Lance\Dev\Scripts\csharp\Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-all</AnalysisLevel>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <SuppressNETCoreSdkPreviewMessage>true</SuppressNETCoreSdkPreviewMessage>
  </PropertyGroup>

  <ItemGroup>
    <Using Include="System" />
    <Using Include="System.Collections.Generic" />
    <Using Include="System.Collections.Frozen" />
    <Using Include="System.Diagnostics" />
    <Using Include="System.Globalization" />
    <Using Include="System.IO" />
    <Using Include="System.Linq" />
    <Using Include="System.Text.Json" />
    <Using Include="System.Text.Json.Serialization" />
    <Using Include="System.Text.RegularExpressions" />
    <Using Include="System.Threading" />
    <Using Include="System.Threading.Tasks" />
    <Using Include="Serilog" />
    <Using Include="Serilog.Events" />
  </ItemGroup>

  <ItemGroup Condition="'$(MSBuildProjectName)' != 'Scripts.Core'">
    <Using Include="CSharpScripts.Core" />
  </ItemGroup>
</Project>
```

### Step 5 — Verify file was written

```powershell
$buildProps = 'C:\Users\Lance\Dev\Scripts\csharp\Directory.Build.props'
if (-not (Test-Path $buildProps)) { throw "Directory.Build.props was not created" }

$content = Get-Content $buildProps -Raw -Encoding UTF8
if ($content -notmatch '<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>') {
    throw "ManagePackageVersionsCentrally is not set to true in Directory.Build.props"
}
Write-Host "OUTCOME: Directory.Build.props verified OK"
```

---

## Task 4 — GREEN: Create Directory.Packages.props

### Step 6 — Write Directory.Packages.props

File: `C:\Users\Lance\Dev\Scripts\csharp\Directory.Packages.props`

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Core -->
    <PackageVersion Include="Serilog" Version="4.2.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />
    <PackageVersion Include="Serilog.Enrichers.Process" Version="3.0.0" />
    <PackageVersion Include="Serilog.Enrichers.Thread" Version="4.0.0" />
    <PackageVersion Include="Serilog.Formatting.Compact" Version="3.0.0" />
    <PackageVersion Include="Polly" Version="8.4.2" />
    <PackageVersion Include="Polly.RateLimiting" Version="8.4.2" />
    <PackageVersion Include="Google.Apis.Auth" Version="1.69.0" />
    <PackageVersion Include="Ben.Demystifier" Version="0.4.1" />
    <!-- Data -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.0" />
    <PackageVersion Include="Npgsql" Version="10.0.0" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    <PackageVersion Include="CsvHelper" Version="33.0.1" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
    <!-- Language -->
    <PackageVersion Include="Azure.AI.Translation.Text" Version="1.0.0" />
    <PackageVersion Include="Azure.Identity" Version="1.13.2" />
    <PackageVersion Include="RestSharp" Version="112.1.0" />
    <PackageVersion Include="SearchPioneer.Lingua" Version="1.0.5" />
    <!-- Music -->
    <PackageVersion Include="MetaBrainz.MusicBrainz" Version="6.2.0" />
    <PackageVersion Include="ParkSquare.Discogs" Version="3.0.0" />
    <PackageVersion Include="Riok.Mapperly" Version="3.7.0" />
    <!-- Orchestrators -->
    <PackageVersion Include="Hqub.Last.fm" Version="3.0.0" />
    <PackageVersion Include="Google.Apis.YouTube.v3" Version="1.69.0" />
    <PackageVersion Include="Google.Apis.Sheets.v4" Version="1.69.0" />
    <PackageVersion Include="Google.Apis.Drive.v3" Version="1.69.0" />
    <PackageVersion Include="Google.Apis" Version="1.69.0" />
    <!-- Reader -->
    <PackageVersion Include="Microsoft.Playwright" Version="1.49.0" />
    <PackageVersion Include="AngleSharp" Version="1.2.0" />
    <PackageVersion Include="SmartReader" Version="1.0.0" />
    <PackageVersion Include="PdfPig" Version="0.1.9" />
    <PackageVersion Include="Azure.AI.DocumentIntelligence" Version="1.0.0" />
    <PackageVersion Include="Google.Cloud.Vision.V1" Version="3.9.0" />
    <PackageVersion Include="Google.Cloud.DocumentAI.V1" Version="3.14.0" />
    <!-- CLI -->
    <PackageVersion Include="Spectre.Console" Version="0.49.1" />
    <PackageVersion Include="Spectre.Console.Cli" Version="0.49.1" />
    <!-- Test -->
    <PackageVersion Include="TUnit" Version="0.9.0" />
    <PackageVersion Include="FluentAssertions" Version="7.0.0" />
    <PackageVersion Include="Testcontainers.PostgreSql" Version="3.10.0" />
    <PackageVersion Include="System.Threading.RateLimiting" Version="10.0.0" />
  </ItemGroup>
</Project>
```

### Step 7 — Verify file was written

```powershell
$pkgProps = 'C:\Users\Lance\Dev\Scripts\csharp\Directory.Packages.props'
if (-not (Test-Path $pkgProps)) { throw "Directory.Packages.props was not created" }
Write-Host "OUTCOME: Directory.Packages.props verified OK"
```

---

## Task 5 — GREEN: Strip inline Version= from all .csproj files

### Step 8 — Scan and strip inline PackageReference Version= attributes

```powershell
Write-Host "STATE: Scanning all .csproj files for inline Version= attributes"
Write-Host "REASON: CPM requires version-free PackageReference elements"

$csprojFiles = Get-ChildItem -Path 'C:\Users\Lance\Dev\Scripts\csharp' -Filter '*.csproj' -Recurse |
    Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' }

$ts = Get-Date -Format 'yyyyMMdd_HHmmss'

foreach ($file in $csprojFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8

    # Check if this file has inline versions
    if ($content -match 'PackageReference.+Version="') {
        Write-Host "WHAT: Stripping inline versions from $($file.Name)"

        # Backup before modifying
        $bak = "$($file.FullName).bak.$ts"
        Copy-Item $file.FullName $bak -ErrorAction Stop
        if (-not (Test-Path $bak)) { throw "Backup failed for $($file.FullName)" }

        # Remove Version="..." attribute from PackageReference lines only
        # Matches: Version="x.y.z" (with optional spaces around =)
        $updated = [System.Text.RegularExpressions.Regex]::Replace(
            $content,
            '(<PackageReference[^>]+?)\s+Version="[^"]*"',
            '$1'
        )

        Set-Content -Path $file.FullName -Value $updated -Encoding UTF8 -ErrorAction Stop

        # Verify
        $verify = Get-Content $file.FullName -Raw -Encoding UTF8
        if ($verify -match 'PackageReference.+Version="') {
            throw "Inline versions still present in $($file.FullName) after stripping"
        }
        Write-Host "OUTCOME: $($file.Name) — inline versions removed"
    } else {
        Write-Host "OUTCOME: $($file.Name) — no inline versions (skip)"
    }
}
```

---

## Task 6 — REFACTOR: Restore + full build verification

### Step 9 — Restore and build

```powershell
Write-Host "STATE: Running dotnet restore then dotnet build to verify CPM is functional"
Write-Host "REASON: CPM will fail loudly if any package is referenced without a PackageVersion entry"

$restoreOutput = dotnet restore 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' 2>&1
Write-Host $restoreOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE" }

$buildOutput = dotnet build 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }

# Expected output contains:
# Build succeeded.
# 0 Error(s)
```

### Step 10 — Run tests GREEN

```powershell
$testOutput = dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' `
    --filter "FullyQualifiedName~CpmFoundationTests" 2>&1
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) { throw "CpmFoundationTests failed" }
# Expected: All 3 tests passed
```

---

## Task 7 — Commit

```powershell
git -C 'C:\Users\Lance\Dev\Scripts' add `
    'csharp/Directory.Build.props' `
    'csharp/Directory.Packages.props' `
    'csharp/tests/Scripts.Tests/CpmFoundationTests.cs'

# Stage any .csproj files that were stripped of inline versions
git -C 'C:\Users\Lance\Dev\Scripts' add 'csharp/src/*.csproj'
git -C 'C:\Users\Lance\Dev\Scripts' add 'csharp/**/*.csproj'

git -C 'C:\Users\Lance\Dev\Scripts' commit -m "feat(t2-00): enable CPM, create Directory.Build.props and Directory.Packages.props, strip inline versions"
```

---

## Sign-off Criteria

- [ ] `C:\Users\Lance\Dev\Scripts\csharp\Directory.Build.props` exists and contains `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`
- [ ] `C:\Users\Lance\Dev\Scripts\csharp\Directory.Packages.props` exists and lists all packages with `<PackageVersion>` elements
- [ ] Zero `.csproj` files contain `PackageReference ... Version="` (inline versions)
- [ ] `dotnet restore csharp/Scripts.slnx` exits 0
- [ ] `dotnet build csharp/Scripts.slnx` exits 0 with 0 errors
- [ ] `CpmFoundationTests` — all 3 tests GREEN
