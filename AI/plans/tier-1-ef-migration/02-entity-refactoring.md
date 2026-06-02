# T1-02: Entity Refactoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove the three obsolete `Mbid` properties from Artist, Album, and Track entities. These properties have zero external references and are dead code.

**Architecture:** Reflection-based TUnit tests assert that each `Mbid` property does NOT exist on its respective entity. After confirming RED (property still exists), the property is removed from the source file. After GREEN (property gone), commit. Three independent tasks, one per entity.

**Key Findings from Research:**
- Artist, Album, and Track each have an obsolete `Mbid` property (MusicBrainz ID)
- Zero external references found across entire codebase — full grep for `.Mbid`, `"Mbid"`, and `nameof.*Mbid` returned only the 3 entity property declarations
- No service, orchestrator, CLI, configuration, or test code references these properties
- No migrations exist yet (first migration is T1-05), so removal has zero migration impact
- Removal is safe and unblocks cleaner entity design
- Track.Metadata audit: Track.cs has NO Metadata property (already clean) — no action needed
- Video and FailedTask entities diverge from plan spec but are intentional — flag for clarification only, do not change in T1-02

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- Phases 00 (environment) and 01 (entities) completed — all 9 entities exist at `csharp/src/Data/Entities/`
- `dotnet build` currently succeeds (entities compile with Mbid properties)
- No test project directory exists yet at `csharp/tests/Scripts.Tests/` — tests will be written into this location

---

## File Map

| File | Path | Action |
|------|------|--------|
| `Artist.cs` | `csharp/src/Data/Entities/Artist.cs:8` | EDIT: remove `Mbid` line |
| `Album.cs` | `csharp/src/Data/Entities/Album.cs:10` | EDIT: remove `Mbid` line |
| `Track.cs` | `csharp/src/Data/Entities/Track.cs:11` | EDIT: remove `Mbid` line |
| Test: ArtistMbidRemovalTests.cs | `csharp/tests/Scripts.Tests/EntityRefactoring/ArtistMbidRemovalTests.cs` | CREATE |
| Test: AlbumMbidRemovalTests.cs | `csharp/tests/Scripts.Tests/EntityRefactoring/AlbumMbidRemovalTests.cs` | CREATE |
| Test: TrackMbidRemovalTests.cs | `csharp/tests/Scripts.Tests/EntityRefactoring/TrackMbidRemovalTests.cs` | CREATE |

---

## Task 1: Remove `Artist.Mbid`

**Files:**
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\EntityRefactoring\ArtistMbidRemovalTests.cs`
- Modify: `/home/lance/Scripts/csharp/src\Data\Entities\Artist.cs:8`

### Step 0: Preflight

```powershell
# Current state: Artist.cs has Mbid property at line 8, zero external references
# Reason: Mbid is dead code per research — never written, never read, never queried
# What: Remove the property from Artist.cs
# Expected: Property gone, tests green, build clean

Test-Path /home/lance/Scripts/csharp/src\Data\Entities\Artist.cs
# Expected: True

Select-String -Path /home/lance/Scripts/csharp/src\Data\Entities\Artist.cs -Pattern 'Mbid'
# Expected: 1 match (line with `public string? Mbid { get; init; }`)
```

### Step 1: Write the failing test

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\EntityRefactoring\ArtistMbidRemovalTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityRefactoring;

public sealed class ArtistMbidRemovalTests
{
    [Test]
    public void Artist_DoesNotHave_MbidProperty()
    {
        var mbidProp = typeof(Artist).GetProperty("Mbid");
        mbidProp.Should().BeNull(because: "Mbid has zero external references and should be removed");
    }
}
```

### Step 2: Read-back — verify file written

```powershell
Test-Path '/home/lance/Scripts/csharp/tests\Scripts.Tests\EntityRefactoring\ArtistMbidRemovalTests.cs'
# Expected: True
```

### Step 3: Run test — confirm RED

