# T1-06: Repository Pattern Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create repository interfaces and implementations for 5 core entities (Scrobble, Video, Track, Artist, Album) using IDbContextFactory with ExecuteUpdateAsync/ExecuteDeleteAsync preference.

**Architecture:** Each repository is `internal sealed class` implementing an `internal interface`. All constructors take `IDbContextFactory<ScriptsDbContext>`. Each method creates its own short-lived context via `CreateDbContextAsync()`. Mutations use `ExecuteUpdateAsync`/`ExecuteDeleteAsync` (not SaveChanges loops). Registration is `services.AddScoped<I...Repository, ...Repository>()`. Tests use real PostgreSQL via connection string (Testcontainers comes in T1-15).

**Key Findings from Research:**
- Seven repositories needed: Scrobble, Video, Track, Artist, Album, ExecutionLog, FailedTask (plus optional management repos)
- PostgresService.cs already uses ExecuteUpdateAsync for single-entity upserts and SaveChangesAsync for bulk inserts — follow this pattern
- Duplicate LastFmService.cs exists at `src/Services/Sync/LastFm/LastFmService.cs` (legacy, sync-only) — will be deleted in T1-09
- ILike/EF.Functions.Like not yet used anywhere — greenfield capability for future name lookups
- Repository pattern: thin wrappers per entity, no generic Repository<T> base class
- Mutation strategy: ExecuteUpdateAsync for upserts, AddRange+SaveChangesAsync for bulk insert, ExecuteDeleteAsync for bulk delete
- All repositories use IDbContextFactory to manage context lifecycle — no shared context instances
- NoTracking is default on DbContext, so queries are naturally read-only unless explicitly tracked

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- Phases 00-05 completed — InitialCreate migration applied, database tables exist
- `PostgresService.cs` exists at `csharp/src/Services/PostgresService.cs` (reference for ExecuteUpdateAsync usage)
- `DbContextRegistration.cs` exists at `csharp/src/Data/DbContextRegistration.cs` (DI registration entry point)

---

## File Map

| File | Path | Action |
|------|------|--------|
| `IScrobbleRepository.cs` | `csharp/src/Data/Repositories/IScrobbleRepository.cs` | CREATE |
| `ScrobbleRepository.cs` | `csharp/src/Data/Repositories/ScrobbleRepository.cs` | CREATE |
| `IVideoRepository.cs` | `csharp/src/Data/Repositories/IVideoRepository.cs` | CREATE |
| `VideoRepository.cs` | `csharp/src/Data/Repositories/VideoRepository.cs` | CREATE |
| `ITrackRepository.cs` | `csharp/src/Data/Repositories/ITrackRepository.cs` | CREATE |
| `TrackRepository.cs` | `csharp/src/Data/Repositories/TrackRepository.cs` | CREATE |
| `IArtistRepository.cs` | `csharp/src/Data/Repositories/IArtistRepository.cs` | CREATE |
| `ArtistRepository.cs` | `csharp/src/Data/Repositories/ArtistRepository.cs` | CREATE |
| `IAlbumRepository.cs` | `csharp/src/Data/Repositories/IAlbumRepository.cs` | CREATE |
| `AlbumRepository.cs` | `csharp/src/Data/Repositories/AlbumRepository.cs` | CREATE |
| `RepositoryRegistration.cs` | `csharp/src/Data/Repositories/RepositoryRegistration.cs` | CREATE |
| Test files | `csharp/tests/Scripts.Tests/Repositories/` | CREATE (5 test files) |

---

## Task 1: ScrobbleRepository

**Files:**
- Create: `/home/lance/Scripts/csharp/src\Data\Repositories\IScrobbleRepository.cs`
- Create: `/home/lance/Scripts/csharp/src\Data\Repositories\ScrobbleRepository.cs`
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\ScrobbleRepositoryTests.cs`

### Step 0: Preflight

```powershell
# Current state: No repository directory exists. PostgresService handles scrobble upserts via ExecuteUpdateAsync
# Reason: Need dedicated repository per entity per AGENTS.md architecture
# What: Create IScrobbleRepository + ScrobbleRepository, test CRUD against real PostgreSQL
# Expected: Repository compiles, tests pass with real DB

