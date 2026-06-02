# T2-09: Duplicate Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Delete duplicate source files in `csharp/src/Infrastructure/` that have authoritative versions in other projects. Each deletion is preceded by a `.bak.YYYYMMDD_HHmmss` backup, followed by a `Test-Path` assertion and a full solution build to confirm no compilation errors remain.

**Architecture:** During the Tier 1 migration, files were duplicated between the old monolithic `Infrastructure/` directory and the new modular project locations. The authoritative versions are: `StateManager.cs` and `ReleaseProgressCache.cs` in `Core/Persistence/`, `Paths.cs`, `Resilience.cs`, and `StringExtensions.cs` in `Core/`, `LastFmService.cs` in `Services/Sync/LastFm/`, and `ValidationAttributes.cs` in `CLI/`. The stale copies in `Infrastructure/` and `Services/Sync/` (root) must be removed to eliminate build confusion and maintain single sources of truth. **Seven Infrastructure files without authoritative duplicates** (`Config.cs`, `Console.cs`, `GoogleCredential.cs`, `Logger.cs`, `SyncProgressRenderer.cs`, `SyncProgressTracker.cs`) are NOT deleted — they will be migrated to their target projects in T3-00 through T3-06.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- [ ] T2-00 through T2-08 are signed off — all 8 projects exist and compile
- [ ] Full solution builds with `dotnet build csharp/Scripts.slnx` exiting 0
- [ ] All 8 test files pass: `dotnet test csharp/Scripts.slnx` exits 0

---

## Task 1 — TDD RED: Write failing duplicate detection test

### Step 1 — Write the test

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\DuplicateCleanupTests.cs`

```csharp
using System.IO;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests;

public class DuplicateCleanupTests
{
    private const string SrcRoot =
        @"/home/lance/Scripts/csharp/src";

    /// <summary>
    /// After deletion of duplicates, no .cs filename should appear in more than one
    /// project directory within csharp/src/. This prevents build confusion and
    /// ensures each type has a single authoritative location.
    /// </summary>
    [Test]
    public void NoDuplicateFileNamesExistAcrossSrcProjects()
    {
        // Get all .cs files under src/ excluding obj/ and bin/
        var allCsFiles = Directory.GetFiles(SrcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
            .ToList();

        allCsFiles.Should().NotBeEmpty("There must be .cs files under csharp/src/");

        // Group by filename only (not full path)
        var duplicates = allCsFiles
            .GroupBy(f => Path.GetFileName(f))
            .Where(g => g.Count() > 1)
            .Select(g => new
            {
                FileName = g.Key,
                Locations = g.ToList()
            })
            .ToList();

        if (duplicates.Any())
        {
            var dupReport = string.Join("\n", duplicates.Select(d =>
                $"  {d.FileName} found at:\n    " + string.Join("\n    ", d.Locations)));

            Assert.Fail(
                $"Duplicate .cs filenames detected across src/ projects. " +
                $"Each file must appear in exactly one project.\n{dupReport}");
        }
    }

    /// <summary>
    /// After deleting Infrastructure duplicates, the deleted files must not exist.
    /// </summary>
    [Test]
    public void StaleInfrastructureFiles_DoNotExist()
    {
        var staleFiles = new[]
        {
            @"/home/lance/Scripts/csharp/src\Infrastructure\StateManager.cs",
            @"/home/lance/Scripts/csharp/src\Infrastructure\ReleaseProgressCache.cs",
            @"/home/lance/Scripts/csharp/src\Infrastructure\Paths.cs",
            @"/home/lance/Scripts/csharp/src\Infrastructure\Resilience.cs",
            @"/home/lance/Scripts/csharp/src\Infrastructure\StringExtensions.cs",
            @"/home/lance/Scripts/csharp/src\Infrastructure\ValidationAttributes.cs",
        };

        foreach (var file in staleFiles)
        {
            File.Exists(file).Should().BeFalse(
                $"Stale file must be deleted: {file}");
        }
    }

    /// <summary>
    /// After deleting the root-level LastFmService.cs, only the
    /// Services/Sync/LastFm/LastFmService.cs should remain.
    /// </summary>
    [Test]
    public void DuplicateLastFmService_InSyncRoot_DoesNotExist()
    {
        var staleLastFm = @"/home/lance/Scripts/csharp/src\Services\Sync\LastFmService.cs";
        var authoritativeLastFm = @"/home/lance/Scripts/csharp/src\Services\Sync\LastFm\LastFmService.cs";

        File.Exists(staleLastFm).Should().BeFalse(
            $"Stale LastFmService.cs at Services/Sync/ root must be deleted. " +
            $"Authoritative version is at: {authoritativeLastFm}");

        File.Exists(authoritativeLastFm).Should().BeTrue(
            $"Authoritative LastFmService.cs must still exist at: {authoritativeLastFm}");
    }

    /// <summary>
    /// Full solution must compile after all deletions.
    /// </summary>
    [Test]
    public void FullSolution_BuildsAfterCleanup()
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            Arguments              = @"build /home/lance/Scripts/csharp/Scripts.slnx",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.WaitForExit();
        var stderr = proc.StandardError.ReadToEnd();
        proc.ExitCode.Should().Be(0,
            $"Full solution build failed after duplicate cleanup. stderr: {stderr}");
    }
}
```

### Step 2 — Run tests RED

```powershell
$result = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~DuplicateCleanupTests" `
    --no-build 2>&1
