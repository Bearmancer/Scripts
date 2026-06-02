using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;
using Scripts.Data.Repositories;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.Repositories;

internal sealed class AlbumRepositoryTests : DatabaseTestBase
{
	[RequiresPgConnStr]
	[Test]
	public async Task AddAsync_InsertsNewAlbum()
	{
		await using var context = Fixture.GetContext();
		var artist = await SetupArtist(context);

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
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

		await using var verifyContext = Fixture.GetContext();
		var count = await verifyContext.Albums.CountAsync();
		count.Should().Be(1);
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByArtistAndTitleAsync_ReturnsAlbumByArtistAndTitle()
	{
		await using var context = Fixture.GetContext();
		var artist = await SetupArtist(context);

		var album = new Album { ArtistId = artist.Id, Title = "Test Album" };
		context.Albums.Add(album);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new AlbumRepository(factory, pipeline);

		var result = await repository.GetByArtistAndTitleAsync(artist.Id, "Test Album");

		result.Should().NotBeNull();
		result!.Title.Should().Be("Test Album");
		result.ArtistId.Should().Be(artist.Id);
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByArtistAndTitleAsync_ReturnsNullWhenNotFound()
	{
		await using var context = Fixture.GetContext();
		var artist = await SetupArtist(context);

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new AlbumRepository(factory, pipeline);

		var result = await repository.GetByArtistAndTitleAsync(artist.Id, "Nonexistent Album");

		result.Should().BeNull();
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByArtistAndTitleAsync_DistinguishesBetweenArtists()
	{
		await using var context = Fixture.GetContext();

		var artist1 = new Artist { Name = "Artist 1" };
		var artist2 = new Artist { Name = "Artist 2" };
		context.Artists.AddRange(artist1, artist2);
		await context.SaveChangesAsync();

		var album1 = new Album { ArtistId = artist1.Id, Title = "Same Title" };
		var album2 = new Album { ArtistId = artist2.Id, Title = "Same Title" };
		context.Albums.AddRange(album1, album2);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new AlbumRepository(factory, pipeline);

		var result = await repository.GetByArtistAndTitleAsync(artist1.Id, "Same Title");

		result.Should().NotBeNull();
		result!.ArtistId.Should().Be(artist1.Id);
	}

	[RequiresPgConnStr]
	[Test]
	public async Task AddAsync_PreservesReleaseDate()
	{
		await using var context = Fixture.GetContext();
		var artist = await SetupArtist(context);

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new AlbumRepository(factory, pipeline);

		var releaseDate = DateOnly.FromDateTime(DateTime.UtcNow);
		var album = new Album { ArtistId = artist.Id, Title = "Test Album", ReleaseDate = releaseDate };

		var result = await repository.AddAsync(album);

		result.ReleaseDate.Should().Be(releaseDate);

		await using var verifyContext = Fixture.GetContext();
		var retrieved = await verifyContext.Albums.FirstAsync();
		retrieved.ReleaseDate.Should().Be(releaseDate);
	}

	private static async Task<Artist> SetupArtist(ScriptsDbContext context)
	{
		var artist = new Artist { Name = "Test Artist" };
		context.Artists.Add(artist);
		await context.SaveChangesAsync();
		return artist;
	}
}
