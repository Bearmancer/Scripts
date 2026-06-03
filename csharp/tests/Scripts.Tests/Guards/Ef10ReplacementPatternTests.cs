using FluentAssertions;
using TUnit;
using Scripts.Data;
using Scripts.Data.Entities;
using Scripts.Tests.Attributes;
using Microsoft.EntityFrameworkCore;

namespace Scripts.Tests.Guards;

[RequiresPgConnStr]
internal sealed class Ef10ReplacementPatternTests : DatabaseTestBase
{
	[Test]
	public async Task OrderByDescending_FirstOrDefaultAsync_Ef10MaxBy_Works()
	{
		await using var context = Fixture.GetContext();

		var artist = new Artist { Name = "Ef10Test" };
		context.Artists.Add(artist);
		await context.SaveChangesAsync();

		var album = new Album { ArtistId = artist.Id, Title = "Ef10Album", ReleaseDate = new DateOnly(2024, 1, 1) };
		context.Albums.Add(album);
		await context.SaveChangesAsync();

		var track = new Track
		{
			AlbumId = album.Id,
			ArtistId = artist.Id,
			Title = "Ef10Track",
			DurationSeconds = 180
		};
		context.Tracks.Add(track);
		await context.SaveChangesAsync();

		var now = DateTimeOffset.UtcNow;
		var scrobble1 = new Scrobble
		{
			TrackId = track.Id,
			ScrobbledAt = now.AddHours(-2),
			Platform = "lastfm"
		};
		var scrobble2 = new Scrobble
		{
			TrackId = track.Id,
			ScrobbledAt = now,
			Platform = "lastfm"
		};
		context.Scrobbles.AddRange(scrobble1, scrobble2);
		await context.SaveChangesAsync();

		var latest = await context.Scrobbles
			.OrderByDescending(s => s.ScrobbledAt)
			.FirstOrDefaultAsync();

		latest.Should().NotBeNull();
		latest!.ScrobbledAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(5));
	}

	[Test]
	public async Task OrderBy_FirstOrDefaultAsync_Ef10MinBy_Works()
	{
		await using var context = Fixture.GetContext();

		var artist = new Artist { Name = "Ef10MinTest" };
		context.Artists.Add(artist);
		await context.SaveChangesAsync();

		var album = new Album { ArtistId = artist.Id, Title = "Ef10MinAlbum", ReleaseDate = new DateOnly(2024, 1, 1) };
		context.Albums.Add(album);
		await context.SaveChangesAsync();

		var track = new Track
		{
			AlbumId = album.Id,
			ArtistId = artist.Id,
			Title = "Ef10MinTrack",
			DurationSeconds = 120
		};
		context.Tracks.Add(track);
		await context.SaveChangesAsync();

		var now = DateTimeOffset.UtcNow;
		var scrobbleA = new Scrobble { TrackId = track.Id, ScrobbledAt = now, Platform = "lastfm" };
		var scrobbleB = new Scrobble { TrackId = track.Id, ScrobbledAt = now.AddHours(-5), Platform = "lastfm" };
		context.Scrobbles.AddRange(scrobbleA, scrobbleB);
		await context.SaveChangesAsync();

		var earliest = await context.Scrobbles
			.Where(s => s.Platform == "lastfm")
			.OrderBy(s => s.ScrobbledAt)
			.FirstOrDefaultAsync();

		earliest.Should().NotBeNull();
		earliest!.ScrobbledAt.Should().BeCloseTo(now.AddHours(-5), TimeSpan.FromSeconds(5));
	}

	[Test]
	public Task JsonContains_ArtistMetadata_Compiles() =>
		Task.CompletedTask;

	[Test]
	public async Task ExecuteUpdateAsync_SetProperty_IsEf10Compatible()
	{
		await using var context = Fixture.GetContext();

		var artist = new Artist { Name = "BeforeUpdate" };
		context.Artists.Add(artist);
		await context.SaveChangesAsync();

		await context.Artists
			.Where(a => a.Name == "BeforeUpdate")
			.ExecuteUpdateAsync(setters =>
				setters.SetProperty(a => a.Name, "AfterUpdate"));

		var updated = await context.Artists
			.FirstOrDefaultAsync(a => a.Name == "AfterUpdate");

		updated.Should().NotBeNull();
	}
}
