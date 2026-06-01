# T1-01: EF Core Entity Creation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create all seven EF Core entity classes in `csharp/src/Data/Entities/` and register them as `DbSet<T>` properties on `ScriptsDbContext`.

**Architecture:** Entities are plain C# classes with primary constructors, no data annotations (configuration goes in `IEntityTypeConfiguration<T>` classes — see `04-entity-configurations.md`). All entities live in `CSharpScripts.Data.Entities` namespace. Navigation properties use `ICollection<T>` for one-to-many relationships. `JsonDocument` is used for JSONB columns (Npgsql-native).

**Key Findings from Research:**
- Nine entities total: Artist, Album, Track, Scrobble, Video, ExecutionLog, FailedTask, FiberyEntity, SourceRecord
- Artist, Album, Track use `int` identity PKs; Scrobble uses `long` (BIGINT) for high-volume scrobble history
- Video uses `int` identity PK with soft-delete via `IsDeleted` bool
- ExecutionLog and FailedTask use `int` and `Guid` PKs respectively with `DateTimeOffset` timestamps
- FiberyEntity and SourceRecord use `Guid` PKs with `gen_random_uuid()` default
- Four entities use `JsonDocument?` for JSONB columns: Artist.Metadata, Video.Metadata, ExecutionLog.Payload, FiberyEntity.RawData
- `JsonDocument` mapping requires NO `mb.Ignore<JsonDocument>()` — allow Npgsql to handle natively (EF Core 10 + Npgsql 10 support this)
- All navigation properties use `ICollection<T>` initialized to empty lists `[]` (C# 14 collection expressions)
- No `Mbid` properties exist (removed in T1-02) — Artist, Album, Track are clean

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- `00-environment.md` completed (all 5 environment tests green)
- `ScriptsDbContext.cs` exists at `csharp/src/Data/ScriptsDbContext.cs`
- Docker running, `$env:PGCONNSTR` loaded

---

## Task 1 — Create `Artist` Entity

### Step 0: Preflight

```powershell
# Current state: csharp/src/Data/Entities/ directory may not exist
# Reason: Artist is the root of the music domain graph
# What: Create Artist.cs with Id, Name, Metadata, Albums nav
# Expected: File created, properties accessible via reflection

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Artist.cs
# Expected: False

New-Item -ItemType Directory -Force -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities
```

### Step 1: Write test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\ArtistEntityTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;
using System.Text.Json;

namespace Scripts.Tests.Entities;

public sealed class ArtistEntityTests
{
    [Test]
    public void Artist_HasRequired_Properties()
    {
        var props = typeof(Artist).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("Name");
        props.Should().Contain("Metadata");
        props.Should().Contain("Albums");
    }

    [Test]
    public void Artist_Id_IsInt()
    {
        var idProp = typeof(Artist).GetProperty("Id");
        idProp.Should().NotBeNull();
        idProp!.PropertyType.Should().Be(typeof(int));
    }

    [Test]
    public void Artist_Name_IsString()
    {
        var nameProp = typeof(Artist).GetProperty("Name");
        nameProp.Should().NotBeNull();
        nameProp!.PropertyType.Should().Be(typeof(string));
    }

    [Test]
    public void Artist_Metadata_IsNullableJsonDocument()
    {
        var metaProp = typeof(Artist).GetProperty("Metadata");
        metaProp.Should().NotBeNull();
        metaProp!.PropertyType.Should().Be(typeof(JsonDocument));
    }

    [Test]
    public void Artist_Albums_IsCollection()
    {
        var albumsProp = typeof(Artist).GetProperty("Albums");
        albumsProp.Should().NotBeNull();
        albumsProp!.PropertyType.IsGenericType.Should().BeTrue();
        albumsProp.PropertyType.GetGenericTypeDefinition().Should().Be(typeof(ICollection<>));
    }

    [Test]
    public void Artist_CanBeInstantiated_WithDefaults()
    {
        var artist = new Artist { Name = "Radiohead" };
        artist.Name.Should().Be("Radiohead");
        artist.Metadata.Should().BeNull();
        artist.Albums.Should().NotBeNull();
    }
}
```

### Step 2: Readback

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\ArtistEntityTests.cs
```

Expected: File exists, contains 6 `[Test]` methods.

### Step 3: Run — expect FAIL

```powershell
dotnet test --filter "ArtistEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Error CS0246: The type or namespace name 'Artist' could not be found
```

### Step 3.5: Assess

Compilation error confirms `Artist` class is missing. Proceed to create it.

### Step 4: Create `Artist.cs`

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Artist.cs`

```csharp
using System.Text.Json;

namespace CSharpScripts.Data.Entities;

/// <summary>
/// Represents a music artist. Metadata is stored as JSONB (configured in ArtistConfiguration).
/// </summary>
public sealed class Artist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public JsonDocument? Metadata { get; set; }
    public ICollection<Album> Albums { get; init; } = new List<Album>();
}
```

Verify:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Artist.cs
# Expected: True
```

