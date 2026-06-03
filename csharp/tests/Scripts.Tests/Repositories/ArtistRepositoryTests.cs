using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
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

		result.Should().NotBeNull();
		result.Name.Should().Be("Test Artist");

		await using var verifyContext = Fixture.GetContext();
		var count = await verifyContext.Artists.CountAsync();
		count.Should().Be(1);
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

		result.Should().NotBeNull();
		result!.Name.Should().Be("Test Artist");
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByNameAsync_ReturnsNullWhenNotFound()
	{
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new ArtistRepository(factory, pipeline);

		var result = await repository.GetByNameAsync("Nonexistent Artist");

		result.Should().BeNull();
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

		result.Should().BeNull();
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

		result.Metadata.Should().BeNull();

		await using var verifyContext = Fixture.GetContext();
		var retrieved = await verifyContext.Artists.FirstAsync();
		retrieved.Metadata.Should().BeNull();
	}
}
