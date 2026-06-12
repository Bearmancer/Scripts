using Microsoft.EntityFrameworkCore;
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

		await Assert.That(workId).IsGreaterThan(0);

		await using var context = Fixture.GetContext();
		var work = await context.MusicWorks.FindAsync(workId);
		await Assert.That(work).IsNotNull();
		await Assert.That(work!.Title).IsEqualTo("Symphony No. 5");
		await Assert.That(work.Composer).IsEqualTo("Beethoven");
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetOrCreateWorkAsync_ReturnsExistingWorkWhenFound()
	{
		var factory = Fixture.GetContextFactory();
		var service = new WorkService(factory);

		var workId1 = await service.GetOrCreateWorkAsync("Symphony No. 5", "Beethoven");
		var workId2 = await service.GetOrCreateWorkAsync("Symphony No. 5", "Beethoven");

		await Assert.That(workId1).IsEqualTo(workId2);

		await using var context = Fixture.GetContext();
		var count = await context.MusicWorks.CountAsync();
		await Assert.That(count).IsEqualTo(1);
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetOrCreateWorkAsync_IsCaseInsensitive()
	{
		var factory = Fixture.GetContextFactory();
		var service = new WorkService(factory);

		var workId1 = await service.GetOrCreateWorkAsync("Symphony No. 5", "Beethoven");
		var workId2 = await service.GetOrCreateWorkAsync("SYMPHONY NO. 5", "BEETHOVEN");

		await Assert.That(workId1).IsEqualTo(workId2);
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetOrCreateWorkAsync_DistinguishesByComposer()
	{
		var factory = Fixture.GetContextFactory();
		var service = new WorkService(factory);

		var workId1 = await service.GetOrCreateWorkAsync("Symphony No. 5", "Beethoven");
		var workId2 = await service.GetOrCreateWorkAsync("Symphony No. 5", "Mahler");

		await Assert.That(workId1).IsNotEqualTo(workId2);

		await using var context = Fixture.GetContext();
		var count = await context.MusicWorks.CountAsync();
		await Assert.That(count).IsEqualTo(2);
	}
}