### Step 5: Run — expect PASS

```powershell
dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test --filter "ArtistEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Build succeeded.
6 passed, 0 failed
```

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Entities/Artist.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Entities/ArtistEntityTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-01): add Artist entity and reflection tests"
```

---

## Task 2 — Create `Album` Entity

### Step 0: Preflight

```powershell
# Current state: Artist.cs exists, Album.cs does not
# Reason: Album has FK to Artist and is parent of Track
# What: Create Album.cs with ArtistId FK, DateOnly? ReleaseDate
# Expected: File created, compiles

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Album.cs
# Expected: False
```

### Step 1: Write test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\AlbumEntityTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Entities;

public sealed class AlbumEntityTests
{
    [Test]
    public void Album_HasRequired_Properties()
    {
        var props = typeof(Album).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("ArtistId");
        props.Should().Contain("Title");
        props.Should().Contain("ReleaseDate");
        props.Should().Contain("Artist");
        props.Should().Contain("Tracks");
    }

    [Test]
    public void Album_ArtistId_IsInt()
    {
        typeof(Album).GetProperty("ArtistId")!.PropertyType.Should().Be(typeof(int));
    }

    [Test]
    public void Album_ReleaseDate_IsNullableDateOnly()
    {
        var prop = typeof(Album).GetProperty("ReleaseDate");
        prop.Should().NotBeNull();
        prop!.PropertyType.Should().Be(typeof(DateOnly?));
    }

    [Test]
    public void Album_Tracks_IsCollection()
    {
        var prop = typeof(Album).GetProperty("Tracks");
        prop.Should().NotBeNull();
        prop!.PropertyType.IsGenericType.Should().BeTrue();
        prop.PropertyType.GetGenericTypeDefinition().Should().Be(typeof(ICollection<>));
    }

    [Test]
    public void Album_CanBeInstantiated_WithDefaults()
    {
        var album = new Album { Title = "OK Computer", ArtistId = 1 };
        album.Title.Should().Be("OK Computer");
        album.ReleaseDate.Should().BeNull();
        album.Tracks.Should().NotBeNull();
    }
}
```

### Step 2: Readback

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\AlbumEntityTests.cs
```

### Step 3: Run — expect FAIL

```powershell
dotnet test --filter "AlbumEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `Error CS0246: 'Album' not found`

### Step 3.5: Assess

Compilation error confirms missing entity. Proceed.

### Step 4: Create `Album.cs`

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Album.cs`

```csharp
namespace CSharpScripts.Data.Entities;

/// <summary>
/// Represents a music album belonging to an artist.
/// ReleaseDate uses DateOnly — maps to PostgreSQL DATE via Npgsql.
/// </summary>
public sealed class Album
{
    public int Id { get; set; }
    public int ArtistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly? ReleaseDate { get; set; }

    // Navigation properties
    public Artist Artist { get; set; } = null!;
    public ICollection<Track> Tracks { get; init; } = new List<Track>();
}
```

Verify:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Album.cs
# Expected: True
```

### Step 5: Run — expect PASS

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test --filter "AlbumEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `5 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Entities/Album.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Entities/AlbumEntityTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-01): add Album entity and reflection tests"
```

---

## Task 3 — Create `Track` Entity

### Step 0: Preflight

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Track.cs
# Expected: False
```

