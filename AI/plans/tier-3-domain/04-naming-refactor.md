# Naming Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rename all namespaces from `CSharpScripts.*` to `Scripts.*`, update `GlobalUsings.cs` and `Directory.Build.props` to match, delete obsolete `FiberyEntity`, enforce `internal sealed record` on all Data entities, strip duplicate global usings from `GlobalUsings.cs`, and relocate `SpectreTypeRegistrar` from Core to CLI.

**Architecture:** The `CSharpScripts.*` namespace prefix is a legacy artifact from the pre-modularization era. All namespaces must align with the new project naming convention (`Scripts.Core`, `Scripts.Data`, etc.). A global find/replace `CSharpScripts.` → `Scripts.` across all `.cs` files in `csharp/` is the authoritative rename step. After that, `Directory.Build.props` global usings and `GlobalUsings.cs` are updated to match. Then per-entity cleanup: entities in `Scripts.Data.Entities` must be `internal sealed record` — the public surface is the repository interfaces in Core. `FiberyEntity` is a PostgreSQL-era remnant with no remaining purpose. `GlobalUsings.cs` must not duplicate entries already in `Directory.Build.props`. `SpectreTypeRegistrar` belongs in the CLI project since it is a Spectre.Console.Cli concern.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Entity Design Context (from ENTITY-DESIGN research)

### FiberyEntity Removal

**FiberyEntity** is a PostgreSQL-era obsolete entity with no remaining purpose:
- No external references found in the codebase
- Not used by any service or orchestrator
- Must be deleted along with `FiberyEntityConfiguration.cs`

### Entity Access Modifiers

All entities in `Scripts.Data.Entities` must be `internal sealed record` or `internal sealed class`:
- **Why internal**: Entities are persistence details. External consumers use repository interfaces from Core.
- **Why sealed**: Prevents accidental inheritance; entities are data containers, not base classes.

### GlobalUsings.cs Cleanup

**Current issue**: `GlobalUsings.cs` contains package-level global using directives that are already declared in `Directory.Build.props`, causing duplication.

**Package-level usings that belong in Directory.Build.props** (remove from GlobalUsings.cs):
- `global using Microsoft.EntityFrameworkCore;`
- `global using Serilog;`
- `global using Spectre.Console;`
- `global using RestSharp;`
- `global using CsvHelper;`
- `global using MetaBrainz.MusicBrainz;`

**Keep in GlobalUsings.cs** (project-internal namespaces and type aliases):
- `global using Scripts.Core;`
- `global using Scripts.Core.Auth;`
- `global using Scripts.Core.Abstractions;`
- `global using Scripts.Models;`
- Type aliases: `global using DiscogsVideoDto = ParkSquare.Discogs.Dto.Video;`
- Spectre aliases: `global using SpectreColor = Spectre.Console.Color;`

### SpectreTypeRegistrar Relocation

**Current**: `SpectreTypeRegistrar.cs` in `Scripts.Core`
**Target**: `SpectreTypeRegistrar.cs` in `Scripts.CLI`
**Reason**: Spectre.Console.Cli is a CLI-only concern. Core should not reference Spectre.Console.Cli types.

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

## Task 0 — GREEN: Global namespace rename `CSharpScripts.*` → `Scripts.*`

**Current State:** All 25+ C# namespaces use `CSharpScripts.*` prefix (`CSharpScripts.Core`, `CSharpScripts.Data`, `CSharpScripts.CLI`, etc.). Project names are `Scripts.*` but namespaces are `CSharpScripts.*` — causing drift.

**Reason:** Namespace prefix must align with project naming convention. This rename is the single authoritative step; all downstream plan files (Tier 3-4) and `GlobalUsings.cs`/`Directory.Build.props` are updated as part of this task.

**What:** Find/replace `CSharpScripts.` → `Scripts.` across all `.cs` files in `csharp/`, excluding `obj/` and `bin/`. Also handle the standalone `CSharpScripts` namespace root. Update `GlobalUsings.cs` and `Directory.Build.props` global usings to match.

**Naming note:** The `CSharpScripts.csproj` monolith file name is NOT renamed — it will be deleted by T2-09. Only namespace references in `.cs` files change here.

**Expected Outcome:** Zero `CSharpScripts` references remain in any `.cs` source file under `csharp/`.

### Step 0.1 — Pre-flight: audit current namespace usage

