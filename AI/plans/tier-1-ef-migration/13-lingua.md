# T1-13: Lingua Language Detection Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace NTextCat with `SearchPioneer.Lingua v1.0.5` in `LanguageIdentifier.cs`, removing the dependency on a missing `Core14.profile.xml` file and enabling self-contained language detection for 79 languages.

**Architecture:** Rewrite `LanguageIdentifier.cs` to use Lingua's fluent API: `LanguageDetectorBuilder.FromAllLanguages().WithPreloadedLanguageModels().Build()`. The public API contract (`Detect → string?`, `IsEnglish → bool`, `RequiresTranslation → bool`) is preserved. `Language.Unknown` replaces NTextCat's null return. The file is un-excluded from the build (`<Compile Remove>` line removed from `.csproj`). No file-based profiles are needed — Lingua embeds all language models.

**Tech Stack:** C# 14 / .NET 10 / SearchPioneer.Lingua 1.0.5 / TUnit / FluentAssertions

---

## Prerequisites

- T1-12 completed (logging relocated, ServiceType cleaned)
- `Scripts.Tests` project exists
- `C:\Users\Lance\Dev\Scripts\csharp\src\Services\Language\LanguageIdentifier.cs` exists (currently excluded from build)
- No callers of `LanguageIdentifier` exist in the codebase (zero-risk migration)

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Services\Language\LanguageIdentifier.cs
# Expected: True

Get-Content C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj | Select-String "LanguageIdentifier.cs"
# Expected: <Compile Remove="src\Services\Language\LanguageIdentifier.cs" />
```

---

## Task 1 — Add SearchPioneer.Lingua 1.0.5 NuGet Package

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj`

### Step 0: Preflight

```powershell
# Current state: SearchPioneer.Lingua is not referenced. NTextCat exists only as transitive dependency from SmartReader.
# LanguageIdentifier.cs is excluded from build.
# Reason: Lingua is the target language detection library. NTextCat requires missing Core14.profile.xml.
# What: Add <PackageReference Include="SearchPioneer.Lingua" Version="1.0.5" /> to csproj.
# Expected: NuGet referenced, dotnet restore succeeds.

Get-Content C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj | Select-String "Lingua"
# Expected: (no output)
```

### Step 1: Write test

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\LinguaPackageReferenceTests.cs
using System.Xml.Linq;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Language;

public sealed class LinguaPackageReferenceTests
{
    private static readonly string CsprojPath =
        @"C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj";

    [Test]
    public async Task Csproj_References_SearchPioneer_Lingua()
    {
        var xml = await File.ReadAllTextAsync(CsprojPath);
        var doc = XDocument.Parse(xml);
        var ns = doc.Root!.GetDefaultNamespace();

        var linguaRef = doc.Root!
            .Elements(ns + "ItemGroup")
            .SelectMany(g => g.Elements(ns + "PackageReference"))
            .FirstOrDefault(e => e.Attribute("Include")?.Value
                == "SearchPioneer.Lingua");

        linguaRef.Should().NotBeNull("because LanguageIdentifier now uses SearchPioneer.Lingua v1.0.5");
    }

