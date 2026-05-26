# Music Domain Isolation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Verify and enforce that the Music domain references only Core, with no dependencies on Data, Orchestrators, Reader, or Language.

**Architecture:** Scripts.Services.Music is an isolated domain library for MetaBrainz and Discogs integrations. It must not directly query the database or reference any other service. All entity persistence is delegated to Orchestrators via interfaces defined in Core.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Domain Isolation Context (from DATA-ACCESS-REPOSITORIES research)

The Music domain must follow the inward-only dependency flow: **Music → Core only**. No direct references to Data, Orchestrators, Reader, or Language services.

### Repository Pattern for Music Persistence

Music domain does not directly instantiate `ScriptsDbContext`. Instead, it receives repository interfaces injected via DI. The recommended repository interfaces for Music domain operations are:

- **ITrackRepository**: Bulk insert tracks, query by artist/album
- **IArtistRepository**: Lookup/upsert artists by name
- **IAlbumRepository**: Lookup/upsert albums by artist and title
- **IScrobbleRepository**: Upsert scrobbles, query by track/platform

These interfaces are defined in `Scripts.Core.Abstractions` and implemented in `Scripts.Data.Repositories`. Music receives them via constructor injection.

### Mutation Strategy for Music Operations

When Music needs to persist data:
- **Single-entity upsert** (PK known): Use `ExecuteUpdateAsync` (no tracking, faster)
- **Bulk insert**: Use `AddRange` + `SaveChangesAsync` (batching)
- **Bulk delete**: Use `ExecuteDeleteAsync` (EF mandate)

Example: Music calls `ITrackRepository.BulkInsertAsync(tracks)` instead of directly calling `DbContext.Tracks.AddRange()`.

---

## Pre-flight Checks

```powershell
if (-not (Get-Command pwsh -ErrorAction SilentlyContinue)) { throw "pwsh not found" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "dotnet SDK not found" }
dotnet --version | Select-String "^10\." || throw ".NET 10 SDK not found"

# T3 depends on T2 sign-off — Scripts.slnx must exist
if (-not (Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx')) {
    throw 'Tier 2 sign-off required — Scripts.slnx not found. Run T2 plans first.'
}

dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx -ErrorAction Stop
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --no-restore -ErrorAction Stop
# Expected: Build succeeded. 0 Error(s).
```

---

## Task 1 — TDD RED: Write Music domain isolation tests

**Current State:** No tests assert Music dependency constraints.
**Reason:** Need failing tests to drive isolation enforcement.
**What:** Create `T301_MusicDomainTests.cs` in `Scripts.Tests\T3\`.
**Expected Outcome:** Tests compile, 1–2 fail if violations exist.

### Step 1.1 — Create test file

```powershell
$dir = "C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\T3"
New-Item -ItemType Directory -Path $dir -Force -ErrorAction Stop
Test-Path $dir | Should -Be $true
```

Create file `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\T3\T301_MusicDomainTests.cs`:

```csharp
using System.IO;
using System.Linq;
using FluentAssertions;
using TUnit.Core;

namespace CSharpScripts.Tests.T3;

public class T301_MusicDomainTests
{
    private const string MusicCsproj =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\Services\Music\Scripts.Services.Music.csproj";

    private const string MusicSrcDir =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\Services\Music";

    [Test]
    public void MusicDomain_HasNoDependencies_OnDataOrOrchestrators()
    {
        File.Exists(MusicCsproj).Should().BeTrue(
            "because Scripts.Services.Music.csproj must exist at the expected path");

        var content = File.ReadAllText(MusicCsproj);

        content.Should().NotContain("Scripts.Data",
            "because Music must not reference the Data layer directly");
        content.Should().NotContain("Scripts.Orchestrators",
            "because Music must not reference Orchestrators");
        content.Should().NotContain("Scripts.Reader",
            "because Music must not reference the Reader domain");
        content.Should().NotContain("Scripts.Services.Language",
            "because Music must not reference the Language service");
    }

    [Test]
    public void MusicDomain_AllFiles_HaveCorrectNamespace()
    {
        Directory.Exists(MusicSrcDir).Should().BeTrue(
            $"because Music source directory must exist at {MusicSrcDir}");

        var files = Directory
            .GetFiles(MusicSrcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        files.Should().NotBeEmpty(
            "because the Music project must contain at least one .cs file");

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("namespace "))
                continue;

            content.Should().Contain("namespace CSharpScripts.Services.Music",
                $"because {Path.GetFileName(file)} has a wrong namespace — expected CSharpScripts.Services.Music.*");
        }
    }

    [Test]
    public void MusicDomain_DoesNotImport_DataNamespace_InSourceFiles()
    {
        var files = Directory
            .GetFiles(MusicSrcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            content.Should().NotContain("using CSharpScripts.Data",
                $"because {Path.GetFileName(file)} must not import from the Data namespace");
            content.Should().NotContain("using CSharpScripts.Orchestrators",
                $"because {Path.GetFileName(file)} must not import from Orchestrators");
        }
    }
}
```

### Step 1.2 — Run to confirm RED

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "FullyQualifiedName~T301_MusicDomainTests" `
    2>&1 | Tee-Object -Variable testOutput

