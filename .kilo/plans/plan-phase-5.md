# Phase 5: Entity Refactoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove obsolete metadata and ID properties from the database entities to normalize the data domain.

**Architecture:** Write reflection-based assertions confirming properties are removed, then modify the entity record classes.

**Tech Stack:** C#, xUnit, EF Core

---

### Task 5.1: Artist — Remove `Mbid` property

**Files:**
- Modify: `csharp/src/Data/Entities/Artist.cs`
- Modify: `csharp/src/Tests/Data/EntityModelTests.cs`

- [ ] **Step 1: Write the failing test**

Add `Artist_DoesNotHave_Mbid` to `csharp/src/Tests/Data/EntityModelTests.cs`.

**Pre-modification code chunk for `csharp/src/Tests/Data/EntityModelTests.cs`:**
```csharp
		Assert.IsType<long>(scrobble.Id);
		Assert.IsType<DateTimeOffset>(scrobble.ScrobbledAt);
	}
}
```

**Post-modification code chunk for `csharp/src/Tests/Data/EntityModelTests.cs`:**
```csharp
		Assert.IsType<long>(scrobble.Id);
		Assert.IsType<DateTimeOffset>(scrobble.ScrobbledAt);
	}

	[Fact]
	public void Artist_DoesNotHave_Mbid()
	{
		Assert.Null(typeof(Artist).GetProperty("Mbid"));
	}
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter EntityModelTests.Artist_DoesNotHave_Mbid`
Expected: FAIL (property exists)

- [ ] **Step 3: Remove property from `Artist.cs`**

**Pre-modification code chunk for `csharp/src/Data/Entities/Artist.cs`:**
```csharp
internal sealed record Artist
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
	public string? Mbid { get; init; }
	public JsonDocument? Metadata { get; init; }

	public ICollection<Album> Albums { get; } = [];
```

**Post-modification code chunk for `csharp/src/Data/Entities/Artist.cs`:**
```csharp
internal sealed record Artist
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
	public JsonDocument? Metadata { get; init; }

	public ICollection<Album> Albums { get; } = [];
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter EntityModelTests.Artist_DoesNotHave_Mbid`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add csharp/src/Data/Entities/Artist.cs csharp/src/Tests/Data/EntityModelTests.cs
git commit -m "refactor: remove Mbid from Artist entity"
```

---

### Task 5.2: Artist — Remove `Metadata` property

**Files:**
- Modify: `csharp/src/Data/Entities/Artist.cs`
- Modify: `csharp/src/Tests/Data/EntityModelTests.cs`

- [ ] **Step 1: Write the failing test**

Add `Artist_DoesNotHave_Metadata` to `csharp/src/Tests/Data/EntityModelTests.cs`.

**Pre-modification code chunk for `csharp/src/Tests/Data/EntityModelTests.cs`:**
```csharp
	[Fact]
	public void Artist_DoesNotHave_Mbid()
	{
		Assert.Null(typeof(Artist).GetProperty("Mbid"));
	}
}
```

**Post-modification code chunk for `csharp/src/Tests/Data/EntityModelTests.cs`:**
```csharp
	[Fact]
	public void Artist_DoesNotHave_Mbid()
	{
		Assert.Null(typeof(Artist).GetProperty("Mbid"));
	}

	[Fact]
	public void Artist_DoesNotHave_Metadata()
	{
		Assert.Null(typeof(Artist).GetProperty("Metadata"));
	}
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter EntityModelTests.Artist_DoesNotHave_Metadata`
Expected: FAIL (property exists)

- [ ] **Step 3: Remove property from `Artist.cs`**

**Pre-modification code chunk for `csharp/src/Data/Entities/Artist.cs`:**
```csharp
internal sealed record Artist
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
	public JsonDocument? Metadata { get; init; }

	public ICollection<Album> Albums { get; } = [];
```

**Post-modification code chunk for `csharp/src/Data/Entities/Artist.cs`:**
```csharp
internal sealed record Artist
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;

	public ICollection<Album> Albums { get; } = [];
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter EntityModelTests.Artist_DoesNotHave_Metadata`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add csharp/src/Data/Entities/Artist.cs csharp/src/Tests/Data/EntityModelTests.cs
git commit -m "refactor: remove Metadata from Artist entity"
```

---

### Task 5.3: Album — Remove `ReleaseDate` property

