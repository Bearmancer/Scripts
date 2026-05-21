# E2E Integration Testing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add full end-to-end integration tests for the scrobble-sync and YouTube-playlist pipelines using Testcontainers, validating the Last.fm → DB → read-back cycle with a real PostgreSQL 18 container.

**Architecture:** Each test spins up a `PostgresContainer` via `DatabaseFixture`, applies EF Core migrations in-process, and exercises repositories against an ephemeral database. No mocks — real SQL, real ORM, real schema. Tests are in `Scripts.Tests` and run with `dotnet test`.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions / Testcontainers

---

## Pre-flight

- [ ] **Step 0: Pre-flight validation**

```powershell
Get-Command pwsh   -ErrorAction Stop
Get-Command dotnet -ErrorAction Stop
Get-Command docker -ErrorAction Stop

# Verify Docker is running
docker info 2>&1 | Select-String "Server Version" | Should -Not -BeNullOrEmpty

dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx -ErrorAction Stop
```

Expected: Docker is running; restore succeeds.

---

## Task 1: Verify DatabaseFixture exists and supports migrations

**Files:**
- Verify: `csharp/tests/Scripts.Tests/Infrastructure/DatabaseFixture.cs`

- [ ] **Step 1: Write fixture verification test**

```csharp
// csharp/tests/Scripts.Tests/E2eTests/FixtureBootstrapTests.cs
using FluentAssertions;
using TUnit;
using Scripts.Tests.Infrastructure;

namespace Scripts.Tests.E2eTests;

public class FixtureBootstrapTests
{
    [Test]
    public async Task DatabaseFixture_InitializesSuccessfully()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();

        fixture.Context.Should().NotBeNull();
        // EF Core can reach the schema
        var canConnect = await fixture.Context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Read-back**

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\E2eTests\FixtureBootstrapTests.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run — confirm RED or GREEN**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "FixtureBootstrapTests" `
    --logger "console;verbosity=detailed" 2>&1
```

If `DatabaseFixture` does not exist yet: compile error (RED — expected). Proceed to Task 2 to implement it.
If it exists and passes: GREEN — skip Task 2 and continue to Task 3.

- [ ] **Step 3.5: State assessment**

Confirm whether `DatabaseFixture` exists at `csharp/tests/Scripts.Tests/Infrastructure/DatabaseFixture.cs`. If not, implement Task 2.

---

## Task 2: Implement DatabaseFixture (if missing from T1-15)

**Files:**
- Create: `csharp/tests/Scripts.Tests/Infrastructure/DatabaseFixture.cs`

- [ ] **Step 4: Write `DatabaseFixture.cs`**

```csharp
// csharp/tests/Scripts.Tests/Infrastructure/DatabaseFixture.cs
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using CSharpScripts.Data;

namespace Scripts.Tests.Infrastructure;

public sealed class DatabaseFixture : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("scripts_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public ScriptsDbContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        Context = new ScriptsDbContext(options);
        await Context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _container.DisposeAsync();
    }
}
```

- [ ] **Step 5: Read-back**

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure\DatabaseFixture.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 6: Run fixture bootstrap test — confirm GREEN**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "FixtureBootstrapTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: PASS.

---

## Task 3: Scrobble sync E2E test

**Files:**
- Create: `csharp/tests/Scripts.Tests/E2eTests/ScrobbleSyncE2eTests.cs`

- [ ] **Step 1: Write failing scrobble E2E test**

```csharp
// csharp/tests/Scripts.Tests/E2eTests/ScrobbleSyncE2eTests.cs
using FluentAssertions;
using TUnit;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using Scripts.Tests.Infrastructure;

namespace Scripts.Tests.E2eTests;

public class ScrobbleSyncE2eTests
{
    [Test]
    public async Task E2E_ScrobbleSync_InsertsAndReadsBack()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var context = fixture.Context;

        // Arrange — seed artist → album → track chain
        var artist = new Artist { Name = "Radiohead", Metadata = null };
        context.Artists.Add(artist);
        await context.SaveChangesAsync();

        var album = new Album
        {
            ArtistId = artist.Id,
            Title    = "OK Computer",
            ReleaseDate = new DateOnly(1997, 5, 21)
        };
        context.Albums.Add(album);
        await context.SaveChangesAsync();

        var track = new Track
        {
            AlbumId  = album.Id,
            ArtistId = artist.Id,
            Title    = "Karma Police",
            Duration = 263
        };
        context.Tracks.Add(track);
        await context.SaveChangesAsync();