Test-Path /home/lance/Scripts/csharp/src\Data\Repositories
# Expected: False

New-Item -ItemType Directory -Force -Path /home/lance/Scripts/csharp/src\Data\Repositories
```

### Step 1: Write the failing test

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\ScrobbleRepositoryTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Data.Repositories;

namespace Scripts.Tests.Repositories;

public sealed class ScrobbleRepositoryTests : IDisposable
{
    private readonly ScriptsDbContext _context;
    private readonly ScrobbleRepository _repository;

    public ScrobbleRepositoryTests()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")
            ?? throw new InvalidOperationException("PGCONNSTR environment variable is not set.");

        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        _context = new ScriptsDbContext(options);
        _context.Database.EnsureCreated();

        var factory = new TestDbContextFactory(_context);
        _repository = new ScrobbleRepository(factory);
    }

    public void Dispose() => _context.Dispose();

    [Test]
    public async Task UpsertAsync_InsertsNewScrobble()
    {
        var result = await _repository.UpsertAsync(1, 1, DateTimeOffset.UtcNow, "lastfm");
        result.Should().Be(1);
    }

    [Test]
    public async Task GetByIdAsync_ReturnsScrobble_AfterUpsert()
    {
        var timestamp = DateTimeOffset.UtcNow;
        await _repository.UpsertAsync(100, 1, timestamp, "spotify");
        var scrobble = await _repository.GetByIdAsync(100);

        scrobble.Should().NotBeNull();
        scrobble!.Platform.Should().Be("spotify");
        scrobble.ScrobbledAt.Should().Be(timestamp);
    }

    [Test]
    public async Task DeleteByTrackIdAsync_RemovesScrobbles()
    {
        await _repository.UpsertAsync(200, 99, DateTimeOffset.UtcNow, "lastfm");
        var deleted = await _repository.DeleteByTrackIdAsync(99);

        deleted.Should().BeGreaterThan(0);
    }
}

internal sealed class TestDbContextFactory(ScriptsDbContext context) : IDbContextFactory<ScriptsDbContext>
{
    public ScriptsDbContext CreateDbContext() => context;
}
```

### Step 2: Read-back

```powershell
Test-Path '/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\ScrobbleRepositoryTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "ScrobbleRepositoryTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: FAIL with `Error CS0246: The type or namespace name 'ScrobbleRepository' could not be found` or `Error CS0246: The type or namespace name 'IScrobbleRepository' could not be found`.

### Step 3.5: Assess

Interface and implementation do not exist. Proceed to create both.

### Step 4: Write minimal implementation

File: `/home/lance/Scripts/csharp/src\Data\Repositories\IScrobbleRepository.cs`

```csharp
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Repositories;

internal interface IScrobbleRepository
{
    Task<int> UpsertAsync(long id, int trackId, DateTimeOffset timestamp, string platform, CancellationToken ct = default);
    Task<Scrobble?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<Scrobble>> GetByTrackIdAsync(int trackId, CancellationToken ct = default);
    Task<List<Scrobble>> GetByPlatformAsync(string platform, int limit, CancellationToken ct = default);
    Task<int> DeleteByTrackIdAsync(int trackId, CancellationToken ct = default);
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default);
}
```

File: `/home/lance/Scripts/csharp/src\Data\Repositories\ScrobbleRepository.cs`

```csharp
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Repositories;

internal sealed class ScrobbleRepository(IDbContextFactory<ScriptsDbContext> contextFactory) : IScrobbleRepository
{
    public async Task<int> UpsertAsync(long id, int trackId, DateTimeOffset timestamp, string platform, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var affected = await context.Scrobbles
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(
                scrobble => scrobble
                    .SetProperty(s => s.TrackId, x => trackId)
                    .SetProperty(s => s.ScrobbledAt, x => timestamp)
                    .SetProperty(s => s.Platform, x => platform),
                cancellationToken: ct);

        if (affected == 0)
        {
            context.Scrobbles.Add(new Scrobble
            {
                Id = id,
                TrackId = trackId,
                ScrobbledAt = timestamp,
                Platform = platform
            });
            affected = await context.SaveChangesAsync(ct);
        }

        return affected;
    }

