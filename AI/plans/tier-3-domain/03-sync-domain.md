# Sync Domain / Orchestrators Isolation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Verify and enforce that the Orchestrators domain references Data and Services but NOT CLI or Reader, and that sync orchestrators use repository interfaces rather than raw DbContext.

**Architecture:** Scripts.Orchestrators sits downstream of Data and Services. It consumes repository interfaces (defined in Core) and entity types (from Data), but must never directly instantiate `ScriptsDbContext`. It must not reference CLI (Spectre.Console.Cli types must stay in CLI) or Reader (ArticleContent, PDF extraction types must stay in Reader).

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Orchestrators Persistence Context (from DATA-ACCESS-REPOSITORIES research)

### Current Data Access: PostgresService

The only current data access service is `PostgresService` in `csharp/src/Services/PostgresService.cs`:

```csharp
internal sealed class PostgresService(IDbContextFactory<ScriptsDbContext> contextFactory)
{
    internal async Task UpsertScrobbleAsync(long id, int trackId, DateTimeOffset timestamp, string platform, CancellationToken ct);
    internal async Task BulkInsertTracksAsync(IEnumerable<Track> tracks, CancellationToken ct);
}
```

**Pattern**: Primary constructor with `IDbContextFactory<ScriptsDbContext>`. Creates new context per method via `CreateDbContextAsync()`, disposes via `await using`.

### Mutation Patterns in Use

| Operation | Pattern | Status |
|-----------|---------|--------|
| `ExecuteUpdateAsync` | Single-entity upsert | ✅ Correct |
| `SaveChangesAsync` | Bulk insert | ⚠️ Should use `ExecuteUpdateAsync` for upserts |
| `ExecuteDeleteAsync` | Bulk delete | ❌ Never used |

### Recommended Repository Interfaces for Orchestrators

Orchestrators must use these repository interfaces (defined in Core, implemented in Data):

- **IScrobbleRepository**: Upsert scrobbles, query by track/platform
- **ITrackRepository**: Bulk insert tracks, query by artist
- **IVideoRepository**: Add/update videos, query by URL/channel
- **IArtistRepository**: Lookup/upsert artists
- **IAlbumRepository**: Lookup/upsert albums
- **IExecutionLogRepository**: Log execution events
- **IFailedTaskRepository**: Track failed operations

### Duplicate LastFmService Cleanup

**Finding**: Two `LastFmService.cs` files exist with the same namespace and class name:
- `Sync/LastFmService.cs` (175 lines) — **CANONICAL** (async StateManager, Serilog logging)
- `Sync/LastFm/LastFmService.cs` (165 lines) — **LEGACY** (sync StateManager, Console logging)

**Action**: Delete `csharp/src/Services/Sync/LastFm/LastFmService.cs` and the entire `Sync/LastFm/` subdirectory.

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

## Task 1 — TDD RED: Write Sync domain isolation tests

**Current State:** No tests assert Orchestrators dependency constraints or that orchestrators use repos instead of raw DbContext.
**Reason:** Failing tests drive isolation enforcement.
**What:** Create `T303_SyncDomainTests.cs` in `Scripts.Tests\T3\`.
**Expected Outcome:** Tests compile. If violations exist, 1+ tests fail.

### Step 1.1 — Create test file

```powershell
$dir = "/home/lance/Scripts/csharp/tests\Scripts.Tests\T3"
New-Item -ItemType Directory -Path $dir -Force -ErrorAction Stop
Test-Path $dir | Should -Be $true
```

Create file `/home/lance/Scripts/csharp/tests\Scripts.Tests\T3\T303_SyncDomainTests.cs`:

```csharp
using System.IO;
using System.Linq;
using FluentAssertions;
using TUnit.Core;

namespace CSharpScripts.Tests.T3;

public class T303_SyncDomainTests
{
    private const string OrchestratorsCsproj =
        @"/home/lance/Scripts/csharp/src\Orchestrators\Scripts.Orchestrators.csproj";

