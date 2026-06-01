using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;
using Scripts.Data.Repositories;
using Scripts.Data.Repositories.Interfaces;

namespace Scripts.Tests.Repositories;

internal sealed class TrackRepositoryTests
{
	private static DbContextOptions<ScriptsDbContext> CreateInMemoryOptions() =>
		new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("TrackTest_" + Guid.NewGuid())
			.Options;

	private static async Task<(Artist, Album)> SetupArtistAndAlbum(ScriptsDbContext context)
	{
		var artist = new Artist { Name = "Test Artist" };
		var album = new Album { Artist = artist, Title = "Test Album" };

		context.Artists.Add(artist);
		context.Albums.Add(album);
		await context.SaveChangesAsync();

		return (artist, album);
	}

	[Test]
	public async Task BulkInsertAsync_InsertsMultipleTracks()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);
		var (artist, album) = await SetupArtistAndAlbum(context);

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new TrackRepository(factory, pipeline);

		var tracks = new[]
		{
			new Track { AlbumId = album.Id, ArtistId = artist.Id, Title = "Track 1", DurationSeconds = 180 },
			new Track { AlbumId = album.Id, ArtistId = artist.Id, Title = "Track 2", DurationSeconds = 200 },
			new Track { AlbumId = album.Id, ArtistId = artist.Id, Title = "Track 3", DurationSeconds = 220 }
		};

		var result = await repository.BulkInsertAsync(tracks);

		result.Should().Be(3);

		await using var verifyContext = new ScriptsDbContext(options);
		var count = await verifyContext.Tracks.CountAsync();
		count.Should().Be(3);
	}

	[Test]
	public async Task BulkInsertAsync_ReturnsZeroForEmptyList()
	{
		var options = CreateInMemoryOptions();
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new TrackRepository(factory, pipeline);

		var result = await repository.BulkInsertAsync(new List<Track>());

		result.Should().Be(0);
	}

	[Test]
	public async Task GetByArtistAndTitleAsync_ReturnsTrackByArtistAndTitle()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);
		var (artist, album) = await SetupArtistAndAlbum(context);

		var track = new Track { AlbumId = album.Id, ArtistId = artist.Id, Title = "Test Track", DurationSeconds = 180 };
		context.Tracks.Add(track);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new TrackRepository(factory, pipeline);

		var result = await repository.GetByArtistAndTitleAsync(artist.Id, "Test Track");

		result.Should().NotBeNull();
		result!.Title.Should().Be("Test Track");
		result.DurationSeconds.Should().Be(180);
	}

	[Test]
	public async Task GetByArtistAndTitleAsync_ReturnsNullWhenNotFound()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);
		var (artist, _) = await SetupArtistAndAlbum(context);

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new TrackRepository(factory, pipeline);

		var result = await repository.GetByArtistAndTitleAsync(artist.Id, "Nonexistent Track");

		result.Should().BeNull();
	}

	[Test]
	public async Task GetByArtistAndTitleAsync_DistinguishesBetweenArtists()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);

		var artist1 = new Artist { Name = "Artist 1" };
		var artist2 = new Artist { Name = "Artist 2" };
		var album1 = new Album { Artist = artist1, Title = "Album 1" };
		var album2 = new Album { Artist = artist2, Title = "Album 2" };

		context.Artists.AddRange(artist1, artist2);
		context.Albums.AddRange(album1, album2);
		await context.SaveChangesAsync();

		var track1 = new Track { AlbumId = album1.Id, ArtistId = artist1.Id, Title = "Same Title" };
		var track2 = new Track { AlbumId = album2.Id, ArtistId = artist2.Id, Title = "Same Title" };
		context.Tracks.AddRange(track1, track2);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new TrackRepository(factory, pipeline);

		var result = await repository.GetByArtistAndTitleAsync(artist1.Id, "Same Title");

		result.Should().NotBeNull();
		result!.ArtistId.Should().Be(artist1.Id);
	}
}