    public async Task<Scrobble?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Scrobbles
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<List<Scrobble>> GetByTrackIdAsync(int trackId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Scrobbles
            .AsNoTracking()
            .Where(s => s.TrackId == trackId)
            .OrderByDescending(s => s.ScrobbledAt)
            .ToListAsync(ct);
    }

    public async Task<List<Scrobble>> GetByPlatformAsync(string platform, int limit, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Scrobbles
            .AsNoTracking()
            .Where(s => s.Platform == platform)
            .OrderByDescending(s => s.ScrobbledAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<int> DeleteByTrackIdAsync(int trackId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Scrobbles
            .Where(s => s.TrackId == trackId)
            .ExecuteDeleteAsync(cancellationToken: ct);
    }

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Scrobbles
            .Where(s => s.ScrobbledAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken: ct);
    }
}
```

Verify:

```powershell
Test-Path /home/lance/Scripts/csharp/src\Data\Repositories\IScrobbleRepository.cs
# Expected: True
Test-Path /home/lance/Scripts/csharp/src\Data\Repositories\ScrobbleRepository.cs
# Expected: True
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "ScrobbleRepositoryTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C /home/lance/Scripts add csharp/src/Data/Repositories/IScrobbleRepository.cs
git -C /home/lance/Scripts add csharp/src/Data/Repositories/ScrobbleRepository.cs
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/Repositories/ScrobbleRepositoryTests.cs
git -C /home/lance/Scripts commit -m "feat(t1-06): add IScrobbleRepository + ScrobbleRepository"
```

---

## Task 2: VideoRepository

**Files:**
- Create: `/home/lance/Scripts/csharp/src\Data\Repositories\IVideoRepository.cs`
- Create: `/home/lance/Scripts/csharp/src\Data\Repositories\VideoRepository.cs`
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\VideoRepositoryTests.cs`

### Step 0: Preflight

```powershell
Test-Path /home/lance/Scripts/csharp/src\Data\Repositories\IVideoRepository.cs
# Expected: False
```

### Step 1: Write the failing test

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\VideoRepositoryTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Data.Repositories;

namespace Scripts.Tests.Repositories;

public sealed class VideoRepositoryTests : IDisposable
{
    private readonly ScriptsDbContext _context;
    private readonly VideoRepository _repository;
    private readonly List<long> _createdIds = [];

    public VideoRepositoryTests()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")!;
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        _context = new ScriptsDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new VideoRepository(new TestDbContextFactory(_context));
    }

    public void Dispose()
    {
        foreach (var id in _createdIds)
        {
            var entity = _context.Videos.Find(id);
            if (entity is not null) { _context.Videos.Remove(entity); }
        }
        if (_createdIds.Count > 0) _context.SaveChanges();
        _context.Dispose();
    }