Write-Host ($testOutput -join "`n")
# If all pass → Music already isolated → skip to Task 5 (commit)
# If any fail → proceed to Task 2
```

---

## Task 2 — Inspect current Music dependencies

**Current State:** Unknown whether Music has illegal imports.
**Reason:** Must identify violations before editing source.
**What:** Grep for disallowed imports in Music `.cs` files and `.csproj`.
**Expected Outcome:** Explicit list of violating files and lines.

```powershell
$musicDir = "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Music"

Write-Host "=== .csproj references ==="
Get-Content "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Music\Scripts.Services.Music.csproj" |
    Select-String "ProjectReference"

Write-Host "=== Source files importing CSharpScripts.Data ==="
Get-ChildItem $musicDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "CSharpScripts\.Data" |
    ForEach-Object { "$($_.Path):$($_.LineNumber)  $($_.Line.Trim())" }

Write-Host "=== Source files importing CSharpScripts.Orchestrators ==="
Get-ChildItem $musicDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "CSharpScripts\.Orchestrators" |
    ForEach-Object { "$($_.Path):$($_.LineNumber)  $($_.Line.Trim())" }

Write-Host "=== Source files importing CSharpScripts.Services.Language ==="
Get-ChildItem $musicDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "CSharpScripts\.Services\.Language" |
    ForEach-Object { "$($_.Path):$($_.LineNumber)  $($_.Line.Trim())" }
```

---

## Task 3 — GREEN: Remove illegal Music → Data/Language references

> Skip if Task 2 found no violations.

**Current State:** Music references Data or Language directly.
**Reason:** Violates inward-only dependency flow.
**What:** Extract any shared type to Core; update Music to use Core interface; remove illegal project reference.
**Expected Outcome:** Music `.csproj` references only `Scripts.Core`.

### Step 3.1 — Back up affected files

```powershell
# For each affected source file (replace <FileName> with actual name):
$src = "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Music\<FileName>.cs"
$bak = "$src.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $src -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true
Write-Host "Backed up: $bak"
```

### Step 3.2 — Extract shared music abstraction to Core (if needed)

If Music calls a language detection method from `Scripts.Services.Language`, extract an interface:

Create file `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Abstractions\ILanguageDetector.cs`:

```csharp
namespace CSharpScripts.Core.Abstractions;

/// <summary>
/// Abstraction for detecting the language of text.
/// Implemented in Scripts.Services.Language; consumed by other services via DI.
/// </summary>
public interface ILanguageDetector
{
    /// <summary>Returns the ISO 639-1 language code of the given text, or null if undetectable.</summary>
    string? Detect(string text);
}
```

```powershell
Test-Path "C:\Users\Lance\Dev\Scripts\csharp\src\Core\Abstractions\ILanguageDetector.cs" | Should -Be $true
Write-Host "ILanguageDetector created in Core"
```

### Step 3.3 — Replace illegal using in Music source file

```powershell
$file    = "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Music\<FileName>.cs"
$content = Get-Content $file -Raw -Encoding UTF8

# Remove illegal using directives
$updated = $content `
    -replace "using CSharpScripts\.Services\.Language[^;]*;(\r?\n)?", "" `
    -replace "using CSharpScripts\.Data[^;]*;(\r?\n)?", ""

# Add Core.Abstractions if not already present
if ($updated -notmatch "using CSharpScripts\.Core\.Abstractions") {
    $updated = "using CSharpScripts.Core.Abstractions;`n" + $updated
}

Set-Content -Path $file -Value $updated -Encoding UTF8 -ErrorAction Stop

# Verify
$check = Get-Content $file -Raw -Encoding UTF8
$check | Should -Not -Match "using CSharpScripts\.Services\.Language"
$check | Should -Not -Match "using CSharpScripts\.Data"
Write-Host "Cleaned: $file"
```

### Step 3.4 — Remove illegal .csproj references

```powershell
$csproj = "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Music\Scripts.Services.Music.csproj"
$bak    = "$csproj.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $csproj -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true

$xml  = [xml](Get-Content $csproj -Encoding UTF8)
$illegal = @("Scripts.Data", "Scripts.Orchestrators", "Scripts.Reader", "Scripts.Services.Language")

