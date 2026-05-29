# T2-02: Scripts.Data Project Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create `Scripts.Data.csproj` at `csharp/src/Data/`, referencing only `Scripts.Core`, and confirm it compiles with EF Core 10 and Npgsql 10 packages correctly declared via CPM.

**Architecture:** `Scripts.Data` is the persistence layer — it contains EF Core `DbContext`, entity classes, migrations, and state management helpers. It depends on `Scripts.Core` for logging and utility types. It must not reference `Services`, `Orchestrators`, `Reader`, or `CLI` to prevent circular dependencies. The `Microsoft.EntityFrameworkCore.Design` and `.Tools` packages are marked `PrivateAssets=all` so they don't leak to consuming projects.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Entity Design & Configuration Context

### Entity Inventory

The `Scripts.Data` project contains 9 entities mapped to PostgreSQL 18 via EF Core 10:

| Entity | PK Type | Table | Status |
|--------|---------|-------|--------|
| `Artist` | `int` identity | `artists` | Remove obsolete `Mbid` property |
| `Album` | `int` identity | `albums` | Remove obsolete `Mbid` property |
| `Track` | `int` identity | `tracks` | Remove obsolete `Mbid` property |
| `Scrobble` | `long` identity | `scrobbles` | Clean — no changes |
| `Video` | `long` identity | `videos` | Has JSONB `Metadata` column |
| `ExecutionLog` | `int` serial | `execution_logs` | Has JSONB `Payload` column, `timestamptz` with `CURRENT_TIMESTAMP` |
| `FailedTask` | `int` serial | `failed_tasks` | Has `timestamptz` with `CURRENT_TIMESTAMP` |
| `FiberyEntity` | `Guid` client-gen | `fibery_entities` | Has JSONB `RawData` column |
| `SourceRecord` | `Guid` client-gen | `source_records` | **UNMAPPED** — must create configuration |

### JSONB Column Mapping (EF Core 10 + Npgsql 10)

Four entities use `JsonDocument` or `Dictionary<string, string>` properties mapped to PostgreSQL `jsonb`:

| Entity | Property | Type | Column |
|--------|----------|------|--------|
| `Artist` | `Metadata` | `JsonDocument?` | `jsonb` |
| `Video` | `Metadata` | `Dictionary<string,string>` | `jsonb` |
| `ExecutionLog` | `Payload` | `JsonDocument?` | `jsonb` |
| `FiberyEntity` | `RawData` | `JsonDocument?` | `jsonb` |

**Critical:** Do NOT declare `mb.Ignore<System.Text.Json.JsonDocument>()` in `OnModelCreating`. Npgsql 10 natively handles `JsonDocument` mapping. Ignoring it causes `NullReferenceException` during context initialization when a property is explicitly typed as `JsonDocument`.

### DbContext Configuration

**File:** `csharp/src/Data/ScriptsDbContext.cs`

The `ScriptsDbContext` must:
1. Set `ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking` in constructor (already done)
2. Call `mb.ApplyConfigurationsFromAssembly()` in `OnModelCreating` (already done)
3. Add `DbSet<SourceRecord>` for the unmapped entity
4. Add PostgreSQL extensions: `HasPostgresExtension("unaccent")` and `HasPostgresExtension("pg_trgm")`

### Configuration Files (8 existing + 1 to create)

All configurations implement `IEntityTypeConfiguration<T>` and live in `csharp/src/Data/Configuration/`:

**Existing configurations:**
- `ArtistConfiguration.cs` — Unique index on `Name`, JSONB `Metadata`
- `AlbumConfiguration.cs` — FK to Artist, unique composite index on `(ArtistId, Title)`
- `TrackConfiguration.cs` — FK to Artist + Album, index on Title
- `ScrobbleConfiguration.cs` — FK to Track, unique on `(TrackId, ScrobbledAt)`, `timestamptz`
- `VideoConfiguration.cs` — Unique index on `Url`, JSONB `Metadata`
- `ExecutionLogConfiguration.cs` — `timestamptz` with `CURRENT_TIMESTAMP` default, JSONB `Payload`
- `FiberyEntityConfiguration.cs` — JSONB `RawData`
- `FailedTaskConfiguration.cs` — `timestamptz` with `CURRENT_TIMESTAMP` default

