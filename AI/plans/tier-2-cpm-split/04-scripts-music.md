# T2-04: Scripts.Services.Music Project Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create `Scripts.Services.Music.csproj` at `csharp/src/Services/Music/`, referencing only `Scripts.Core`, with MusicBrainz, Discogs, and Mapperly packages via CPM.

**Architecture:** `Scripts.Services.Music` is a leaf service project — it depends only on `Scripts.Core`. It provides music metadata enrichment (MusicBrainz, Discogs) and object mapping (Mapperly). It must not reference `Scripts.Data`, `Scripts.Services.Language`, or any downstream project. Peer services (Language and Music) never reference each other; they are composed by `Orchestrators` and `CLI`.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Music Service Architecture

### Peer Service Isolation

`Scripts.Services.Music` and `Scripts.Services.Language` are **peer services** — they must NOT reference each other. Both depend only on `Scripts.Core`. They are composed together by higher-level projects (`Orchestrators`, `CLI`) that depend on both.

**Dependency Graph:**
```
Scripts.Core (innermost)
  ↑
  ├─ Scripts.Services.Language (peer)
  └─ Scripts.Services.Music (peer)
       ↑
       └─ Scripts.Orchestrators (composes both services)
            ↑
            └─ Scripts.CLI (entry point)
```

### MusicBrainz Integration

**Package:** `MetaBrainz.MusicBrainz` v6.2.0

Provides REST client for MusicBrainz API:
- Artist lookup by name
- Album/release metadata enrichment
- Recording details (duration, composers, performers)
- Automatic rate limiting (1 request/sec per MusicBrainz policy)

**Usage Pattern:**
```csharp
var client = new MusicBrainzClient();
var artist = await client.Artists.GetByNameAsync(artistName);
var releases = await client.Releases.GetByArtistAsync(artistId);
```

### Discogs Integration

**Package:** `ParkSquare.Discogs` v3.0.0

Provides REST client for Discogs API:
- Artist/label lookup
- Release/master release metadata
- Vinyl/format details
- Requires API token (free tier available)

**Usage Pattern:**
```csharp
var client = new DiscogsClient(userAgent, token);
var artist = await client.SearchArtistAsync(artistName);
var release = await client.GetReleaseAsync(releaseId);
```

### Object Mapping: Mapperly

**Package:** `Riok.Mapperly` v3.7.0

Source generator for compile-time object mapping (no reflection overhead):
- Maps MusicBrainz DTOs → domain entities
- Maps Discogs DTOs → domain entities
- Zero runtime cost — code generated at compile time

**Usage Pattern:**
```csharp
[Mapper]
public partial class MusicBrainzMapper
{
    public partial Artist MapArtist(MusicBrainzArtist source);
    public partial Album MapRelease(MusicBrainzRelease source);
}
```

### Data Access Pattern

Music service does NOT directly access the database. It returns enriched DTOs to `Orchestrators`, which handle persistence via `Scripts.Data` repositories. This maintains clean separation of concerns:
- **Music Service:** Enrichment logic only
- **Data Layer:** Persistence only
- **Orchestrators:** Composition and coordination

---

## Prerequisites

- [ ] T2-01 (Scripts.Core) is signed off — `Scripts.Core.csproj` exists and compiles
- [ ] CPM is active — `Directory.Packages.props` lists `MetaBrainz.MusicBrainz`, `ParkSquare.Discogs`, `Riok.Mapperly`
- [ ] `/home/lance/Scripts/csharp/src\Services\Music\` directory exists (create if absent)

---

## Task 1 — Verify directory and back up any existing csproj

### Step 1 — Log current state

```powershell
Write-Host "STATE: Verifying src/Services/Music directory and any existing Scripts.Services.Music.csproj"
Write-Host "REASON: Must not overwrite without backup (Zero-Presumption Rule 9)"

$musicDir  = '/home/lance/Scripts/csharp/src\Services\Music'
$musicProj = Join-Path $musicDir 'Scripts.Services.Music.csproj'
$ts        = Get-Date -Format 'yyyyMMdd_HHmmss'

if (-not (Test-Path $musicDir)) {
    New-Item -ItemType Directory -Path $musicDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $musicDir)) { throw "Failed to create $musicDir" }
    Write-Host "OUTCOME: Created directory $musicDir"
} else {
    Write-Host "OUTCOME: Directory $musicDir already exists"
}

if (Test-Path $musicProj) {
    $bak = "$musicProj.bak.$ts"
    Copy-Item $musicProj $bak -ErrorAction Stop
    if (-not (Test-Path $bak)) { throw "Backup of Scripts.Services.Music.csproj failed" }
    Write-Host "OUTCOME: Backed up existing Scripts.Services.Music.csproj → $bak"
}
```

---

## Task 2 — TDD RED: Write failing tests

### Step 2 — Write tests

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\ScriptsMusicProjectTests.cs`