### Step 1: Write test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\TrackEntityTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Entities;

public sealed class TrackEntityTests
{
    [Test]
    public void Track_HasRequired_Properties()
    {
        var props = typeof(Track).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("AlbumId");
        props.Should().Contain("ArtistId");
        props.Should().Contain("Title");
        props.Should().Contain("DurationSeconds");
        props.Should().Contain("Album");
        props.Should().Contain("Artist");
        props.Should().Contain("Scrobbles");
    }

    [Test]
    public void Track_DurationSeconds_IsNullableInt()
    {
        var prop = typeof(Track).GetProperty("DurationSeconds");
        prop.Should().NotBeNull();
        prop!.PropertyType.Should().Be(typeof(int?));
    }

    [Test]
    public void Track_Scrobbles_IsCollection()
    {
        var prop = typeof(Track).GetProperty("Scrobbles");
        prop!.PropertyType.IsGenericType.Should().BeTrue();
        prop.PropertyType.GetGenericTypeDefinition().Should().Be(typeof(ICollection<>));
    }

    [Test]
    public void Track_CanBeInstantiated_WithDefaults()
    {
        var track = new Track { Title = "Karma Police", AlbumId = 1, ArtistId = 1 };
        track.DurationSeconds.Should().BeNull();
        track.Scrobbles.Should().NotBeNull();
    }
}
```

### Step 2: Readback

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\TrackEntityTests.cs
```

### Step 3: Run — expect FAIL

```powershell
dotnet test --filter "TrackEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `Error CS0246: 'Track' not found`

### Step 3.5: Assess

Compilation error confirmed. Proceed.

### Step 4: Create `Track.cs`

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Track.cs`

```csharp
namespace CSharpScripts.Data.Entities;

/// <summary>
/// Represents a music track belonging to an album and artist.
/// Duration is stored in seconds (integer) for simplicity.
/// </summary>
public sealed class Track
{
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public int ArtistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? DurationSeconds { get; set; }

    // Navigation properties
    public Album Album { get; set; } = null!;
    public Artist Artist { get; set; } = null!;
    public ICollection<Scrobble> Scrobbles { get; init; } = new List<Scrobble>();
}
```

Verify:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Track.cs
# Expected: True
```

### Step 5: Run — expect PASS

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test --filter "TrackEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `4 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Entities/Track.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Entities/TrackEntityTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-01): add Track entity and reflection tests"
```

---

## Task 4 — Create `Scrobble` Entity

### Step 0: Preflight

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Scrobble.cs
# Expected: False
```

### Step 1: Write test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\ScrobbleEntityTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Entities;

public sealed class ScrobbleEntityTests
{
    [Test]
    public void Scrobble_HasRequired_Properties()
    {
        var props = typeof(Scrobble).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("TrackId");
        props.Should().Contain("ScrobbledAt");
        props.Should().Contain("Platform");
        props.Should().Contain("Track");
    }

    [Test]
    public void Scrobble_Id_IsLong()
    {
        typeof(Scrobble).GetProperty("Id")!.PropertyType.Should().Be(typeof(long));
    }

    [Test]
    public void Scrobble_ScrobbledAt_IsDateTimeOffset()
    {
        typeof(Scrobble).GetProperty("ScrobbledAt")!.PropertyType
            .Should().Be(typeof(DateTimeOffset));
    }

    [Test]
    public void Scrobble_Platform_IsString()
    {
        typeof(Scrobble).GetProperty("Platform")!.PropertyType.Should().Be(typeof(string));
    }

    [Test]
    public void Scrobble_CanBeInstantiated_WithDefaults()
    {
        var scrobble = new Scrobble
        {
            Id = 1,
            TrackId = 1,
            ScrobbledAt = DateTimeOffset.UtcNow,
            Platform = "lastfm"
        };
        scrobble.Platform.Should().Be("lastfm");
    }
}
```

### Step 2: Readback

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\ScrobbleEntityTests.cs
```

### Step 3: Run — expect FAIL