```powershell
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "ArtistMbidRemovalTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: FAIL with
```
Expected mbidProp to be <null> because Mbid has zero external references and should be removed, but found System.Reflection.RuntimePropertyInfo.
```

### Step 3.5: Assess

Property exists. Test correctly detects it. Proceed to remove.

### Step 4: Write minimal implementation

Remove line 8 from `/home/lance/Scripts/csharp/src\Data\Entities\Artist.cs`:

```csharp
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
namespace CSharpScripts.Data.Entities;

internal sealed record Artist
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
	public JsonDocument? Metadata { get; init; }

	public ICollection<Album> Albums { get; } = [];
	public ICollection<Track> Tracks { get; } = [];
}
```

Verify:

```powershell
Select-String -Path /home/lance/Scripts/csharp/src\Data\Entities\Artist.cs -Pattern 'Mbid'
# Expected: 0 matches
```

### Step 5: Run test — confirm GREEN

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "ArtistMbidRemovalTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `1 passed, 0 failed`

### Step 6: Commit

```powershell
git -C /home/lance/Scripts add csharp/src/Data/Entities/Artist.cs
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/EntityRefactoring/ArtistMbidRemovalTests.cs
git -C /home/lance/Scripts commit -m "feat(t1-02): remove Artist.Mbid dead property"
```

---

## Task 2: Remove `Album.Mbid`

**Files:**
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\EntityRefactoring\AlbumMbidRemovalTests.cs`
- Modify: `/home/lance/Scripts/csharp/src\Data\Entities\Album.cs:10`

### Step 0: Preflight

```powershell
# Current state: Album.cs has Mbid property at line 10, zero external references
# Reason: Same as Artist — dead code with no callers
# What: Remove the property
# Expected: Property gone, tests green

Test-Path /home/lance/Scripts/csharp/src\Data\Entities\Album.cs
# Expected: True

Select-String -Path /home/lance/Scripts/csharp/src\Data\Entities\Album.cs -Pattern 'Mbid'
# Expected: 1 match
```

### Step 1: Write the failing test

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\EntityRefactoring\AlbumMbidRemovalTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityRefactoring;

public sealed class AlbumMbidRemovalTests
{
    [Test]
    public void Album_DoesNotHave_MbidProperty()
    {
        var mbidProp = typeof(Album).GetProperty("Mbid");
        mbidProp.Should().BeNull(because: "Mbid has zero external references and should be removed");
    }
}
```

### Step 2: Read-back — verify file written

```powershell
Test-Path '/home/lance/Scripts/csharp/tests\Scripts.Tests\EntityRefactoring\AlbumMbidRemovalTests.cs'
# Expected: True
```

### Step 3: Run test — confirm RED

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "AlbumMbidRemovalTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: FAIL with
```
Expected mbidProp to be <null> because Mbid has zero external references and should be removed, but found System.Reflection.RuntimePropertyInfo.
```

### Step 3.5: Assess

Property exists. Test correctly detects it. Proceed.

### Step 4: Write minimal implementation

Remove line 10 from `/home/lance/Scripts/csharp/src\Data\Entities\Album.cs`:

```csharp
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
namespace CSharpScripts.Data.Entities;

internal sealed record Album
{
	public int Id { get; init; }
	public int ArtistId { get; init; }
	public string Title { get; init; } = null!;
	public DateOnly? ReleaseDate { get; init; }

	public Artist Artist { get; init; } = null!;
	public ICollection<Track> Tracks { get; } = [];
}
```

Verify:

```powershell
Select-String -Path /home/lance/Scripts/csharp/src\Data\Entities\Album.cs -Pattern 'Mbid'
# Expected: 0 matches
```

### Step 5: Run test — confirm GREEN

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "AlbumMbidRemovalTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `1 passed, 0 failed`

### Step 6: Commit

```powershell
git -C /home/lance/Scripts add csharp/src/Data/Entities/Album.cs
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/EntityRefactoring/AlbumMbidRemovalTests.cs
git -C /home/lance/Scripts commit -m "feat(t1-02): remove Album.Mbid dead property"
```

---

## Task 3: Remove `Track.Mbid`

**Files:**
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\EntityRefactoring\TrackMbidRemovalTests.cs`
- Modify: `/home/lance/Scripts/csharp/src\Data\Entities\Track.cs:11`