    [Test]
    public async Task AddAsync_InsertsVideo()
    {
        var video = new Video
        {
            Id = DateTimeOffset.UtcNow.Ticks,
            Url = "https://youtube.com/watch?v=test123",
            Title = "Test Video",
            ChannelName = "TestChannel",
            UploadDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        await _repository.AddAsync(video);
        _createdIds.Add(video.Id);

        var fetched = await _repository.GetByUrlAsync("https://youtube.com/watch?v=test123");
        fetched.Should().NotBeNull();
        fetched!.Title.Should().Be("Test Video");
    }

    [Test]
    public async Task DeleteByIdAsync_RemovesVideo()
    {
        var video = new Video
        {
            Id = DateTimeOffset.UtcNow.Ticks + 1,
            Url = "https://youtube.com/watch?v=deleteMe",
            Title = "Delete Me",
            ChannelName = "DelChannel",
            UploadDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        await _repository.AddAsync(video);
        var deleted = await _repository.DeleteByIdAsync(video.Id);

        deleted.Should().Be(1);
        var fetched = await _repository.GetByIdAsync(video.Id);
        fetched.Should().BeNull();
    }
}
```

### Step 2: Read-back

```powershell
Test-Path '/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\VideoRepositoryTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet test --filter "VideoRepositoryTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: FAIL — `VideoRepository` or `IVideoRepository` not found.

### Step 3.5: Assess

Confirmed. Proceed.

### Step 4: Write minimal implementation

File: `/home/lance/Scripts/csharp/src\Data\Repositories\IVideoRepository.cs`

```csharp
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Repositories;

internal interface IVideoRepository
{
    Task AddAsync(Video video, CancellationToken ct = default);
    Task<Video?> GetByUrlAsync(string url, CancellationToken ct = default);
    Task<Video?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<Video>> GetByChannelAsync(string channelName, CancellationToken ct = default);
    Task<int> UpdateTitleAsync(long id, string title, CancellationToken ct = default);
    Task<int> DeleteByIdAsync(long id, CancellationToken ct = default);
}
```

File: `/home/lance/Scripts/csharp/src\Data\Repositories\VideoRepository.cs`

```csharp
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Repositories;

internal sealed class VideoRepository(IDbContextFactory<ScriptsDbContext> contextFactory) : IVideoRepository
{
    public async Task AddAsync(Video video, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        context.Videos.Add(video);
        await context.SaveChangesAsync(ct);
    }

    public async Task<Video?> GetByUrlAsync(string url, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Videos
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Url == url, ct);
    }

    public async Task<Video?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Videos
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<List<Video>> GetByChannelAsync(string channelName, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Videos
            .AsNoTracking()
            .Where(v => v.ChannelName == channelName)
            .ToListAsync(ct);
    }

    public async Task<int> UpdateTitleAsync(long id, string title, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Videos
            .Where(v => v.Id == id)
            .ExecuteUpdateAsync(
                video => video.SetProperty(v => v.Title, x => title),
                cancellationToken: ct);
    }

    public async Task<int> DeleteByIdAsync(long id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Videos
            .Where(v => v.Id == id)
            .ExecuteDeleteAsync(cancellationToken: ct);
    }
}
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "VideoRepositoryTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `2 passed, 0 failed`

### Step 6: Commit

```powershell
git -C /home/lance/Scripts add csharp/src/Data/Repositories/IVideoRepository.cs
git -C /home/lance/Scripts add csharp/src/Data/Repositories/VideoRepository.cs
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/Repositories/VideoRepositoryTests.cs
git -C /home/lance/Scripts commit -m "feat(t1-06): add IVideoRepository + VideoRepository"
```

---

## Task 3: TrackRepository

**Files:**
- Create: `/home/lance/Scripts/csharp/src\Data\Repositories\ITrackRepository.cs`
- Create: `/home/lance/Scripts/csharp/src\Data\Repositories\TrackRepository.cs`
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\TrackRepositoryTests.cs`

### Step 0: Preflight

```powershell
Test-Path /home/lance/Scripts/csharp/src\Data\Repositories\ITrackRepository.cs
# Expected: False
```

### Step 1: Write the failing test

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\TrackRepositoryTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Data.Repositories;

namespace Scripts.Tests.Repositories;

public sealed class TrackRepositoryTests : IDisposable
{
    private readonly ScriptsDbContext _context;
    private readonly TrackRepository _repository;
    private readonly List<int> _createdTrackIds = [];

    public TrackRepositoryTests()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")!;
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        _context = new ScriptsDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new TrackRepository(new TestDbContextFactory(_context));
    }

    public void Dispose()
    {
        foreach (var id in _createdTrackIds)
        {
            _context.Scrobbles.Where(s => s.TrackId == id).ExecuteDelete();
        }
        _context.SaveChanges();
        _context.Dispose();
    }

    [Test]
    public async Task BulkInsertAsync_InsertsTracks()
    {
        var tracks = new List<Track>
        {
            new() { Id = 1000, ArtistId = 1, Title = "Test Track A", Duration = 240 },
            new() { Id = 1001, ArtistId = 1, Title = "Test Track B", Duration = 300 }
        };

        await _repository.BulkInsertAsync(tracks);
        _createdTrackIds.AddRange([1000, 1001]);

        var fetched = await _repository.GetByArtistIdAsync(1);
        fetched.Should().Contain(t => t.Title == "Test Track A");
        fetched.Should().Contain(t => t.Title == "Test Track B");
    }

    [Test]
    public async Task GetByTitleAndArtistAsync_ReturnsTrack()
    {
        var track = new Track { Id = 1002, ArtistId = 2, Title = "Unique Title", Duration = 200 };
        await _repository.BulkInsertAsync([track]);
        _createdTrackIds.Add(1002);

        var fetched = await _repository.GetByTitleAndArtistAsync("Unique Title", 2);
        fetched.Should().NotBeNull();
        fetched!.Title.Should().Be("Unique Title");
    }
}
```

### Step 2: Read-back

```powershell
Test-Path '/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\TrackRepositoryTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet test --filter "TrackRepositoryTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: FAIL — `TrackRepository` or `ITrackRepository` not found.

### Step 3.5: Assess

Confirmed. Proceed.

### Step 4: Write minimal implementation

File: `/home/lance/Scripts/csharp/src\Data\Repositories\ITrackRepository.cs`

```csharp
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Repositories;

internal interface ITrackRepository
{
    Task BulkInsertAsync(IEnumerable<Track> tracks, CancellationToken ct = default);
    Task<Track?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Track>> GetByArtistIdAsync(int artistId, CancellationToken ct = default);
    Task<Track?> GetByTitleAndArtistAsync(string title, int artistId, CancellationToken ct = default);
}
```

File: `/home/lance/Scripts/csharp/src\Data\Repositories\TrackRepository.cs`

```csharp
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Repositories;

internal sealed class TrackRepository(IDbContextFactory<ScriptsDbContext> contextFactory) : ITrackRepository
{
    public async Task BulkInsertAsync(IEnumerable<Track> tracks, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        context.Tracks.AddRange(tracks);
        await context.SaveChangesAsync(ct);
    }

    public async Task<Track?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Tracks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<List<Track>> GetByArtistIdAsync(int artistId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Tracks
            .AsNoTracking()
            .Where(t => t.ArtistId == artistId)
            .ToListAsync(ct);
    }

    public async Task<Track?> GetByTitleAndArtistAsync(string title, int artistId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Tracks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Title == title && t.ArtistId == artistId, ct);
    }
}
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "TrackRepositoryTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `2 passed, 0 failed`

### Step 6: Commit

```powershell
git -C /home/lance/Scripts add csharp/src/Data/Repositories/ITrackRepository.cs
git -C /home/lance/Scripts add csharp/src/Data/Repositories/TrackRepository.cs
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/Repositories/TrackRepositoryTests.cs
git -C /home/lance/Scripts commit -m "feat(t1-06): add ITrackRepository + TrackRepository"
```

---

## Task 4: ArtistRepository + AlbumRepository

**Files:**
- Create: `/home/lance/Scripts/csharp/src\Data\Repositories\IArtistRepository.cs`
- Create: `/home/lance/Scripts/csharp/src\Data\Repositories\ArtistRepository.cs`
- Create: `/home/lance/Scripts/csharp/src\Data\Repositories\IAlbumRepository.cs`
- Create: `/home/lance/Scripts/csharp/src\Data\Repositories\AlbumRepository.cs`
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\ArtistRepositoryTests.cs`
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\AlbumRepositoryTests.cs`
- Create: `/home/lance/Scripts/csharp/src\Data\Repositories\RepositoryRegistration.cs`

### Step 0: Preflight

```powershell
Test-Path /home/lance/Scripts/csharp/src\Data\Repositories\IArtistRepository.cs
# Expected: False

Test-Path /home/lance/Scripts/csharp/src\Data\Repositories\IAlbumRepository.cs
# Expected: False
```

### Step 1: Write both failing tests

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\ArtistRepositoryTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Data.Repositories;

namespace Scripts.Tests.Repositories;

public sealed class ArtistRepositoryTests : IDisposable
{
    private readonly ScriptsDbContext _context;
    private readonly ArtistRepository _repository;

    public ArtistRepositoryTests()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")!;
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        _context = new ScriptsDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new ArtistRepository(new TestDbContextFactory(_context));
    }

    public void Dispose() => _context.Dispose();

    [Test]
    public async Task AddAsync_InsertsArtist()
    {
        var artist = new Artist { Name = "Test Artist " + Guid.NewGuid().ToString("N")[..8] };
        await _repository.AddAsync(artist);

        var fetched = await _repository.GetByNameAsync(artist.Name);
        fetched.Should().NotBeNull();
        fetched!.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetByNameAsync_ReturnsNull_WhenNotFound()
    {
        var fetched = await _repository.GetByNameAsync("NonExistentArtist" + Guid.NewGuid());
        fetched.Should().BeNull();
    }
}
```

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\AlbumRepositoryTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Data.Repositories;

namespace Scripts.Tests.Repositories;

public sealed class AlbumRepositoryTests : IDisposable
{
    private readonly ScriptsDbContext _context;
    private readonly AlbumRepository _repository;