Write-Host $result
# Expected: FAILED — NoDuplicateFileNamesExistAcrossSrcProjects and StaleInfrastructureFiles_DoNotExist fail
```

---

## Task 2 — GREEN: Delete stale Infrastructure/StateManager.cs

### Step 3 — Backup, delete, verify

```powershell
Write-Host "STATE: Deleting stale src/Infrastructure/StateManager.cs"
Write-Host "REASON: Authoritative version is at src/Core/Persistence/StateManager.cs"
Write-Host "WHAT: Backup with .bak.YYYYMMDD_HHmmss, delete, assert gone"

$file = '/home/lance/Scripts/csharp/src\Infrastructure\StateManager.cs'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$file.bak.$ts"

if (-not (Test-Path $file)) {
    Write-Host "OUTCOME: $file does not exist (already deleted, skip)"
    return
}

Copy-Item $file $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of $file failed" }
Write-Host "OUTCOME: Backed up → $bak"

Remove-Item $file -ErrorAction Stop
if (Test-Path $file) { throw "Delete failed — $file still exists" }
Write-Host "OUTCOME: Deleted $file"

# Verify build still succeeds
$buildOutput = dotnet build '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed after deleting StateManager.cs" }
Write-Host "OUTCOME: Build verified OK after StateManager.cs deletion"
```

---

## Task 3 — GREEN: Delete stale Infrastructure/ReleaseProgressCache.cs

### Step 4 — Backup, delete, verify

```powershell
Write-Host "STATE: Deleting stale src/Infrastructure/ReleaseProgressCache.cs"
Write-Host "REASON: Authoritative version is at src/Core/Persistence/ReleaseProgressCache.cs"

$file = '/home/lance/Scripts/csharp/src\Infrastructure\ReleaseProgressCache.cs'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$file.bak.$ts"

if (-not (Test-Path $file)) {
    Write-Host "OUTCOME: $file does not exist (already deleted, skip)"
    return
}

Copy-Item $file $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of $file failed" }
Write-Host "OUTCOME: Backed up → $bak"

Remove-Item $file -ErrorAction Stop
if (Test-Path $file) { throw "Delete failed — $file still exists" }
Write-Host "OUTCOME: Deleted $file"

$buildOutput = dotnet build '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed after deleting ReleaseProgressCache.cs" }
Write-Host "OUTCOME: Build verified OK after ReleaseProgressCache.cs deletion"
```

---

## Task 4 — GREEN: Delete stale Infrastructure/Paths.cs

### Step 5 — Backup, delete, verify

```powershell
Write-Host "STATE: Deleting stale src/Infrastructure/Paths.cs"
Write-Host "REASON: Authoritative version is at src/Core/Paths.cs"

$file = '/home/lance/Scripts/csharp/src\Infrastructure\Paths.cs'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$file.bak.$ts"

if (-not (Test-Path $file)) {
    Write-Host "OUTCOME: $file does not exist (already deleted, skip)"
    return
}

Copy-Item $file $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of $file failed" }
Write-Host "OUTCOME: Backed up → $bak"

