using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;
using Scripts.Data.Repositories;
using Scripts.Data.Repositories.Interfaces;

namespace Scripts.Tests.Repositories;

internal sealed class AlbumRepositoryTests
{
	private static DbContextOptions<ScriptsDbContext> CreateInMemoryOptions() =>
		new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("AlbumTest_" + Guid.NewGuid())
			.Options;

	private static async Task<Artist> SetupArtist(ScriptsDbContext context)
	{
		var artist = new Artist { Name = "Test Artist" };
		context.Artists.Add(artist);
		await context.SaveChangesAsync();
		return artist;
	}

	[Test]
	public async Task AddAsync_InsertsNewAlbum()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);
		var artist = await SetupArtist(context);

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new AlbumRepository(factory, pipeline);

		var album = new Album
		{
			ArtistId = artist.Id,
			Title = "Test Album",
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow)
		};

		var result = await repository.AddAsync(album);

		result.Should().NotBeNull();
		result.Title.Should().Be("Test Album");

		await using var verifyContext = new ScriptsDbContext(options);
		var count = await verifyContext.Albums.CountAsync();
		count.Should().Be(1);
	}

	[Test]
	public async Task GetByArtistAndTitleAsync_ReturnsAlbumByArtistAndTitle()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);
		var artist = await SetupArtist(context);

		var album = new Album { ArtistId = artist.Id, Title = "Test Album" };
		context.Albums.Add(album);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new AlbumRepository(factory, pipeline);

		var result = await repository.GetByArtistAndTitleAsync(artist.Id, "Test Album");

		result.Should().NotBeNull();
		result!.Title.Should().Be("Test Album");
		result.ArtistId.Should().Be(artist.Id);
	}

	[Test]
	public async Task GetByArtistAndTitleAsync_ReturnsNullWhenNotFound()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);
		var artist = await SetupArtist(context);

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new AlbumRepository(factory, pipeline);

		var result = await repository.GetByArtistAndTitleAsync(artist.Id, "Nonexistent Album");

		result.Should().BeNull();
	}

	[Test]
	public async Task GetByArtistAndTitleAsync_DistinguishesBetweenArtists()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);

		var artist1 = new Artist { Name = "Artist 1" };
		var artist2 = new Artist { Name = "Artist 2" };
		context.Artists.AddRange(artist1, artist2);
		await context.SaveChangesAsync();

		var album1 = new Album { ArtistId = artist1.Id, Title = "Same Title" };
		var album2 = new Album { ArtistId = artist2.Id, Title = "Same Title" };
		context.Albums.AddRange(album1, album2);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new AlbumRepository(factory, pipeline);

		var result = await repository.GetByArtistAndTitleAsync(artist1.Id, "Same Title");

		result.Should().NotBeNull();
		result!.ArtistId.Should().Be(artist1.Id);
	}

	[Test]
	public async Task AddAsync_PreservesReleaseDate()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);
		var artist = await SetupArtist(context);

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new AlbumRepository(factory, pipeline);

		var releaseDate = DateOnly.FromDateTime(DateTime.UtcNow);
		var album = new Album { ArtistId = artist.Id, Title = "Test Album", ReleaseDate = releaseDate };

		var result = await repository.AddAsync(album);

		result.ReleaseDate.Should().Be(releaseDate);

		await using var verifyContext = new ScriptsDbContext(options);
		var retrieved = await verifyContext.Albums.FirstAsync();
		retrieved.ReleaseDate.Should().Be(releaseDate);
	}
}
