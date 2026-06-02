# T2-05: Scripts.Orchestrators Project Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create `Scripts.Orchestrators.csproj` at `csharp/src/Orchestrators/`, referencing `Scripts.Data`, `Scripts.Services.Language`, and `Scripts.Services.Music`, with Last.fm and Google APIs via CPM. Retain Google Sheets for compile parity.

**Architecture:** `Scripts.Orchestrators` is a mid-layer project that coordinates multiple services and the data layer. It depends on `Scripts.Data` (persistence), `Scripts.Services.Language` (translation/detection), and `Scripts.Services.Music` (metadata enrichment). It also depends transitively on `Scripts.Core` through `Scripts.Data`. It must not reference `Scripts.Reader` or `Scripts.CLI`. Google Sheets packages are retained for backward compatibility and compile parity during the migration phase; they will be deprecated in a future tier.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Data Access & Repository Pattern

### Current Data Access Service

**File:** `csharp/src/Services/PostgresService.cs`

The only existing data access service uses `IDbContextFactory<ScriptsDbContext>` for scope management:
- Creates new context per method via `CreateDbContextAsync()`
- Disposes via `await using`
- Uses both `ExecuteUpdateAsync` (single-entity upsert) and `SaveChangesAsync` (bulk insert)

### Recommended Repository Pattern

Orchestrators should use thin repository wrappers per domain entity, not a generic Repository\<T\> pattern. Each repository:
- Injects `IDbContextFactory<ScriptsDbContext>`
- Creates context per method
- Uses `ExecuteUpdateAsync` for upserts, `ExecuteDeleteAsync` for deletes, `SaveChangesAsync` for bulk inserts

**Recommended repositories:**
- `IScrobbleRepository` / `ScrobbleRepository` — Upsert, query by track/platform
- `ITrackRepository` / `TrackRepository` — Bulk insert, query by artist/title
- `IVideoRepository` / `VideoRepository` — Add, query by URL/channel
- `IArtistRepository` / `ArtistRepository` — Query by name, upsert metadata
- `IAlbumRepository` / `AlbumRepository` — Query by artist/title
- `IExecutionLogRepository` / `ExecutionLogRepository` — Add, query recent
- `IFailedTaskRepository` / `FailedTaskRepository` — Add, query unresolved

### Duplicate Service Cleanup

**Two `LastFmService.cs` files exist:**
1. `src/Services/Sync/LastFmService.cs` (175 lines) — **CANONICAL** — Uses async StateManager, Serilog logging
2. `src/Services/Sync/LastFm/LastFmService.cs` (165 lines) — **LEGACY** — Uses sync StateManager, Console logging

**Action:** Delete the entire `src/Services/Sync/LastFm/` subdirectory. The canonical version is in `src/Services/Sync/LastFmService.cs`.

### State Management Migration

**Current:** `csharp/src/Core/Persistence/StateManager.cs` (canonical, async-first)

**Target:** Move to `csharp/src/Data/State/StateManager.cs` (co-located with data layer)

**Steps:**
1. Create `csharp/src/Data/State/` directory
2. Move `StateManager.cs` and `ReleaseProgressCache.cs` to new location
3. Change namespace from `CSharpScripts.Core` to `CSharpScripts.Data.State`
4. Add `global using CSharpScripts.Data.State;` to `GlobalUsings.cs`
5. Delete `csharp/src/Infrastructure/StateManager.cs` (legacy duplicate)

**Usage:** StateManager provides async cache CRUD for playlist state, release enrichment state, and migrations. All callers (Orchestrators, CLI commands) use the async API: `LoadStateAsync<T>()`, `SaveStateAsync()`, `Delete()`.

### Resilience & Retry Policies

**Current:** `csharp/src/Core/Resilience.cs` (Polly v8 complete implementation)

Provides:
- Circuit breaker (50% failure ratio, 3-min window, 30-sec break)
- Rate limiter (Last.fm only, 1 permit/sec)
- Retry (10 attempts, exponential backoff, jitter)
- Timeout (per-service: 30s-120s)

**Gap:** No EF Core retry strategy configured. Both `DbContextRegistration.cs` and `ScriptsDbContextFactory.cs` must add `EnableRetryOnFailure`:

```csharp
npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
    maxRetryCount: 3,
    maxRetryDelay: TimeSpan.FromSeconds(30),
    errorCodesToAdd: null
)
```

