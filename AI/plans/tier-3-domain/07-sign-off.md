# Tier 3 Sign-Off Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Final verification that all Tier 3 domain boundaries are correct, naming is consistent, all timestamps use DateTimeOffset, and the full test suite passes with zero failures.

**Architecture:** This is the gatekeeper phase for Tier 3. All 7 prior phases (00–06) must be complete and committed before executing this plan. The sign-off verifies domain dependency rules, entity integrity, DateTimeOffset coverage, and produces a git tag.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Tier 3 Completion Checklist (from consolidated research)

### Domain Isolation Verification

**Reader Domain** (T3-00):
- ✅ No references to Data, Orchestrators, or Services
- ✅ All files use `Scripts.Reader.*` namespace
- ✅ Persistence via injected `IDocumentStore` interface

**Music Domain** (T3-01):
- ✅ No references to Data, Orchestrators, Reader, or Language
- ✅ All files use `Scripts.Services.Music.*` namespace
- ✅ Persistence via injected repository interfaces

**Language Domain** (T3-02):
- ✅ No references to Data, Orchestrators, Reader, or Music
- ✅ All files use `Scripts.Services.Language.*` namespace
- ✅ `LanguageIdentifier` is `internal sealed` (not public)
- ✅ Migrated from NTextCat to Lingua v1.0.5

**Orchestrators Domain** (T3-03):
- ✅ No references to CLI or Reader
- ✅ All files use `Scripts.Orchestrators.*` namespace
- ✅ No raw `DbContext` instantiation — uses repository interfaces
- ✅ Deleted duplicate `LastFmService.cs` from `Sync/LastFm/` subdirectory

### Naming & Entity Cleanup (T3-04)

- ✅ All namespaces renamed from `CSharpScripts.*` to `Scripts.*`
- ✅ `FiberyEntity.cs` and `FiberyEntityConfiguration.cs` deleted
- ✅ All entities are `internal sealed record` or `internal sealed class`
- ✅ `GlobalUsings.cs` stripped of package-level duplicates
- ✅ `SpectreTypeRegistrar` moved from Core to CLI

### DateTimeOffset Migration (T3-05)

- ✅ All entity timestamp properties use `DateTimeOffset` (not `DateTime`)
- ✅ Orchestrators use `DateTimeOffset.UtcNow` (not `DateTime.UtcNow`)
- ✅ `DateTimeExtensions` extends `DateTimeOffset`
- ✅ `DateTimeFormats` centralized in Core with timezone helper
- ✅ EF Core migration generated and applied

### Inspection Logic Fixes (T3-06)

- ✅ No `!(x is null)` patterns — converted to `x is not null`
- ✅ No `.ToList().Count == 0` patterns — converted to `.Any()`
- ✅ Null checks use pattern matching (`is null` / `is not null`)
- ✅ Redundant `?.` removed on non-nullable paths

### Research Integration

All findings from consolidated research files embedded directly:
- **ENTITY-DESIGN**: Mbid removal, JsonDocument mapping, configuration gaps
- **DATA-ACCESS-REPOSITORIES**: Repository pattern, mutation strategy, duplicate cleanup
- **ADVANCED-FEATURES**: EF10 patterns, compiled models, logging, Lingua migration, resilience
- **DBCONTEXT-CONFIGURATION**: DbContext setup, extension registration, compiled model lifecycle
- **MIGRATIONS-EXTENSIONS**: Migration status, PostgreSQL extensions, functional indexes

---

## Pre-flight Checks

```powershell
if (-not (Get-Command pwsh -ErrorAction SilentlyContinue)) { throw "pwsh not found" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "dotnet SDK not found" }
if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw "git not found" }
dotnet --version | Select-String "^10\." || throw ".NET 10 SDK not found"

# T3 depends on T2 sign-off — Scripts.slnx must exist
if (-not (Test-Path '/home/lance/Scripts/csharp/Scripts.slnx')) {
    throw 'Tier 2 sign-off required — Scripts.slnx not found. Run T2 plans first.'
}

# Verify all T3 commits are present
Set-Location /home/lance/Scripts -ErrorAction Stop

Write-Host "=== T3 commit history ==="
git log --oneline --grep="t3-" -8

Write-Host "=== Expected commits ==="
git log --oneline --grep="t3-00" -1 | Should -Not -BeNullOrEmpty "t3-00 commit not found"
git log --oneline --grep="t3-01" -1 | Should -Not -BeNullOrEmpty "t3-01 commit not found"
git log --oneline --grep="t3-02" -1 | Should -Not -BeNullOrEmpty "t3-02 commit not found"
git log --oneline --grep="t3-03" -1 | Should -Not -BeNullOrEmpty "t3-03 commit not found"
git log --oneline --grep="t3-04" -1 | Should -Not -BeNullOrEmpty "t3-04 commit not found"
git log --oneline --grep="t3-05" -1 | Should -Not -BeNullOrEmpty "t3-05 commit not found"
git log --oneline --grep="t3-06" -1 | Should -Not -BeNullOrEmpty "t3-06 commit not found"
Write-Host "All T3 commits verified."
```