**To create:**
- `SourceRecordConfiguration.cs` — Map `SourceRecord` to `source_records` table with composite unique index on `(SourceId, EntityType)`

### DbContext Registration & Retry Policy

**File:** `csharp/src/Data/DbContextRegistration.cs`

Must add `EnableRetryOnFailure` to Npgsql options:

```csharp
services.AddDbContext<ScriptsDbContext>(opts => opts.UseNpgsql(connectionString: connStr,
    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 3,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorCodesToAdd: null
    )));
```

**File:** `csharp/src/Data/ScriptsDbContextFactory.cs`

Design-time factory must also include `EnableRetryOnFailure` for `dotnet ef` commands.

### Compiled Models

EF Core 10 supports compiled models for startup performance. When enabled:
1. Add to `Directory.Build.props`: `<EFOptimizeContext>true</EFOptimizeContext>` and `<EFScaffoldModelStage>build</EFScaffoldModelStage>`
2. Add to `.csproj`: `<PackageReference Include="Microsoft.EntityFrameworkCore.Tasks" />`
3. Run: `dotnet ef dbcontext optimize --project csharp/src/Data/Scripts.Data.csproj --output-dir CompiledModels`
4. EF9+ auto-detects compiled models — no `.UseModel()` call needed

**Note:** When `OnModelCreating` changes (e.g., removing `Ignore` statements), the compiled model must be regenerated and a new migration added.

---

## Prerequisites

- [ ] T2-01 (Scripts.Core) is signed off — `Scripts.Core.csproj` exists and compiles
- [ ] CPM is active — `Directory.Packages.props` lists `Microsoft.EntityFrameworkCore`, `Npgsql`, etc.
- [ ] `C:\Users\Lance\Dev\Scripts\csharp\src\Data\` directory exists (create if absent)

---

## Task 1 — Verify directory and back up any existing csproj

### Step 1 — Log current state

```powershell
Write-Host "STATE: Verifying src/Data directory and any existing Scripts.Data.csproj"
Write-Host "REASON: Must not overwrite without backup (Zero-Presumption Rule 9)"

$dataDir  = 'C:\Users\Lance\Dev\Scripts\csharp\src\Data'
$dataProj = Join-Path $dataDir 'Scripts.Data.csproj'
$ts       = Get-Date -Format 'yyyyMMdd_HHmmss'

if (-not (Test-Path $dataDir)) {
    New-Item -ItemType Directory -Path $dataDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $dataDir)) { throw "Failed to create $dataDir" }
    Write-Host "OUTCOME: Created directory $dataDir"
} else {
    Write-Host "OUTCOME: Directory $dataDir already exists"
}

if (Test-Path $dataProj) {
    $bak = "$dataProj.bak.$ts"
    Copy-Item $dataProj $bak -ErrorAction Stop
    if (-not (Test-Path $bak)) { throw "Backup of Scripts.Data.csproj failed" }
    Write-Host "OUTCOME: Backed up existing Scripts.Data.csproj → $bak"
}
```

---

## Task 2 — TDD RED: Write failing tests

### Step 2 — Write tests

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ScriptsDataProjectTests.cs`