**Recommendation:** Delete `csharp/src/Infrastructure/Resilience.cs` (legacy duplicate that lacks circuit breaker, timeout, and rate limiter).

---

## Prerequisites

- [ ] T2-01 (Scripts.Core) is signed off — `Scripts.Core.csproj` exists and compiles
- [ ] T2-02 (Scripts.Data) is signed off — `Scripts.Data.csproj` exists and compiles
- [ ] T2-03 (Scripts.Services.Language) is signed off — `Scripts.Services.Language.csproj` exists and compiles
- [ ] T2-04 (Scripts.Services.Music) is signed off — `Scripts.Services.Music.csproj` exists and compiles
- [ ] CPM is active — `Directory.Packages.props` lists `Hqub.Last.fm`, `Google.Apis.*`, `Google.Apis.Sheets.v4`
- [ ] `/home/lance/Scripts/csharp/src\Orchestrators\` directory exists (create if absent)

---

## Task 1 — Verify directory and back up any existing csproj

### Step 1 — Log current state

```powershell
Write-Host "STATE: Verifying src/Orchestrators directory and any existing Scripts.Orchestrators.csproj"
Write-Host "REASON: Must not overwrite without backup (Zero-Presumption Rule 9)"

$orchDir  = '/home/lance/Scripts/csharp/src\Orchestrators'
$orchProj = Join-Path $orchDir 'Scripts.Orchestrators.csproj'
$ts       = Get-Date -Format 'yyyyMMdd_HHmmss'

if (-not (Test-Path $orchDir)) {
    New-Item -ItemType Directory -Path $orchDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $orchDir)) { throw "Failed to create $orchDir" }
    Write-Host "OUTCOME: Created directory $orchDir"
} else {
    Write-Host "OUTCOME: Directory $orchDir already exists"
}

if (Test-Path $orchProj) {
    $bak = "$orchProj.bak.$ts"
    Copy-Item $orchProj $bak -ErrorAction Stop
    if (-not (Test-Path $bak)) { throw "Backup of Scripts.Orchestrators.csproj failed" }
    Write-Host "OUTCOME: Backed up existing Scripts.Orchestrators.csproj → $bak"
}
```

---

## Task 2 — TDD RED: Write failing tests

### Step 2 — Write tests

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\ScriptsOrchestratorsProjectTests.cs`

```csharp
using System.IO;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests;

public class ScriptsOrchestratorsProjectTests
{
    private const string OrchCsproj =
        @"/home/lance/Scripts/csharp/src\Orchestrators\Scripts.Orchestrators.csproj";

    private const string AssemblyInfoPath =
        @"/home/lance/Scripts/csharp/src\Orchestrators\Properties\AssemblyInfo.cs";

    [Test]
    public void ScriptsOrchestrators_CsprojFile_Exists()
    {
        File.Exists(OrchCsproj).Should().BeTrue(
            "Scripts.Orchestrators.csproj must exist at csharp/src/Orchestrators/");
    }

    [Test]
    public void ScriptsOrchestrators_References_DataAndServices()
    {
        File.Exists(OrchCsproj).Should().BeTrue();
        var content = File.ReadAllText(OrchCsproj);

        content.Should().Contain("Scripts.Data.csproj",
            "Scripts.Orchestrators must reference Scripts.Data");
        content.Should().Contain("Scripts.Services.Language.csproj",
            "Scripts.Orchestrators must reference Scripts.Services.Language");
        content.Should().Contain("Scripts.Services.Music.csproj",
            "Scripts.Orchestrators must reference Scripts.Services.Music");
    }

    [Test]
    public void ScriptsOrchestrators_DoesNotReference_CLI_Or_Reader()
    {
        File.Exists(OrchCsproj).Should().BeTrue();
        var content = File.ReadAllText(OrchCsproj);

        content.Should().NotContain("Scripts.CLI",
            "Scripts.Orchestrators must not reference CLI (CLI depends on Orchestrators, not the reverse)");
        content.Should().NotContain("Scripts.Reader",
            "Scripts.Orchestrators must not reference Reader (Reader is a leaf project, peer to Orchestrators)");
    }

    [Test]
    public void ScriptsOrchestrators_RetainsGoogleSheets_ForCompileParity()
    {
        File.Exists(OrchCsproj).Should().BeTrue();
        var content = File.ReadAllText(OrchCsproj);

        content.Should().Contain("Google.Apis.Sheets.v4",
            "Google.Apis.Sheets.v4 must be retained for backward-compatible compile parity during migration");
        content.Should().Contain("Google.Apis.YouTube.v3",
            "Google.Apis.YouTube.v3 package reference must be present");
        content.Should().Contain("Google.Apis",
            "Google.Apis package reference must be present");
    }

    [Test]
    public void ScriptsOrchestrators_HasNoInlineVersions()
    {
        File.Exists(OrchCsproj).Should().BeTrue();
        var content = File.ReadAllText(OrchCsproj);
        content.Should().NotMatchRegex(@"PackageReference.+Version=""",
            "Scripts.Orchestrators.csproj must not contain inline Version= (CPM violation)");
    }

    [Test]
    public void ScriptsOrchestrators_AssemblyInfo_HasInternalsVisibleTo()
    {
        File.Exists(AssemblyInfoPath).Should().BeTrue(
            "Properties/AssemblyInfo.cs must exist in Scripts.Orchestrators");
        var content = File.ReadAllText(AssemblyInfoPath);
        content.Should().Contain("InternalsVisibleTo");
        content.Should().Contain("Scripts.Tests");
    }

    [Test]
    public void ScriptsOrchestrators_CompilesIndependently()
    {
        File.Exists(OrchCsproj).Should().BeTrue();
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            Arguments              = @"build /home/lance/Scripts/csharp/src\Orchestrators\Scripts.Orchestrators.csproj",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.WaitForExit();
        var stderr = proc.StandardError.ReadToEnd();
        proc.ExitCode.Should().Be(0, $"Scripts.Orchestrators.csproj did not compile independently. stderr: {stderr}");
    }
}
```

