# Phase 7: Entity Configurations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Verify and assert the EF Core database entity configurations (naming, primary keys, identity always generation, and indexing).

**Architecture:** Add xUnit tests that inspect the EF Core model metadata created by `ScriptsDbContext` to verify configuration rules.

**Tech Stack:** C#, xUnit, EF Core

---

### Task 7.1: Add Entity Configuration Validation Tests

**Files:**
- Create Test: `csharp/src/Tests/Data/EntityConfigurationTests.cs`

- [ ] **Step 1: Write the test code**

Create `csharp/src/Tests/Data/EntityConfigurationTests.cs` with the following content:
```csharp
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace CSharpScripts.Tests.Data;

public class EntityConfigurationTests
{
	private IModel GetModel()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("test_configs")
			.Options;
		using var ctx = new ScriptsDbContext(options);
		return ctx.Model;
	}

	[Fact]
	public void ArtistConfiguration_IsCorrect()
	{
		var model = GetModel();
		var entity = model.FindEntityType(typeof(Artist));
		Assert.NotNull(entity);
		Assert.Equal("artists", entity.GetTableName());

		var id = entity.FindProperty(nameof(Artist.Id));
		Assert.NotNull(id);
		Assert.True(id.IsPrimaryKey());
		Assert.Equal(ValueGenerated.OnAdd, id.ValueGenerated);

		var index = entity.FindIndex(entity.FindProperty(nameof(Artist.Name))!);
		Assert.NotNull(index);
		Assert.True(index.IsUnique);
	}

	[Fact]
	public void AlbumConfiguration_IsCorrect()
	{
		var model = GetModel();
		var entity = model.FindEntityType(typeof(Album));
		Assert.NotNull(entity);
		Assert.Equal("albums", entity.GetTableName());

		var id = entity.FindProperty(nameof(Album.Id));
		Assert.NotNull(id);
		Assert.True(id.IsPrimaryKey());

		var artistIndex = entity.FindIndex(entity.FindProperty(nameof(Album.ArtistId))!);
		Assert.NotNull(artistIndex);

		var titleProp = entity.FindProperty(nameof(Album.Title))!;
		var artistIdProp = entity.FindProperty(nameof(Album.ArtistId))!;
		var compositeIndex = entity.FindIndex(new[] { artistIdProp, titleProp });
		Assert.NotNull(compositeIndex);
		Assert.True(compositeIndex.IsUnique);
	}

	[Fact]
	public void TrackConfiguration_IsCorrect()
	{
		var model = GetModel();
		var entity = model.FindEntityType(typeof(Track));
		Assert.NotNull(entity);
		Assert.Equal("tracks", entity.GetTableName());

		var id = entity.FindProperty(nameof(Track.Id));
		Assert.NotNull(id);
		Assert.True(id.IsPrimaryKey());

		var artistIndex = entity.FindIndex(entity.FindProperty(nameof(Track.ArtistId))!);
		Assert.NotNull(artistIndex);

		var albumIndex = entity.FindIndex(entity.FindProperty(nameof(Track.AlbumId))!);
		Assert.NotNull(albumIndex);
	}

	[Fact]
	public void ScrobbleConfiguration_IsCorrect()
	{
		var model = GetModel();
		var entity = model.FindEntityType(typeof(Scrobble));
		Assert.NotNull(entity);
		Assert.Equal("scrobbles", entity.GetTableName());

		var id = entity.FindProperty(nameof(Scrobble.Id));
		Assert.NotNull(id);
		Assert.True(id.IsPrimaryKey());

		var trackIdProp = entity.FindProperty(nameof(Scrobble.TrackId))!;
		var scrobbledAtProp = entity.FindProperty(nameof(Scrobble.ScrobbledAt))!;
		var index = entity.FindIndex(new[] { trackIdProp, scrobbledAtProp });
		Assert.NotNull(index);
		Assert.True(index.IsUnique);
	}

	[Fact]
	public void VideoConfiguration_IsCorrect()
	{
		var model = GetModel();
		var entity = model.FindEntityType(typeof(Video));
		Assert.NotNull(entity);
		Assert.Equal("videos", entity.GetTableName());

		var id = entity.FindProperty(nameof(Video.Id));
		Assert.NotNull(id);
		Assert.True(id.IsPrimaryKey());

		var urlIndex = entity.FindIndex(entity.FindProperty(nameof(Video.Url))!);
		Assert.NotNull(urlIndex);
		Assert.True(urlIndex.IsUnique);

		var channelIndex = entity.FindIndex(entity.FindProperty(nameof(Video.ChannelName))!);
		Assert.NotNull(channelIndex);

		var uploadIndex = entity.FindIndex(entity.FindProperty(nameof(Video.UploadDate))!);
		Assert.NotNull(uploadIndex);
		
		var metadata = entity.FindProperty(nameof(Video.Metadata));
		Assert.NotNull(metadata);
	}
}
```

- [ ] **Step 2: Run the configuration validation tests**

Run: `dotnet test --filter EntityConfigurationTests`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add csharp/src/Tests/Data/EntityConfigurationTests.cs
git commit -m "test: add configuration verification tests for all entities"
```