**Files:**
- Modify: `csharp/src/Data/Entities/Album.cs`
- Modify: `csharp/src/Tests/Data/EntityModelTests.cs`

- [ ] **Step 1: Write the failing test**

Add `Album_DoesNotHave_ReleaseDate` to `csharp/src/Tests/Data/EntityModelTests.cs`.

**Pre-modification code chunk for `csharp/src/Tests/Data/EntityModelTests.cs`:**
```csharp
	[Fact]
	public void Artist_DoesNotHave_Metadata()
	{
		Assert.Null(typeof(Artist).GetProperty("Metadata"));
	}
}
```

**Post-modification code chunk for `csharp/src/Tests/Data/EntityModelTests.cs`:**
```csharp
	[Fact]
	public void Artist_DoesNotHave_Metadata()
	{
		Assert.Null(typeof(Artist).GetProperty("Metadata"));
	}

	[Fact]
	public void Album_DoesNotHave_ReleaseDate()
	{
		Assert.Null(typeof(Album).GetProperty("ReleaseDate"));
	}
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter EntityModelTests.Album_DoesNotHave_ReleaseDate`
Expected: FAIL (property exists)

- [ ] **Step 3: Remove property from `Album.cs`**

**Pre-modification code chunk for `csharp/src/Data/Entities/Album.cs`:**
```csharp
internal sealed record Album
{
	public int Id { get; init; }
	public int ArtistId { get; init; }
	public string Title { get; init; } = null!;
	public DateOnly? ReleaseDate { get; init; }
	public string? Mbid { get; init; }

	public Artist Artist { get; init; } = null!;
```

**Post-modification code chunk for `csharp/src/Data/Entities/Album.cs`:**
```csharp
internal sealed record Album
{
	public int Id { get; init; }
	public int ArtistId { get; init; }
	public string Title { get; init; } = null!;
	public string? Mbid { get; init; }

	public Artist Artist { get; init; } = null!;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter EntityModelTests.Album_DoesNotHave_ReleaseDate`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add csharp/src/Data/Entities/Album.cs csharp/src/Tests/Data/EntityModelTests.cs
git commit -m "refactor: remove ReleaseDate from Album entity"
```

---

### Task 5.4: Album — Remove `Mbid` property

**Files:**
- Modify: `csharp/src/Data/Entities/Album.cs`
- Modify: `csharp/src/Tests/Data/EntityModelTests.cs`

- [ ] **Step 1: Write the failing test**

Add `Album_DoesNotHave_Mbid` to `csharp/src/Tests/Data/EntityModelTests.cs`.

**Pre-modification code chunk for `csharp/src/Tests/Data/EntityModelTests.cs`:**
```csharp
	[Fact]
	public void Album_DoesNotHave_ReleaseDate()
	{
		Assert.Null(typeof(Album).GetProperty("ReleaseDate"));
	}
}
```

**Post-modification code chunk for `csharp/src/Tests/Data/EntityModelTests.cs`:**
```csharp
	[Fact]
	public void Album_DoesNotHave_ReleaseDate()
	{
		Assert.Null(typeof(Album).GetProperty("ReleaseDate"));
	}

	[Fact]
	public void Album_DoesNotHave_Mbid()
	{
		Assert.Null(typeof(Album).GetProperty("Mbid"));
	}
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter EntityModelTests.Album_DoesNotHave_Mbid`
Expected: FAIL (property exists)

- [ ] **Step 3: Remove property from `Album.cs`**

**Pre-modification code chunk for `csharp/src/Data/Entities/Album.cs`:**
```csharp
internal sealed record Album
{
	public int Id { get; init; }
	public int ArtistId { get; init; }
	public string Title { get; init; } = null!;
	public string? Mbid { get; init; }

	public Artist Artist { get; init; } = null!;
```

**Post-modification code chunk for `csharp/src/Data/Entities/Album.cs`:**
```csharp
internal sealed record Album
{
	public int Id { get; init; }
	public int ArtistId { get; init; }
	public string Title { get; init; } = null!;