### Step 3 — Run tests RED

```powershell
$result = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsOrchestratorsProjectTests" `
    --no-build 2>&1
Write-Host $result
# Expected: FAILED — ScriptsOrchestrators_CsprojFile_Exists and all others fail because csproj does not exist yet
```

---

## Task 3 — GREEN: Create Scripts.Orchestrators.csproj

### Step 4 — Write the project file

File: `/home/lance/Scripts/csharp/src\Orchestrators\Scripts.Orchestrators.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\Data\Scripts.Data.csproj" />
    <ProjectReference Include="..\Services\Language\Scripts.Services.Language.csproj" />
    <ProjectReference Include="..\Services\Music\Scripts.Services.Music.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Hqub.Last.fm" />
    <PackageReference Include="Google.Apis" />
    <PackageReference Include="Google.Apis.YouTube.v3" />
    <PackageReference Include="Google.Apis.Sheets.v4" />
    <PackageReference Include="Google.Apis.Drive.v3" />
  </ItemGroup>
</Project>
```

### Step 5 — Verify the project file

```powershell
$orchProj = '/home/lance/Scripts/csharp/src\Orchestrators\Scripts.Orchestrators.csproj'
if (-not (Test-Path $orchProj)) { throw "Scripts.Orchestrators.csproj was not created" }

$content = Get-Content $orchProj -Raw -Encoding UTF8

if ($content -notmatch 'Scripts\.Data\.csproj') {
    throw "Scripts.Orchestrators.csproj must reference Scripts.Data.csproj"
}
if ($content -notmatch 'Scripts\.Services\.Language\.csproj') {
    throw "Scripts.Orchestrators.csproj must reference Scripts.Services.Language.csproj"
}
if ($content -notmatch 'Scripts\.Services\.Music\.csproj') {
    throw "Scripts.Orchestrators.csproj must reference Scripts.Services.Music.csproj"
}
if ($content -match 'Scripts\.CLI') {
    throw "Scripts.Orchestrators.csproj must not reference CLI"
}
if ($content -match 'Scripts\.Reader') {
    throw "Scripts.Orchestrators.csproj must not reference Reader"
}
if ($content -match 'PackageReference.+Version="') {
    throw "Scripts.Orchestrators.csproj must not contain inline Version= (CPM violation)"
}
Write-Host "OUTCOME: Scripts.Orchestrators.csproj verified OK"
```

---

## Task 4 — GREEN: Create Properties/AssemblyInfo.cs

### Step 6 — Create AssemblyInfo.cs

```powershell
$propsDir = '/home/lance/Scripts/csharp/src\Orchestrators\Properties'
if (-not (Test-Path $propsDir)) {
    New-Item -ItemType Directory -Path $propsDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $propsDir)) { throw "Failed to create $propsDir" }
    Write-Host "OUTCOME: Created Properties directory"
}
```

File: `/home/lance/Scripts/csharp/src\Orchestrators\Properties\AssemblyInfo.cs`

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Scripts.Tests")]
```