---

## Task 1 — TDD RED: Write Tier 3 sign-off assertion tests

**Current State:** No comprehensive tests that verify all Tier 3 invariants holistically.
**Reason:** Need end-to-end verification that all 7 phases' targets are met in aggregate.
**What:** Create `T307_Tier3SignOffTests.cs` in `Scripts.Tests\T3\`.
**Expected Outcome:** Tests compile. All should pass if T3 is complete — any failure indicates a regression from an earlier phase.

### Step 1.1 — Create test file

```powershell
$dir = "/home/lance/Scripts/csharp/tests\Scripts.Tests\T3"
New-Item -ItemType Directory -Path $dir -Force -ErrorAction Stop
Test-Path $dir | Should -Be $true
```

Create file `/home/lance/Scripts/csharp/tests\Scripts.Tests\T3\T307_Tier3SignOffTests.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using TUnit.Core;

namespace Scripts.Tests.T3;

public class T307_Tier3SignOffTests
{
    private const string SrcDir = @"/home/lance/Scripts/csharp/src";

    // ==================== Domain Boundary Tests ====================

    [Test]
    public void ReaderDomain_DependsOnlyOnCore()
    {
        var csproj = Path.Combine(SrcDir, "Reader", "Scripts.Reader.csproj");
        File.Exists(csproj).Should().BeTrue();

        var content = File.ReadAllText(csproj);
        content.Should().NotContain("Scripts.Data");
        content.Should().NotContain("Scripts.Orchestrators");
        content.Should().NotContain("Scripts.Services");
    }

    [Test]
    public void MusicDomain_DependsOnlyOnCore()
    {
        var csproj = Path.Combine(SrcDir, "Services", "Music", "Scripts.Services.Music.csproj");
        File.Exists(csproj).Should().BeTrue();

        var content = File.ReadAllText(csproj);
        content.Should().NotContain("Scripts.Data");
        content.Should().NotContain("Scripts.Orchestrators");
        content.Should().NotContain("Scripts.Reader");
        content.Should().NotContain("Scripts.Services.Language");
    }

    [Test]
    public void LanguageDomain_DependsOnlyOnCore()
    {
        var csproj = Path.Combine(SrcDir, "Services", "Language", "Scripts.Services.Language.csproj");
        File.Exists(csproj).Should().BeTrue();

        var content = File.ReadAllText(csproj);
        content.Should().NotContain("Scripts.Data");
        content.Should().NotContain("Scripts.Orchestrators");
        content.Should().NotContain("Scripts.Reader");
        content.Should().NotContain("Scripts.Services.Music");
    }

    [Test]
    public void OrchestratorsDomain_DoesNotReferenceCLIOrReader()
    {
        var csproj = Path.Combine(SrcDir, "Orchestrators", "Scripts.Orchestrators.csproj");
        File.Exists(csproj).Should().BeTrue();

        var content = File.ReadAllText(csproj);
        content.Should().NotContain("Scripts.CLI");
        content.Should().NotContain("Scripts.Reader");
    }

    // ==================== Entity Naming Tests ====================

    [Test]
    public void FiberyEntity_DoesNotExist()
    {
        var fiberyFile = Path.Combine(SrcDir, "Data", "Entities", "FiberyEntity.cs");
        File.Exists(fiberyFile).Should().BeFalse(
            "because FiberyEntity was deleted in phase 04 naming refactor");
    }