	public Artist Artist { get; init; } = null!;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter EntityModelTests.Album_DoesNotHave_Mbid`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add csharp/src/Data/Entities/Album.cs csharp/src/Tests/Data/EntityModelTests.cs
git commit -m "refactor: remove Mbid from Album entity"
```

---

### Task 5.5: Track — Remove `Mbid` property

**Files:**
- Modify: `csharp/src/Data/Entities/Track.cs`
- Modify: `csharp/src/Tests/Data/EntityModelTests.cs`

- [ ] **Step 1: Write the failing test**

Add `Track_DoesNotHave_Mbid` to `csharp/src/Tests/Data/EntityModelTests.cs`.

**Pre-modification code chunk for `csharp/src/Tests/Data/EntityModelTests.cs`:**
```csharp
	[Fact]
	public void Album_DoesNotHave_Mbid()
	{
		Assert.Null(typeof(Album).GetProperty("Mbid"));
	}
}
```

**Post-modification code chunk for `csharp/src/Tests/Data/EntityModelTests.cs`:**
```csharp
	[Fact]
	public void Album_DoesNotHave_Mbid()
	{
		Assert.Null(typeof(Album).GetProperty("Mbid"));
	}

	[Fact]
	public void Track_DoesNotHave_Mbid()
	{
		Assert.Null(typeof(Track).GetProperty("Mbid"));
	}
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter EntityModelTests.Track_DoesNotHave_Mbid`
Expected: FAIL (property exists)

- [ ] **Step 3: Remove property from `Track.cs`**

**Pre-modification code chunk for `csharp/src/Data/Entities/Track.cs`:**
```csharp
internal sealed record Track
{
	public int Id { get; init; }
	public int ArtistId { get; init; }
	public int? AlbumId { get; init; }
	public string Title { get; init; } = null!;
	public int? Duration { get; init; }
	public string? Mbid { get; init; }

	public Artist Artist { get; init; } = null!;
```

**Post-modification code chunk for `csharp/src/Data/Entities/Track.cs`:**
```csharp
internal sealed record Track
{
	public int Id { get; init; }
	public int ArtistId { get; init; }
	public int? AlbumId { get; init; }
	public string Title { get; init; } = null!;
	public int? Duration { get; init; }

	public Artist Artist { get; init; } = null!;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter EntityModelTests.Track_DoesNotHave_Mbid`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add csharp/src/Data/Entities/Track.cs csharp/src/Tests/Data/EntityModelTests.cs
git commit -m "refactor: remove Mbid from Track entity"
```

---

### Task 5.6: Scrobble — Remove `Platform` property

**Files:**
- Modify: `csharp/src/Data/Entities/Scrobble.cs`
- Modify: `csharp/src/Tests/Data/EntityModelTests.cs`

- [ ] **Step 1: Write the failing test**

Add `Scrobble_DoesNotHave_Platform` to `csharp/src/Tests/Data/EntityModelTests.cs`.

**Pre-modification code chunk for `csharp/src/Tests/Data/EntityModelTests.cs`:**
```csharp
	[Fact]
	public void Track_DoesNotHave_Mbid()
	{
		Assert.Null(typeof(Track).GetProperty("Mbid"));
	}
}
```

**Post-modification code chunk for `csharp/src/Tests/Data/EntityModelTests.cs`:**
```csharp
	[Fact]
	public void Track_DoesNotHave_Mbid()
	{
		Assert.Null(typeof(Track).GetProperty("Mbid"));
	}

	[Fact]
	public void Scrobble_DoesNotHave_Platform()
	{
		Assert.Null(typeof(Scrobble).GetProperty("Platform"));
	}
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter EntityModelTests.Scrobble_DoesNotHave_Platform`
Expected: FAIL (property exists)

- [ ] **Step 3: Remove property from `Scrobble.cs`**

**Pre-modification code chunk for `csharp/src/Data/Entities/Scrobble.cs`:**
```csharp
internal sealed record Scrobble
{
	public long Id { get; init; }
	public int TrackId { get; init; }
	public DateTimeOffset ScrobbledAt { get; init; }
	public string Platform { get; init; } = null!;

	public Track Track { get; init; } = null!;
```

**Post-modification code chunk for `csharp/src/Data/Entities/Scrobble.cs`:**
```csharp
internal sealed record Scrobble
{
	public long Id { get; init; }
	public int TrackId { get; init; }
	public DateTimeOffset ScrobbledAt { get; init; }

	public Track Track { get; init; } = null!;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter EntityModelTests.Scrobble_DoesNotHave_Platform`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add csharp/src/Data/Entities/Scrobble.cs csharp/src/Tests/Data/EntityModelTests.cs
git commit -m "refactor: remove Platform from Scrobble entity"
```
