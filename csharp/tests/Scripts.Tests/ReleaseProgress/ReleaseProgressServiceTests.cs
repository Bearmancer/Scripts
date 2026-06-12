using Scripts.Data.Persistence;
using Scripts.Models;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.ReleaseProgress;

[RequiresPgConnStr]
internal sealed class ReleaseProgressServiceTests : DatabaseTestBase
{
	private readonly string _releaseId = "test-release-" + Guid.NewGuid().ToString("N")[..8];

	[Test]
	public async Task AppendTrackAsync_InsertsTrack()
	{
		var factory = Fixture.GetContextFactory();
		var service = new ReleaseProgressService(factory);

		var track = new TrackInfo(
			1,
			1,
			"Test Track",
			null,
			null,
			null,
			null,
			null,
			null,
			[],
			null,
			null,
			null
		);

		await service.AppendTrackAsync(_releaseId, track);

		var loaded = await service.LoadAsync(_releaseId);
		await Assert.That(loaded).Count().IsEqualTo(1);
		await Assert.That(loaded[0].Title).IsEqualTo("Test Track");
		await Assert.That(loaded[0].DiscNumber).IsEqualTo(1);
		await Assert.That(loaded[0].TrackNumber).IsEqualTo(1);
	}

	[Test]
	public async Task LoadAsync_ReturnsOrderedTracks()
	{
		var factory = Fixture.GetContextFactory();
		var service = new ReleaseProgressService(factory);

		var track1 = new TrackInfo(
			1,
			2,
			"Track 2",
			null,
			null,
			null,
			null,
			null,
			null,
			[],
			null,
			null,
			null
		);
		var track2 = new TrackInfo(
			1,
			1,
			"Track 1",
			null,
			null,
			null,
			null,
			null,
			null,
			[],
			null,
			null,
			null
		);

		await service.AppendTrackAsync(_releaseId, track1);
		await service.AppendTrackAsync(_releaseId, track2);

		var loaded = await service.LoadAsync(_releaseId);
		await Assert.That(loaded).Count().IsEqualTo(2);
		await Assert.That(loaded[0].TrackNumber).IsEqualTo(1);
		await Assert.That(loaded[1].TrackNumber).IsEqualTo(2);
	}

	[Test]
	public async Task DeleteAsync_RemovesAllTracks()
	{
		var factory = Fixture.GetContextFactory();
		var service = new ReleaseProgressService(factory);

		var track = new TrackInfo(
			1,
			1,
			"Delete Me",
			null,
			null,
			null,
			null,
			null,
			null,
			[],
			null,
			null,
			null
		);
		await service.AppendTrackAsync(_releaseId, track);

		await service.DeleteAsync(_releaseId);

		var loaded = await service.LoadAsync(_releaseId);
		await Assert.That(loaded).IsEmpty();
	}
}