    private const string OrchestratorsSrcDir =
        @"/home/lance/Scripts/csharp/src\Orchestrators";

    [Test]
    public void OrchestratorsDomain_DoesNotReferenceCLIOrReader()
    {
        File.Exists(OrchestratorsCsproj).Should().BeTrue(
            "because Scripts.Orchestrators.csproj must exist at the expected path");

        var content = File.ReadAllText(OrchestratorsCsproj);

        content.Should().NotContain("Scripts.CLI",
            "because Orchestrators must not reference the CLI project (Spectre.Console.Cli must stay in CLI)");
        content.Should().NotContain("Scripts.Reader",
            "because Orchestrators must not reference the Reader domain");
    }

    [Test]
    public void OrchestratorsDomain_DoesNotContain_DbContextInstantiation()
    {
        Directory.Exists(OrchestratorsSrcDir).Should().BeTrue(
            $"because Orchestrators source directory must exist at {OrchestratorsSrcDir}");

        var files = Directory
            .GetFiles(OrchestratorsSrcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        files.Should().NotBeEmpty(
            "because Scripts.Orchestrators must contain at least one .cs file");

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            content.Should().NotContain("new ScriptsDbContext",
                $"because {Path.GetFileName(file)} must not directly instantiate the DbContext — use repository interfaces instead");
        }
    }

    [Test]
    public void OrchestratorsDomain_DoesNotImport_CLIOrReaderNamespaces()
    {
        Directory.Exists(OrchestratorsSrcDir).Should().BeTrue();

        var files = Directory
            .GetFiles(OrchestratorsSrcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            content.Should().NotContain("using CSharpScripts.CLI",
                $"because {Path.GetFileName(file)} must not import from the CLI namespace");
            content.Should().NotContain("using CSharpScripts.Reader",
                $"because {Path.GetFileName(file)} must not import from the Reader namespace");
        }
    }

    [Test]
    public void OrchestratorsDomain_AllFiles_HaveCorrectNamespace()
    {
        Directory.Exists(OrchestratorsSrcDir).Should().BeTrue();

        var files = Directory
            .GetFiles(OrchestratorsSrcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        files.Should().NotBeEmpty();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("namespace "))
                continue;

            content.Should().Contain("namespace CSharpScripts.Orchestrators",
                $"because {Path.GetFileName(file)} has a wrong namespace — expected CSharpScripts.Orchestrators.*");
        }
    }
}
```

### Step 1.2 — Run to confirm RED

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "FullyQualifiedName~T303_SyncDomainTests" `
    2>&1 | Tee-Object -Variable testOutput

Write-Host ($testOutput -join "`n")
# If all pass → Sync domain already clean → skip to Task 5 (commit)
# If any fail → proceed to Task 2
```

---

## Task 2 — Inspect current Orchestrators dependencies

**Current State:** Unknown whether Orchestrators has illegal imports or DbContext instantiation.
**Reason:** Must identify violations before editing source.
**What:** Grep `.csproj` and all `.cs` files for illegal references.
**Expected Outcome:** Explicit list of violating files and lines.

```powershell
$syncDir = "/home/lance/Scripts/csharp/src\Orchestrators"

Write-Host "=== .csproj ProjectReferences ==="
Get-Content "/home/lance/Scripts/csharp/src\Orchestrators\Scripts.Orchestrators.csproj" `
    -ErrorAction SilentlyContinue |
    Select-String "ProjectReference"

Write-Host "=== Source files importing CSharpScripts.CLI ==="
Get-ChildItem $syncDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "CSharpScripts\.CLI" |
    ForEach-Object { "$($_.Path):$($_.LineNumber)  $($_.Line.Trim())" }

Write-Host "=== Source files importing CSharpScripts.Reader ==="
Get-ChildItem $syncDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "CSharpScripts\.Reader" |
    ForEach-Object { "$($_.Path):$($_.LineNumber)  $($_.Line.Trim())" }

Write-Host "=== Source files with raw DbContext instantiation ==="
Get-ChildItem $syncDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "new ScriptsDbContext" |
    ForEach-Object { "$($_.Path):$($_.LineNumber)  $($_.Line.Trim())" }

Write-Host "=== Source files with Spectre.Console.Cli imports (should be in CLI only) ==="
Get-ChildItem $syncDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "Spectre\.Console\.Cli" |
    ForEach-Object { "$($_.Path):$($_.LineNumber)  $($_.Line.Trim())" }
```