```csharp
using System.IO;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests;

public class ScriptsMusicProjectTests
{
    private const string MusicCsproj =
        @"/home/lance/Scripts/csharp/src\Services\Music\Scripts.Services.Music.csproj";

    private const string AssemblyInfoPath =
        @"/home/lance/Scripts/csharp/src\Services\Music\Properties\AssemblyInfo.cs";

    [Test]
    public void ScriptsMusic_CsprojFile_Exists()
    {
        File.Exists(MusicCsproj).Should().BeTrue(
            "Scripts.Services.Music.csproj must exist at csharp/src/Services/Music/");
    }

    [Test]
    public void ScriptsMusic_DoesNotReference_Language()
    {
        File.Exists(MusicCsproj).Should().BeTrue();
        var content = File.ReadAllText(MusicCsproj);
        content.Should().NotContain("Scripts.Services.Language",
            "Scripts.Services.Music must not reference Language (peer services must not depend on each other)");
    }

    [Test]
    public void ScriptsMusic_References_OnlyCore()
    {
        File.Exists(MusicCsproj).Should().BeTrue();
        var content = File.ReadAllText(MusicCsproj);

        content.Should().Contain("Scripts.Core.csproj",
            "Scripts.Services.Music must reference Scripts.Core");

        content.Should().NotContain("Scripts.Data",
            "Scripts.Services.Music must not reference Data");
        content.Should().NotContain("Scripts.Orchestrators",
            "Scripts.Services.Music must not reference Orchestrators");
        content.Should().NotContain("Scripts.CLI",
            "Scripts.Services.Music must not reference CLI");
        content.Should().NotContain("Scripts.Reader",
            "Scripts.Services.Music must not reference Reader");
    }

    [Test]
    public void ScriptsMusic_HasNoInlineVersions()
    {
        File.Exists(MusicCsproj).Should().BeTrue();
        var content = File.ReadAllText(MusicCsproj);
        content.Should().NotMatchRegex(@"PackageReference.+Version=""",
            "Scripts.Services.Music.csproj must not contain inline Version= (CPM violation)");
    }

    [Test]
    public void ScriptsMusic_AssemblyInfo_HasInternalsVisibleTo()
    {
        File.Exists(AssemblyInfoPath).Should().BeTrue(
            "Properties/AssemblyInfo.cs must exist in Scripts.Services.Music");
        var content = File.ReadAllText(AssemblyInfoPath);
        content.Should().Contain("InternalsVisibleTo");
        content.Should().Contain("Scripts.Tests");
    }

    [Test]
    public void ScriptsMusic_CompilesIndependently()
    {
        File.Exists(MusicCsproj).Should().BeTrue();
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            Arguments              = @"build /home/lance/Scripts/csharp/src\Services\Music\Scripts.Services.Music.csproj",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.WaitForExit();
        var stderr = proc.StandardError.ReadToEnd();
        proc.ExitCode.Should().Be(0, $"Scripts.Services.Music.csproj did not compile independently. stderr: {stderr}");
    }
}
```

### Step 3 — Run tests RED

```powershell
$result = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsMusicProjectTests" `
    --no-build 2>&1
Write-Host $result
# Expected: FAILED — ScriptsMusic_CsprojFile_Exists and all others fail because csproj does not exist yet
```

---

## Task 3 — GREEN: Create Scripts.Services.Music.csproj

### Step 4 — Write the project file

File: `/home/lance/Scripts/csharp/src\Services\Music\Scripts.Services.Music.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\..\Core\Scripts.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MetaBrainz.MusicBrainz" />
    <PackageReference Include="ParkSquare.Discogs" />
    <PackageReference Include="Riok.Mapperly" />
  </ItemGroup>
</Project>
```

### Step 5 — Verify the project file

```powershell
$musicProj = '/home/lance/Scripts/csharp/src\Services\Music\Scripts.Services.Music.csproj'
if (-not (Test-Path $musicProj)) { throw "Scripts.Services.Music.csproj was not created" }

$content = Get-Content $musicProj -Raw -Encoding UTF8

if ($content -notmatch 'Scripts\.Core\.csproj') {
    throw "Scripts.Services.Music.csproj must reference Scripts.Core.csproj"
}
if ($content -match 'Scripts\.Services\.Language') {
    throw "Scripts.Services.Music.csproj must not reference Language (peer service)"
}
if ($content -match 'Scripts\.Data') {
    throw "Scripts.Services.Music.csproj must not reference Data"
}
if ($content -match 'PackageReference.+Version="') {
    throw "Scripts.Services.Music.csproj must not contain inline Version= (CPM violation)"
}
Write-Host "OUTCOME: Scripts.Services.Music.csproj verified OK"
```

---

## Task 4 — GREEN: Create Properties/AssemblyInfo.cs

### Step 6 — Create AssemblyInfo.cs

```powershell
$propsDir = '/home/lance/Scripts/csharp/src\Services\Music\Properties'
if (-not (Test-Path $propsDir)) {
    New-Item -ItemType Directory -Path $propsDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $propsDir)) { throw "Failed to create $propsDir" }
    Write-Host "OUTCOME: Created Properties directory"
}
```

File: `/home/lance/Scripts/csharp/src\Services\Music\Properties\AssemblyInfo.cs`

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Scripts.Tests")]
```

