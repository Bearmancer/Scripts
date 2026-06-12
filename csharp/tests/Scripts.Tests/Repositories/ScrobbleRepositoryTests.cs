using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;
using Scripts.Data.Repositories;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.Repositories;

[RequiresPgConnStr]
internal sealed class ScrobbleRepositoryTests : DatabaseTestBase
{
	private static async Task<(Artist, Album, Track)> SetupArtistAlbumTrack(
		ScriptsDbContext context
	)
	{
		var artist = new Artist { Name = "Test Artist" };
		var album = new Album { Artist = artist, Title = "Test Album" };
		var track = new Track
		{
			Album = album,
			Artist = artist,
			Title = "Test Track",
		};

		context.Artists.Add(artist);
		context.Albums.Add(album);
		context.Tracks.Add(track);
		await context.SaveChangesAsync();

		return (artist, album, track);
	}

	[Test]
	public async Task UpsertAsync_InsertsNewScrobbles()
	{
		await using var context = Fixture.GetContext();
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ScrobbleRepository(factory, pipeline);

		var scrobbles = new[]
		{
			new Scrobble
			{
				TrackId = track.Id,
				ScrobbledAt = DateTimeOffset.UtcNow,
				Platform = "lastfm",
			},
			new Scrobble
			{
				TrackId = track.Id,
				ScrobbledAt = DateTimeOffset.UtcNow.AddHours(-1),
				Platform = "spotify",
			},
		};

		var result = await repository.UpsertAsync(scrobbles);

		await Assert.That(result).IsEqualTo(2);

		await using var verifyContext = Fixture.GetContext();
		var count = await verifyContext.Scrobbles.CountAsync();
		await Assert.That(count).IsEqualTo(2);
	}

	[Test]
	public async Task UpsertAsync_UpdatesExistingScrobbles()
	{
		await using var context = Fixture.GetContext();
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		var scrobbledAt = DateTimeOffset.UtcNow;
		var scrobble = new Scrobble
		{
			TrackId = track.Id,
			ScrobbledAt = scrobbledAt,
			Platform = "lastfm",
		};
		context.Scrobbles.Add(scrobble);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ScrobbleRepository(factory, pipeline);

		var updatedScrobbles = new[]
		{
			new Scrobble
			{
				TrackId = track.Id,
				ScrobbledAt = scrobbledAt,
				Platform = "spotify",
			},
		};
		var result = await repository.UpsertAsync(updatedScrobbles);

		await Assert.That(result).IsEqualTo(1);

		await using var verifyContext = Fixture.GetContext();
		var updated = await verifyContext.Scrobbles.FirstAsync();
		await Assert.That(updated.Platform).IsEqualTo("spotify");
	}

	[Test]
	public async Task DeleteByTrackIdAsync_DeletesAllScrobblesForTrack()
	{
		await using var context = Fixture.GetContext();
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		context.Scrobbles.AddRange(
			new Scrobble
			{
				TrackId = track.Id,
				ScrobbledAt = DateTimeOffset.UtcNow,
				Platform = "lastfm",
			},
			new Scrobble
			{
				TrackId = track.Id,
				ScrobbledAt = DateTimeOffset.UtcNow.AddHours(-1),
				Platform = "spotify",
			}
		);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ScrobbleRepository(factory, pipeline);

		var result = await repository.DeleteByTrackIdAsync(track.Id);

		await Assert.That(result).IsEqualTo(2);

		await using var verifyContext = Fixture.GetContext();
		var count = await verifyContext.Scrobbles.CountAsync();
		await Assert.That(count).IsEqualTo(0);
	}

	[Test]
	public async Task GetByTrackIdAsync_ReturnsScrobblesOrderedByMostRecent()
	{
		await using var context = Fixture.GetContext();
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		var now = DateTimeOffset.UtcNow;
		context.Scrobbles.AddRange(
			new Scrobble
			{
				TrackId = track.Id,
				ScrobbledAt = now.AddHours(-2),
				Platform = "lastfm",
			},
			new Scrobble
			{
				TrackId = track.Id,
				ScrobbledAt = now,
				Platform = "spotify",
			},
			new Scrobble
			{
				TrackId = track.Id,
				ScrobbledAt = now.AddHours(-1),
				Platform = "lastfm",
			}
		);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ScrobbleRepository(factory, pipeline);

		var result = await repository.GetByTrackIdAsync(track.Id);

		await Assert.That(result).Count().IsEqualTo(3);
		await Assert
			.That(result[0].ScrobbledAt)
			.IsEqualTo(now)
			.Within(TimeSpan.FromMilliseconds(1));
		await Assert
			.That(result[1].ScrobbledAt)
			.IsEqualTo(now.AddHours(-1))
			.Within(TimeSpan.FromMilliseconds(1));
		await Assert
			.That(result[2].ScrobbledAt)
			.IsEqualTo(now.AddHours(-2))
			.Within(TimeSpan.FromMilliseconds(1));
	}

	[Test]
	public async Task GetByPlatformAsync_ReturnsScrobblesForPlatform()
	{
		await using var context = Fixture.GetContext();
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		context.Scrobbles.AddRange(
			new Scrobble
			{
				TrackId = track.Id,
				ScrobbledAt = DateTimeOffset.UtcNow,
				Platform = "lastfm",
			},
			new Scrobble
			{
				TrackId = track.Id,
				ScrobbledAt = DateTimeOffset.UtcNow.AddHours(-1),
				Platform = "spotify",
			},
			new Scrobble
			{
				TrackId = track.Id,
				ScrobbledAt = DateTimeOffset.UtcNow.AddHours(-2),
				Platform = "lastfm",
			}
		);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ScrobbleRepository(factory, pipeline);

		var result = await repository.GetByPlatformAsync("lastfm");

		await Assert.That(result).Count().IsEqualTo(2);
		await Assert.That(result).All(s => s.Platform == "lastfm");
	}
}
