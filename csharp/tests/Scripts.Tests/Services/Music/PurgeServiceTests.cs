using Microsoft.EntityFrameworkCore;
using Scripts.Data.Entities;
using Scripts.Services.Music;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.Services.Music;

internal sealed class PurgeServiceTests : DatabaseTestBase
{
	[RequiresPgConnStr]
	[Test]
	public async Task PurgeOrphansAsync_PurgesEmptyAlbumsAndArtists()
	{
		await using var context = Fixture.GetContext();

		var artist = new Artist { Name = "Orphan Artist" };
		context.Artists.Add(artist);

		var album = new Album { Artist = artist, Title = "Orphan Album" };
		context.Albums.Add(album);

		await context.SaveChangesAsync();

		var factory = Fixture.GetContextFactory();
		var service = new PurgeService(factory);

		var result = await service.PurgeOrphansAsync();

		await Assert.That(result.AlbumsPurged).IsEqualTo(1);
		await Assert.That(result.ArtistsPurged).IsEqualTo(1);

		await using var verifyContext = Fixture.GetContext();
		await Assert.That(await verifyContext.Albums.AnyAsync()).IsFalse();
		await Assert.That(await verifyContext.Artists.AnyAsync()).IsFalse();
	}

	[RequiresPgConnStr]
	[Test]
	public async Task PurgeOrphansAsync_DoesNotPurgeAlbumsWithTracks()
	{
		await using var context = Fixture.GetContext();

		var artist = new Artist { Name = "Active Artist" };
		context.Artists.Add(artist);

		var album = new Album { Artist = artist, Title = "Active Album" };
		context.Albums.Add(album);

		var track = new Track
		{
			Album = album,
			Artist = artist,
			Title = "Active Track",
		};
		context.Tracks.Add(track);

		await context.SaveChangesAsync();

		var factory = Fixture.GetContextFactory();
		var service = new PurgeService(factory);

		var result = await service.PurgeOrphansAsync();

		await Assert.That(result.AlbumsPurged).IsEqualTo(0);
		await Assert.That(result.ArtistsPurged).IsEqualTo(0);

		await using var verifyContext = Fixture.GetContext();
		await Assert.That(await verifyContext.Albums.AnyAsync()).IsTrue();
		await Assert.That(await verifyContext.Artists.AnyAsync()).IsTrue();
	}
}
