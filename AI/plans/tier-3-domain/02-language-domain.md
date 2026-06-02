# Language Domain Isolation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Verify and enforce that the Language domain references only Core, and that `LanguageIdentifier` is declared `internal` so it is not accidentally consumed outside the Language assembly.

**Architecture:** Scripts.Services.Language wraps Azure Translation and Lingua language detection. It must be fully self-contained — no knowledge of the database, Music, or Orchestrators. The `LanguageIdentifier` class is internal to the assembly; external consumers receive the `ILanguageDetector` abstraction from Core.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Language Detection Migration Context (from ADVANCED-FEATURES research)

### Current State: NTextCat (Obsolete)

The Language domain currently uses **NTextCat** with a `Core14.profile.xml` file that does NOT exist in the repository. This is a blocker for compilation.

### Target: SearchPioneer.Lingua v1.0.5

**Lingua** is the replacement:
- **Package**: `SearchPioneer.Lingua` v1.0.5
- **Languages**: 79 (vs NTextCat's 15)
- **Model Loading**: Embedded in NuGet package — no file-based profile distribution needed
- **Dependencies**: Zero — fully self-contained

### Updated LanguageIdentifier Implementation

```csharp
using Lingua;
using static Lingua.Language;

namespace Scripts.Services.Language;

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

### Changes Required

1. Add `<PackageReference Include="SearchPioneer.Lingua" Version="1.0.5" />` to `Scripts.Services.Language.csproj`
2. Replace NTextCat builder with Lingua builder in `LanguageIdentifier.cs`
3. Update method calls: `.Identify(text).FirstOrDefault()?.Item1.Iso639_3` → `.DetectLanguageOf(text).IsoCode6393()`
4. Remove `Core14.profile.xml` file reference (no longer needed)

---

## Pre-flight Checks

```powershell
if (-not (Get-Command pwsh -ErrorAction SilentlyContinue)) { throw "pwsh not found" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "dotnet SDK not found" }
dotnet --version | Select-String "^10\." || throw ".NET 10 SDK not found"

# T3 depends on T2 sign-off — Scripts.slnx must exist
if (-not (Test-Path '/home/lance/Scripts/csharp/Scripts.slnx')) {
    throw 'Tier 2 sign-off required — Scripts.slnx not found. Run T2 plans first.'
}

dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx --no-restore -ErrorAction Stop
# Expected: Build succeeded. 0 Error(s).
```

---

## Task 1 — TDD RED: Write Language domain isolation tests

**Current State:** No tests assert Language dependency constraints or `LanguageIdentifier` visibility.
**Reason:** Failing tests drive isolation and access-modifier enforcement.
**What:** Create `T302_LanguageDomainTests.cs` in `Scripts.Tests\T3\`.
**Expected Outcome:** Tests compile; isolation tests fail if violations exist; `LanguageIdentifier` test fails if class is `public`.

### Step 1.1 — Create test file

```powershell
$dir = "/home/lance/Scripts/csharp/tests\Scripts.Tests\T3"
New-Item -ItemType Directory -Path $dir -Force -ErrorAction Stop
Test-Path $dir | Should -Be $true
```

Create file `/home/lance/Scripts/csharp/tests\Scripts.Tests\T3\T302_LanguageDomainTests.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using TUnit.Core;

namespace CSharpScripts.Tests.T3;

public class T302_LanguageDomainTests
{
    private const string LanguageCsproj =
        @"/home/lance/Scripts/csharp/src\Services\Language\Scripts.Services.Language.csproj";

    private const string LanguageSrcDir =
        @"/home/lance/Scripts/csharp/src\Services\Language";

    [Test]
    public void LanguageDomain_HasNoDependencies_OnDataOrMusic()
    {
        File.Exists(LanguageCsproj).Should().BeTrue(
            "because Scripts.Services.Language.csproj must exist at the expected path");

        var content = File.ReadAllText(LanguageCsproj);

        content.Should().NotContain("Scripts.Data",
            "because Language must not reference the Data layer");
        content.Should().NotContain("Scripts.Services.Music",
            "because Language must not reference the Music service");
        content.Should().NotContain("Scripts.Orchestrators",
            "because Language must not reference Orchestrators");
        content.Should().NotContain("Scripts.Reader",
            "because Language must not reference the Reader domain");
    }

    [Test]
    public void LanguageDomain_AllFiles_HaveCorrectNamespace()
    {
        Directory.Exists(LanguageSrcDir).Should().BeTrue(
            $"because Language source directory must exist at {LanguageSrcDir}");

        var files = Directory
            .GetFiles(LanguageSrcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        files.Should().NotBeEmpty(
            "because Scripts.Services.Language must contain at least one .cs file");

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("namespace "))
                continue;

            content.Should().Contain("namespace CSharpScripts.Services.Language",
                $"because {Path.GetFileName(file)} has a wrong namespace — expected CSharpScripts.Services.Language.*");
        }
    }

    [Test]
    public void LanguageIdentifier_IsInternal_ToLanguageProject()
    {
        // LanguageIdentifier should be internal so it cannot be consumed outside the assembly
        // without the ILanguageDetector abstraction (defined in Core).
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Scripts.Services.Language");

        // If the assembly isn't loaded, we verify via reflection on the source file
        if (assembly is null)
        {
            // Fallback: grep the source for class declaration
            var files = Directory
                .GetFiles(LanguageSrcDir, "LanguageIdentifier.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(@"\obj\"))
                .ToList();

            files.Should().HaveCount(1,
                "because there must be exactly one LanguageIdentifier.cs in the Language project");

            var src = File.ReadAllText(files[0]);
            src.Should().NotMatchRegex(@"public\s+(class|sealed\s+class|partial\s+class)\s+LanguageIdentifier",
                "because LanguageIdentifier must be declared internal, not public");
            src.Should().MatchRegex(@"internal\s+(sealed\s+)?(class|partial\s+class)\s+LanguageIdentifier",
                "because LanguageIdentifier must be declared internal");
        }
        else
        {
            var type = assembly.GetType("CSharpScripts.Services.Language.LanguageIdentifier");
            type.Should().NotBeNull(
                "because LanguageIdentifier must exist in the Language assembly");
            type!.IsPublic.Should().BeFalse(
                "because LanguageIdentifier is an implementation detail — use ILanguageDetector from Core");
        }
    }

    [Test]
    public void LanguageDomain_DoesNotImport_DataNamespace_InSourceFiles()
    {
        var files = Directory
            .GetFiles(LanguageSrcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            content.Should().NotContain("using CSharpScripts.Data",
                $"because {Path.GetFileName(file)} must not import from the Data namespace");
            content.Should().NotContain("using CSharpScripts.Services.Music",
                $"because {Path.GetFileName(file)} must not import from the Music namespace");
        }
    }
}
```

### Step 1.2 — Run to confirm RED

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "FullyQualifiedName~T302_LanguageDomainTests" `
    2>&1 | Tee-Object -Variable testOutput

Write-Host ($testOutput -join "`n")
# If all pass → Language already isolated → skip to Task 5 (commit)
# If any fail → proceed to Task 2
```

---

## Task 2 — Inspect current Language dependencies

**Current State:** Unknown whether Language has illegal imports.
**Reason:** Need the exact list of violations before editing.
**What:** Grep `.csproj` and all `.cs` files for illegal references.
**Expected Outcome:** Explicit list with file paths and line numbers.

```powershell
$langDir = "/home/lance/Scripts/csharp/src\Services\Language"

Write-Host "=== .csproj ProjectReferences ==="
Get-Content "/home/lance/Scripts/csharp/src\Services\Language\Scripts.Services.Language.csproj" |
    Select-String "ProjectReference"

Write-Host "=== Source files importing CSharpScripts.Data ==="
Get-ChildItem $langDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "CSharpScripts\.Data" |
    ForEach-Object { "$($_.Path):$($_.LineNumber)  $($_.Line.Trim())" }

Write-Host "=== Source files importing CSharpScripts.Services.Music ==="
Get-ChildItem $langDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "CSharpScripts\.Services\.Music" |
    ForEach-Object { "$($_.Path):$($_.LineNumber)  $($_.Line.Trim())" }

Write-Host "=== LanguageIdentifier access modifier ==="
Get-ChildItem $langDir -Recurse -Filter "LanguageIdentifier.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    ForEach-Object { Select-String "class LanguageIdentifier" $_.FullName }
```

---

## Task 3 — GREEN: Remove illegal Language → Data/Music references

> Skip if Task 2 found no `.csproj` or source violations.

**Current State:** Language references Data or Music.
**Reason:** Violates the dependency rule: Language → Core only.
**What:** Remove illegal project references from `.csproj`; remove using directives from source files.
**Expected Outcome:** Language `.csproj` references only `Scripts.Core`.

### Step 3.1 — Back up affected files

```powershell
$src = "/home/lance/Scripts/csharp/src\Services\Language\<FileName>.cs"
$bak = "$src.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $src -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true
Write-Host "Backed up: $bak"
```

### Step 3.2 — Remove illegal using directives from source files

```powershell
$file    = "/home/lance/Scripts/csharp/src\Services\Language\<FileName>.cs"
$content = Get-Content $file -Raw -Encoding UTF8

$updated = $content `
    -replace "using CSharpScripts\.Data[^;]*;(\r?\n)?", "" `
    -replace "using CSharpScripts\.Services\.Music[^;]*;(\r?\n)?", ""

Set-Content -Path $file -Value $updated -Encoding UTF8 -ErrorAction Stop

$check = Get-Content $file -Raw -Encoding UTF8
$check | Should -Not -Match "using CSharpScripts\.Data"
$check | Should -Not -Match "using CSharpScripts\.Services\.Music"
Write-Host "Cleaned: $file"
```

### Step 3.3 — Remove illegal project references from .csproj

```powershell
$csproj = "/home/lance/Scripts/csharp/src\Services\Language\Scripts.Services.Language.csproj"
$bak    = "$csproj.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $csproj -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true

$xml     = [xml](Get-Content $csproj -Encoding UTF8)
$illegal = @("Scripts.Data", "Scripts.Orchestrators", "Scripts.Reader", "Scripts.Services.Music")

foreach ($pattern in $illegal) {
    $refs = $xml.Project.ItemGroup.ProjectReference |
        Where-Object { $_.Include -like "*$pattern*" }
    foreach ($ref in $refs) {
        Write-Host "Removing: $($ref.Include)"
        $ref.ParentNode.RemoveChild($ref) | Out-Null
    }
}

$xml.Save($csproj)
Test-Path $csproj | Should -Be $true
Write-Host "Updated: $csproj"
```

---

## Task 4 — GREEN: Make LanguageIdentifier internal

> Skip if the `LanguageIdentifier_IsInternal` test already passes.

**Current State:** `LanguageIdentifier` is declared `public`.
**Reason:** It is an implementation detail — external consumers must use `ILanguageDetector` from Core.
**What:** Change access modifier from `public` to `internal sealed`.
**Expected Outcome:** `LanguageIdentifier` is no longer publicly accessible; `ILanguageDetector` is the public contract.

### Step 4.1 — Locate LanguageIdentifier.cs

```powershell
$langDir = "/home/lance/Scripts/csharp/src\Services\Language"
$file = Get-ChildItem $langDir -Recurse -Filter "LanguageIdentifier.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-Object -ExpandProperty FullName -First 1

if (-not $file) { throw "LanguageIdentifier.cs not found in Language project" }
Write-Host "Found: $file"
```

### Step 4.2 — Back up and modify

```powershell
$bak = "$file.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $file -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true

$content = Get-Content $file -Raw -Encoding UTF8

# Replace 'public class' or 'public sealed class' with 'internal sealed class'
$updated = $content `
    -replace "public\s+class\s+LanguageIdentifier",         "internal sealed class LanguageIdentifier" `
    -replace "public\s+sealed\s+class\s+LanguageIdentifier","internal sealed class LanguageIdentifier" `
    -replace "public\s+partial\s+class\s+LanguageIdentifier","internal sealed partial class LanguageIdentifier"

Set-Content -Path $file -Value $updated -Encoding UTF8 -ErrorAction Stop

# Verify
$check = Get-Content $file -Raw -Encoding UTF8
$check | Should -Match "internal sealed class LanguageIdentifier"
$check | Should -Not -Match "public\s+(sealed\s+)?class LanguageIdentifier"
Write-Host "LanguageIdentifier is now internal sealed"
```

### Step 4.3 — Add InternalsVisibleTo for Tests (if needed)

If `Scripts.Tests` directly instantiates `LanguageIdentifier` (it shouldn't — but check):

```powershell
$langCsproj = "/home/lance/Scripts/csharp/src\Services\Language\Scripts.Services.Language.csproj"
$content    = Get-Content $langCsproj -Raw -Encoding UTF8

if ($content -notmatch "InternalsVisibleTo") {
    # No InternalsVisibleTo needed — tests use ILanguageDetector, not LanguageIdentifier directly
    Write-Host "No InternalsVisibleTo needed — tests use the public ILanguageDetector interface"
}
```

---

## Task 5 — Build and test GREEN

**Current State:** Source changes applied.
**Reason:** Confirm compilation succeeds and all Language isolation tests pass.
**What:** Full restore → build → targeted test run.
**Expected Outcome:** 0 errors, all T302 tests pass.

```powershell
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop

$buildOut = dotnet build /home/lance/Scripts/csharp/Scripts.slnx --no-restore 2>&1
$buildOut | Select-String "0 Error" | Should -Not -BeNullOrEmpty
Write-Host "Build: GREEN"

$testOut = dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "FullyQualifiedName~T302_LanguageDomainTests" 2>&1
$testOut | Select-String "Failed: 0" | Should -Not -BeNullOrEmpty
Write-Host "T302 tests: GREEN"
```

Expected output:
```
Test Run Successful.
Tests: 4 (4 passed)
```

---

## Task 6 — REFACTOR: Commit isolation

**Current State:** Tests green, Language isolated, `LanguageIdentifier` internal.
**Reason:** Record as a discrete commit.
**What:** Stage all Language + test changes, commit.
**Expected Outcome:** Commit `feat(t3-02)` in git log.

```powershell
Set-Location /home/lance/Scripts -ErrorAction Stop

gitleaks detect --no-git 2>&1 | Select-String "leaks found" | ForEach-Object {
    throw "Gitleaks found secrets — abort commit"
}

git add csharp/src/Services/Language/ `
        csharp/tests/Scripts.Tests/T3/T302_LanguageDomainTests.cs 2>&1
git status 2>&1 | Write-Host

git commit -m "feat(t3-02): isolate Language domain — internal LanguageIdentifier, remove Data/Music refs" `
    -ErrorAction Stop 2>&1 | Tee-Object -Variable commitOut

$commitOut | Select-String "feat\(t3-02\)" | Should -Not -BeNullOrEmpty
Write-Host "Committed: t3-02"
```

---

## Completion Criteria

| Check | Command | Expected |
|-------|---------|----------|
| Build clean | `dotnet build csharp/Scripts.slnx` | `0 Error(s)` |
| Tests pass | `dotnet test --filter T302` | `Failed: 0` |
| No Data ref | `grep -r "Scripts.Data" csharp/src/Services/Language/` | No output |
| No Music ref | `grep -r "Scripts.Services.Music" csharp/src/Services/Language/` | No output |
| `LanguageIdentifier` internal | Source file contains `internal sealed class LanguageIdentifier` | Verified |
| Namespace correct | All files contain `CSharpScripts.Services.Language` | Verified |
| Commit present | `git log --oneline -1` | `feat(t3-02)` |