```powershell
dotnet test --filter "ScrobbleEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `Error CS0246: 'Scrobble' not found`

### Step 3.5: Assess

Compilation error confirmed. Proceed.

### Step 4: Create `Scrobble.cs`

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Scrobble.cs`

```csharp
namespace CSharpScripts.Data.Entities;

/// <summary>
/// Records a single music play event (scrobble) from a streaming platform.
/// Id is long (BIGINT) to handle high-volume scrobble history.
/// Platform is stored as a string (e.g., "lastfm", "spotify").
/// </summary>
public sealed class Scrobble
{
    public long Id { get; set; }
    public int TrackId { get; set; }
    public DateTimeOffset ScrobbledAt { get; set; }
    public string Platform { get; set; } = string.Empty;

    // Navigation property
    public Track Track { get; set; } = null!;
}
```

Verify:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Scrobble.cs
# Expected: True
```

### Step 5: Run — expect PASS

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test --filter "ScrobbleEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `5 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Entities/Scrobble.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Entities/ScrobbleEntityTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-01): add Scrobble entity and reflection tests"
```

---

## Task 5 — Create `Video` Entity

### Step 0: Preflight

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Video.cs
# Expected: False
```

### Step 1: Write test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\VideoEntityTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Entities;

public sealed class VideoEntityTests
{
    [Test]
    public void Video_HasRequired_Properties()
    {
        var props = typeof(Video).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("YoutubeId");
        props.Should().Contain("Title");
        props.Should().Contain("PlaylistId");
        props.Should().Contain("IsDeleted");
    }

    [Test]
    public void Video_YoutubeId_IsString()
    {
        typeof(Video).GetProperty("YoutubeId")!.PropertyType.Should().Be(typeof(string));
    }

    [Test]
    public void Video_IsDeleted_IsBool()
    {
        typeof(Video).GetProperty("IsDeleted")!.PropertyType.Should().Be(typeof(bool));
    }

    [Test]
    public void Video_CanBeInstantiated_WithDefaults()
    {
        var video = new Video { YoutubeId = "dQw4w9WgXcQ", Title = "Never Gonna Give You Up", PlaylistId = "PL123" };
        video.IsDeleted.Should().BeFalse();
    }
}
```

### Step 2: Readback

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\VideoEntityTests.cs
```

### Step 3: Run — expect FAIL

```powershell
dotnet test --filter "VideoEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `Error CS0246: 'Video' not found`

### Step 3.5: Assess

Missing entity confirmed. Proceed.

### Step 4: Create `Video.cs`

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Video.cs`

```csharp
namespace CSharpScripts.Data.Entities;

/// <summary>
/// Represents a YouTube video tracked in a playlist.
/// IsDeleted uses soft-delete to preserve history of removed videos.
/// </summary>
public sealed class Video
{
    public int Id { get; set; }
    public string YoutubeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PlaylistId { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
```

Verify:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Video.cs
# Expected: True
```

### Step 5: Run — expect PASS

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test --filter "VideoEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `4 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Entities/Video.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Entities/VideoEntityTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-01): add Video entity and reflection tests"
```

---

## Task 6 — Create `ExecutionLog` Entity

### Step 0: Preflight

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\ExecutionLog.cs
# Expected: False
```

### Step 1: Write test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\ExecutionLogEntityTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;
using System.Text.Json;

namespace Scripts.Tests.Entities;

public sealed class ExecutionLogEntityTests
{
    [Test]
    public void ExecutionLog_HasRequired_Properties()
    {
        var props = typeof(ExecutionLog).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("Timestamp");
        props.Should().Contain("SessionId");
        props.Should().Contain("Payload");
        props.Should().Contain("ExitCode");
    }

    [Test]
    public void ExecutionLog_Payload_IsJsonDocument()
    {
        typeof(ExecutionLog).GetProperty("Payload")!.PropertyType
            .Should().Be(typeof(JsonDocument));
    }

    [Test]
    public void ExecutionLog_Timestamp_IsDateTimeOffset()
    {
        typeof(ExecutionLog).GetProperty("Timestamp")!.PropertyType
            .Should().Be(typeof(DateTimeOffset));
    }
}
```

### Step 2: Readback

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\ExecutionLogEntityTests.cs
```