```powershell
Write-Host "STATE: Auditing current CSharpScripts namespace usage"

$srcRoot = '/home/lance/Scripts/csharp'

Write-Host "=== All unique CSharpScripts.* namespaces ==="
Get-ChildItem $srcRoot -Filter '*.cs' -Recurse |
    Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' } |
    Select-String '^(namespace|using)\s+CSharpScripts' |
    ForEach-Object { $_.Line.Trim() } |
    Sort-Object -Unique

Write-Host ''
$count = (Get-ChildItem $srcRoot -Filter '*.cs' -Recurse |
    Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' } |
    Select-String 'CSharpScripts' -SimpleMatch).Count
Write-Host "Total CSharpScripts references in .cs files: $count"
```

### Step 0.2 — Backup all affected files

```powershell
Write-Host "REASON: Global rename — backup entire src/ tree"
$ts = Get-Date -Format 'yyyyMMdd_HHmmss'
$bakDir = "/home/lance/Scripts/csharp/src-backup-$ts"

Copy-Item '/home/lance/Scripts/csharp/src' $bakDir -Recurse -ErrorAction Stop
if (-not (Test-Path $bakDir)) { throw "Full src/ backup failed" }
Write-Host "OUTCOME: Full src/ backed up to $bakDir"
```

### Step 0.3 — Execute global namespace rename

```powershell
Write-Host "STATE: Executing global rename CSharpScripts. → Scripts. across all .cs files"
Write-Host "REASON: Align namespaces with project naming convention"
Write-Host "WHAT: Replace 'CSharpScripts.' with 'Scripts.' in all .cs files"
Write-Host "EXPECTED OUTCOME: Zero CSharpScripts references remaining"

$srcRoot = '/home/lance/Scripts/csharp'
$csFiles = Get-ChildItem $srcRoot -Filter '*.cs' -Recurse |
    Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' } |
    Where-Object { $_.FullName -notmatch '\\src-backup-' }

$renamed = 0
foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    if ($content -match 'CSharpScripts') {
        # Replace CSharpScripts. → Scripts. (dotted namespace)
        # Also handle standalone CSharpScripts (root namespace) → Scripts
        $updated = $content -replace 'CSharpScripts\.', 'Scripts.'
        $updated = $updated -replace '(?<!\w)CSharpScripts(?!\.\w)', 'Scripts'

        Set-Content -Path $file.FullName -Value $updated -Encoding UTF8 -NoNewline -ErrorAction Stop
        $renamed++
    }
}
Write-Host "OUTCOME: Renamed CSharpScripts → Scripts in $renamed file(s)"
```

### Step 0.4 — Verify rename completeness

```powershell
Write-Host "STATE: Verifying zero CSharpScripts references remain"

$srcRoot = '/home/lance/Scripts/csharp'
$remaining = Get-ChildItem $srcRoot -Filter '*.cs' -Recurse |
    Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' } |
    Where-Object { $_.FullName -notmatch '\\src-backup-' } |
    Select-String 'CSharpScripts' -SimpleMatch

if ($remaining) {
    Write-Host "BLOCKER: CSharpScripts references still present:"
    $remaining | ForEach-Object { Write-Host "  $($_.Path):$($_.LineNumber) — $($_.Line.Trim())" }
    throw "Namespace rename incomplete — CSharpScripts references still exist"
}
Write-Host "OUTCOME: Zero CSharpScripts references — rename complete"
```

### Step 0.5 — Update GlobalUsings.cs to use new namespaces

```powershell
Write-Host "STATE: Updating GlobalUsings.cs namespace references"

$globalUsings = '/home/lance/Scripts/csharp/src\GlobalUsings.cs'
$bak = "$globalUsings.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item $globalUsings $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true

$content = Get-Content $globalUsings -Raw -Encoding UTF8
$updated = $content -replace 'CSharpScripts\.', 'Scripts.'
Set-Content -Path $globalUsings -Value $updated -Encoding UTF8 -ErrorAction Stop

# Verify
$check = Get-Content $globalUsings -Raw -Encoding UTF8
if ($check -match 'CSharpScripts') { throw "CSharpScripts still in GlobalUsings.cs" }
if ($check -notmatch 'Scripts\.Core') { throw "Scripts.Core missing from GlobalUsings.cs" }
Write-Host "OUTCOME: GlobalUsings.cs updated — all references changed to Scripts.*"
```

