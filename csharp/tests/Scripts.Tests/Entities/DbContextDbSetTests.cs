using Microsoft.EntityFrameworkCore;
using Scripts.Data;

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
	public async Task DbContext_HasArtists_DbSet()
	{
		using var context = CreateContext();
		await Assert.That(context.Artists).IsNotNull();
	}

	[Test]
	public async Task DbContext_HasAlbums_DbSet()
	{
		using var context = CreateContext();
		await Assert.That(context.Albums).IsNotNull();
	}

	[Test]
	public async Task DbContext_HasTracks_DbSet()
	{
		using var context = CreateContext();
		await Assert.That(context.Tracks).IsNotNull();
	}

	[Test]
	public async Task DbContext_HasScrobbles_DbSet()
	{
		using var context = CreateContext();
		await Assert.That(context.Scrobbles).IsNotNull();
	}

	[Test]
	public async Task DbContext_HasVideos_DbSet()
	{
		using var context = CreateContext();
		await Assert.That(context.Videos).IsNotNull();
	}

}
