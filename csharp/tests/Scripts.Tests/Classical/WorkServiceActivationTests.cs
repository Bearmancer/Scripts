using Scripts.Services.Music;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.Services.Music;

internal sealed class WorkServiceActivationTests : DatabaseTestBase
{
	[RequiresPgConnStr]
	[Test]
	public async Task WorkService_CanBeInstantiated()
	{
		var service = new WorkService(Fixture.GetContextFactory());

		await Assert.That(service).IsNotNull();
	}

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
}