Remove-Item $file -ErrorAction Stop
if (Test-Path $file) { throw "Delete failed — $file still exists" }
Write-Host "OUTCOME: Deleted $file"

$buildOutput = dotnet build '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed after deleting Paths.cs" }
Write-Host "OUTCOME: Build verified OK after Paths.cs deletion"
```

---

## Task 5 — GREEN: Delete stale Infrastructure/Resilience.cs

### Step 6 — Backup, delete, verify

```powershell
Write-Host "STATE: Deleting stale src/Infrastructure/Resilience.cs"
Write-Host "REASON: Authoritative version is at src/Core/Resilience.cs"

$file = '/home/lance/Scripts/csharp/src\Infrastructure\Resilience.cs'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$file.bak.$ts"

if (-not (Test-Path $file)) {
    Write-Host "OUTCOME: $file does not exist (already deleted, skip)"
    return
}

Copy-Item $file $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of $file failed" }
Write-Host "OUTCOME: Backed up → $bak"

Remove-Item $file -ErrorAction Stop
if (Test-Path $file) { throw "Delete failed — $file still exists" }
Write-Host "OUTCOME: Deleted $file"

$buildOutput = dotnet build '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed after deleting Resilience.cs" }
Write-Host "OUTCOME: Build verified OK after Resilience.cs deletion"
```

---

## Task 6 — GREEN: Delete stale Infrastructure/StringExtensions.cs

### Step 7 — Backup, delete, verify

```powershell
Write-Host "STATE: Deleting stale src/Infrastructure/StringExtensions.cs"
Write-Host "REASON: Authoritative version is at src/Core/StringExtensions.cs"

$file = '/home/lance/Scripts/csharp/src\Infrastructure\StringExtensions.cs'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$file.bak.$ts"

if (-not (Test-Path $file)) {
    Write-Host "OUTCOME: $file does not exist (already deleted, skip)"
    return
}

Copy-Item $file $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of $file failed" }
Write-Host "OUTCOME: Backed up → $bak"

Remove-Item $file -ErrorAction Stop
if (Test-Path $file) { throw "Delete failed — $file still exists" }
Write-Host "OUTCOME: Deleted $file"

$buildOutput = dotnet build '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed after deleting StringExtensions.cs" }
Write-Host "OUTCOME: Build verified OK after StringExtensions.cs deletion"
```

---

## Task 7 — GREEN: Delete stale Services/Sync/LastFmService.cs (root-level duplicate)

### Step 8 — Backup, delete, verify

```powershell
Write-Host "STATE: Deleting stale src/Services/Sync/LastFmService.cs (root-level duplicate)"
Write-Host "REASON: Authoritative version is at src/Services/Sync/LastFm/LastFmService.cs"
Write-Host "WHAT: Backup with .bak.YYYYMMDD_HHmmss, delete, assert gone"

$file = '/home/lance/Scripts/csharp/src\Services\Sync\LastFmService.cs'
$authFile = '/home/lance/Scripts/csharp/src\Services\Sync\LastFm\LastFmService.cs'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$file.bak.$ts"

if (-not (Test-Path $file)) {
    Write-Host "OUTCOME: $file does not exist (already deleted, skip)"
    return
}

# Verify authoritative version exists before deleting the stale one
if (-not (Test-Path $authFile)) {
    throw "Authoritative LastFmService.cs does not exist at $authFile — cannot delete stale copy"
}

Copy-Item $file $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of $file failed" }
Write-Host "OUTCOME: Backed up → $bak"

Remove-Item $file -ErrorAction Stop
if (Test-Path $file) { throw "Delete failed — $file still exists" }
Write-Host "OUTCOME: Deleted $file"

# Verify authoritative version still exists
if (-not (Test-Path $authFile)) {
    throw "Authoritative LastFmService.cs is missing at $authFile — restore from backup immediately"
}
Write-Host "OUTCOME: Authoritative LastFmService.cs at $authFile still present"