### Step 0.6 — Update Directory.Build.props global usings

```powershell
Write-Host "STATE: Updating Directory.Build.props global usings"

$buildProps = '/home/lance/Scripts/csharp/Directory.Build.props'
$bak = "$buildProps.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item $buildProps $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true

$content = Get-Content $buildProps -Raw -Encoding UTF8

# Update the conditional global using from CSharpScripts.Core to Scripts.Core
$updated = $content -replace 'CSharpScripts\.Core', 'Scripts.Core'
Set-Content -Path $buildProps -Value $updated -Encoding UTF8 -ErrorAction Stop

# Verify
$check = Get-Content $buildProps -Raw -Encoding UTF8
if ($check -match 'CSharpScripts') { throw "CSharpScripts still in Directory.Build.props" }
if ($check -notmatch "Scripts\.Core") { throw "Scripts.Core missing from Directory.Build.props" }
Write-Host "OUTCOME: Directory.Build.props updated — Scripts.Core global using"
```

### Step 0.7 — Build verification after namespace rename

```powershell
Write-Host "STATE: Building solution after namespace rename"
Write-Host "REASON: Verify all references resolve with new Scripts.* namespaces"

dotnet restore '/home/lance/Scripts/csharp/Scripts.slnx' -ErrorAction Stop

$buildOut = dotnet build '/home/lance/Scripts/csharp/Scripts.slnx' --no-restore 2>&1
Write-Host ($buildOut -join "`n")
if ($LASTEXITCODE -ne 0) {
    Write-Host "BLOCKER: Build failed after namespace rename — check for missed references"
    throw "dotnet build failed with exit code $LASTEXITCODE"
}
if ($buildOut -notmatch 'Build succeeded') {
    throw "Build output does not contain 'Build succeeded'"
}
Write-Host "OUTCOME: Build succeeded with Scripts.* namespaces"
```

---

## Task 1 — TDD RED: Write naming refactor pre-condition tests

### Step 1.1 — Create test file

```powershell
$dir = "/home/lance/Scripts/csharp/tests\Scripts.Tests\T3"
New-Item -ItemType Directory -Path $dir -Force -ErrorAction Stop
Test-Path $dir | Should -Be $true
```

Create file `/home/lance/Scripts/csharp/tests\Scripts.Tests\T3\T304_NamingRefactorTests.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using TUnit.Core;

namespace Scripts.Tests.T3;

public class T304_NamingRefactorTests
{
    private const string DataEntitiesDir =
        @"/home/lance/Scripts/csharp/src\Data\Entities";

    private const string GlobalUsingsFile =
        @"/home/lance/Scripts/csharp/src\GlobalUsings.cs";

    private const string CoreDir =
        @"/home/lance/Scripts/csharp/src\Core";

    private const string CLIDir =
        @"/home/lance/Scripts/csharp/src\CLI";

    [Test]
    public void FiberyEntity_DoesNotExist()
    {
        var fiberyFile = Path.Combine(DataEntitiesDir, "FiberyEntity.cs");
        File.Exists(fiberyFile).Should().BeFalse(
            "because FiberyEntity is a PostgreSQL-era obsolete entity — it must be deleted");
    }

