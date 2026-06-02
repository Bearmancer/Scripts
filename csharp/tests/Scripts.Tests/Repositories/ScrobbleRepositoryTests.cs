using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;
using Scripts.Data.Repositories;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.Repositories;

[RequiresPgConnStr]
internal sealed class ScrobbleRepositoryTests : DatabaseTestBase
{

	private static async Task<(Artist, Album, Track)> SetupArtistAlbumTrack(ScriptsDbContext context)
	{
		var artist = new Artist { Name = "Test Artist" };
		var album = new Album { Artist = artist, Title = "Test Album" };
		var track = new Track { Album = album, Artist = artist, Title = "Test Track" };

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
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow, Platform = "lastfm" },
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow.AddHours(-1), Platform = "spotify" }
		};

		var result = await repository.UpsertAsync(scrobbles);

		result.Should().Be(2);

		await using var verifyContext = Fixture.GetContext();
		var count = await verifyContext.Scrobbles.CountAsync();
		count.Should().Be(2);
	}

	[Test]
	public async Task UpsertAsync_UpdatesExistingScrobbles()
	{
		await using var context = Fixture.GetContext();
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		var scrobbledAt = DateTimeOffset.UtcNow;
		var scrobble = new Scrobble { TrackId = track.Id, ScrobbledAt = scrobbledAt, Platform = "lastfm" };
		context.Scrobbles.Add(scrobble);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ScrobbleRepository(factory, pipeline);

		var updatedScrobbles = new[] { new Scrobble { TrackId = track.Id, ScrobbledAt = scrobbledAt, Platform = "spotify" } };
		var result = await repository.UpsertAsync(updatedScrobbles);

		result.Should().Be(1);

		await using var verifyContext = Fixture.GetContext();
		var updated = await verifyContext.Scrobbles.FirstAsync();
		updated.Platform.Should().Be("spotify");
	}

	[Test]
	public async Task DeleteByTrackIdAsync_DeletesAllScrobblesForTrack()
	{
		await using var context = Fixture.GetContext();
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		context.Scrobbles.AddRange(
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow, Platform = "lastfm" },
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow.AddHours(-1), Platform = "spotify" }
		);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ScrobbleRepository(factory, pipeline);

		var result = await repository.DeleteByTrackIdAsync(track.Id);

		result.Should().Be(2);

		await using var verifyContext = Fixture.GetContext();
		var count = await verifyContext.Scrobbles.CountAsync();
		count.Should().Be(0);
	}

	[Test]
	public async Task GetByTrackIdAsync_ReturnsScrobblesOrderedByMostRecent()
	{
		await using var context = Fixture.GetContext();
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		var now = DateTimeOffset.UtcNow;
		context.Scrobbles.AddRange(
			new Scrobble { TrackId = track.Id, ScrobbledAt = now.AddHours(-2), Platform = "lastfm" },
			new Scrobble { TrackId = track.Id, ScrobbledAt = now, Platform = "spotify" },
			new Scrobble { TrackId = track.Id, ScrobbledAt = now.AddHours(-1), Platform = "lastfm" }
		);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ScrobbleRepository(factory, pipeline);

		var result = await repository.GetByTrackIdAsync(track.Id);

		result.Should().HaveCount(3);
		result[0].ScrobbledAt.Should().BeCloseTo(now, TimeSpan.FromMilliseconds(1));
		result[1].ScrobbledAt.Should().BeCloseTo(now.AddHours(-1), TimeSpan.FromMilliseconds(1));
		result[2].ScrobbledAt.Should().BeCloseTo(now.AddHours(-2), TimeSpan.FromMilliseconds(1));
	}

	[Test]
	public async Task GetByPlatformAsync_ReturnsScrobblesForPlatform()
	{
		await using var context = Fixture.GetContext();
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		context.Scrobbles.AddRange(
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow, Platform = "lastfm" },
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow.AddHours(-1), Platform = "spotify" },
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow.AddHours(-2), Platform = "lastfm" }
		);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ScrobbleRepository(factory, pipeline);

		var result = await repository.GetByPlatformAsync("lastfm");

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(s => s.Platform.Should().Be("lastfm"));
	}
}