        // Act — bulk-insert a scrobble via repository
        var repo = new ScrobbleRepository(context);
        var scrobble = new Scrobble
        {
            TrackId     = track.Id,
            ScrobbledAt = DateTimeOffset.UtcNow,
            Platform    = Platform.LastFm
        };
        var inserted = await repo.BulkInsertAsync([scrobble], CancellationToken.None);

        // Assert
        inserted.Should().Be(1);
        var latest = await repo.GetLatestAsync(CancellationToken.None);
        latest.Should().NotBeNull();
        latest!.Platform.Should().Be(Platform.LastFm);
    }

    [Test]
    public async Task E2E_ScrobbleSync_BulkInsert_DeduplicatesOnConflict()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var context = fixture.Context;

        var artist = new Artist { Name = "Portishead", Metadata = null };
        context.Artists.Add(artist);
        await context.SaveChangesAsync();

        var album = new Album { ArtistId = artist.Id, Title = "Dummy", ReleaseDate = new DateOnly(1994, 8, 22) };
        context.Albums.Add(album);
        await context.SaveChangesAsync();

        var track = new Track { AlbumId = album.Id, ArtistId = artist.Id, Title = "Sour Times", Duration = 255 };
        context.Tracks.Add(track);
        await context.SaveChangesAsync();

        var timestamp = DateTimeOffset.UtcNow;
        var repo = new ScrobbleRepository(context);
        var scrobble = new Scrobble { TrackId = track.Id, ScrobbledAt = timestamp, Platform = Platform.LastFm };

        // Insert once
        await repo.BulkInsertAsync([scrobble], CancellationToken.None);
        // Insert duplicate — must not throw, must not double-count
        var secondInsert = await repo.BulkInsertAsync([scrobble], CancellationToken.None);

        secondInsert.Should().Be(0, "duplicate scrobble must be ignored via ON CONFLICT DO NOTHING");
    }
}
```

- [ ] **Step 2: Read-back**

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\E2eTests\ScrobbleSyncE2eTests.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run — confirm RED**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "ScrobbleSyncE2eTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: fail — `ScrobbleRepository.BulkInsertAsync` or `GetLatestAsync` may be missing.

- [ ] **Step 3.5: State assessment**

Identify which method is missing. If both exist and tests merely fail on assertion, diagnose the failure message.

- [ ] **Step 4: Implement missing `ScrobbleRepository` methods (if required)**

Add to `csharp/src/Data/Repositories/ScrobbleRepository.cs`:

```csharp
public async Task<int> BulkInsertAsync(
    IEnumerable<Scrobble> scrobbles,
    CancellationToken ct = default)
{
    // Uses Npgsql ExecuteUpdateAsync-style bulk insert with ON CONFLICT DO NOTHING
    var rows = scrobbles.ToList();
    if (rows.Count == 0) return 0;

    return await _context.Scrobbles
        .UpsertRange(rows)
        .On(s => new { s.TrackId, s.ScrobbledAt, s.Platform })
        .NoUpdate()
        .RunAsync(ct);
}

public async Task<Scrobble?> GetLatestAsync(CancellationToken ct = default)
    => await _context.Scrobbles
        .OrderByDescending(s => s.ScrobbledAt)
        .FirstOrDefaultAsync(ct);
```

> **Note:** If `EFCore.BulkExtensions` is not in `Directory.Packages.props`, use `ExecuteInsertAsync` with raw SQL via `_context.Database.ExecuteSqlRawAsync` or insert via `AddRangeAsync` + `SaveChangesAsync` with an `ON CONFLICT DO NOTHING` hint. Adjust to match what's already in the codebase.

- [ ] **Step 5: Run — confirm GREEN**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "ScrobbleSyncE2eTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: both tests PASS.

---

## Task 4: YouTube playlist E2E test

**Files:**
- Create: `csharp/tests/Scripts.Tests/E2eTests/YouTubePlaylistE2eTests.cs`

- [ ] **Step 1: Write failing YouTube E2E test**

```csharp
// csharp/tests/Scripts.Tests/E2eTests/YouTubePlaylistE2eTests.cs
using FluentAssertions;
using TUnit;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using Scripts.Tests.Infrastructure;

namespace Scripts.Tests.E2eTests;

public class YouTubePlaylistE2eTests
{
    [Test]
    public async Task E2E_YouTubePlaylist_UpsertAndReadBack()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var context = fixture.Context;