    public AlbumRepositoryTests()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")!;
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        _context = new ScriptsDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new AlbumRepository(new TestDbContextFactory(_context));

        var artist = _context.Artists.FirstOrDefault(a => a.Name == "__test_artist__");
        if (artist is null)
        {
            artist = new Artist { Name = "__test_artist__" };
            _context.Artists.Add(artist);
            _context.SaveChanges();
        }
    }

    public void Dispose() => _context.Dispose();

    [Test]
    public async Task AddAsync_InsertsAlbum()
    {
        var artist = _context.Artists.First(a => a.Name == "__test_artist__");
        var album = new Album { ArtistId = artist.Id, Title = "Test Album " + Guid.NewGuid().ToString("N")[..8] };

        await _repository.AddAsync(album);

        var fetched = await _repository.GetByArtistAndTitleAsync(artist.Id, album.Title);
        fetched.Should().NotBeNull();
        fetched!.Title.Should().Be(album.Title);
    }

    [Test]
    public async Task GetByArtistIdAsync_ReturnsAlbums()
    {
        var artist = _context.Artists.First(a => a.Name == "__test_artist__");
        var albums = await _repository.GetByArtistIdAsync(artist.Id);

        albums.Should().NotBeNull();
    }
}
```

### Step 2: Read-back

```powershell
Test-Path '/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\ArtistRepositoryTests.cs'
# Expected: True
Test-Path '/home/lance/Scripts/csharp/tests\Scripts.Tests\Repositories\AlbumRepositoryTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet test --filter "ArtistRepositoryTests|AlbumRepositoryTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: FAIL — interfaces and implementations not found.

