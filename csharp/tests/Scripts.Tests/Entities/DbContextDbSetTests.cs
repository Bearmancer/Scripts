using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class DbContextDbSetTests
{
	private static ScriptsDbContext CreateContext()
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
