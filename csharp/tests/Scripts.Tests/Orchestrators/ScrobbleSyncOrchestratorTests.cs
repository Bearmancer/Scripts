using Microsoft.EntityFrameworkCore;
using Scripts.Data.Entities;
using Scripts.Services.Music;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.Orchestrators;

internal sealed class ScrobbleSyncOrchestratorTests : DatabaseTestBase
{
	[RequiresPgConnStr]
	[Test]
	public async Task ForceResync_PurgeOrphansAsync_RemovesEmptyAlbumsAndArtists()
	{
		await using var context = Fixture.GetContext();

		var artist = new Artist { Name = "Force Resync Artist" };
		context.Artists.Add(artist);

		var album = new Album { Artist = artist, Title = "Force Resync Album" };
		context.Albums.Add(album);

		await context.SaveChangesAsync();

		var factory = Fixture.GetContextFactory();
		var purgeService = new PurgeService(factory);

		var result = await purgeService.PurgeOrphansAsync();

		await Assert.That(result.AlbumsPurged).IsEqualTo(1);
		await Assert.That(result.ArtistsPurged).IsEqualTo(1);

		await using var verifyContext = Fixture.GetContext();
		await Assert.That(await verifyContext.Albums.AnyAsync()).IsFalse();
		await Assert.That(await verifyContext.Artists.AnyAsync()).IsFalse();
	}

	[RequiresPgConnStr]
	[Test]
	public async Task PurgeOrphansAsync_LeavesAlbumsWithTracksIntact()
	{
		await using var context = Fixture.GetContext();

		var artist = new Artist { Name = "Active Artist" };
		context.Artists.Add(artist);

		var album = new Album { Artist = artist, Title = "Active Album" };
		context.Albums.Add(album);

		context.Tracks.Add(
			new Track
			{
				Artist = artist,
				Album = album,
				Title = "Active Track",
			}
		);

		await context.SaveChangesAsync();

		var factory = Fixture.GetContextFactory();
		var purgeService = new PurgeService(factory);

		var result = await purgeService.PurgeOrphansAsync();

		await Assert.That(result.AlbumsPurged).IsEqualTo(0);
		await Assert.That(result.ArtistsPurged).IsEqualTo(0);

		await using var verifyContext = Fixture.GetContext();
		await Assert.That(await verifyContext.Albums.AnyAsync()).IsTrue();
		await Assert.That(await verifyContext.Artists.AnyAsync()).IsTrue();
	}
}
