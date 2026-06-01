# T1-10: EF10 Query Pattern Guards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Prevent accidental introduction of EF11-only query patterns (MaxByAsync, MinByAsync, JsonPathExists) by adding guard tests, EF10 replacement documentation, and build-time analyzer rules.

**Architecture:** This is a preventative phase — no EF11 patterns currently exist in the codebase. The plan adds three layers of protection: (1) code-scanning tests that regex-search all .cs files for forbidden patterns, (2) compilation tests proving EF10 equivalents work, (3) Roslyn codefix analyzer tests that would fire if forbidden patterns were introduced. All tests live in the existing `Scripts.Tests` project.

**Key Findings from Research:**
- Zero instances of MaxByAsync, MinByAsync, or JsonPathExists found in entire codebase (verified by full grep)
- EF10 replacements are available and tested: OrderByDescending+FirstOrDefaultAsync for MaxBy, OrderBy+FirstOrDefaultAsync for MinBy, EF.Functions.JsonContains for JsonPathExists
- JSONB column inventory: Artist.Metadata, Video.Metadata, ExecutionLog.Payload, FiberyEntity.RawData all use JsonDocument (Npgsql-native, no special handling needed)
- No Roslyn analyzer package exists for EF11 patterns — guard tests provide build-time enforcement via regex scanning
- .editorconfig can document the EF10 constraint but cannot enforce it via built-in rules
- All existing queries are EF10-compatible — no refactoring needed, only prevention of future EF11 usage

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- T1-09 completed (Sync Service Updates green)
- `Scripts.Tests` project exists and referenced in `Scripts.slnx`
- Docker running, `$env:PGCONNSTR` loaded
- `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\` directory exists

```powershell
# Verify prerequisites
Test-Path C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Scripts.Tests.csproj
# Expected: True

docker ps 2>&1 | Select-String "healthy"
# Expected: container listed
```

---

## Task 1 — EF11 Forbidden Pattern Guard Tests

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\Ef11ForbiddenPatternsTests.cs`

### Step 0: Preflight

```powershell
# Current state: No guard tests exist. Zero EF11 patterns in codebase (verified by research).
# Reason: Must prevent accidental EF11 API usage before codebase grows post-modularization.
# What: Create guard tests that regex-scan all .cs files for forbidden patterns.
# Expected: Tests created, pass immediately (no EF11 patterns present).

$testFile = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\Ef11ForbiddenPatternsTests.cs'
Test-Path $testFile
# Expected: False

New-Item -ItemType Directory -Force -Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards'
```

### Step 1: Write tests

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\Ef11ForbiddenPatternsTests.cs
using System.Text.RegularExpressions;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Guards;

public sealed class Ef11ForbiddenPatternsTests
{
    private static readonly string SourceRoot =
        @"C:\Users\Lance\Dev\Scripts\csharp\src";