### Step 3: Run — expect FAIL

```powershell
dotnet test --filter "ExecutionLogEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `Error CS0246: 'ExecutionLog' not found`

### Step 3.5: Assess

Missing entity confirmed. Proceed.

### Step 4: Create `ExecutionLog.cs`

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\ExecutionLog.cs`

```csharp
using System.Text.Json;

namespace CSharpScripts.Data.Entities;

/// <summary>
/// Records a CLI execution session. Payload is JSONB for flexible structured metadata.
/// Id is SERIAL (auto-increment int) — high frequency writes expected.
/// </summary>
public sealed class ExecutionLog
{
    public int Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    public int ExitCode { get; set; }
}
```

Verify:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\ExecutionLog.cs
# Expected: True
```

### Step 5: Run — expect PASS

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test --filter "ExecutionLogEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Entities/ExecutionLog.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Entities/ExecutionLogEntityTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-01): add ExecutionLog entity and reflection tests"
```

---

## Task 7 — Create `FailedTask` Entity

### Step 0: Preflight

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\FailedTask.cs
# Expected: False
```

### Step 1: Write test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\FailedTaskEntityTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Entities;

public sealed class FailedTaskEntityTests
{
    [Test]
    public void FailedTask_HasRequired_Properties()
    {
        var props = typeof(FailedTask).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("Operation");
        props.Should().Contain("ErrorMessage");
        props.Should().Contain("CreatedAt");
    }

    [Test]
    public void FailedTask_Id_IsGuid()
    {
        typeof(FailedTask).GetProperty("Id")!.PropertyType.Should().Be(typeof(Guid));
    }

    [Test]
    public void FailedTask_CreatedAt_IsDateTimeOffset()
    {
        typeof(FailedTask).GetProperty("CreatedAt")!.PropertyType
            .Should().Be(typeof(DateTimeOffset));
    }
}
```

### Step 2: Readback

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\FailedTaskEntityTests.cs
```

### Step 3: Run — expect FAIL

```powershell
dotnet test --filter "FailedTaskEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `Error CS0246: 'FailedTask' not found`

### Step 3.5: Assess

Missing entity confirmed. Proceed.

### Step 4: Create `FailedTask.cs`

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\FailedTask.cs`

```csharp
namespace CSharpScripts.Data.Entities;