    [Test]
    public async Task Csproj_Lingua_Version_Is_One_Dot_Zero_Dot_Five()
    {
        var xml = await File.ReadAllTextAsync(CsprojPath);
        var doc = XDocument.Parse(xml);
        var ns = doc.Root!.GetDefaultNamespace();

        var linguaRef = doc.Root!
            .Elements(ns + "ItemGroup")
            .SelectMany(g => g.Elements(ns + "PackageReference"))
            .FirstOrDefault(e => e.Attribute("Include")?.Value
                == "SearchPioneer.Lingua");

        linguaRef.Should().NotBeNull();

        var version = linguaRef!.Attribute("Version")?.Value;
        version.Should().Be("1.0.5",
            $"because the target version is 1.0.5. Actual: {version}"
        );
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\LinguaPackageReferenceTests.cs'
Test-Path $file
# Expected: True

New-Item -ItemType Directory -Force -Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language' -ErrorAction SilentlyContinue
```

### Step 3: Run test (expect RED — Lingua not yet referenced)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "LinguaPackageReferenceTests" 2>&1
```

Expected: RED — `linguaRef` is null. SearchPioneer.Lingua is not in csproj.

### Step 4: Assess

Add one `PackageReference` line. No other csproj changes needed at this stage.

### Step 5: Implement

Add after line 50 (`<PackageReference Include="Polly.RateLimiting" Version="*" />`) in `C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj`:

```xml
		<PackageReference Include="SearchPioneer.Lingua" Version="1.0.5" />
```

Run restore:

```powershell
dotnet restore C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj 2>&1
```

Expected: Restore completed successfully.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "LinguaPackageReferenceTests" 2>&1
```

Expected: GREEN — both tests pass.

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\LinguaPackageReferenceTests.cs
git commit -m "feat(t1-13): add searchpioneer lingua 1.0.5 nuget package"
```

---

## Task 2 — Rewrite LanguageIdentifier.cs with Lingua API

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Services\Language\LanguageIdentifier.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\LanguageIdentifierTests.cs`

### Step 0: Preflight

```powershell
# Current state: LanguageIdentifier.cs uses NTextCat API (RankedLanguageIdentifier, RankedLanguageIdentifierFactory, LanguageInfo).
# Core14.profile.xml does not exist. File is excluded from build.
# Reason: Replace NTextCat with Lingua for self-contained detection without profile files.
# What: Rewrite using Lingua API, preserve public contract.
# Expected: LanguageIdentifier.cs compiles with Lingua only, no NTextCat types.

Get-Content C:\Users\Lance\Dev\Scripts\csharp\src\Services\Language\LanguageIdentifier.cs
# Expected: NTextCat types visible (RankedLanguageIdentifier, RankedLanguageIdentifierFactory, LanguageInfo)

Test-Path C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\LanguageIdentifierTests.cs
# Expected: False
```

### Step 1: Write tests

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\LanguageIdentifierTests.cs
using FluentAssertions;
using TUnit;
using CSharpScripts.Services.Language;

namespace Scripts.Tests.Language;

public sealed class LanguageIdentifierTests
{
    [Test]
    public void Detect_English_Returns_eng()
    {
        var result = LanguageIdentifier.Detect(
            "This is a test sentence in English language with enough characters"
        );

        result.Should().Be("eng",
            $"because Lingua must detect English text correctly. Actual: {result}"
        );
    }

    [Test]
    public void Detect_Japanese_Returns_jpn()
    {
        var result = LanguageIdentifier.Detect(
            "これは日本語のテスト文章です十分な文字数があります"
        );

        result.Should().Be("jpn",
            $"because Lingua must detect Japanese text. Actual: {result}"
        );
    }

    [Test]
    public void Detect_Short_Text_Returns_Null()
    {
        var result = LanguageIdentifier.Detect("hi");

        result.Should().BeNull(
            "because text shorter than 15 characters returns null"
        );
    }

    [Test]
    public void Detect_Empty_Text_Returns_Null()
    {
        var result = LanguageIdentifier.Detect("");

        result.Should().BeNull("because empty string returns null");
    }

    [Test]
    public void Detect_Null_Text_Returns_Null()
    {
        var result = LanguageIdentifier.Detect(null!);

        result.Should().BeNull("because null text returns null");
    }

    [Test]
    public void Detect_Whitespace_Only_Returns_Null()
    {
        var result = LanguageIdentifier.Detect("               ");

        result.Should().BeNull("because whitespace-only string returns null");
    }

    [Test]
    public void IsEnglish_Returns_True_For_English_Text()
    {
        var result = LanguageIdentifier.IsEnglish(
            "The quick brown fox jumps over the lazy dog in the meadow"
        );

        result.Should().BeTrue("because English text must be identified as English");
    }

    [Test]
    public void RequiresTranslation_Returns_True_For_Non_English_Text()
    {
        var result = LanguageIdentifier.RequiresTranslation(
            "Bonjour le monde ceci est une phrase francaise"
        );

        result.Should().BeTrue("because French text requires translation");
    }

    [Test]
    public void RequiresTranslation_Returns_False_For_English_Text()
    {
        var result = LanguageIdentifier.RequiresTranslation(
            "This is a very long English sentence that should not require any translation"
        );

        result.Should().BeFalse("because English text does not require translation");
    }

    [Test]
    public void Detect_Does_Not_Throw_For_Missing_Profile()
    {
        // Lingua has no file-based profile — this test asserts no FileNotFound-like exceptions
        var action = () => LanguageIdentifier.Detect(
            "Some random text that is long enough for detection purposes"
        );

        action.Should().NotThrow(
            "because Lingua embeds language models — no profile file needed"
        );
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\LanguageIdentifierTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect RED — LanguageIdentifier.cs is excluded from build, won't compile)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "LanguageIdentifierTests" 2>&1
```

Expected: RED — LanguageIdentifier is excluded from build. Compilation fails with "The type or namespace name 'LanguageIdentifier' could not be found".

### Step 4: Assess

Three changes needed:
1. Rewrite `LanguageIdentifier.cs` with Lingua API
2. Remove `<Compile Remove>` line from `CSharpScripts.csproj`
3. Verify build compiles without NTextCat dependency

### Step 5: Implement

**Rewrite `C:\Users\Lance\Dev\Scripts\csharp\src\Services\Language\LanguageIdentifier.cs`:**

OLD (full file):
```csharp
namespace CSharpScripts.Services.Language;

internal static class LanguageIdentifier
{
	private static Lazy<RankedLanguageIdentifier?> Detector { get; } =
		new(() =>
		{
			var exeDir = AppContext.BaseDirectory;
			var profilePath = Path.Combine(path1: exeDir, path2: "Core14.profile.xml");

			if (!File.Exists(path: profilePath))
			{
				Log.Warning(messageTemplate: "Language profile not found: {Path}", profilePath);
				return null;
			}

			return new RankedLanguageIdentifierFactory().Load(inputFilePath: profilePath);
		});

	public static string? Detect(string text)
	{
		if (IsNullOrWhiteSpace(value: text) || text.Length < 15)
			return null;

		Tuple<LanguageInfo, double>? result = Detector.Value?.Identify(text: text).FirstOrDefault();

		return result?.Item1.Iso639_3;
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

NEW:
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

**Remove `<Compile Remove>` from `C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj`:**

Delete line 21:
```xml
		<Compile Remove="src\Services\Language\LanguageIdentifier.cs" />
```

Remove this line entirely.

**Verify build:**

```powershell
dotnet build C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj 2>&1
```

Expected: Build succeeded with 0 errors.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "LanguageIdentifierTests" 2>&1
```

Expected: GREEN — all 10 tests pass:
- `Detect_English_Returns_eng`: PASS
- `Detect_Japanese_Returns_jpn`: PASS
- `Detect_Short_Text_Returns_Null`: PASS
- `Detect_Empty_Text_Returns_Null`: PASS
- `Detect_Null_Text_Returns_Null`: PASS
- `Detect_Whitespace_Only_Returns_Null`: PASS
- `IsEnglish_Returns_True_For_English_Text`: PASS
- `RequiresTranslation_Returns_True_For_Non_English_Text`: PASS
- `RequiresTranslation_Returns_False_For_English_Text`: PASS
- `Detect_Does_Not_Throw_For_Missing_Profile`: PASS

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\src\Services\Language\LanguageIdentifier.cs
git add C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\LanguageIdentifierTests.cs
git commit -m "feat(t1-13): rewrite languageidentifier with searchpioneer lingua api"
```

---

## Task 3 — Verify No NTextCat Types Remain

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\NTextCatRemovalGuardTests.cs`

### Step 0: Preflight

```powershell
# Current state: After rewrite, no NTextCat types should remain in source. SmartReader may still
# transitively reference NTextCat, but our source code must not.
# Reason: Ensure complete removal — no residual NTextCat API usage.
# What: Regex-scan all source files for NTextCat type names.
# Expected: Zero matches.

Get-ChildItem C:\Users\Lance\Dev\Scripts\csharp\src\*.cs -Recurse | Select-String "RankedLanguageIdentifier|LanguageInfo" -SimpleMatch
# Expected: (no output)
```

### Step 1: Write test

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\NTextCatRemovalGuardTests.cs
using System.Text.RegularExpressions;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Language;

public sealed class NTextCatRemovalGuardTests
{
    private static readonly string SourceRoot =
        @"C:\Users\Lance\Dev\Scripts\csharp\src";

    private static readonly string[] NTextCatTypes =
    {
        "RankedLanguageIdentifier",
        "RankedLanguageIdentifierFactory",
        "LanguageInfo"
    };

    [Test]
    public async Task No_NTextCat_Types_In_Source()
    {
        var allFiles = Directory.GetFiles(
            SourceRoot,
            "*.cs",
            SearchOption.AllDirectories
        );

        var violations = new List<string>();
        foreach (var file in allFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            foreach (var typeName in NTextCatTypes)
            {
                if (content.Contains(typeName))
                    violations.Add($"{file}: contains {typeName}");
            }
        }

        violations.Should().BeEmpty(
            $"because NTextCat has been replaced with Lingua.\nViolations:\n{string.Join("\n", violations)}"
        );
    }

    [Test]
    public async Task No_Core14_Profile_Xml_Reference_In_Source()
    {
        var allFiles = Directory.GetFiles(
            SourceRoot,
            "*.cs",
            SearchOption.AllDirectories
        );

        var violations = new List<string>();
        foreach (var file in allFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            if (content.Contains("Core14.profile.xml"))
                violations.Add(file);
        }

        violations.Should().BeEmpty(
            "because Lingua embeds language models — no profile file reference should remain"
        );
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\NTextCatRemovalGuardTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect GREEN — NTextCat already removed)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "NTextCatRemovalGuardTests" 2>&1
```

Expected: GREEN — 2 tests pass. No NTextCat types or Core14.profile.xml references remain.

### Step 4: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\NTextCatRemovalGuardTests.cs
git commit -m "feat(t1-13): add ntextcat removal guard tests"
```

---

## Verification Checklist

- [ ] `SearchPioneer.Lingua` version 1.0.5 referenced in `CSharpScripts.csproj`
- [ ] `LanguageIdentifier.cs` no longer excluded from build
- [ ] `LanguageIdentifier.cs` uses `ILanguageDetector`, `LanguageDetectorBuilder`, `IsoCode6393()`
- [ ] `LanguageIdentifier.cs` has zero NTextCat types
- [ ] `LanguageIdentifier.Detect("English text...")` returns `"eng"`
- [ ] `LanguageIdentifier.Detect("日本語...")` returns `"jpn"`
- [ ] `LanguageIdentifier.Detect("short")` returns `null`
- [ ] `LanguageIdentifier.Detect(null!)` returns `null`
- [ ] No `Core14.profile.xml` references anywhere in `csharp/src/`
- [ ] `dotnet build` passes with 0 errors
- [ ] `dotnet test` — LinguaPackageReferenceTests: 2/2 PASS
- [ ] `dotnet test` — LanguageIdentifierTests: 10/10 PASS
- [ ] `dotnet test` — NTextCatRemovalGuardTests: 2/2 PASS