### Step 3.5: Assess

Confirmed. Proceed.

### Step 4: Write minimal implementations

File: `/home/lance/Scripts/csharp/src\Data\Repositories\IArtistRepository.cs`

```csharp
using CSharpScripts.Data.Entities;
using System.Text.Json;

namespace CSharpScripts.Data.Repositories;

internal interface IArtistRepository
{
    Task<Artist?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Artist?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Artist artist, CancellationToken ct = default);
    Task<int> UpsertMetadataAsync(int id, JsonDocument metadata, CancellationToken ct = default);
}
```

File: `/home/lance/Scripts/csharp/src\Data\Repositories\ArtistRepository.cs`

```csharp
using CSharpScripts.Data.Entities;
using System.Text.Json;

namespace CSharpScripts.Data.Repositories;

internal sealed class ArtistRepository(IDbContextFactory<ScriptsDbContext> contextFactory) : IArtistRepository
{
    public async Task<Artist?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Name == name, ct);
    }

    public async Task<Artist?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task AddAsync(Artist artist, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        context.Artists.Add(artist);
        await context.SaveChangesAsync(ct);
    }

    public async Task<int> UpsertMetadataAsync(int id, JsonDocument metadata, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Artists
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(
                artist => artist.SetProperty(a => a.Metadata, x => metadata),
                cancellationToken: ct);
    }
}
```

File: `/home/lance/Scripts/csharp/src\Data\Repositories\IAlbumRepository.cs`

```csharp
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Repositories;

internal interface IAlbumRepository
{
    Task<Album?> GetByArtistAndTitleAsync(int artistId, string title, CancellationToken ct = default);
    Task<Album?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Album>> GetByArtistIdAsync(int artistId, CancellationToken ct = default);
    Task AddAsync(Album album, CancellationToken ct = default);
}
```

File: `/home/lance/Scripts/csharp/src\Data\Repositories\AlbumRepository.cs`

