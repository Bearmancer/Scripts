using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;
using Scripts.Services.Music;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.Services.Music;

internal sealed class WorkServiceTests : DatabaseTestBase
{
	[RequiresPgConnStr]
	[Test]
	public async Task GetOrCreateWorkAsync_CreatesNewWorkWhenNotFound()
	{
		var factory = Fixture.GetContextFactory();
		var service = new WorkService(factory);

		var workId = await service.GetOrCreateWorkAsync("Symphony No. 5", "Beethoven");

		workId.Should().BeGreaterThan(0);

		await using var context = Fixture.GetContext();
		var work = await context.MusicWorks.FindAsync(workId);
		work.Should().NotBeNull();
		work!.Title.Should().Be("Symphony No. 5");
		work.Composer.Should().Be("Beethoven");
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetOrCreateWorkAsync_ReturnsExistingWorkWhenFound()
	{
		var factory = Fixture.GetContextFactory();
		var service = new WorkService(factory);

		var workId1 = await service.GetOrCreateWorkAsync("Symphony No. 5", "Beethoven");
		var workId2 = await service.GetOrCreateWorkAsync("Symphony No. 5", "Beethoven");

		workId1.Should().Be(workId2);

		await using var context = Fixture.GetContext();
		var count = await context.MusicWorks.CountAsync();
		count.Should().Be(1);
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetOrCreateWorkAsync_IsCaseInsensitive()
	{
		var factory = Fixture.GetContextFactory();
		var service = new WorkService(factory);

		var workId1 = await service.GetOrCreateWorkAsync("Symphony No. 5", "Beethoven");
		var workId2 = await service.GetOrCreateWorkAsync("SYMPHONY NO. 5", "BEETHOVEN");

		workId1.Should().Be(workId2);
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetOrCreateWorkAsync_DistinguishesByComposer()
	{
		var factory = Fixture.GetContextFactory();
		var service = new WorkService(factory);

		var workId1 = await service.GetOrCreateWorkAsync("Symphony No. 5", "Beethoven");
		var workId2 = await service.GetOrCreateWorkAsync("Symphony No. 5", "Mahler");

		workId1.Should().NotBe(workId2);

		await using var context = Fixture.GetContext();
		var count = await context.MusicWorks.CountAsync();
		count.Should().Be(2);
	}
}
