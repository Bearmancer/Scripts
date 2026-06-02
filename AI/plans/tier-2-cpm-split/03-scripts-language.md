# T2-03: Scripts.Services.Language Project Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create `Scripts.Services.Language.csproj` at `csharp/src/Services/Language/`, referencing only `Scripts.Core`, with Azure Translation, Lingua language detection, RestSharp HTTP, and Azure.Identity packages via CPM.

**Architecture:** `Scripts.Services.Language` is a leaf service project in the dependency graph — it depends only on `Scripts.Core` for logging and resilience. It must not reference `Scripts.Data`, `Scripts.Services.Music`, or any downstream project. It provides translation (Azure Cognitive Services), language detection (Lingua), and HTTP client abstraction (RestSharp) to the solution.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Language Detection: Lingua Migration

### Current State: NTextCat (Legacy)

The existing `LanguageIdentifier.cs` uses NTextCat with a `Core14.profile.xml` file that does not exist in the repository. This is a blocker for compilation.

### Target: SearchPioneer.Lingua v1.0.5

| Property | Value |
|----------|-------|
| **Package** | `SearchPioneer.Lingua` |
| **Version** | `1.0.5` |
| **Languages Supported** | 79 (vs NTextCat's 15) |
| **Model Loading** | Embedded in NuGet package — no external file distribution needed |
| **Dependencies** | Zero — fully self-contained |

### Implementation: LanguageIdentifier.cs

Replace the entire `LanguageIdentifier.cs` with:

```csharp
using Lingua;
using static Lingua.Language;

namespace CSharpScripts.Services.Language;

internal static class LanguageIdentifier
{
    private static readonly ILanguageDetector Detector = LanguageDetectorBuilder
        .FromAllLanguages()
        .WithPreloadedLanguageModels()
        .Build();

    public static string? Detect(string text)
    {
        if (IsNullOrWhiteSpace(value: text) || text.Length < 15)
            return null;

        var result = Detector.DetectLanguageOf(text);
        return result == Unknown ? null : result.IsoCode6393();
    }

    public static bool IsEnglish(string text) =>
        Detect(text: text)?.EqualsIgnoreCase(other: "eng") == true;

    public static bool RequiresTranslation(string text)
    {
        var lang = Detect(text: text);
        return lang is { } && !lang.EqualsIgnoreCase(other: "eng");
    }
}
```

### Changes Summary

| # | Change | Location |
|---|--------|----------|
| 1 | Add `using Lingua;` and `using static Lingua.Language;` | `LanguageIdentifier.cs` |
| 2 | Replace `Lazy<RankedLanguageIdentifier?>` with `ILanguageDetector` field | `LanguageIdentifier.cs` |
| 3 | Replace builder pattern with `LanguageDetectorBuilder.FromAllLanguages().WithPreloadedLanguageModels().Build()` | `LanguageIdentifier.cs` |
| 4 | Replace `.Identify(text).FirstOrDefault()?.Item1.Iso639_3` with `.DetectLanguageOf(text).IsoCode6393()` | `LanguageIdentifier.cs` |
| 5 | Add `<PackageReference Include="SearchPioneer.Lingua" />` to `Scripts.Services.Language.csproj` | Project file |

### Azure Translation Service

The `TranslationClient.cs` uses `Azure.AI.Translation.Text` for multi-language translation. This package is already declared in `Directory.Packages.props` and requires:
- Azure subscription with Translator resource
- Credentials via `Azure.Identity` (DefaultAzureCredential pattern)
- Endpoint URL from Azure portal

No code changes needed — the existing implementation is compatible with the current package version.

---

## Prerequisites

- [ ] T2-01 (Scripts.Core) is signed off — `Scripts.Core.csproj` exists and compiles
- [ ] CPM is active — `Directory.Packages.props` lists `Azure.AI.Translation.Text`, `Azure.Identity`, `RestSharp`, `SearchPioneer.Lingua`
- [ ] `/home/lance/Scripts/csharp/src\Services\Language\` directory exists (create if absent)

---

## Task 1 — Verify directory and back up any existing csproj

### Step 1 — Log current state

```powershell
Write-Host "STATE: Verifying src/Services/Language directory and any existing Scripts.Services.Language.csproj"
Write-Host "REASON: Must not overwrite without backup (Zero-Presumption Rule 9)"

$langDir  = '/home/lance/Scripts/csharp/src\Services\Language'
$langProj = Join-Path $langDir 'Scripts.Services.Language.csproj'
$ts       = Get-Date -Format 'yyyyMMdd_HHmmss'

if (-not (Test-Path $langDir)) {
    New-Item -ItemType Directory -Path $langDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $langDir)) { throw "Failed to create $langDir" }
    Write-Host "OUTCOME: Created directory $langDir"
} else {
    Write-Host "OUTCOME: Directory $langDir already exists"
}