```csharp
using System.IO;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests;

public class ScriptsDataProjectTests
{
    private const string DataCsproj =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\Data\Scripts.Data.csproj";

    private const string AssemblyInfoPath =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\Data\Properties\AssemblyInfo.cs";

    [Test]
    public void ScriptsData_CsprojFile_Exists()
    {
        File.Exists(DataCsproj).Should().BeTrue(
            "Scripts.Data.csproj must exist at csharp/src/Data/");
    }

    [Test]
    public void ScriptsData_References_ScriptsCore()
    {
        File.Exists(DataCsproj).Should().BeTrue();
        var content = File.ReadAllText(DataCsproj);
        content.Should().Contain("Scripts.Core.csproj",
            "Scripts.Data must reference Scripts.Core");
    }

    [Test]
    public void ScriptsData_DoesNotReference_ServicesOrOrchestrators()
    {
        File.Exists(DataCsproj).Should().BeTrue();
        var content = File.ReadAllText(DataCsproj);
        content.Should().NotContain("Scripts.Services",
            "Scripts.Data must not reference any Services project (would create circular dependency)");
        content.Should().NotContain("Scripts.Orchestrators",
            "Scripts.Data must not reference Scripts.Orchestrators");
    }

    [Test]
    public void ScriptsData_DoesNotReference_CLI_Or_Reader()
    {
        File.Exists(DataCsproj).Should().BeTrue();
        var content = File.ReadAllText(DataCsproj);
        content.Should().NotContain("Scripts.CLI",
            "Scripts.Data must not reference CLI");
        content.Should().NotContain("Scripts.Reader",
            "Scripts.Data must not reference Reader");
    }

    [Test]
    public void ScriptsData_DesignAndTools_HavePrivateAssets()
    {
        File.Exists(DataCsproj).Should().BeTrue();
        var content = File.ReadAllText(DataCsproj);
        // EF Design and Tools must not leak to consuming assemblies
        content.Should().Contain("Microsoft.EntityFrameworkCore.Design",
            "EF Design package must be listed");
        content.Should().Contain("PrivateAssets>all</PrivateAssets",
            "EF Design and Tools must have PrivateAssets=all to prevent transitive leakage");
    }

    [Test]
    public void ScriptsData_HasNoInlineVersions()
    {
        File.Exists(DataCsproj).Should().BeTrue();
        var content = File.ReadAllText(DataCsproj);
        content.Should().NotMatchRegex(@"PackageReference.+Version=""",
            "Scripts.Data.csproj must not contain inline Version= (CPM violation)");
    }

    [Test]
    public void ScriptsData_AssemblyInfo_HasInternalsVisibleTo()
    {
        File.Exists(AssemblyInfoPath).Should().BeTrue(
            "Properties/AssemblyInfo.cs must exist in Scripts.Data");
        var content = File.ReadAllText(AssemblyInfoPath);
        content.Should().Contain("InternalsVisibleTo");
        content.Should().Contain("Scripts.Tests");
    }

    [Test]
    public void ScriptsData_CompilesIndependently()
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            Arguments              = @"build C:\Users\Lance\Dev\Scripts\csharp\src\Data\Scripts.Data.csproj",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.WaitForExit();
        var stderr = proc.StandardError.ReadToEnd();
        proc.ExitCode.Should().Be(0, $"Scripts.Data.csproj did not compile. stderr: {stderr}");
    }
}
```

### Step 3 — Run tests RED

```powershell
$result = dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsDataProjectTests" `
    --no-build 2>&1
Write-Host $result
# Expected: FAILED — ScriptsData_CsprojFile_Exists and all others
```

---

## Task 3 — GREEN: Create Scripts.Data.csproj

### Step 4 — Write the project file

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Scripts.Data.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\Core\Scripts.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Npgsql" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="CsvHelper" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>
</Project>
```

### Step 5 — Verify the project file

```powershell
$dataProj = 'C:\Users\Lance\Dev\Scripts\csharp\src\Data\Scripts.Data.csproj'
if (-not (Test-Path $dataProj)) { throw "Scripts.Data.csproj was not created" }

$content = Get-Content $dataProj -Raw -Encoding UTF8

if ($content -notmatch 'Scripts\.Core\.csproj') {
    throw "Scripts.Data.csproj must reference Scripts.Core.csproj"
}
if ($content -match 'Scripts\.Services') {
    throw "Scripts.Data.csproj must not reference Services"
}
if ($content -match 'Scripts\.Orchestrators') {
    throw "Scripts.Data.csproj must not reference Orchestrators"
}
if ($content -match 'PackageReference.+Version="') {
    throw "Scripts.Data.csproj must not contain inline Version= attributes (CPM violation)"
}
Write-Host "OUTCOME: Scripts.Data.csproj verified OK"
```

---

## Task 4 — GREEN: Create Properties/AssemblyInfo.cs

### Step 6 — Create AssemblyInfo.cs

