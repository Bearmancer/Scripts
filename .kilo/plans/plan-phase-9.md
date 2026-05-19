# Phase 9: Sync Service Updates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement robust Postgres synchronization methods in `PostgresService.cs` with normalization, ILike queries, `ExecuteUpdateAsync` for video upserts, and `ExecuteDeleteAsync` for scrobble deletion.

**Architecture:** Add upsert and deletion methods to `PostgresService.cs` and write unit tests in a new test file `PostgresServiceTests.cs` using SQLite or InMemory DB.

**Tech Stack:** C#, EF Core, xUnit, Postgres

---

### Task 9.1: Implement Artist, Album, and Track Upserts with ILike and Normalization

**Files:**
- Modify: `csharp/src/Services/PostgresService.cs`

- [ ] **Step 1: Implement lookup and upsert methods in `PostgresService.cs`**

**Pre-modification code chunk for `csharp/src/Services/PostgresService.cs`:**
```csharp
namespace CSharpScripts.Services;

internal sealed class PostgresService(IDbContextFactory<ScriptsDbContext> contextFactory)
{
	internal async Task UpsertScrobbleAsync(
			long id,
			int trackId,
			DateTimeOffset timestamp,
			string platform,
			CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);

		await context.Scrobbles
				.ExecuteUpdateAsync(
						scrobble => scrobble
								.SetProperty(s => s.TrackId, trackId)
								.SetProperty(s => s.ScrobbledAt, timestamp)
								.SetProperty(s => s.Platform, platform),
						ct);
	}

	internal async Task BulkInsertTracksAsync(
			IEnumerable<Data.Entities.Track> tracks,
			CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);

		context.Tracks.AddRange(tracks);
		await context.SaveChangesAsync(ct);
	}
}
```

**Post-modification code chunk for `csharp/src/Services/PostgresService.cs` (adding normalization, ILike queries, and entities upsert):**
```csharp
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CSharpScripts.Services;

internal sealed class PostgresService(IDbContextFactory<ScriptsDbContext> contextFactory)
{
	internal async Task<Artist> UpsertArtistAsync(string name, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		var normalized = TextNormalizer.ToStorageKey(name);

		var artist = await context.Artists.FirstOrDefaultAsync(
			a => EF.Functions.ILike(a.Name, normalized), ct);

		if (artist == null)
		{
			artist = new Artist { Name = name };
			context.Artists.Add(artist);
			await context.SaveChangesAsync(ct);
		}
		return artist;
	}

	internal async Task<Album> UpsertAlbumAsync(int artistId, string title, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		var normalized = TextNormalizer.ToStorageKey(title);

		var album = await context.Albums.FirstOrDefaultAsync(
			a => a.ArtistId == artistId && EF.Functions.ILike(a.Title, normalized), ct);

		if (album == null)
		{
			album = new Album { ArtistId = artistId, Title = title };
			context.Albums.Add(album);
			await context.SaveChangesAsync(ct);
		}
		return album;
	}

	internal async Task<Track> UpsertTrackAsync(int artistId, int? albumId, string title, int? duration, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		var normalized = TextNormalizer.ToStorageKey(title);

		var track = await context.Tracks.FirstOrDefaultAsync(
			t => t.ArtistId == artistId && t.AlbumId == albumId && EF.Functions.ILike(t.Title, normalized), ct);

		if (track == null)
		{
			track = new Track { ArtistId = artistId, AlbumId = albumId, Title = title, Duration = duration };
			context.Tracks.Add(track);
			await context.SaveChangesAsync(ct);
		}
		return track;
	}

	internal async Task UpsertScrobbleAsync(
			long id,
			int trackId,
			DateTimeOffset timestamp,
			CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);

		var exists = await context.Scrobbles.AnyAsync(s => s.Id == id, ct);
		if (!exists)
		{
			var scrobble = new Scrobble { Id = id, TrackId = trackId, ScrobbledAt = timestamp };
			context.Scrobbles.Add(scrobble);
			await context.SaveChangesAsync(ct);
		}
	}

	internal async Task BulkInsertTracksAsync(
			IEnumerable<Data.Entities.Track> tracks,
			CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);

		context.Tracks.AddRange(tracks);
		await context.SaveChangesAsync(ct);
	}
}
```

- [ ] **Step 2: Commit**

```bash
git add csharp/src/Services/PostgresService.cs
git commit -m "feat: implement normalization and ILike lookup in PostgresService"
```