```powershell
$infoPath = '/home/lance/Scripts/csharp/src\Services\Music\Properties\AssemblyInfo.cs'
if (-not (Test-Path $infoPath)) { throw "AssemblyInfo.cs was not created in Scripts.Services.Music" }

$content = Get-Content $infoPath -Raw -Encoding UTF8
if ($content -notmatch 'InternalsVisibleTo') { throw "InternalsVisibleTo missing from AssemblyInfo.cs" }
if ($content -notmatch 'Scripts\.Tests')    { throw "Scripts.Tests not listed in InternalsVisibleTo" }
Write-Host "OUTCOME: AssemblyInfo.cs verified OK"
```

---

## Task 5 — GREEN: Register Scripts.Services.Music in Scripts.slnx

### Step 7 — Add to solution

```powershell
Write-Host "STATE: Adding Scripts.Services.Music.csproj to Scripts.slnx"

$slnx = '/home/lance/Scripts/csharp/Scripts.slnx'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$slnx.bak.$ts"
Copy-Item $slnx $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of Scripts.slnx failed" }

dotnet sln '/home/lance/Scripts/csharp/Scripts.slnx' `
    add '/home/lance/Scripts/csharp/src\Services\Music\Scripts.Services.Music.csproj' `
    2>&1 | Tee-Object -Variable slnOutput
Write-Host $slnOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet sln add failed for Scripts.Services.Music.csproj" }

$slnContent = Get-Content $slnx -Raw -Encoding UTF8
if ($slnContent -notmatch 'Scripts\.Services\.Music\.csproj') {
    throw "Scripts.Services.Music.csproj not found in Scripts.slnx after dotnet sln add"
}
Write-Host "OUTCOME: Scripts.Services.Music.csproj registered in solution"
```

---

## Task 6 — GREEN: Build Scripts.Services.Music

### Step 8 — Restore and build

```powershell
Write-Host "STATE: Running dotnet restore and dotnet build for Scripts.Services.Music"

$restoreOutput = dotnet restore '/home/lance/Scripts/csharp/src\Services\Music\Scripts.Services.Music.csproj' 2>&1
Write-Host $restoreOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed for Scripts.Services.Music" }

$buildOutput = dotnet build '/home/lance/Scripts/csharp/src\Services\Music\Scripts.Services.Music.csproj' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for Scripts.Services.Music" }

# Expected:
# Build succeeded.
# 0 Error(s)
```

---

## Task 7 — REFACTOR: Run all tests GREEN

### Step 9 — Run project tests

```powershell
$testOutput = dotnet test '/home/lance/Scripts/csharp/Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsMusicProjectTests" 2>&1
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) { throw "ScriptsMusicProjectTests failed" }
# Expected: All 6 tests passed
```

---

## Task 8 — Commit

```powershell
git -C '/home/lance/Scripts' add `
    'csharp/src/Services/Music/Scripts.Services.Music.csproj' `
    'csharp/src/Services/Music/Properties/AssemblyInfo.cs' `
    'csharp/tests/Scripts.Tests/ScriptsMusicProjectTests.cs' `
    'csharp/Scripts.slnx'

git -C '/home/lance/Scripts' commit `
    -m "feat(t2-04): add Scripts.Services.Music.csproj referencing Core only, MusicBrainz + Discogs + Mapperly via CPM"
```

---

## Sign-off Criteria

- [ ] `csharp/src/Services/Music/Scripts.Services.Music.csproj` exists
- [ ] References `Scripts.Core.csproj` and nothing else in `<ProjectReference>`
- [ ] Does NOT reference `Scripts.Services.Language`, `Scripts.Data`, `Scripts.Orchestrators`, `Scripts.CLI`, or `Scripts.Reader`
- [ ] Zero inline `Version=` attributes (CPM compliant)
- [ ] `csharp/src/Services/Music/Properties/AssemblyInfo.cs` has `[assembly: InternalsVisibleTo("Scripts.Tests")]`
- [ ] `Scripts.slnx` references `Scripts.Services.Music.csproj`
- [ ] `dotnet build csharp/src/Services/Music/Scripts.Services.Music.csproj` exits 0
- [ ] `ScriptsMusicProjectTests` — all 6 tests GREEN