```powershell
$propsDir = 'C:\Users\Lance\Dev\Scripts\csharp\src\Data\Properties'
if (-not (Test-Path $propsDir)) {
    New-Item -ItemType Directory -Path $propsDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $propsDir)) { throw "Failed to create $propsDir" }
    Write-Host "OUTCOME: Created Properties directory"
}
```

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Properties\AssemblyInfo.cs`

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Scripts.Tests")]
```

```powershell
$infoPath = 'C:\Users\Lance\Dev\Scripts\csharp\src\Data\Properties\AssemblyInfo.cs'
if (-not (Test-Path $infoPath)) { throw "AssemblyInfo.cs was not created in Scripts.Data" }

$content = Get-Content $infoPath -Raw -Encoding UTF8
if ($content -notmatch 'InternalsVisibleTo') { throw "InternalsVisibleTo missing" }
if ($content -notmatch 'Scripts\.Tests')     { throw "Scripts.Tests not listed in InternalsVisibleTo" }
Write-Host "OUTCOME: Scripts.Data AssemblyInfo.cs verified OK"
```

---

## Task 5 — GREEN: Register Scripts.Data in Scripts.slnx

### Step 7 — Add to solution

```powershell
Write-Host "STATE: Adding Scripts.Data.csproj to Scripts.slnx"

$slnx = 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$slnx.bak.$ts"
Copy-Item $slnx $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of Scripts.slnx failed" }

dotnet sln 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' `
    add 'C:\Users\Lance\Dev\Scripts\csharp\src\Data\Scripts.Data.csproj' `
    2>&1 | Tee-Object -Variable slnOutput
Write-Host $slnOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet sln add failed for Scripts.Data.csproj" }

$slnContent = Get-Content $slnx -Raw -Encoding UTF8
if ($slnContent -notmatch 'Scripts\.Data\.csproj') {
    throw "Scripts.Data.csproj not found in Scripts.slnx after dotnet sln add"
}
Write-Host "OUTCOME: Scripts.Data.csproj registered in solution"
```

---

## Task 6 — GREEN: Build Scripts.Data

### Step 8 — Restore and build

```powershell
Write-Host "STATE: Running dotnet restore and dotnet build for Scripts.Data"

$restoreOutput = dotnet restore 'C:\Users\Lance\Dev\Scripts\csharp\src\Data\Scripts.Data.csproj' 2>&1
Write-Host $restoreOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed for Scripts.Data" }

$buildOutput = dotnet build 'C:\Users\Lance\Dev\Scripts\csharp\src\Data\Scripts.Data.csproj' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for Scripts.Data" }

# Expected:
# Build succeeded.
# 0 Error(s)
```

---

## Task 7 — REFACTOR: Run all tests GREEN

### Step 9 — Run project tests

```powershell
$testOutput = dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsDataProjectTests" 2>&1
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) { throw "ScriptsDataProjectTests failed" }
# Expected: All 8 tests passed
```

---

## Task 8 — Commit

```powershell
git -C 'C:\Users\Lance\Dev\Scripts' add `
    'csharp/src/Data/Scripts.Data.csproj' `
    'csharp/src/Data/Properties/AssemblyInfo.cs' `
    'csharp/tests/Scripts.Tests/ScriptsDataProjectTests.cs' `
    'csharp/Scripts.slnx'

git -C 'C:\Users\Lance\Dev\Scripts' commit `
    -m "feat(t2-02): add Scripts.Data.csproj referencing Core only, EF10 + Npgsql10 via CPM"
```

---

## Sign-off Criteria

- [ ] `csharp/src/Data/Scripts.Data.csproj` exists
- [ ] References `Scripts.Core.csproj` and nothing else in `<ProjectReference>`
- [ ] `Microsoft.EntityFrameworkCore.Design` and `.Tools` have `PrivateAssets=all`
- [ ] Zero inline `Version=` attributes
- [ ] `csharp/src/Data/Properties/AssemblyInfo.cs` has `[assembly: InternalsVisibleTo("Scripts.Tests")]`
- [ ] `Scripts.slnx` references `Scripts.Data.csproj`
- [ ] `dotnet build csharp/src/Data/Scripts.Data.csproj` exits 0
- [ ] `ScriptsDataProjectTests` — all 8 tests GREEN