```csharp
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Repositories;

internal sealed class AlbumRepository(IDbContextFactory<ScriptsDbContext> contextFactory) : IAlbumRepository
{
    public async Task<Album?> GetByArtistAndTitleAsync(int artistId, string title, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Albums
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ArtistId == artistId && a.Title == title, ct);
    }

    public async Task<Album?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Albums
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<List<Album>> GetByArtistIdAsync(int artistId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Albums
            .AsNoTracking()
            .Where(a => a.ArtistId == artistId)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Album album, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        context.Albums.Add(album);
        await context.SaveChangesAsync(ct);
    }
}
```

File: `/home/lance/Scripts/csharp/src\Data\Repositories\RepositoryRegistration.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace CSharpScripts.Data.Repositories;

internal static class RepositoryRegistration
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IScrobbleRepository, ScrobbleRepository>();
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddScoped<ITrackRepository, TrackRepository>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IAlbumRepository, AlbumRepository>();
        return services;
    }
}
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "ArtistRepositoryTests|AlbumRepositoryTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `4 passed, 0 failed`

### Step 6: Commit

```powershell
git -C /home/lance/Scripts add csharp/src/Data/Repositories/IArtistRepository.cs
git -C /home/lance/Scripts add csharp/src/Data/Repositories/ArtistRepository.cs
git -C /home/lance/Scripts add csharp/src/Data/Repositories/IAlbumRepository.cs
git -C /home/lance/Scripts add csharp/src/Data/Repositories/AlbumRepository.cs
git -C /home/lance/Scripts add csharp/src/Data/Repositories/RepositoryRegistration.cs
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/Repositories/ArtistRepositoryTests.cs
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/Repositories/AlbumRepositoryTests.cs
git -C /home/lance/Scripts commit -m "feat(t1-06): add Artist/Album repositories + DI registration"
```

---

## Final Verification

```powershell
# Run all repository tests
dotnet test --filter "Scripts.Tests.Repositories" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected:
```
Passed ScrobbleRepositoryTests (3 tests)
Passed VideoRepositoryTests (2 tests)
Passed TrackRepositoryTests (2 tests)
Passed ArtistRepositoryTests (2 tests)
Passed AlbumRepositoryTests (2 tests)
11 passed, 0 failed
```

**→ Proceed to `07-state-manager.md`**

---

## Research Provenance

<!-- from research/DATA-ACCESS-REPOSITORIES-consolidated.md -->

Source: `AI/plans/research/DATA-ACCESS-REPOSITORIES-consolidated.md` (consolidated 2026-06-01; dir deleted)

Content already covered: 7 repository pairs, `IDbContextFactory<ScriptsDbContext>` constructor, `ExecuteUpdateAsync`/`ExecuteDeleteAsync` preference, DI registration. LastFmService duplicate deletion is in `09-sync-service-updates.md`.

### PostgresService Mutation Patterns (research §1.2)

| Operation            | Pattern              | File                    | Status                                         |
| -------------------- | -------------------- | ----------------------- | ---------------------------------------------- |
| `ExecuteUpdateAsync` | Single-entity upsert | `PostgresService.cs:20` | ✅ Correct                                      |
| `SaveChangesAsync`   | Bulk insert          | `PostgresService.cs:39` | ⚠️ Keep — bulk `AddRange` is correct pattern  |
| `ExecuteDeleteAsync` | Bulk delete          | —                       | ❌ Never used (introduced via repos in this plan) |

### ILike / EF.Functions.Like Future Use (research §3.1)

`EF.Functions.ILike` / `EF.Functions.Like` not referenced anywhere in codebase. Greenfield capability — for future lookups:

| Entity   | String Field | Query Pattern                             | Current DB Index                      |
| -------- | ------------ | ----------------------------------------- | ------------------------------------- |
| `Artist` | `Name`       | Lookup by name before insert              | `idx_artists_name` (unique)           |
| `Track`  | `Title`      | Lookup by title + artist_id before insert | `idx_tracks_title`                    |
| `Album`  | `Title`      | Lookup by title + artist_id before insert | `idx_albums_title` (unique composite) |

Adding `ILike` is scoped to `09-sync-service-updates.md` Task 3.