---

### Task 9.2: Implement Video Sync with ExecuteUpdateAsync and deletion with ExecuteDeleteAsync

**Files:**
- Modify: `csharp/src/Services/PostgresService.cs`

- [ ] **Step 1: Implement Video Upsert and Scrobble Deletion in `PostgresService.cs`**

**Pre-modification code chunk for `csharp/src/Services/PostgresService.cs`:**
```csharp
	internal async Task BulkInsertTracksAsync(
			IEnumerable<Data.Entities.Track> tracks,
			CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);

		context.Tracks.AddRange(tracks);
		await context.SaveChangesAsync(ct);
	}
}
```

**Post-modification code chunk for `csharp/src/Services/PostgresService.cs` (adding Video sync and Scrobble deletion):**
```csharp
	internal async Task BulkInsertTracksAsync(
			IEnumerable<Data.Entities.Track> tracks,
			CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);

		context.Tracks.AddRange(tracks);
		await context.SaveChangesAsync(ct);
	}

	internal async Task UpsertVideoAsync(Video video, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);

		var existing = await context.Videos.FirstOrDefaultAsync(v => v.Url == video.Url, ct);
		if (existing != null)
		{
			await context.Videos.Where(v => v.Id == existing.Id).ExecuteUpdateAsync(
				setters => setters
					.SetProperty(v => v.Title, video.Title)
					.SetProperty(v => v.Description, video.Description)
					.SetProperty(v => v.ChannelName, video.ChannelName)
					.SetProperty(v => v.SyncedAt, video.SyncedAt),
				ct);
		}
		else
		{
			context.Videos.Add(video);
			await context.SaveChangesAsync(ct);
		}
	}

	internal async Task<int> ResyncFromAsync(DateTimeOffset fromDate, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Scrobbles.Where(s => s.ScrobbledAt >= fromDate).ExecuteDeleteAsync(ct);
	}
}
```

- [ ] **Step 2: Commit**

```bash
git add csharp/src/Services/PostgresService.cs
git commit -m "feat: implement video ExecuteUpdateAsync and scrobble ExecuteDeleteAsync in PostgresService"
```

---

### Task 9.3: Add PostgresService Unit Tests

**Files:**
- Create Test: `csharp/src/Tests/Services/PostgresServiceTests.cs`

- [ ] **Step 1: Write service tests**

Create `csharp/src/Tests/Services/PostgresServiceTests.cs` with the following content:
```csharp
using System;
using System.Threading.Tasks;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CSharpScripts.Tests.Services;

public class PostgresServiceTests
{
	private readonly IDbContextFactory<ScriptsDbContext> Factory;

	public PostgresServiceTests()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("test_postgres_service")
			.Options;

		var factoryMock = new MockDbContextFactory(options);
		Factory = factoryMock;
	}

	[Fact]
	public async Task UpsertArtistAsync_NormalizesAndFindsExisting()
	{
		var service = new PostgresService(Factory);
		var artist1 = await service.UpsertArtistAsync("Sigur Rós");
		var artist2 = await service.UpsertArtistAsync("SIGUR RÓS");

		Assert.Equal(artist1.Id, artist2.Id);
	}

	[Fact]
	public async Task ResyncFromAsync_DeletesCorrectScrobbles()
	{
		var service = new PostgresService(Factory);
		var artist = await service.UpsertArtistAsync("Radiohead");
		var track = await service.UpsertTrackAsync(artist.Id, null, "Creep", null);

		await service.UpsertScrobbleAsync(100L, track.Id, DateTimeOffset.UtcNow.AddDays(-5));
		await service.UpsertScrobbleAsync(101L, track.Id, DateTimeOffset.UtcNow.AddDays(-1));

		var deleted = await service.ResyncFromAsync(DateTimeOffset.UtcNow.AddDays(-2));
		Assert.Equal(1, deleted);
	}

	private class MockDbContextFactory(DbContextOptions<ScriptsDbContext> options) : IDbContextFactory<ScriptsDbContext>
	{
		public ScriptsDbContext CreateDbContext() => new(options);
	}
}
```

- [ ] **Step 2: Run PostgresService tests**

Run: `dotnet test --filter PostgresServiceTests`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add csharp/src/Tests/Services/PostgresServiceTests.cs
git commit -m "test: add PostgresService unit tests"
```
