# Phase 10: EF11 Query Upgrades Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement advanced querying patterns using EF11 `MaxByAsync`, PG-specific `JsonTypeof` searches, and `TrigramsSimilarity` for fuzzy artist lookup.

**Architecture:** Implement helper query methods in `PostgresService.cs` and add integration tests in `PostgresServiceTests.cs`.

**Tech Stack:** C#, EF Core 11, Postgres / SQLite

---

### Task 10.1: Last Played — Implement GetLastPlayedScrobbleAsync using MaxByAsync

**Files:**
- Modify: `csharp/src/Services/PostgresService.cs`

- [ ] **Step 1: Implement the query method**

**Pre-modification code chunk for `csharp/src/Services/PostgresService.cs`:**
```csharp
	internal async Task<int> ResyncFromAsync(DateTimeOffset fromDate, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Scrobbles.Where(s => s.ScrobbledAt >= fromDate).ExecuteDeleteAsync(ct);
	}
}
```

**Post-modification code chunk for `csharp/src/Services/PostgresService.cs`:**
```csharp
	internal async Task<int> ResyncFromAsync(DateTimeOffset fromDate, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Scrobbles.Where(s => s.ScrobbledAt >= fromDate).ExecuteDeleteAsync(ct);
	}

	internal async Task<Scrobble?> GetLastPlayedScrobbleAsync(CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Scrobbles.MaxByAsync(s => s.ScrobbledAt, ct);
	}
}
```

- [ ] **Step 2: Commit**

```bash
git add csharp/src/Services/PostgresService.cs
git commit -m "feat: implement GetLastPlayedScrobbleAsync using MaxByAsync"
```

---

### Task 10.2: Most Played — Implement GetMostPlayedTrackAsync using MaxByAsync

**Files:**
- Modify: `csharp/src/Services/PostgresService.cs`

- [ ] **Step 1: Implement the query method**

**Pre-modification code chunk for `csharp/src/Services/PostgresService.cs`:**
```csharp
	internal async Task<Scrobble?> GetLastPlayedScrobbleAsync(CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Scrobbles.MaxByAsync(s => s.ScrobbledAt, ct);
	}
}
```

**Post-modification code chunk for `csharp/src/Services/PostgresService.cs`:**
```csharp
	internal async Task<Scrobble?> GetLastPlayedScrobbleAsync(CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Scrobbles.MaxByAsync(s => s.ScrobbledAt, ct);
	}

	internal async Task<Track?> GetMostPlayedTrackAsync(int artistId, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Tracks.Where(t => t.ArtistId == artistId).MaxByAsync(t => t.Scrobbles.Count, ct);
	}
}
```

- [ ] **Step 2: Commit**

```bash
git add csharp/src/Services/PostgresService.cs
git commit -m "feat: implement GetMostPlayedTrackAsync using MaxByAsync"
```

---

### Task 10.3: Video Metadata Search — Implement FindVideoByMetadataKeyAsync using JsonTypeof

**Files:**
- Modify: `csharp/src/Services/PostgresService.cs`

- [ ] **Step 1: Implement the query method**

**Pre-modification code chunk for `csharp/src/Services/PostgresService.cs`:**
```csharp
	internal async Task<Track?> GetMostPlayedTrackAsync(int artistId, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Tracks.Where(t => t.ArtistId == artistId).MaxByAsync(t => t.Scrobbles.Count, ct);
	}
}
```

**Post-modification code chunk for `csharp/src/Services/PostgresService.cs`:**
```csharp
	internal async Task<Track?> GetMostPlayedTrackAsync(int artistId, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Tracks.Where(t => t.ArtistId == artistId).MaxByAsync(t => t.Scrobbles.Count, ct);
	}

	internal async Task<List<Video>> FindVideosByMetadataKeyAsync(string key, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Videos.Where(v => EF.Functions.JsonTypeof(v.Metadata, $"$.{key}") != null).ToListAsync(ct);
	}
}
```

- [ ] **Step 2: Commit**

```bash
git add csharp/src/Services/PostgresService.cs
git commit -m "feat: implement FindVideosByMetadataKeyAsync using JsonTypeof"
```

---

### Task 10.4: Search — Implement SearchArtistsFuzzyAsync using TrigramsSimilarity

**Files:**
- Modify: `csharp/src/Services/PostgresService.cs`

- [ ] **Step 1: Implement the query method**

**Pre-modification code chunk for `csharp/src/Services/PostgresService.cs`:**
```csharp
	internal async Task<List<Video>> FindVideosByMetadataKeyAsync(string key, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Videos.Where(v => EF.Functions.JsonTypeof(v.Metadata, $"$.{key}") != null).ToListAsync(ct);
	}
}
```

**Post-modification code chunk for `csharp/src/Services/PostgresService.cs`:**
```csharp
	internal async Task<List<Video>> FindVideosByMetadataKeyAsync(string key, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Videos.Where(v => EF.Functions.JsonTypeof(v.Metadata, $"$.{key}") != null).ToListAsync(ct);
	}

	internal async Task<List<Artist>> SearchArtistsFuzzyAsync(string term, CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Artists
			.Where(a => EF.Functions.TrigramsSimilarity(a.Name, term) > 0.3)
			.OrderByDescending(a => EF.Functions.TrigramsSimilarity(a.Name, term))
			.ToListAsync(ct);
	}
}
```

- [ ] **Step 2: Commit**

```bash
git add csharp/src/Services/PostgresService.cs
git commit -m "feat: implement SearchArtistsFuzzyAsync using TrigramsSimilarity"
```

---

### Task 10.5: Add Query Upgrade Tests

**Files:**
- Modify: `csharp/src/Tests/Services/PostgresServiceTests.cs`

- [ ] **Step 1: Add tests**

**Pre-modification code chunk for `csharp/src/Tests/Services/PostgresServiceTests.cs`:**
```csharp
		var deleted = await service.ResyncFromAsync(DateTimeOffset.UtcNow.AddDays(-2));
		Assert.Equal(1, deleted);
	}

	private class MockDbContextFactory(DbContextOptions<ScriptsDbContext> options) : IDbContextFactory<ScriptsDbContext>
	{
		public ScriptsDbContext CreateDbContext() => new(options);
	}
}
```

**Post-modification code chunk for `csharp/src/Tests/Services/PostgresServiceTests.cs` (adding query verification tests):**
```csharp
		var deleted = await service.ResyncFromAsync(DateTimeOffset.UtcNow.AddDays(-2));
		Assert.Equal(1, deleted);
	}

	[Fact]
	public async Task GetLastPlayedScrobbleAsync_ReturnsMaxScrobbledAt()
	{
		var service = new PostgresService(Factory);
		var artist = await service.UpsertArtistAsync("Pink Floyd");
		var track = await service.UpsertTrackAsync(artist.Id, null, "Time", null);

		var now = DateTimeOffset.UtcNow;
		await service.UpsertScrobbleAsync(200L, track.Id, now.AddHours(-1));
		await service.UpsertScrobbleAsync(201L, track.Id, now);

		var last = await service.GetLastPlayedScrobbleAsync();
		Assert.NotNull(last);
		Assert.Equal(201L, last.Id);
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
git commit -m "test: add query upgrades verification tests"
```