if (Test-Path $langProj) {
    $bak = "$langProj.bak.$ts"
    Copy-Item $langProj $bak -ErrorAction Stop
    if (-not (Test-Path $bak)) { throw "Backup of Scripts.Services.Language.csproj failed" }
    Write-Host "OUTCOME: Backed up existing Scripts.Services.Language.csproj → $bak"
}
```

---

## Task 2 — TDD RED: Write failing tests

### Step 2 — Write tests

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\ScriptsLanguageProjectTests.cs`

```csharp
using System.IO;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests;

public class ScriptsLanguageProjectTests
{
    private const string LangCsproj =
        @"/home/lance/Scripts/csharp/src\Services\Language\Scripts.Services.Language.csproj";

    private const string AssemblyInfoPath =
        @"/home/lance/Scripts/csharp/src\Services\Language\Properties\AssemblyInfo.cs";

    [Test]
    public void ScriptsLanguage_CsprojFile_Exists()
    {
        File.Exists(LangCsproj).Should().BeTrue(
            "Scripts.Services.Language.csproj must exist at csharp/src/Services/Language/");
    }

    [Test]
    public void ScriptsLanguage_References_OnlyCore()
    {
        File.Exists(LangCsproj).Should().BeTrue();
        var content = File.ReadAllText(LangCsproj);

        content.Should().Contain("Scripts.Core.csproj",
            "Scripts.Services.Language must reference Scripts.Core");

        content.Should().NotContain("Scripts.Data",
            "Scripts.Services.Language must not reference Data (would create circular dependency)");
        content.Should().NotContain("Scripts.Services.Music",
            "Scripts.Services.Language must not reference Music (peer service, no dependency)");
        content.Should().NotContain("Scripts.Orchestrators",
            "Scripts.Services.Language must not reference Orchestrators");
        content.Should().NotContain("Scripts.CLI",
            "Scripts.Services.Language must not reference CLI");
        content.Should().NotContain("Scripts.Reader",
            "Scripts.Services.Language must not reference Reader");
    }

    [Test]
    public void ScriptsLanguage_HasNoInlineVersions()
    {
        File.Exists(LangCsproj).Should().BeTrue();
        var content = File.ReadAllText(LangCsproj);
        content.Should().NotMatchRegex(@"PackageReference.+Version=""",
            "Scripts.Services.Language.csproj must not contain inline Version= (CPM violation)");
    }

    [Test]
    public void ScriptsLanguage_AssemblyInfo_HasInternalsVisibleTo()
    {
        File.Exists(AssemblyInfoPath).Should().BeTrue(
            "Properties/AssemblyInfo.cs must exist in Scripts.Services.Language");
        var content = File.ReadAllText(AssemblyInfoPath);
        content.Should().Contain("InternalsVisibleTo");
        content.Should().Contain("Scripts.Tests");
    }

    [Test]
    public void ScriptsLanguage_CompilesIndependently()
    {
        File.Exists(LangCsproj).Should().BeTrue();
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            Arguments              = @"build /home/lance/Scripts/csharp/src\Services\Language\Scripts.Services.Language.csproj",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.WaitForExit();
        var stderr = proc.StandardError.ReadToEnd();
        proc.ExitCode.Should().Be(0, $"Scripts.Services.Language.csproj did not compile independently. stderr: {stderr}");
    }
}
```

### Step 3 — Run tests RED

```powershell
$result = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsLanguageProjectTests" `
    --no-build 2>&1
Write-Host $result
# Expected: FAILED — ScriptsLanguage_CsprojFile_Exists and all others fail because csproj does not exist yet
```

---

## Task 3 — GREEN: Create Scripts.Services.Language.csproj

### Step 4 — Write the project file

File: `/home/lance/Scripts/csharp/src\Services\Language\Scripts.Services.Language.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\..\Core\Scripts.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Azure.AI.Translation.Text" />
    <PackageReference Include="Azure.Identity" />
    <PackageReference Include="RestSharp" />
    <PackageReference Include="SearchPioneer.Lingua" />
  </ItemGroup>
</Project>
```

### Step 5 — Verify the project file

```powershell
$langProj = '/home/lance/Scripts/csharp/src\Services\Language\Scripts.Services.Language.csproj'
if (-not (Test-Path $langProj)) { throw "Scripts.Services.Language.csproj was not created" }

$content = Get-Content $langProj -Raw -Encoding UTF8