---

## Task 3 — GREEN: Remove illegal Orchestrators → CLI/Reader references

> Skip if Task 2 found no violations.

**Current State:** Orchestrators references CLI or Reader directly.
**Reason:** Violates dependency flow: Orchestrators must depend on Data + Services only.
**What:** Remove illegal project references and using directives.
**Expected Outcome:** No CLI/Reader references in Orchestrators `.csproj` or `.cs` files.

### Step 3.1 — Back up affected files

For each affected source file (replace `<FileName>` with the actual filename):

```powershell
$src = "/home/lance/Scripts/csharp/src\Orchestrators\<FileName>.cs"
$bak = "$src.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $src -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true
Write-Host "Backed up: $bak"
```

### Step 3.2 — Remove illegal using directives from source files

```powershell
$file    = "/home/lance/Scripts/csharp/src\Orchestrators\<FileName>.cs"
$content = Get-Content $file -Raw -Encoding UTF8

$updated = $content `
    -replace "using CSharpScripts\.CLI[^;]*;(\r?\n)?", "" `
    -replace "using CSharpScripts\.Reader[^;]*;(\r?\n)?", ""

Set-Content -Path $file -Value $updated -Encoding UTF8 -ErrorAction Stop

$check = Get-Content $file -Raw -Encoding UTF8
$check | Should -Not -Match "using CSharpScripts\.CLI"
$check | Should -Not -Match "using CSharpScripts\.Reader"
Write-Host "Cleaned: $file"
```

### Step 3.3 — Remove illegal project references from .csproj

```powershell
$csproj = "/home/lance/Scripts/csharp/src\Orchestrators\Scripts.Orchestrators.csproj"
$bak    = "$csproj.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $csproj -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true

$xml     = [xml](Get-Content $csproj -Encoding UTF8)
$illegal = @("Scripts.CLI", "Scripts.Reader")

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

## Task 4 — GREEN: Replace raw DbContext usage with repository interfaces

> Skip if Task 2 found no `new ScriptsDbContext` in Orchestrators source.

**Current State:** Orchestrators directly instantiate `ScriptsDbContext`.
**Reason:** Orchestrators must use repository interfaces (defined in Core) injected via DI, not raw DbContext.
**What:** Replace each `new ScriptsDbContext` with a constructor-injected repository interface.
**Expected Outcome:** No `new ScriptsDbContext` anywhere in Orchestrators source.

### Step 4.1 — Identify the interface needed

Examine each violating file to determine what the DbContext is used for (e.g., query scrobbles, save videos). Create a corresponding interface in Core if one does not already exist.

For ScrobbleSyncOrchestrator, the likely interface is:

Create file `/home/lance/Scripts/csharp/src\Core\Abstractions\IScrobbleRepository.cs`:

```csharp
namespace CSharpScripts.Core.Abstractions;

public interface IScrobbleRepository
{
    Task AddScrobblesAsync(IEnumerable<Scrobble> scrobbles, CancellationToken ct = default);
    Task<IReadOnlyList<Scrobble>> GetRecentScrobblesAsync(int count, CancellationToken ct = default);
}
```

```powershell
Test-Path "/home/lance/Scripts/csharp/src\Core\Abstractions\IScrobbleRepository.cs" | Should -Be $true
Write-Host "IScrobbleRepository created in Core"
```

### Step 4.2 — Update orchestrator to use interface via DI

```powershell
$file    = "/home/lance/Scripts/csharp/src\Orchestrators\<FileName>.cs"
$content = Get-Content $file -Raw -Encoding UTF8

