using Microsoft.EntityFrameworkCore;
using Scripts.Data.Entities;
using Scripts.Data.Repositories;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.Repositories;

internal sealed class ArtistRepositoryTests : DatabaseTestBase
{
	[RequiresPgConnStr]
	[Test]
	public async Task AddAsync_InsertsNewArtist()
	{
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ArtistRepository(factory, pipeline);

		var artist = new Artist { Name = "Test Artist" };

		var result = await repository.AddAsync(artist);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.Name).IsEqualTo("Test Artist");

		await using var verifyContext = Fixture.GetContext();
		var count = await verifyContext.Artists.CountAsync();
		await Assert.That(count).IsEqualTo(1);
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByNameAsync_ReturnsArtistByName()
	{
		await using var context = Fixture.GetContext();

		var artist = new Artist { Name = "Test Artist" };
		context.Artists.Add(artist);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ArtistRepository(factory, pipeline);

		var result = await repository.GetByNameAsync("Test Artist");

		await Assert.That(result).IsNotNull();
		await Assert.That(result!.Name).IsEqualTo("Test Artist");
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByNameAsync_ReturnsNullWhenNotFound()
	{
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ArtistRepository(factory, pipeline);

		var result = await repository.GetByNameAsync("Nonexistent Artist");

		await Assert.That(result).IsNull();
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByNameAsync_IsCaseSensitive()
	{
		await using var context = Fixture.GetContext();

		var artist = new Artist { Name = "Test Artist" };
		context.Artists.Add(artist);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ArtistRepository(factory, pipeline);

		var result = await repository.GetByNameAsync("test artist");

		await Assert.That(result).IsNull();
	}

	[RequiresPgConnStr]
	[Test]
	public async Task AddAsync_PreservesMetadata()
	{
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ArtistRepository(factory, pipeline);

		var artist = new Artist { Name = "Test Artist" };

		var result = await repository.AddAsync(artist);

		await Assert.That(result.Metadata).IsNull();

		await using var verifyContext = Fixture.GetContext();
		var retrieved = await verifyContext.Artists.FirstAsync();
		await Assert.That(retrieved.Metadata).IsNull();
	}
}
