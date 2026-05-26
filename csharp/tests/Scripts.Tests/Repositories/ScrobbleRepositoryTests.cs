using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Data.Repositories;
using CSharpScripts.Data.Repositories.Interfaces;
using Polly;

namespace Scripts.Tests.Repositories;

internal sealed class ScrobbleRepositoryTests
{
	private static DbContextOptions<ScriptsDbContext> CreateInMemoryOptions() =>
		new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("ScrobbleTest_" + Guid.NewGuid())
			.Options;

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
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new ScrobbleRepository(factory, pipeline);

		var scrobbles = new[]
		{
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow, Platform = "lastfm" },
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow.AddHours(-1), Platform = "spotify" }
		};

		var result = await repository.UpsertAsync(scrobbles);

		result.Should().Be(2);

		await using var verifyContext = new ScriptsDbContext(options);
		var count = await verifyContext.Scrobbles.CountAsync();
		count.Should().Be(2);
	}

	[Test]
	public async Task UpsertAsync_UpdatesExistingScrobbles()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		var scrobbledAt = DateTimeOffset.UtcNow;
		var scrobble = new Scrobble { TrackId = track.Id, ScrobbledAt = scrobbledAt, Platform = "lastfm" };
		context.Scrobbles.Add(scrobble);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new ScrobbleRepository(factory, pipeline);

		var updatedScrobbles = new[] { new Scrobble { TrackId = track.Id, ScrobbledAt = scrobbledAt, Platform = "spotify" } };
		var result = await repository.UpsertAsync(updatedScrobbles);

		result.Should().Be(1);

		await using var verifyContext = new ScriptsDbContext(options);
		var updated = await verifyContext.Scrobbles.FirstAsync();
		updated.Platform.Should().Be("spotify");
	}

	[Test]
	public async Task DeleteByTrackIdAsync_DeletesAllScrobblesForTrack()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		context.Scrobbles.AddRange(
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow, Platform = "lastfm" },
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow.AddHours(-1), Platform = "spotify" }
		);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new ScrobbleRepository(factory, pipeline);

		var result = await repository.DeleteByTrackIdAsync(track.Id);

		result.Should().Be(2);

		await using var verifyContext = new ScriptsDbContext(options);
		var count = await verifyContext.Scrobbles.CountAsync();
		count.Should().Be(0);
	}

	[Test]
	public async Task GetByTrackIdAsync_ReturnsScrobblesOrderedByMostRecent()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		var now = DateTimeOffset.UtcNow;
		context.Scrobbles.AddRange(
			new Scrobble { TrackId = track.Id, ScrobbledAt = now.AddHours(-2), Platform = "lastfm" },
			new Scrobble { TrackId = track.Id, ScrobbledAt = now, Platform = "spotify" },
			new Scrobble { TrackId = track.Id, ScrobbledAt = now.AddHours(-1), Platform = "lastfm" }
		);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new ScrobbleRepository(factory, pipeline);

		var result = await repository.GetByTrackIdAsync(track.Id);

		result.Should().HaveCount(3);
		result[0].ScrobbledAt.Should().Be(now);
		result[1].ScrobbledAt.Should().Be(now.AddHours(-1));
		result[2].ScrobbledAt.Should().Be(now.AddHours(-2));
	}

	[Test]
	public async Task GetByPlatformAsync_ReturnsScrobblesForPlatform()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);
		var (_, _, track) = await SetupArtistAlbumTrack(context);

		context.Scrobbles.AddRange(
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow, Platform = "lastfm" },
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow.AddHours(-1), Platform = "spotify" },
			new Scrobble { TrackId = track.Id, ScrobbledAt = DateTimeOffset.UtcNow.AddHours(-2), Platform = "lastfm" }
		);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new ScrobbleRepository(factory, pipeline);

		var result = await repository.GetByPlatformAsync("lastfm");

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(s => s.Platform.Should().Be("lastfm"));
	}
}
