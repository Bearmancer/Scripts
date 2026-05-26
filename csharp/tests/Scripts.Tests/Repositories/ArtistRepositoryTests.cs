using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Data.Repositories;
using CSharpScripts.Data.Repositories.Interfaces;

namespace Scripts.Tests.Repositories;

internal sealed class ArtistRepositoryTests
{
	private static DbContextOptions<ScriptsDbContext> CreateInMemoryOptions() =>
		new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("ArtistTest_" + Guid.NewGuid())
			.Options;

	[Test]
	public async Task AddAsync_InsertsNewArtist()
	{
		var options = CreateInMemoryOptions();
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new ArtistRepository(factory, pipeline);

		var artist = new Artist { Name = "Test Artist" };

		var result = await repository.AddAsync(artist);

		result.Should().NotBeNull();
		result.Name.Should().Be("Test Artist");

		await using var verifyContext = new ScriptsDbContext(options);
		var count = await verifyContext.Artists.CountAsync();
		count.Should().Be(1);
	}

	[Test]
	public async Task GetByNameAsync_ReturnsArtistByName()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);

		var artist = new Artist { Name = "Test Artist" };
		context.Artists.Add(artist);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new ArtistRepository(factory, pipeline);

		var result = await repository.GetByNameAsync("Test Artist");

		result.Should().NotBeNull();
		result!.Name.Should().Be("Test Artist");
	}

	[Test]
	public async Task GetByNameAsync_ReturnsNullWhenNotFound()
	{
		var options = CreateInMemoryOptions();
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new ArtistRepository(factory, pipeline);

		var result = await repository.GetByNameAsync("Nonexistent Artist");

		result.Should().BeNull();
	}

	[Test]
	public async Task GetByNameAsync_IsCaseSensitive()
	{
		var options = CreateInMemoryOptions();
		await using var context = new ScriptsDbContext(options);

		var artist = new Artist { Name = "Test Artist" };
		context.Artists.Add(artist);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new ArtistRepository(factory, pipeline);

		var result = await repository.GetByNameAsync("test artist");

		result.Should().BeNull();
	}

	[Test]
	public async Task AddAsync_PreservesMetadata()
	{
		var options = CreateInMemoryOptions();
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = new TestDbContextFactory(options);
		var repository = new ArtistRepository(factory, pipeline);

		var artist = new Artist { Name = "Test Artist" };

		var result = await repository.AddAsync(artist);

		result.Metadata.Should().BeNull();

		await using var verifyContext = new ScriptsDbContext(options);
		var retrieved = await verifyContext.Artists.FirstAsync();
		retrieved.Metadata.Should().BeNull();
	}
}