    [Test]
    public void AllEntities_AreInternal()
    {
        Directory.Exists(DataEntitiesDir).Should().BeTrue(
            $"because Data Entities directory must exist at {DataEntitiesDir}");

        var entityFiles = Directory
            .GetFiles(DataEntitiesDir, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        entityFiles.Should().NotBeEmpty(
            "because Scripts.Data must contain entity files");

        foreach (var file in entityFiles)
        {
            var content = File.ReadAllText(file);

            // Skip files that are not class/record declarations
            if (!content.Contains("class ") && !content.Contains("record "))
                continue;

            content.Should().NotMatchRegex(
                @"public\s+(class|record|sealed\s+class|sealed\s+record)\s+\w+",
                $"because {Path.GetFileName(file)} must use internal access modifier, not public");
            content.Should().MatchRegex(
                @"internal\s+sealed\s+(class|record)\s+\w+",
                $"because {Path.GetFileName(file)} must be 'internal sealed record' or 'internal sealed class'");
        }
    }

    [Test]
    public void GlobalUsings_DoesNotContain_PackageLevelUsings()
    {
        File.Exists(GlobalUsingsFile).Should().BeTrue(
            $"because GlobalUsings.cs must exist at {GlobalUsingsFile}");

        var content = File.ReadAllText(GlobalUsingsFile);

        // Package-level usings that belong in Directory.Build.props:
        content.Should().NotContain("global using Microsoft.EntityFrameworkCore",
            "because Microsoft.EntityFrameworkCore global using belongs in Directory.Build.props");
        content.Should().NotContain("global using Serilog",
            "because Serilog global using belongs in Directory.Build.props");
        content.Should().NotContain("global using Spectre.Console",
            "because Spectre.Console global using belongs in Directory.Build.props");
        content.Should().NotContain("global using RestSharp",
            "because RestSharp global using belongs in Directory.Build.props");
        content.Should().NotContain("global using CsvHelper",
            "because CsvHelper global using belongs in Directory.Build.props");
        content.Should().NotContain("global using MetaBrainz.MusicBrainz",
            "because MetaBrainz.MusicBrainz global using belongs in Directory.Build.props");
    }

    [Test]
    public void SpectreTypeRegistrar_IsInCLI_NotCore()
    {
        var coreFile = Path.Combine(CoreDir, "SpectreTypeRegistrar.cs");
        var cliFile  = Path.Combine(CLIDir, "SpectreTypeRegistrar.cs");

        File.Exists(coreFile).Should().BeFalse(
            "because SpectreTypeRegistrar must be removed from Core — it is a Spectre.Console.Cli concern");
        File.Exists(cliFile).Should().BeTrue(
            "because SpectreTypeRegistrar must be moved to the CLI project");
    }
}
```

### Step 1.2 — Run to confirm RED

```powershell
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop

dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "FullyQualifiedName~T304_NamingRefactorTests" `
    2>&1 | Tee-Object -Variable testOutput

Write-Host ($testOutput -join "`n")
# Expected: 3-4 tests fail (FiberyEntity exists, some entities public, GlobalUsings has package-level entries, SpectreTypeRegistrar in Core)
# If all pass → already clean → skip to commit
```

---

## Task 2 — GREEN: Delete FiberyEntity

**Current State:** `FiberyEntity.cs` exists at `csharp/src/Data/Entities/FiberyEntity.cs`.
**Reason:** It is a PostgreSQL-era obsolete entity with no remaining purpose in the EF Core 10 schema.
**What:** Back up and delete the file. If any other file references `FiberyEntity`, remove those references.
**Expected Outcome:** `FiberyEntity.cs` does not exist. No file in the solution imports `FiberyEntity`.

### Step 2.0 — Pre-flight: Find all FiberyEntity references

```powershell
$repoRoot = "/home/lance/Scripts/csharp/src"

Write-Host "=== Files referencing FiberyEntity ==="
Get-ChildItem $repoRoot -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "FiberyEntity" |
    ForEach-Object { "$($_.Path):$($_.LineNumber)  $($_.Line.Trim())" }
```

### Step 2.1 — Remove references in other files

For each file that references `FiberyEntity` (beyond the entity file itself), remove the reference:

```powershell
# Remove DbSet<FiberyEntity> FiberyEntities from ScriptsDbContext.cs
$file = "/home/lance/Scripts/csharp/src\Data\ScriptsDbContext.cs"
$bak  = "$file.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $file -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true

$content = Get-Content $file -Raw -Encoding UTF8
$updated = $content -replace "public DbSet<FiberyEntity>[^\n]*(\r?\n)?", ""
Set-Content -Path $file -Value $updated -Encoding UTF8 -ErrorAction Stop

# Verify
$check = Get-Content $file -Raw -Encoding UTF8
$check | Should -Not -Match "FiberyEntity"
Write-Host "Removed FiberyEntity reference from: $file"
```

### Step 2.2 — Back up and delete FiberyEntity.cs

```powershell
$fiberyFile = "/home/lance/Scripts/csharp/src\Data\Entities\FiberyEntity.cs"
$bak = "$fiberyFile.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"

Copy-Item -Path $fiberyFile -Destination $bak -ErrorAction Stop
Test-Path $bak    | Should -Be $true
Write-Host "Backed up: $bak"

Remove-Item -Path $fiberyFile -ErrorAction Stop
Test-Path $fiberyFile | Should -Be $false
Write-Host "Deleted: $fiberyFile"
```

### Step 2.3 — Back up and delete FiberyEntityConfiguration.cs

```powershell
$configFile = "/home/lance/Scripts/csharp/src\Data\Configuration\FiberyEntityConfiguration.cs"

if (Test-Path $configFile) {
    $bak = "$configFile.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Copy-Item -Path $configFile -Destination $bak -ErrorAction Stop
    Test-Path $bak | Should -Be $true
    Write-Host "Backed up: $bak"

    Remove-Item -Path $configFile -ErrorAction Stop
    Test-Path $configFile | Should -Be $false
    Write-Host "Deleted: $configFile"
} else {
    Write-Host "FiberyEntityConfiguration.cs not found — skipping"
}
```

---

## Task 3 — GREEN: Make all entities internal sealed record

**Current State:** One or more entities may be `public` instead of `internal`.
**Reason:** Entities are persistence details — external consumers use repository interfaces from Core.
**What:** Audit all files in `Data/Entities/` and change `public` declarations to `internal`.
**Expected Outcome:** Every entity file contains `internal sealed record` or `internal sealed class`.

### Step 3.1 — Audit current entity access modifiers

```powershell
$entitiesDir = "/home/lance/Scripts/csharp/src\Data\Entities"

Get-ChildItem $entitiesDir -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    ForEach-Object {
        $content = Get-Content $_.FullName -Raw -Encoding UTF8
        if ($content -match "public\s+(sealed\s+)?(class|record)\s+(\w+)") {
            Write-Host "PUBLIC: $($_.Name) — $($matches[3])"
        }
        elseif ($content -match "internal\s+sealed\s+(class|record)\s+(\w+)") {
            Write-Host "OK: $($_.Name) — $($matches[2])"
        }
        else {
            Write-Host "UNKNOWN: $($_.Name) — check manually"
        }
    }
```

### Step 3.2 — Fix each public entity

For each entity that is `public`, back it up and change the access modifier:

```powershell
$file = "/home/lance/Scripts/csharp/src\Data\Entities\<EntityName>.cs"
$bak  = "$file.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $file -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true

$content = Get-Content $file -Raw -Encoding UTF8

# Replace public class/record with internal sealed record
$updated = $content `
    -replace "public\s+class\s+(\w+)",           "internal sealed class `$1" `
    -replace "public\s+sealed\s+class\s+(\w+)",   "internal sealed class `$1" `
    -replace "public\s+record\s+(\w+)",           "internal sealed record `$1" `
    -replace "public\s+sealed\s+record\s+(\w+)",  "internal sealed record `$1"

Set-Content -Path $file -Value $updated -Encoding UTF8 -ErrorAction Stop

# Verify
$check = Get-Content $file -Raw -Encoding UTF8
$check | Should -Not -Match "public\s+(sealed\s+)?(class|record)\s+(\w+)"
$check | Should -Match "internal\s+sealed\s+(class|record)\s+(\w+)"
Write-Host "Fixed: $file"
```

---

## Task 4 — GREEN: Strip duplicate global usings from GlobalUsings.cs

**Current State:** `GlobalUsings.cs` contains package-level global using directives that are already declared in `Directory.Build.props`.
**Reason:** Avoid duplicate declarations that can cause confusion and maintenance burden.
**What:** Remove all package-level global usings from `GlobalUsings.cs` — keep only project-internal namespace aliases.
**Expected Outcome:** `GlobalUsings.cs` contains only internal project namespace imports and type aliases (no `Serilog`, `Spectre.Console`, `RestSharp`, `CsvHelper`, `MetaBrainz.MusicBrainz`, `Microsoft.EntityFrameworkCore`).

### Step 4.1 — Back up GlobalUsings.cs

```powershell
$globalUsings = "/home/lance/Scripts/csharp/src\GlobalUsings.cs"
$bak = "$globalUsings.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $globalUsings -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true
Write-Host "Backed up: $bak"
```

### Step 4.2 — Rewrite GlobalUsings.cs

Write the cleaned file. Keep only project-internal namespaces and type aliases:

```csharp
global using System.Collections.Frozen;
global using System.Diagnostics;
global using static System.Environment;
global using System.Globalization;
global using static System.String;
global using static System.StringComparison;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using Scripts.Core;
global using Scripts.Core.Auth;
global using Scripts.Core.Abstractions;
global using Scripts.Models;
global using DiscogsVideoDto = ParkSquare.Discogs.Dto.Video;
global using Log = Scripts.Core.Log;
global using SearchResult = Scripts.Models.SearchResult;
global using SpectreColor = Spectre.Console.Color;
global using SpectreProgress = Spectre.Console.Progress;
global using SpectreTable = Spectre.Console.Table;
```

```powershell
Set-Content -Path $globalUsings -Value @'
global using System.Collections.Frozen;
global using System.Diagnostics;
global using static System.Environment;
global using System.Globalization;
global using static System.String;
global using static System.StringComparison;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using Scripts.Core;
global using Scripts.Core.Auth;
global using Scripts.Core.Abstractions;
global using Scripts.Models;
global using DiscogsVideoDto = ParkSquare.Discogs.Dto.Video;
global using Log = Scripts.Core.Log;
global using SearchResult = Scripts.Models.SearchResult;
global using SpectreColor = Spectre.Console.Color;
global using SpectreProgress = Spectre.Console.Progress;
global using SpectreTable = Spectre.Console.Table;
'@ -Encoding UTF8 -ErrorAction Stop

# Verify
$check = Get-Content $globalUsings -Raw -Encoding UTF8
$check | Should -Not -Match "global using CsvHelper"
$check | Should -Not -Match "global using Microsoft\.EntityFrameworkCore"
$check | Should -Not -Match "global using RestSharp"
$check | Should -Not -Match "global using MetaBrainz\.MusicBrainz"
$check | Should -Not -Match "global using Serilog"
$check | Should -Not -Match "global using Spectre\.Console;"
$check | Should -Match "global using SpectreColor"
$check | Should -Match "global using SpectreProgress"
$check | Should -Match "global using SpectreTable"
Write-Host "GlobalUsings.cs stripped of package-level duplicates"
```

---

## Task 5 — GREEN: Move SpectreTypeRegistrar from Core to CLI

**Current State:** `SpectreTypeRegistrar.cs` lives in `/home/lance/Scripts/csharp/src\Core\`.
**Reason:** Spectre.Console.Cli is a CLI-only concern. Core should not reference Spectre.Console.Cli types.
**What:** Move the file to the CLI project directory and update its namespace.
**Expected Outcome:** `SpectreTypeRegistrar.cs` exists in CLI, not in Core.

### Step 5.1 — Move the file

```powershell
$srcFile = "/home/lance/Scripts/csharp/src\Core\SpectreTypeRegistrar.cs"
$dstDir  = "/home/lance/Scripts/csharp/src\CLI"
$dstFile = "/home/lance/Scripts/csharp/src\CLI\SpectreTypeRegistrar.cs"

# Backup original
$bak = "$srcFile.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $srcFile -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true
Write-Host "Backed up: $bak"

# Copy to CLI directory
Copy-Item -Path $srcFile -Destination $dstFile -ErrorAction Stop
Test-Path $dstFile | Should -Be $true
Write-Host "Copied to CLI: $dstFile"

# Update namespace from Scripts.Core to Scripts.CLI
$content = Get-Content $dstFile -Raw -Encoding UTF8
$updated = $content -replace "namespace Scripts\.Core;", "namespace Scripts.CLI;"
Set-Content -Path $dstFile -Value $updated -Encoding UTF8 -ErrorAction Stop

# Verify namespace changed
$check = Get-Content $dstFile -Raw -Encoding UTF8
$check | Should -Match "namespace Scripts\.CLI;"
Write-Host "Namespace updated to Scripts.CLI"

# Remove original from Core
Remove-Item -Path $srcFile -ErrorAction Stop
Test-Path $srcFile | Should -Be $false
Write-Host "Removed from Core: $srcFile"
```

### Step 5.2 — Update all references to SpectreTypeRegistrar

```powershell
$repoRoot = "/home/lance/Scripts/csharp/src"

Write-Host "=== Files referencing SpectreTypeRegistrar ==="
Get-ChildItem $repoRoot -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "SpectreTypeRegistrar" |
    ForEach-Object { "$($_.Path):$($_.LineNumber)  $($_.Line.Trim())" }
```

For any file outside CLI that references `SpectreTypeRegistrar`:

```powershell
$file = "/home/lance/Scripts/csharp/src\<Dir>\<FileName>.cs"
$bak  = "$file.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $file -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true

$content = Get-Content $file -Raw -Encoding UTF8

# If Program.cs uses SpectreTypeRegistrar, ensure it imports Scripts.CLI
if ($content -match "SpectreTypeRegistrar" -and $content -notmatch "using Scripts\.CLI") {
    $updated = "using Scripts.CLI;`n" + $content
    Set-Content -Path $file -Value $updated -Encoding UTF8 -ErrorAction Stop
    Write-Host "Added using Scripts.CLI to: $file"
}
```

---

## Task 6 — Build and test GREEN

**Current State:** Source changes applied — FiberyEntity deleted, entities internal, GlobalUsings stripped, SpectreTypeRegistrar moved.
**Reason:** Confirm compilation succeeds and all T304 tests pass.
**What:** Full restore → build → targeted test run → full test suite.
**Expected Outcome:** 0 build errors, all T304 tests pass, full suite green.

```powershell
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop

$buildOut = dotnet build /home/lance/Scripts/csharp/Scripts.slnx --no-restore 2>&1
$buildOut | Select-String "0 Error" | Should -Not -BeNullOrEmpty
Write-Host "Build: GREEN"

# Run naming refactor tests
$testOut = dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "FullyQualifiedName~T304_NamingRefactorTests" 2>&1
$testOut | Select-String "Failed: 0" | Should -Not -BeNullOrEmpty
Write-Host "T304 tests: GREEN"

# Full suite to catch regressions from FiberyEntity deletion and SpectreTypeRegistrar move
$fullTestOut = dotnet test /home/lance/Scripts/csharp/Scripts.slnx 2>&1
$fullTestOut | Select-String "Failed: 0" | Should -Not -BeNullOrEmpty
Write-Host "Full test suite: GREEN"
```

Expected output:
```
Test Run Successful.
Tests: 4 (4 passed)
```

---

## Task 7 — REFACTOR: Commit naming refactor

**Current State:** Tests green, FiberyEntity deleted, entities internal, GlobalUsings stripped, SpectreTypeRegistrar in CLI.
**Reason:** Record naming refactor as a discrete commit.
**What:** Stage all changes, verify, commit.
**Expected Outcome:** Commit `feat(t3-04)` in git log.

```powershell
Set-Location /home/lance/Scripts -ErrorAction Stop

gitleaks detect --no-git 2>&1 | Select-String "leaks found" | ForEach-Object {
    throw "Gitleaks found secrets — abort commit"
}

# Stage entity changes
git add csharp/src/Data/Entities/ 2>&1
git add csharp/src/GlobalUsings.cs 2>&1
git add csharp/src/Core/SpectreTypeRegistrar.cs 2>&1  # deletion
git add csharp/src/CLI/SpectreTypeRegistrar.cs 2>&1    # new location
git add csharp/tests/Scripts.Tests/T3/T304_NamingRefactorTests.cs 2>&1

git status 2>&1 | Write-Host

git commit -m "feat(t3-04): naming refactor — delete FiberyEntity, internalize entities, strip GlobalUsings, move SpectreTypeRegistrar to CLI" `
    -ErrorAction Stop 2>&1 | Tee-Object -Variable commitOut

$commitOut | Select-String "feat\(t3-04\)" | Should -Not -BeNullOrEmpty
Write-Host "Committed: t3-04"
```

---

## Completion Criteria

| Check | Command | Expected |
|-------|---------|----------|
| Build clean | `dotnet build csharp/Scripts.slnx` | `0 Error(s)` |
| Tests pass | `dotnet test --filter T304` | `Failed: 0` |
| Full suite green | `dotnet test csharp/Scripts.slnx` | `Failed: 0` |
| FiberyEntity gone | `Test-Path csharp/src/Data/Entities/FiberyEntity.cs` | `False` |
| All entities internal | `grep "public.*class\|public.*record" csharp/src/Data/Entities/*.cs` | No output |
| No package-level global usings | `grep "global using (CsvHelper\|Serilog\|RestSharp\|Spectre.Console;\|Microsoft.EntityFrameworkCore\|MetaBrainz)" csharp/src/GlobalUsings.cs` | No output |
| SpectreTypeRegistrar in CLI | `Test-Path csharp/src/CLI/SpectreTypeRegistrar.cs` | `True` |
| SpectreTypeRegistrar not in Core | `Test-Path csharp/src/Core/SpectreTypeRegistrar.cs` | `False` |
| Commit present | `git log --oneline -1` | `feat(t3-04)` |
