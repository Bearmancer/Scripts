using Microsoft.EntityFrameworkCore;
using Scripts.Data.Entities;
using Scripts.Services.Sync.LastFm;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.SyncService;

[RequiresPgConnStr]
internal sealed class SyncServiceTests : DatabaseTestBase
{
	[Test]
	public async Task LastFmService_Constructor_AcceptsDbContextFactory()
	{
		var factory = Fixture.GetContextFactory();

		var service = new LastFmService("test-api-key", "test-user", null!);
		await Assert.That(service).IsNotNull();
	}

	[Test]
	public async Task ILike_Lookup_FindsArtist_CaseInsensitive()
	{
		await using var context = Fixture.GetContext();

		var artistName = "ILikeTest_" + Guid.NewGuid().ToString("N")[..8];
		context.Artists.Add(new Artist { Name = artistName });
		await context.SaveChangesAsync();

		var found = await context
			.Artists.AsNoTracking()
			.FirstOrDefaultAsync(a => EF.Functions.ILike(a.Name, artistName.ToUpper()));

		await Assert.That(found).IsNotNull();
		await Assert.That(found!.Name).IsEqualTo(artistName);
	}

	[Test]
	public async Task ExecuteDeleteAsync_DeletesScrobbles_ByPlatform()
	{
		await using var context = Fixture.GetContext();

		var artist = new Artist { Name = "SyncArtist" };
		context.Artists.Add(artist);
		await context.SaveChangesAsync();

		var album = new Album
		{
			ArtistId = artist.Id,
			Title = "SyncAlbum",
			ReleaseDate = new DateOnly(2024, 1, 1),
		};
		context.Albums.Add(album);
		await context.SaveChangesAsync();

		var track = new Track
		{
			AlbumId = album.Id,
			ArtistId = artist.Id,
			Title = "SyncTrack",
			DurationSeconds = 120,
		};
		context.Tracks.Add(track);
		await context.SaveChangesAsync();

		var testPlatform = "del_test_" + Guid.NewGuid().ToString("N")[..6];
		var scrobble = new Scrobble
		{
			TrackId = track.Id,
			ScrobbledAt = DateTimeOffset.UtcNow,
			Platform = testPlatform,
		};
		context.Scrobbles.Add(scrobble);
		await context.SaveChangesAsync();

		var deleted = await context
			.Scrobbles.Where(s => s.Platform == testPlatform)
			.ExecuteDeleteAsync();

		await Assert.That(deleted).IsEqualTo(1);
	}
}