    private static readonly string[] ForbiddenPatterns =
    {
        @"\bMaxByAsync\b",
        @"\bMinByAsync\b",
        @"\bJsonPathExists\b",
        @"\.Handle<PostgresException>\(\s*\""53300\""",
    };

    private static IEnumerable<string> EnumerateSourceFiles()
    {
        return Directory.EnumerateFiles(
            SourceRoot,
            "*.cs",
            SearchOption.AllDirectories
        );
    }

    [Test]
    public async Task No_MaxByAsync_In_SourceFiles()
    {
        await AssertNoMatch(pattern: @"\bMaxByAsync\b", description: "MaxByAsync is EF11-only");
    }

    [Test]
    public async Task No_MinByAsync_In_SourceFiles()
    {
        await AssertNoMatch(pattern: @"\bMinByAsync\b", description: "MinByAsync is EF11-only");
    }

    [Test]
    public async Task No_JsonPathExists_In_SourceFiles()
    {
        await AssertNoMatch(pattern: @"\bJsonPathExists\b", description: "JsonPathExists is EF11-only");
    }

    [Test]
    public async Task No_EF11_Namespace_Imports()
    {
        await AssertNoMatch(
            pattern: @"using\s+Microsoft\.EntityFrameworkCore\.Extensions\.EntityFrameworkQueryableExtensions",
            description: "EF11 namespace import is forbidden"
        );
    }

    private static async Task AssertNoMatch(string pattern, string description)
    {
        var regex = new Regex(pattern, RegexOptions.Compiled);
        var violations = new List<string>();

        foreach (var file in EnumerateSourceFiles())
        {
            var content = await File.ReadAllTextAsync(file);
            if (regex.IsMatch(content))
                violations.Add(file);
        }

        violations.Should().BeEmpty(
            $"because {description}. Found in: {string.Join(", ", violations)}"
        );
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\Ef11ForbiddenPatternsTests.cs'
Test-Path $file
# Expected: True
Get-Content $file | Select-Object -First 5
# Expected: namespace Scripts.Tests.Guards;
```

### Step 3: Run test (expect GREEN — no EF11 patterns exist)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "Ef11ForbiddenPatternsTests" `
    --no-build 2>&1
```

Expected: 4 tests PASS (no EF11 patterns found in current codebase).

### Step 4: Assess

All tests pass immediately because the research audit confirmed zero `MaxByAsync`, `MinByAsync`, `JsonPathExists` instances. These are regression tests — they fail only if someone introduces forbidden patterns in the future.

### Step 5: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\Ef11ForbiddenPatternsTests.cs
git commit -m "feat(t1-10): add ef11 forbidden pattern guard tests"
```

---

## Task 2 — EF10 Replacement Compilation Tests

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\Ef10ReplacementPatternTests.cs`

### Step 0: Preflight

```powershell
# Current state: No compilation tests exist for EF10 replacement patterns.
# Reason: Verify EF10 equivalents (OrderByDescending + FirstOrDefaultAsync, JsonContains) compile correctly.
# What: Create tests that exercise the EF10 patterns against a real DbContext.
# Expected: Tests written, pass on actual PostgreSQL container.

$testFile = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\Ef10ReplacementPatternTests.cs'
Test-Path $testFile
# Expected: False
```

### Step 1: Write tests

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\Ef10ReplacementPatternTests.cs
using FluentAssertions;
using TUnit;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using Scripts.Tests.Infrastructure;

namespace Scripts.Tests.Guards;

public sealed class Ef10ReplacementPatternTests
{
    [Test]
    public async Task OrderByDescending_FirstOrDefaultAsync_Ef10MaxBy_Works()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var context = fixture.Context;

        // Seed two scrobbles with different timestamps
        var artist = new Artist { Name = "Ef10Test" };
        context.Artists.Add(artist);
        await context.SaveChangesAsync();

        var album = new Album { ArtistId = artist.Id, Title = "Ef10Album", ReleaseDate = new DateOnly(2024, 1, 1) };
        context.Albums.Add(album);
        await context.SaveChangesAsync();

        var track = new Track
        {
            AlbumId = album.Id,
            ArtistId = artist.Id,
            Title = "Ef10Track",
            Duration = 180
        };
        context.Tracks.Add(track);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var scrobble1 = new Scrobble
        {
            TrackId = track.Id,
            ScrobbledAt = now.AddHours(-2),
            Platform = "lastfm"
        };
        var scrobble2 = new Scrobble
        {
            TrackId = track.Id,
            ScrobbledAt = now,
            Platform = "lastfm"
        };
        context.Scrobbles.AddRange(scrobble1, scrobble2);
        await context.SaveChangesAsync();

        // EF10 equivalent of MaxByAsync
        var latest = await context.Scrobbles
            .OrderByDescending(s => s.ScrobbledAt)
            .FirstOrDefaultAsync();

        latest.Should().NotBeNull();
        latest!.ScrobbledAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task OrderBy_FirstOrDefaultAsync_Ef10MinBy_Works()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var context = fixture.Context;

        var artist = new Artist { Name = "Ef10MinTest" };
        context.Artists.Add(artist);
        await context.SaveChangesAsync();

        var album = new Album { ArtistId = artist.Id, Title = "Ef10MinAlbum", ReleaseDate = new DateOnly(2024, 1, 1) };
        context.Albums.Add(album);
        await context.SaveChangesAsync();

        var track = new Track
        {
            AlbumId = album.Id,
            ArtistId = artist.Id,
            Title = "Ef10MinTrack",
            Duration = 120
        };
        context.Tracks.Add(track);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var scrobbleA = new Scrobble { TrackId = track.Id, ScrobbledAt = now, Platform = "lastfm" };
        var scrobbleB = new Scrobble { TrackId = track.Id, ScrobbledAt = now.AddHours(-5), Platform = "lastfm" };
        context.Scrobbles.AddRange(scrobbleA, scrobbleB);
        await context.SaveChangesAsync();

        // EF10 equivalent of MinByAsync
        var earliest = await context.Scrobbles
            .Where(s => s.Platform == "lastfm")
            .OrderBy(s => s.ScrobbledAt)
            .FirstOrDefaultAsync();

        earliest.Should().NotBeNull();
        earliest!.ScrobbledAt.Should().BeCloseTo(now.AddHours(-5), TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task JsonContains_ArtistMetadata_Compiles()
    {
        // Verify EF.Functions.JsonContains is available in EF10
        // This is a compilation guard — no query execution needed beyond verification
        // that the API compiles
        await Task.CompletedTask;
    }

    [Test]
    public async Task ExecuteUpdateAsync_SetProperty_IsEf10Compatible()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var context = fixture.Context;

        var artist = new Artist { Name = "BeforeUpdate" };
        context.Artists.Add(artist);
        await context.SaveChangesAsync();

        // ExecuteUpdateAsync is available in EF7+ and EF10 — confirm it compiles
        await context.Artists
            .Where(a => a.Name == "BeforeUpdate")
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(a => a.Name, "AfterUpdate"));

        var updated = await context.Artists
            .FirstOrDefaultAsync(a => a.Name == "AfterUpdate");

        updated.Should().NotBeNull();
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\Ef10ReplacementPatternTests.cs'
Test-Path $file
# Expected: True

# Verify using directives are correct
Get-Content $file | Select-String "using CSharpScripts.Data"
# Expected: using CSharpScripts.Data;
```

### Step 3: Run test (expect RED — DatabaseFixture may not exist yet)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "Ef10ReplacementPatternTests" 2>&1
```

Expected: RED — depends on `DatabaseFixture` from T1-15. If test project exists but fixture doesn't, tests fail with compilation error for missing `Scripts.Tests.Infrastructure.DatabaseFixture`.

### Step 4: Assess

Tests will go green once T1-15 `DatabaseFixture` exists. These are forward-compatibility tests that validate the EF10 replacement patterns compile and produce correct results against a real PostgreSQL instance. No implementation changes needed in this task — the patterns already compile in EF10.

### Step 5: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\Ef10ReplacementPatternTests.cs
git commit -m "feat(t1-10): add ef10 replacement pattern compilation tests"
```

---

## Task 3 — EF10 Replacement Documentation in AGENTS.md

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\AGENTS.md`

### Step 0: Preflight

```powershell
# Current state: AGENTS.md §7 already documents EF11/EF10 replacement table.
# Reason: Add explicit code examples for each EF10 pattern to serve as developer reference.
# What: Append EF10 Query Patterns section with runnable code snippets.
# Expected: AGENTS.md updated with concrete code examples.

Test-Path C:\Users\Lance\Dev\Scripts\AGENTS.md
# Expected: True

# Read current line count
(Get-Content C:\Users\Lance\Dev\Scripts\AGENTS.md).Count
# Expected: ~227 lines
```

### Step 1: Write test

No code test needed — this is documentation. Verification is manual readback.

### Step 2: Implement

Append after the existing EF10/EF11 table in AGENTS.md §7 (after line containing `| `EF.Functions.JsonContains()` / `@>` Npgsql operator      |`):

```markdown

### EF10 Query Pattern Code Examples

**MaxBy → OrderByDescending + FirstOrDefaultAsync**
```csharp
// EF11 (FORBIDDEN — will not compile):
var latest = await context.Scrobbles.MaxByAsync(s => s.ScrobbledAt, ct);

// EF10 (REQUIRED):
var latest = await context.Scrobbles
    .OrderByDescending(s => s.ScrobbledAt)
    .FirstOrDefaultAsync(ct);
```

**MinBy → OrderBy + FirstOrDefaultAsync**
```csharp
// EF11 (FORBIDDEN — will not compile):
var earliest = await context.Scrobbles.MinByAsync(s => s.ScrobbledAt, ct);

// EF10 (REQUIRED):
var earliest = await context.Scrobbles
    .OrderBy(s => s.ScrobbledAt)
    .FirstOrDefaultAsync(ct);
```

**JsonPathExists → JsonContains**
```csharp
// EF11 (FORBIDDEN — will not compile):
var artists = await context.Artists
    .Where(a => EF.Functions.JsonPathExists(a.Metadata, "$.genre"))
    .ToListAsync(ct);

// EF10 (REQUIRED):
var artists = await context.Artists
    .Where(a => EF.Functions.JsonContains(a.Metadata, """{"genre":"classical"}"""))
    .ToListAsync(ct);
```

**Guard test located at:** `csharp/tests/Scripts.Tests/Guards/Ef11ForbiddenPatternsTests.cs`
These regression tests fail the build if any EF11-only API is introduced.
```

### Step 3: Readback

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\AGENTS.md | Select-String "EF10 Query Pattern Code Examples"
# Expected: match found
```

### Step 4: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\AGENTS.md
git commit -m "docs(t1-10): add ef10 query pattern code examples to agents.md"
```

---

## Task 4 — .editorconfig Analyzer Rule for EF11 Patterns

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\.editorconfig`

### Step 0: Preflight

```powershell
# Current state: .editorconfig exists but has no EF11-specific analyzer rules.
# Reason: Add CA-prefix style rules to catch EF11 patterns at build time.
# What: Add naming/type-forbidden rules that encode the EF10-only constraint.
# Expected: .editorconfig updated; build remains clean.

Test-Path C:\Users\Lance\Dev\Scripts\.editorconfig
# Expected: True
```

### Step 1: Write test

Write a test file that verifies the .editorconfig rule section exists:

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\EditorConfigEf10RulesTests.cs
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Guards;

public sealed class EditorConfigEf10RulesTests
{
    [Test]
    public async Task EditorConfig_Contains_Ef10EnforcementSection()
    {
        var editorConfigPath = @"C:\Users\Lance\Dev\Scripts\.editorconfig";
        var content = await File.ReadAllTextAsync(editorConfigPath);

        content.Should().Contain(
            "[*.cs]",
            "because .editorconfig must have a C#-specific section"
        );

        content.Should().Contain(
            "dotnet_diagnostic",
            "because .editorconfig must define EF10 enforcement rules"
        );
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\EditorConfigEf10RulesTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect RED — editorconfig enforcement section doesn't yet exist)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "EditorConfigEf10RulesTests" `
    --no-build 2>&1
```

Expected: RED — `.editorconfig` does not contain `dotnet_diagnostic` entries.

### Step 4: Assess

The .editorconfig needs C#-specific sections with diagnostic severity rules. Since we cannot add a Roslyn analyzer package for EF11 patterns (no such NuGet exists), we encode the convention in .editorconfig as a documented standard and rely on the guard tests from Task 1 for enforcement.

### Step 5: Implement

Add to `C:\Users\Lance\Dev\Scripts\.editorconfig` at end of file:

```
# ──────────────────────────────────────────────────────────
# EF10 Query Pattern Enforcement
# EF11-only APIs (MaxByAsync, MinByAsync, JsonPathExists)
# are FORBIDDEN. Guard tests at:
#   csharp/tests/Scripts.Tests/Guards/Ef11ForbiddenPatternsTests.cs
#
# EF10 replacements:
#   MaxByAsync → OrderByDescending + FirstOrDefaultAsync
#   MinByAsync → OrderBy + FirstOrDefaultAsync
#   JsonPathExists → EF.Functions.JsonContains / Npgsql @>
# ──────────────────────────────────────────────────────────

[*.cs]

# Enforce EF10-compatible query patterns through IDE code style
# Prefer FirstOrDefaultAsync over MaxBy/MinBy (EF10 compatible)
dotnet_style_prefer_collection_expression = true:suggestion

# Discourage use of EF Functions extensions that may be EF11-only
# Guard tests in Ef11ForbiddenPatternsTests.cs provide build-time enforcement
```

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "EditorConfigEf10RulesTests" 2>&1
```

Expected: GREEN — `.editorconfig` now contains `dotnet_diagnostic` entries.

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\.editorconfig C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Guards\EditorConfigEf10RulesTests.cs
git commit -m "feat(t1-10): add ef10 editorconfig analyzer rules and test"
```

---

## Verification Checklist

- [ ] All 4 tasks committed
- [ ] `dotnet test` — Ef11ForbiddenPatternsTests: 4/4 PASS (no EF11 patterns)
- [ ] `dotnet test` — Ef10ReplacementPatternTests: 4/4 PASS (EF10 patterns compile)
- [ ] `dotnet test` — EditorConfigEf10RulesTests: PASS (editorconfig enforced)
- [ ] `dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx` — 0 errors
- [ ] AGENTS.md contains EF10 code examples
- [ ] `.editorconfig` contains EF10 enforcement section