# Add using for Core.Abstractions if not present
if ($content -notmatch "using CSharpScripts\.Core\.Abstractions") {
    $content = "using CSharpScripts.Core.Abstractions;`n" + $content
}

# Replace constructor to accept the repository interface
# The exact replacement depends on the orchestrator's structure
# Pattern: add repository field, inject via constructor parameter

Set-Content -Path $file -Value $content -Encoding UTF8 -ErrorAction Stop

# Verify no raw DbContext instantiation remains
$check = Get-Content $file -Raw -Encoding UTF8
$check | Should -Not -Match "new ScriptsDbContext"
Write-Host "Updated to use repository interface: $file"
```

---

## Task 5 — Build and test GREEN

**Current State:** Source changes applied.
**Reason:** Confirm compilation succeeds and all T303 sync domain tests pass.
**What:** Full restore → build → targeted test run.
**Expected Outcome:** 0 build errors, all T303 tests pass.

```powershell
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop

$buildOut = dotnet build /home/lance/Scripts/csharp/Scripts.slnx --no-restore 2>&1
$buildOut | Select-String "0 Error" | Should -Not -BeNullOrEmpty
Write-Host "Build: GREEN"

# Run the domain isolation tests
$testOut = dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "FullyQualifiedName~T303_SyncDomainTests" 2>&1
$testOut | Select-String "Failed: 0" | Should -Not -BeNullOrEmpty
Write-Host "T303 tests: GREEN"

# Also run ALL tests to catch regressions from interface extraction
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

## Task 6 — REFACTOR: Commit isolation

**Current State:** Tests green, source clean.
**Reason:** Record Orchestrators isolation as a discrete commit.
**What:** Stage all Orchestrators + test + Core abstraction changes, commit.
**Expected Outcome:** Commit `feat(t3-03)` visible in log.

```powershell
Set-Location /home/lance/Scripts -ErrorAction Stop

gitleaks detect --no-git 2>&1 | Select-String "leaks found" | ForEach-Object {
    throw "Gitleaks found secrets — abort commit"
}

git add csharp/src/Orchestrators/ `
        csharp/tests/Scripts.Tests/T3/T303_SyncDomainTests.cs 2>&1

if (Test-Path "/home/lance/Scripts/csharp/src\Core\Abstractions\IScrobbleRepository.cs") {
    git add csharp/src/Core/Abstractions/IScrobbleRepository.cs 2>&1
}

git status 2>&1 | Write-Host

git commit -m "feat(t3-03): isolate Sync/Orchestrators domain — remove CLI/Reader refs, enforce repository interfaces" `
    -ErrorAction Stop 2>&1 | Tee-Object -Variable commitOut

$commitOut | Select-String "feat\(t3-03\)" | Should -Not -BeNullOrEmpty
Write-Host "Committed: t3-03"
```

---

## Completion Criteria

| Check | Command | Expected |
|-------|---------|----------|
| Build clean | `dotnet build csharp/Scripts.slnx` | `0 Error(s)` |
| Tests pass | `dotnet test --filter T303` | `Failed: 0` |
| Full suite green | `dotnet test csharp/Scripts.slnx` | `Failed: 0` |
| No CLI ref | `grep -r "Scripts.CLI" csharp/src/Orchestrators/` | No output |
| No Reader ref | `grep -r "Scripts.Reader" csharp/src/Orchestrators/` | No output |
| No raw DbContext | `grep -r "new ScriptsDbContext" csharp/src/Orchestrators/` | No output |
| No Spectre.Cli import | `grep -r "Spectre.Console.Cli" csharp/src/Orchestrators/` | No output |
| Namespace correct | All files contain `CSharpScripts.Orchestrators` | Verified |
| Commit present | `git log --oneline -1` | `feat(t3-03)` |