/// <summary>
/// Records a failed background operation for retry or alerting.
/// Guid PK prevents collisions across distributed or long-running sessions.
/// </summary>
public sealed class FailedTask
{
    public Guid Id { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
```

Verify:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\FailedTask.cs
# Expected: True
```

### Step 5: Run — expect PASS

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test --filter "FailedTaskEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Entities/FailedTask.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Entities/FailedTaskEntityTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-01): add FailedTask entity and reflection tests"
```

---

## Task 8 — Register All Entities on `ScriptsDbContext`

### Step 0: Preflight

```powershell
# Current state: All 7 entity files exist, DbContext has no DbSet properties
# Reason: EF Core needs DbSet<T> to track entity types
# What: Add DbSet properties for all 7 entities to ScriptsDbContext
# Expected: Context compiles with all DbSet properties visible

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs -Pattern "DbSet" | Measure-Object | Select-Object -ExpandProperty Count
# Expected: 0 (no DbSet properties yet)
```

### Step 1: Write test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\DbContextDbSetTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Entities;

public sealed class DbContextDbSetTests
{
    private ScriptsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase("DbSetTest_" + Guid.NewGuid())
            .Options;
        return new ScriptsDbContext(options);
    }

    [Test]
    public void DbContext_HasArtists_DbSet()
    {
        using var context = CreateContext();
        context.Artists.Should().NotBeNull();
    }

    [Test]
    public void DbContext_HasAlbums_DbSet()
    {
        using var context = CreateContext();
        context.Albums.Should().NotBeNull();
    }

    [Test]
    public void DbContext_HasTracks_DbSet()
    {
        using var context = CreateContext();
        context.Tracks.Should().NotBeNull();
    }

    [Test]
    public void DbContext_HasScrobbles_DbSet()
    {
        using var context = CreateContext();
        context.Scrobbles.Should().NotBeNull();
    }

    [Test]
    public void DbContext_HasVideos_DbSet()
    {
        using var context = CreateContext();
        context.Videos.Should().NotBeNull();
    }

    [Test]
    public void DbContext_HasExecutionLogs_DbSet()
    {
        using var context = CreateContext();
        context.ExecutionLogs.Should().NotBeNull();
    }

    [Test]
    public void DbContext_HasFailedTasks_DbSet()
    {
        using var context = CreateContext();
        context.FailedTasks.Should().NotBeNull();
    }
}
```

> **Note:** In-memory database is used here only for DbSet existence checks. All functional tests use the real PostgreSQL via Testcontainers (see `15-testcontainers.md`). Add `Microsoft.EntityFrameworkCore.InMemory` to `Scripts.Tests.csproj` for this task only.

### Step 2: Readback

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Entities\DbContextDbSetTests.cs
```

### Step 3: Run — expect FAIL

```powershell
dotnet test --filter "DbContextDbSetTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Error CS1061: 'ScriptsDbContext' does not contain a definition for 'Artists'
```

### Step 3.5: Assess

DbSet properties are missing. Proceed to add them.

### Step 4: Update `ScriptsDbContext.cs`

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data;

/// <summary>
/// Primary EF Core DbContext for the Scripts application.
/// NoTracking is the default; enable tracking explicitly per-operation when needed.
/// Entity type configurations are loaded from assembly in OnModelCreating.
/// </summary>
public sealed class ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
    : DbContext(options)
{
    // Music domain
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<Scrobble> Scrobbles => Set<Scrobble>();
    public DbSet<Video> Videos => Set<Video>();

    // Management domain
    public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();
    public DbSet<FailedTask> FailedTasks => Set<FailedTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // IEntityTypeConfiguration<T> classes loaded in 04-entity-configurations.md
    }
}
```

Verify:

```powershell
Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs -Pattern "DbSet" | Measure-Object | Select-Object -ExpandProperty Count
# Expected: 7
```

### Step 5: Run — expect PASS

```powershell
dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test --filter "DbContextDbSetTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `7 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/ScriptsDbContext.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Entities/DbContextDbSetTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-01): register all entity DbSets on ScriptsDbContext"
```

---

## Task 9 — Generate Initial EF Migration

### Step 0: Preflight

```powershell
# Current state: All entities registered, no migrations exist
# Reason: Migrations capture the schema baseline for PostgreSQL
# What: Generate InitialEntities migration
# Expected: Migrations/ directory created with snapshot and migration file

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Migrations
# Expected: False (no migrations yet)

# Load env for EF CLI
Get-Content C:\Users\Lance\Dev\Scripts\.env | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.+)$') {
        [System.Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
    }
}
```

### Step 3: Run migration command

```powershell
dotnet ef migrations add InitialEntities `
    --project C:\Users\Lance\Dev\Scripts\csharp\src\Data\Scripts.Data.csproj `
    --startup-project C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Scripts.CLI.csproj `
    2>&1
```

Expected output:
```
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

Verify migration files created:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Migrations
# Expected: True

Get-ChildItem C:\Users\Lance\Dev\Scripts\csharp\src\Data\Migrations | Select-Object Name
# Expected: files matching *_InitialEntities.cs, *_InitialEntities.Designer.cs, ScriptsDbContextModelSnapshot.cs
```

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Migrations/
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-01): generate InitialEntities EF Core migration"
```

---

## Final Verification

```powershell
dotnet test --filter "Scripts.Tests.Entities" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Passed ArtistEntityTests (6 tests)
Passed AlbumEntityTests (5 tests)
Passed TrackEntityTests (4 tests)
Passed ScrobbleEntityTests (5 tests)
Passed VideoEntityTests (4 tests)
Passed ExecutionLogEntityTests (3 tests)
Passed FailedTaskEntityTests (3 tests)
Passed DbContextDbSetTests (7 tests)
37 passed, 0 failed
```

**→ Proceed to `02-entity-refactoring.md`**