$buildOutput = dotnet build '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed after deleting stale LastFmService.cs" }
Write-Host "OUTCOME: Build verified OK after LastFmService.cs deletion"
```

---

## Task 8 — GREEN: Delete stale Infrastructure/ValidationAttributes.cs

### Step 9 — Backup, delete, verify

```powershell
Write-Host "STATE: Deleting stale src/Infrastructure/ValidationAttributes.cs"
Write-Host "REASON: Authoritative version is at src/CLI/ValidationAttributes.cs"

$file = '/home/lance/Scripts/csharp/src\Infrastructure\ValidationAttributes.cs'
$authFile = '/home/lance/Scripts/csharp/src\CLI\ValidationAttributes.cs'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$file.bak.$ts"

if (-not (Test-Path $file)) {
    Write-Host "OUTCOME: $file does not exist (already deleted, skip)"
    return
}

# Verify authoritative version exists before deleting the stale one
if (-not (Test-Path $authFile)) {
    throw "Authoritative ValidationAttributes.cs does not exist at $authFile — cannot delete stale copy"
}

Copy-Item $file $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of $file failed" }
Write-Host "OUTCOME: Backed up → $bak"

Remove-Item $file -ErrorAction Stop
if (Test-Path $file) { throw "Delete failed — $file still exists" }
Write-Host "OUTCOME: Deleted $file"

if (-not (Test-Path $authFile)) {
    throw "Authoritative ValidationAttributes.cs is missing at $authFile — restore from backup immediately"
}
Write-Host "OUTCOME: Authoritative ValidationAttributes.cs at $authFile still present"

$buildOutput = dotnet build '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed after deleting stale ValidationAttributes.cs" }
Write-Host "OUTCOME: Build verified OK after ValidationAttributes.cs deletion"
```

---

## Task 9 — REFACTOR: Run duplicate test suite GREEN

### Step 9 — Verify all duplicate detection tests pass

```powershell
Write-Host "STATE: Running DuplicateCleanupTests after all deletions"
Write-Host "REASON: Confirm no duplicates remain and full solution builds"

$testOutput = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~DuplicateCleanupTests" 2>&1
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) { throw "DuplicateCleanupTests failed" }

# Also run the full test suite to confirm no regressions
$fullTestOutput = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' 2>&1
Write-Host $fullTestOutput
if ($LASTEXITCODE -ne 0) { throw "Full test suite failed after duplicate cleanup" }

# Expected:
# NoDuplicateFileNamesExistAcrossSrcProjects: PASSED
# StaleInfrastructureFiles_DoNotExist: PASSED
# DuplicateLastFmService_InSyncRoot_DoesNotExist: PASSED
# FullSolution_BuildsAfterCleanup: PASSED
# All other tests: PASSED
```

---

## Task 10 — Commit

```powershell
git -C '/home/lance/Scripts' add `
    'csharp/tests/Scripts.Tests/DuplicateCleanupTests.cs'

git -C '/home/lance/Scripts' add `
    'csharp/src/Infrastructure/StateManager.cs' `
    'csharp/src/Infrastructure/ReleaseProgressCache.cs' `
    'csharp/src/Infrastructure/Paths.cs' `
    'csharp/src/Infrastructure/Resilience.cs' `
    'csharp/src/Infrastructure/StringExtensions.cs' `
    'csharp/src/Services/Sync/LastFmService.cs' 2>$null

git -C '/home/lance/Scripts' commit `
    -m "feat(t2-09): delete 7 stale duplicate .cs files from Infrastructure/ and Services/Sync/ root"
```

---

## Sign-off Criteria

- [ ] All 6 stale `src/Infrastructure/*.cs` files deleted (StateManager, ReleaseProgressCache, Paths, Resilience, StringExtensions, ValidationAttributes)
- [ ] Stale `src/Services/Sync/LastFmService.cs` (root-level) deleted
- [ ] Authoritative `src/Services/Sync/LastFm/LastFmService.cs` still intact
- [ ] Authoritative `src/CLI/ValidationAttributes.cs` still intact
- [ ] `NoDuplicateFileNamesExistAcrossSrcProjects` test PASSES — no .cs filenames duplicated across src/ projects
- [ ] `dotnet build csharp/Scripts.slnx` exits 0 with 0 errors
- [ ] `dotnet test csharp/Scripts.slnx` — full test suite GREEN, exit code 0
- [ ] Each deletion has a `.bak.YYYYMMDD_HHmmss` backup for rollback