### Step 0: Preflight

```powershell
# Current state: Track.cs has Mbid property at line 11, zero external references
# Reason: Dead code — final Mbid property to remove
# What: Remove the property
# Expected: Property gone, tests green

Test-Path /home/lance/Scripts/csharp/src\Data\Entities\Track.cs
# Expected: True

Select-String -Path /home/lance/Scripts/csharp/src\Data\Entities\Track.cs -Pattern 'Mbid'
# Expected: 1 match
```

### Step 1: Write the failing test

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\EntityRefactoring\TrackMbidRemovalTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityRefactoring;

public sealed class TrackMbidRemovalTests
{
    [Test]
    public void Track_DoesNotHave_MbidProperty()
    {
        var mbidProp = typeof(Track).GetProperty("Mbid");
        mbidProp.Should().BeNull(because: "Mbid has zero external references and should be removed");
    }
}
```

### Step 2: Read-back — verify file written

```powershell
Test-Path '/home/lance/Scripts/csharp/tests\Scripts.Tests\EntityRefactoring\TrackMbidRemovalTests.cs'
# Expected: True
```

### Step 3: Run test — confirm RED

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "TrackMbidRemovalTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: FAIL with
```
Expected mbidProp to be <null> because Mbid has zero external references and should be removed, but found System.Reflection.RuntimePropertyInfo.
```

### Step 3.5: Assess

Property exists. Test correctly detects it. Proceed — final removal.

### Step 4: Write minimal implementation

Remove line 11 from `/home/lance/Scripts/csharp/src\Data\Entities\Track.cs`:

```csharp
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
namespace CSharpScripts.Data.Entities;

internal sealed record Track
{
	public int Id { get; init; }
	public int ArtistId { get; init; }
	public int? AlbumId { get; init; }
	public string Title { get; init; } = null!;
	public int? Duration { get; init; }

	public Artist Artist { get; init; } = null!;
	public Album? Album { get; init; }
	public ICollection<Scrobble> Scrobbles { get; } = [];
}
```

Verify:

```powershell
Select-String -Path /home/lance/Scripts/csharp/src\Data\Entities\Track.cs -Pattern 'Mbid'
# Expected: 0 matches
```

### Step 5: Run test — confirm GREEN

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "TrackMbidRemovalTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `1 passed, 0 failed`

### Step 6: Commit

```powershell
git -C /home/lance/Scripts add csharp/src/Data/Entities/Track.cs
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/EntityRefactoring/TrackMbidRemovalTests.cs
git -C /home/lance/Scripts commit -m "feat(t1-02): remove Track.Mbid dead property"
```

---

## Final Verification

```powershell
# Confirm no Mbid remains anywhere in entity files
Select-String -Path /home/lance/Scripts/csharp/src\Data\Entities\*.cs -Pattern 'Mbid'
# Expected: 0 matches across all entity files

# Run all refactoring tests
dotnet test --filter "Scripts.Tests.EntityRefactoring" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected:
```
Passed ArtistMbidRemovalTests (1 test)
Passed AlbumMbidRemovalTests (1 test)
Passed TrackMbidRemovalTests (1 test)
3 passed, 0 failed
```

**→ Proceed to `03-dbcontext-config.md`**

---

## Research Provenance

<!-- from research/ENTITY-DESIGN-consolidated.md -->

Source: `AI/plans/research/ENTITY-DESIGN-consolidated.md` (consolidated 2026-06-01; dir deleted)

Content already covered by this plan: Mbid removal (Tasks 1-3), Track metadata audit (already clean), Video/FailedTask "flag for clarification" (Tasks 1-3 do not touch them). See `01-entities.md` Research Provenance for the full Video/FailedTask divergence tables and the int→UUID migration audit deferred to a later phase.