        var videoRepo = new VideoRepository(context);
        var video = new Video
        {
            YoutubeId  = "dQw4w9WgXcQ",
            Title      = "Test Video",
            PlaylistId = "PL123",
            IsDeleted  = false
        };

        await videoRepo.UpsertAsync(video, CancellationToken.None);
        var loaded = await videoRepo.GetByYoutubeIdAsync("dQw4w9WgXcQ", CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Title.Should().Be("Test Video");
        loaded.IsDeleted.Should().BeFalse();
    }

    [Test]
    public async Task E2E_YouTubePlaylist_MarkDeleted_SetsFlag()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var context = fixture.Context;

        var videoRepo = new VideoRepository(context);
        var video = new Video
        {
            YoutubeId  = "abc123xyz",
            Title      = "Deletable Video",
            PlaylistId = "PL456",
            IsDeleted  = false
        };

        await videoRepo.UpsertAsync(video, CancellationToken.None);
        await videoRepo.MarkDeletedAsync("abc123xyz", CancellationToken.None);

        var deleted = await videoRepo.GetByYoutubeIdAsync("abc123xyz", CancellationToken.None);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task E2E_YouTubePlaylist_Upsert_UpdatesExistingTitle()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var context = fixture.Context;

        var videoRepo = new VideoRepository(context);
        var video = new Video
        {
            YoutubeId  = "update-test-id",
            Title      = "Original Title",
            PlaylistId = "PL789",
            IsDeleted  = false
        };

        await videoRepo.UpsertAsync(video, CancellationToken.None);

        var updated = video with { Title = "Updated Title" };
        await videoRepo.UpsertAsync(updated, CancellationToken.None);

        var result = await videoRepo.GetByYoutubeIdAsync("update-test-id", CancellationToken.None);
        result!.Title.Should().Be("Updated Title");
    }
}
```

- [ ] **Step 2: Read-back**

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\E2eTests\YouTubePlaylistE2eTests.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run — confirm RED**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "YouTubePlaylistE2eTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: fail — `VideoRepository.UpsertAsync`, `GetByYoutubeIdAsync`, or `MarkDeletedAsync` may be missing.

- [ ] **Step 4: Implement missing `VideoRepository` methods (if required)**

Add to `csharp/src/Data/Repositories/VideoRepository.cs`:

```csharp
public async Task UpsertAsync(Video video, CancellationToken ct = default)
{
    await _context.Videos
        .Where(v => v.YoutubeId == video.YoutubeId)
        .ExecuteDeleteAsync(ct);

    _context.Videos.Add(video);
    await _context.SaveChangesAsync(ct);
}

public async Task<Video?> GetByYoutubeIdAsync(string youtubeId, CancellationToken ct = default)
    => await _context.Videos
        .AsNoTracking()
        .FirstOrDefaultAsync(v => v.YoutubeId == youtubeId, ct);

public async Task MarkDeletedAsync(string youtubeId, CancellationToken ct = default)
    => await _context.Videos
        .Where(v => v.YoutubeId == youtubeId)
        .ExecuteUpdateAsync(s => s.SetProperty(v => v.IsDeleted, true), ct);
```

- [ ] **Step 5: Run — confirm GREEN**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "YouTubePlaylistE2eTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: all 3 tests PASS.

- [ ] **Step 6: Full test suite — no regressions**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --logger "console;verbosity=normal" 2>&1
```

Expected: all tests PASS (no regressions).

- [ ] **Step 7: Commit**

```powershell
git -C C:\Users\Lance\Dev\Scripts add `
    csharp/tests/Scripts.Tests/E2eTests/ `
    csharp/tests/Scripts.Tests/Infrastructure/DatabaseFixture.cs `
    csharp/src/Data/Repositories/ScrobbleRepository.cs `
    csharp/src/Data/Repositories/VideoRepository.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t4-01): add E2E Testcontainers tests for scrobble sync and YouTube playlist"
```

---

## Acceptance Criteria

- [ ] `DatabaseFixture` exists and spins up a `postgres:18-alpine` container
- [ ] `E2E_ScrobbleSync_InsertsAndReadsBack` passes
- [ ] `E2E_ScrobbleSync_BulkInsert_DeduplicatesOnConflict` passes
- [ ] `E2E_YouTubePlaylist_UpsertAndReadBack` passes
- [ ] `E2E_YouTubePlaylist_MarkDeleted_SetsFlag` passes
- [ ] `E2E_YouTubePlaylist_Upsert_UpdatesExistingTitle` passes
- [ ] No mocks — all tests use a real PostgreSQL 18 container
- [ ] Full test suite still passes