if ($content -notmatch 'Scripts\.Core\.csproj') {
    throw "Scripts.Services.Language.csproj must reference Scripts.Core.csproj"
}
if ($content -match 'Scripts\.Data') {
    throw "Scripts.Services.Language.csproj must not reference Data"
}
if ($content -match 'Scripts\.Services\.Music') {
    throw "Scripts.Services.Language.csproj must not reference Music"
}
if ($content -match 'PackageReference.+Version="') {
    throw "Scripts.Services.Language.csproj must not contain inline Version= (CPM violation)"
}
Write-Host "OUTCOME: Scripts.Services.Language.csproj verified OK"
```

---

## Task 4 — GREEN: Create Properties/AssemblyInfo.cs

### Step 6 — Create AssemblyInfo.cs

```powershell
$propsDir = '/home/lance/Scripts/csharp/src\Services\Language\Properties'
if (-not (Test-Path $propsDir)) {
    New-Item -ItemType Directory -Path $propsDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $propsDir)) { throw "Failed to create $propsDir" }
    Write-Host "OUTCOME: Created Properties directory"
}
```

File: `/home/lance/Scripts/csharp/src\Services\Language\Properties\AssemblyInfo.cs`

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Scripts.Tests")]
```

```powershell
$infoPath = '/home/lance/Scripts/csharp/src\Services\Language\Properties\AssemblyInfo.cs'
if (-not (Test-Path $infoPath)) { throw "AssemblyInfo.cs was not created in Scripts.Services.Language" }

$content = Get-Content $infoPath -Raw -Encoding UTF8
if ($content -notmatch 'InternalsVisibleTo') { throw "InternalsVisibleTo missing from AssemblyInfo.cs" }
if ($content -notmatch 'Scripts\.Tests')    { throw "Scripts.Tests not listed in InternalsVisibleTo" }
Write-Host "OUTCOME: AssemblyInfo.cs verified OK"
```

---

## Task 5 — GREEN: Register Scripts.Services.Language in Scripts.slnx

### Step 7 — Add to solution

```powershell
Write-Host "STATE: Adding Scripts.Services.Language.csproj to Scripts.slnx"

$slnx = '/home/lance/Scripts/csharp/Scripts.slnx'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$slnx.bak.$ts"
Copy-Item $slnx $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of Scripts.slnx failed" }

dotnet sln '/home/lance/Scripts/csharp/Scripts.slnx' `
    add '/home/lance/Scripts/csharp/src\Services\Language\Scripts.Services.Language.csproj' `
    2>&1 | Tee-Object -Variable slnOutput
Write-Host $slnOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet sln add failed for Scripts.Services.Language.csproj" }

$slnContent = Get-Content $slnx -Raw -Encoding UTF8
if ($slnContent -notmatch 'Scripts\.Services\.Language\.csproj') {
    throw "Scripts.Services.Language.csproj not found in Scripts.slnx after dotnet sln add"
}
Write-Host "OUTCOME: Scripts.Services.Language.csproj registered in solution"
```

---

## Task 6 — GREEN: Build Scripts.Services.Language

### Step 8 — Restore and build

```powershell
Write-Host "STATE: Running dotnet restore and dotnet build for Scripts.Services.Language"

$restoreOutput = dotnet restore '/home/lance/Scripts/csharp/src\Services\Language\Scripts.Services.Language.csproj' 2>&1
Write-Host $restoreOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed for Scripts.Services.Language" }

$buildOutput = dotnet build '/home/lance/Scripts/csharp/src\Services\Language\Scripts.Services.Language.csproj' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for Scripts.Services.Language" }

# Expected:
# Build succeeded.
# 0 Error(s)
```

---

## Task 7 — REFACTOR: Run all tests GREEN

### Step 9 — Run project tests

```powershell
$testOutput = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsLanguageProjectTests" 2>&1
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) { throw "ScriptsLanguageProjectTests failed" }
# Expected: All 5 tests passed
```

---

## Task 8 — Commit

```powershell
git -C '/home/lance/Scripts' add `
    'csharp/src/Services/Language/Scripts.Services.Language.csproj' `
    'csharp/src/Services/Language/Properties/AssemblyInfo.cs' `
    'csharp/tests/Scripts.Tests/ScriptsLanguageProjectTests.cs' `
    'csharp/Scripts.slnx'

git -C '/home/lance/Scripts' commit `
    -m "feat(t2-03): add Scripts.Services.Language.csproj referencing Core only, Azure Translation + Lingua + RestSharp via CPM"
```

---

## Sign-off Criteria

- [ ] `csharp/src/Services/Language/Scripts.Services.Language.csproj` exists
- [ ] References `Scripts.Core.csproj` and nothing else in `<ProjectReference>`
- [ ] Does NOT reference `Scripts.Data`, `Scripts.Services.Music`, `Scripts.Orchestrators`, `Scripts.CLI`, or `Scripts.Reader`
- [ ] Zero inline `Version=` attributes (CPM compliant)
- [ ] `csharp/src/Services/Language/Properties/AssemblyInfo.cs` has `[assembly: InternalsVisibleTo("Scripts.Tests")]`
- [ ] `Scripts.slnx` references `Scripts.Services.Language.csproj`
- [ ] `dotnet build csharp/src/Services/Language/Scripts.Services.Language.csproj` exits 0
- [ ] `ScriptsLanguageProjectTests` — all 5 tests GREEN