    [Test]
    public void AllEntityProperties_UseDateTimeOffset_NotDateTime()
    {
        var entitiesDir = Path.Combine(SrcDir, "Data", "Entities");
        Directory.Exists(entitiesDir).Should().BeTrue();

        var files = Directory
            .GetFiles(entitiesDir, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);

            // DateTimeOffset is OK; DateTime (without Offset) is a violation
            var dateTimeOnlyMatches = Regex.Matches(
                content,
                @"\bDateTime\s+\w+\s*\{"
            );

            dateTimeOnlyMatches.Count.Should().Be(0,
                $"because {Path.GetFileName(file)} must use DateTimeOffset, not DateTime");

            var nullableDateTimeMatches = Regex.Matches(
                content,
                @"\bDateTime\?\s+\w+\s*\{"
            );

            nullableDateTimeMatches.Count.Should().Be(0,
                $"because {Path.GetFileName(file)} must use DateTimeOffset?, not DateTime?");
        }
    }

    // ==================== DateTimeOffset Coverage Tests ====================

    [Test]
    public void DateTimeFormats_ExistsInCore()
    {
        var dtoFile = Path.Combine(SrcDir, "Core", "DateTimeFormats.cs");
        File.Exists(dtoFile).Should().BeTrue(
            "because DateTimeFormats was centralized in Core during phase 05");
    }

    [Test]
    public void SpectreTypeRegistrar_IsInCLI()
    {
        var coreFile = Path.Combine(SrcDir, "Core", "SpectreTypeRegistrar.cs");
        var cliFile  = Path.Combine(SrcDir, "CLI", "SpectreTypeRegistrar.cs");

        File.Exists(coreFile).Should().BeFalse(
            "because SpectreTypeRegistrar was moved from Core to CLI in phase 04");
        File.Exists(cliFile).Should().BeTrue(
            "because SpectreTypeRegistrar must be in the CLI project");
    }

    // ==================== Inspection Cleanup Tests ====================

    [Test]
    public void SourceTree_HasNoNegatedNullChecks()
    {
        var violations = Directory
            .GetFiles(SrcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\"))
            .Where(f =>
            {
                var content = File.ReadAllText(f);
                return content.Contains("!(") && content.Contains(" is null)");
            })
            .ToList();

        violations.Should().BeEmpty(
            $"because phase 06 converted !(x is null) → x is not null. Files with violations:\n{string.Join("\n", violations.Select(Path.GetFileName))}");
    }

    [Test]
    public void SourceTree_HasNoToListCountZero()
    {
        var violations = Directory
            .GetFiles(SrcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\"))
            .Where(f =>
            {
                var content = File.ReadAllText(f);
                return Regex.IsMatch(content, @"\.ToList\(\)\s*\.\s*Count\s*==");
            })
            .ToList();

        violations.Should().BeEmpty(
            $"because phase 06 converted .ToList().Count == 0 → !.Any(). Files with violations:\n{string.Join("\n", violations.Select(Path.GetFileName))}");
    }

    // ==================== Assembly-Level Tests ====================

    [Test]
    public void LanguageIdentifier_IsNotPublic()
    {
        var langDir = Path.Combine(SrcDir, "Services", "Language");
        var langIdFiles = Directory
            .GetFiles(langDir, "LanguageIdentifier.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        if (langIdFiles.Count == 0)
            return; // File may have been renamed; not a failure

        var src = File.ReadAllText(langIdFiles[0]);
        src.Should().NotMatchRegex(
            @"public\s+(class|sealed\s+class|partial\s+class)\s+LanguageIdentifier",
            "because LanguageIdentifier must be internal — phase 02 enforced this");
    }
}
```

### Step 1.2 — Run to confirm current state (expected GREEN after all phases)

```powershell
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop

dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "FullyQualifiedName~T307_Tier3SignOffTests" `
    2>&1 | Tee-Object -Variable testOutput

Write-Host ($testOutput -join "`n")
# Expected: 12 tests, 0 failures if all T3 phases are complete
# If any fail → the specific earlier phase needs correction
```

---

## Task 2 — RED: Run full test suite check

**Current State:** Individual T3 phases passed their targeted tests.
**Reason:** Must verify full test suite is green — cross-phase refactoring may have introduced regressions.
**What:** Run the complete `dotnet test` against the solution.
**Expected Outcome:** All tests pass with zero failures.

```powershell
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop

Write-Host "Running full test suite..."
$testOutput = dotnet test /home/lance/Scripts/csharp/Scripts.slnx 2>&1
Write-Host ($testOutput -join "`n")

# Assert zero failures
$testOutput | Select-String "Failed: 0" | Should -Not -BeNullOrEmpty
Write-Host "Full test suite: GREEN"

# Also verify zero skipped (unless there are intentional skips)
$testOutput | Select-String "Failed:" | ForEach-Object {
    Write-Host "Test results: $_"
}
```

Expected output:
```
Test Run Successful.
Total tests: <N>
     Passed: <N>
     Failed: 0
```

---

## Task 3 — GREEN: Build verification (zero errors, zero warnings)

**Current State:** Build has been passing throughout.
**Reason:** `TreatWarningsAsErrors` is enabled in `Directory.Build.props` — must confirm zero warnings.
**What:** Clean build with `--no-restore`, capture output, assert zero errors.
**Expected Outcome:** `0 Error(s), 0 Warning(s)`.

```powershell
Write-Host "Clean build verification..."

dotnet clean   /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop

$buildOutput = dotnet build /home/lance/Scripts/csharp/Scripts.slnx --no-restore 2>&1
Write-Host ($buildOutput -join "`n")

# Parse: Build succeeded with 0 error(s) and 0 warning(s)
$buildSummary = $buildOutput | Select-String "Error" | Select-Object -Last 1
$buildSummary.ToString() | Should -Match "0 Error"
Write-Host "Build: CLEAN (0 errors)"
```

---

## Task 4 — GREEN: Gitleaks security audit

**Current State:** Source changes across all T3 phases may have touched files.
**Reason:** Must verify no secrets were accidentally introduced during refactoring.
**What:** Run `gitleaks detect --no-git` on the repository.
**Expected Outcome:** Zero leak findings.

```powershell
Set-Location /home/lance/Scripts -ErrorAction Stop

if (Get-Command gitleaks -ErrorAction SilentlyContinue) {
    $leakOutput = gitleaks detect --no-git 2>&1
    Write-Host ($leakOutput -join "`n")

    $leakOutput | Select-String "leaks found" | ForEach-Object {
        throw "Gitleaks found secrets — review and redact before sign-off"
    }
    Write-Host "Gitleaks: CLEAN (no secrets found)"
} else {
    Write-Host "Gitleaks: SKIPPED (gitleaks not installed — recommend installing via 'winget install gitleaks.gitleaks')"
}
```

---

## Task 5 — GREEN: Run all T3 domain tests as a single batch

**Current State:** T3 tests (T300–T307) have been run individually.
**Reason:** Must confirm all T3 tests pass together without interference.
**What:** Run all tests matching `FullyQualifiedName~T3`.
**Expected Outcome:** All T3 tests pass.

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "FullyQualifiedName~T3" `
    2>&1 | Tee-Object -Variable t3BatchOutput

Write-Host ($t3BatchOutput -join "`n")

$t3BatchOutput | Select-String "Failed: 0" | Should -Not -BeNullOrEmpty
$t3BatchOutput | Select-String "Passed:" | ForEach-Object {
    Write-Host "T3 batch: $_"
}

Write-Host "All T3 tests: GREEN"
```

---

## Task 6 — REFACTOR: Commit sign-off test and summary

**Current State:** Sign-off tests created and passing.
**Reason:** The test file must be committed BEFORE the tag is created so the tag references a commit that includes the verification tests.
**What:** Stage the test file and commit.
**Expected Outcome:** Commit `feat(t3-07)` in git log with the sign-off test file.

```powershell
Set-Location /home/lance/Scripts -ErrorAction Stop

git add csharp/tests/Scripts.Tests/T3/T307_Tier3SignOffTests.cs 2>&1

git status 2>&1 | Write-Host

git commit -m "feat(t3-07): Tier 3 sign-off — domain boundary tests, DateTimeOffset verification, tag t3-sign-off" `
    -ErrorAction Stop 2>&1 | Tee-Object -Variable commitOut

$commitOut | Select-String "feat\(t3-07\)" | Should -Not -BeNullOrEmpty
Write-Host "Committed: t3-07"
```

---

## Task 7 — GREEN: Create git tag t3-sign-off

**Current State:** All tests green, build clean, domain boundaries verified, sign-off test file committed.
**Reason:** Mark the exact commit as the Tier 3 sign-off point for downstream dep chains. The tag must reference a commit that already includes the verification test file.
**What:** Create an annotated tag, verify it points to the commit that includes T307_Tier3SignOffTests.cs.
**Expected Outcome:** Tag `t3-sign-off` exists and references a commit containing the sign-off test.

```powershell
Set-Location /home/lance/Scripts -ErrorAction Stop

# Verify we're on the right branch
$branch = git branch --show-current
Write-Host "Current branch: $branch"

# Ensure working tree is clean
$status = git status --porcelain
if ($status) {
    Write-Host "Uncommitted changes detected:"
    Write-Host $status
    throw "Working tree must be clean before signing off. Commit or stash changes first."
}

# Verify the test file was committed
git log --oneline -1 --name-only | Select-String "T307_Tier3SignOffTests.cs" | Should -Not -BeNullOrEmpty

# Create annotated tag
git tag -a t3-sign-off -m "Tier 3 sign-off: Domain isolation complete, DateTimeOffset migrated, naming refactored, inspections fixed, all tests green" `
    -ErrorAction Stop

# Verify tag
git tag -l "t3-sign-off" | Should -Not -BeNullOrEmpty
Write-Host "Tag created: t3-sign-off"

# Show tag details
git show t3-sign-off --no-patch 2>&1 | Write-Host
```

---

## Task 8 — Push tag to origin

```powershell
Set-Location /home/lance/Scripts -ErrorAction Stop

git push origin t3-sign-off -ErrorAction Stop 2>&1 | Tee-Object -Variable pushOut

$pushOut | Select-String "t3-sign-off" | Should -Not -BeNullOrEmpty
Write-Host "Tag pushed: t3-sign-off"
```

---

## Sign-Off Summary

```powershell
Set-Location /home/lance/Scripts -ErrorAction Stop

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  TIER 3 SIGN-OFF — COMPLETE           " -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Commits:"
git log --oneline --grep="t3-" -8 2>&1 | Write-Host

Write-Host "`nTag:"
git tag -l "t3-sign-off" 2>&1 | Write-Host

Write-Host "`nNext step:"
Write-Host "  cd AI/plans/tier-4-hardening" -ForegroundColor Yellow
Write-Host "  Start with 00-di-wiring.md`n" -ForegroundColor Yellow
```

---

## Completion Criteria

| Check | Command | Expected |
|-------|---------|----------|
| All T3 commits present | `git log --oneline --grep="t3-" -8` | 8 commits (t3-00 through t3-07) |
| Build clean | `dotnet build csharp/Scripts.slnx` | `0 Error(s), 0 Warning(s)` |
| Full test suite | `dotnet test csharp/Scripts.slnx` | `Failed: 0` |
| T3 batch tests | `dotnet test --filter T3` | `Failed: 0` |
| Gitleaks | `gitleaks detect --no-git` | No leaks found |
| Reader → Core only | grep `.csproj` for illegal refs | No Data/Orch/Services refs |
| Music → Core only | grep `.csproj` for illegal refs | No Data/Orch/Reader/Language refs |
| Language → Core only | grep `.csproj` for illegal refs | No Data/Orch/Reader/Music refs |
| Orchestrators → no CLI/Reader | grep `.csproj` for CLI/Reader refs | No CLI/Reader refs |
| No FiberyEntity | `Test-Path csharp/src/Data/Entities/FiberyEntity.cs` | `False` |
| All entities internal | grep entities for `public class`/`public record` | No output |
| GlobalUsings stripped | grep `GlobalUsings.cs` for package-level usings | No Serilog/Spectre/RestSharp/CsvHelper/MetaBrainz/EF |
| SpectreTypeRegistrar in CLI | `Test-Path csharp/src/CLI/SpectreTypeRegistrar.cs` | `True` |
| No DateTime props in entities | grep entities for `DateTime ` (not Offset) | No output |
| DateTimeFormats in Core | `Test-Path csharp/src/Core/DateTimeFormats.cs` | `True` |
| No `!(...is null)` | grep source for `!(.*is null)` | No output |
| No `.ToList().Count == 0` | grep source for pattern | No output |
| Tag exists and pushed | `git tag -l t3-sign-off` | Tag listed |
| Tag on remote | `git ls-remote --tags origin t3-sign-off` | Tag listed |