foreach ($pattern in $illegal) {
    $refs = $xml.Project.ItemGroup.ProjectReference |
        Where-Object { $_.Include -like "*$pattern*" }
    foreach ($ref in $refs) {
        Write-Host "Removing reference: $($ref.Include)"
        $ref.ParentNode.RemoveChild($ref) | Out-Null
    }
}

$xml.Save($csproj)
Test-Path $csproj | Should -Be $true
Write-Host "Updated: $csproj"
```

---

## Task 4 — GREEN: Fix namespace violations

> Skip if Task 1 namespace test already passes.

**Current State:** One or more Music files use an incorrect namespace.
**Reason:** Namespace must be `CSharpScripts.Services.Music.*`.
**What:** Back up and correct each violating file.
**Expected Outcome:** All Music files declare `namespace CSharpScripts.Services.Music.*`.

```powershell
$musicDir = "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Music"

Get-ChildItem $musicDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    ForEach-Object {
        $file    = $_.FullName
        $content = Get-Content $file -Raw -Encoding UTF8

        if ($content -match "namespace " -and
            $content -notmatch "namespace CSharpScripts\.Services\.Music") {

            $bak = "$file.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
            Copy-Item -Path $file -Destination $bak -ErrorAction Stop
            Test-Path $bak | Should -Be $true

            $current = ([regex]"namespace\s+([\w\.]+)").Match($content).Groups[1].Value
            Write-Host "Fixing namespace in $($_.Name): $current → CSharpScripts.Services.Music"

            $fixed = $content -replace "namespace\s+$([regex]::Escape($current))",
                                       "namespace CSharpScripts.Services.Music"
            Set-Content -Path $file -Value $fixed -Encoding UTF8 -ErrorAction Stop

            (Get-Content $file -Raw -Encoding UTF8) | Should -Match "namespace CSharpScripts\.Services\.Music"
        }
    }
```

---

## Task 5 — Build and test GREEN

**Current State:** Source changes applied.
**Reason:** Confirm compilation succeeds and isolation tests pass.
**What:** Full restore → build → targeted test run.
**Expected Outcome:** 0 build errors, all T301 tests pass.

```powershell
dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx -ErrorAction Stop

$buildOut = dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --no-restore 2>&1
$buildOut | Select-String "0 Error" | Should -Not -BeNullOrEmpty
Write-Host "Build: GREEN"

$testOut = dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "FullyQualifiedName~T301_MusicDomainTests" 2>&1
$testOut | Select-String "Failed: 0" | Should -Not -BeNullOrEmpty
Write-Host "T301 tests: GREEN"
```

Expected output:
```
Test Run Successful.
Tests: 3 (3 passed)
```

---

## Task 6 — REFACTOR: Commit isolation

**Current State:** Tests green, source clean.
**Reason:** Record Music isolation as a discrete commit.
**What:** Stage, verify, commit.
**Expected Outcome:** Commit `feat(t3-01)` visible in log.

```powershell
Set-Location C:\Users\Lance\Dev\Scripts -ErrorAction Stop

gitleaks detect --no-git 2>&1 | Select-String "leaks found" | ForEach-Object {
    throw "Gitleaks found secrets — abort commit"
}

git add csharp/src/Services/Music/ `
        csharp/tests/Scripts.Tests/T3/T301_MusicDomainTests.cs 2>&1

# Only add Core changes if new abstractions were created
if (Test-Path "C:\Users\Lance\Dev\Scripts\csharp\src\Core\Abstractions\ILanguageDetector.cs") {
    git add csharp/src/Core/Abstractions/ILanguageDetector.cs 2>&1
}

git status 2>&1 | Write-Host

git commit -m "feat(t3-01): isolate Music domain — remove Data/Language refs, fix namespaces" `
    -ErrorAction Stop 2>&1 | Tee-Object -Variable commitOut

$commitOut | Select-String "feat\(t3-01\)" | Should -Not -BeNullOrEmpty
Write-Host "Committed: t3-01"
```

---

## Completion Criteria

| Check | Command | Expected |
|-------|---------|----------|
| Build clean | `dotnet build csharp/Scripts.slnx` | `0 Error(s)` |
| Tests pass | `dotnet test --filter T301` | `Failed: 0` |
| No Data ref | `grep -r "Scripts.Data" csharp/src/Services/Music/` | No output |
| No Language ref | `grep -r "Scripts.Services.Language" csharp/src/Services/Music/` | No output |
| No Orchestrators ref | `grep -r "Scripts.Orchestrators" csharp/src/Services/Music/` | No output |
| Namespace correct | All files contain `CSharpScripts.Services.Music` | Verified |
| Commit present | `git log --oneline -1` | `feat(t3-01)` |