```powershell
$infoPath = '/home/lance/Scripts/csharp/src\Orchestrators\Properties\AssemblyInfo.cs'
if (-not (Test-Path $infoPath)) { throw "AssemblyInfo.cs was not created in Scripts.Orchestrators" }

$content = Get-Content $infoPath -Raw -Encoding UTF8
if ($content -notmatch 'InternalsVisibleTo') { throw "InternalsVisibleTo missing from AssemblyInfo.cs" }
if ($content -notmatch 'Scripts\.Tests')    { throw "Scripts.Tests not listed in InternalsVisibleTo" }
Write-Host "OUTCOME: AssemblyInfo.cs verified OK"
```

---

## Task 5 — GREEN: Register Scripts.Orchestrators in Scripts.slnx

### Step 7 — Add to solution

```powershell
Write-Host "STATE: Adding Scripts.Orchestrators.csproj to Scripts.slnx"

$slnx = '/home/lance/Scripts/csharp/Scripts.slnx'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$slnx.bak.$ts"
Copy-Item $slnx $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of Scripts.slnx failed" }

dotnet sln '/home/lance/Scripts/csharp/Scripts.slnx' `
    add '/home/lance/Scripts/csharp/src\Orchestrators\Scripts.Orchestrators.csproj' `
    2>&1 | Tee-Object -Variable slnOutput
Write-Host $slnOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet sln add failed for Scripts.Orchestrators.csproj" }

$slnContent = Get-Content $slnx -Raw -Encoding UTF8
if ($slnContent -notmatch 'Scripts\.Orchestrators\.csproj') {
    throw "Scripts.Orchestrators.csproj not found in Scripts.slnx after dotnet sln add"
}
Write-Host "OUTCOME: Scripts.Orchestrators.csproj registered in solution"
```

---

## Task 6 — GREEN: Build Scripts.Orchestrators

### Step 8 — Restore and build

```powershell
Write-Host "STATE: Running dotnet restore and dotnet build for Scripts.Orchestrators"

$restoreOutput = dotnet restore '/home/lance/Scripts/csharp/src\Orchestrators\Scripts.Orchestrators.csproj' 2>&1
Write-Host $restoreOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed for Scripts.Orchestrators" }

$buildOutput = dotnet build '/home/lance/Scripts/csharp/src\Orchestrators\Scripts.Orchestrators.csproj' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for Scripts.Orchestrators" }

# Expected:
# Build succeeded.
# 0 Error(s)
```

---

## Task 7 — REFACTOR: Run all tests GREEN

### Step 9 — Run project tests

```powershell
$testOutput = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsOrchestratorsProjectTests" 2>&1
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) { throw "ScriptsOrchestratorsProjectTests failed" }
# Expected: All 7 tests passed
```

---

## Task 8 — Commit

```powershell
git -C '/home/lance/Scripts' add `
    'csharp/src/Orchestrators/Scripts.Orchestrators.csproj' `
    'csharp/src/Orchestrators/Properties/AssemblyInfo.cs' `
    'csharp/tests/Scripts.Tests/ScriptsOrchestratorsProjectTests.cs' `
    'csharp/Scripts.slnx'

git -C '/home/lance/Scripts' commit `
    -m "feat(t2-05): add Scripts.Orchestrators.csproj referencing Data + Language + Music, retain Google Sheets via CPM"
```

---

## Sign-off Criteria

- [ ] `csharp/src/Orchestrators/Scripts.Orchestrators.csproj` exists
- [ ] References `Scripts.Data.csproj`, `Scripts.Services.Language.csproj`, and `Scripts.Services.Music.csproj`
- [ ] Does NOT reference `Scripts.CLI` or `Scripts.Reader`
- [ ] Contains `Google.Apis.Sheets.v4` PackageReference (retained for compile parity)
- [ ] Zero inline `Version=` attributes (CPM compliant)
- [ ] `csharp/src/Orchestrators/Properties/AssemblyInfo.cs` has `[assembly: InternalsVisibleTo("Scripts.Tests")]`
- [ ] `Scripts.slnx` references `Scripts.Orchestrators.csproj`
- [ ] `dotnet build csharp/src/Orchestrators/Scripts.Orchestrators.csproj` exits 0
- [ ] `ScriptsOrchestratorsProjectTests` — all 7 tests GREEN
