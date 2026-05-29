using CSharpScripts.Data;
using CSharpScripts.Data.Persistence;
using CSharpScripts.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TUnit;

namespace Scripts.Tests.ReleaseProgressTests;

#pragma warning disable CA1001
internal sealed class ReleaseProgressServiceTests
{
	private ScriptsDbContext _context = null!;
	private ReleaseProgressService _service = null!;
	private readonly string _releaseId = "test-release-" + Guid.NewGuid().ToString("N")[..8];

	[Before(Test)]
	public async Task Setup()
	{
		var connStr = System.Environment.GetEnvironmentVariable("PGCONNSTR");
		if (connStr is null)
			throw new InvalidOperationException("PGCONNSTR environment variable not set");

		var options = new DbContextOptionsBuilder<ScriptsDbContext>().UseNpgsql(connStr).Options;

		_context = new ScriptsDbContext(options);
		await _context.Database.EnsureCreatedAsync();

		var factory = new TestDbContextFactory(_context);
		_service = new ReleaseProgressService(factory);
	}

	[After(Test)]
	public async Task Cleanup()
	{
		await _context.ReleaseProgress.Where(r => r.ReleaseId == _releaseId).ExecuteDeleteAsync();
		await _context.DisposeAsync();
	}

	[Test]
	public async Task AppendTrackAsync_InsertsTrack()
	{
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

		await _service.AppendTrackAsync(_releaseId, track);

		var loaded = await _service.LoadAsync(_releaseId);
		loaded.Should().HaveCount(1);
		loaded[0].Title.Should().Be("Test Track");
		loaded[0].DiscNumber.Should().Be(1);
		loaded[0].TrackNumber.Should().Be(1);
	}

	[Test]
	public async Task LoadAsync_ReturnsOrderedTracks()
	{
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

		await _service.AppendTrackAsync(_releaseId, track1);
		await _service.AppendTrackAsync(_releaseId, track2);

		var loaded = await _service.LoadAsync(_releaseId);
		loaded.Should().HaveCount(2);
		loaded[0].TrackNumber.Should().Be(1);
		loaded[1].TrackNumber.Should().Be(2);
	}

	[Test]
	public async Task DeleteAsync_RemovesAllTracks()
	{
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
		await _service.AppendTrackAsync(_releaseId, track);

		await _service.DeleteAsync(_releaseId);

		var loaded = await _service.LoadAsync(_releaseId);
		loaded.Should().BeEmpty();
	}
}
#pragma warning restore CA1001

internal sealed class TestDbContextFactory(ScriptsDbContext context)
	: IDbContextFactory<ScriptsDbContext>
{
	public ScriptsDbContext CreateDbContext() => context;

	public ValueTask<ScriptsDbContext> CreateDbContextAsync() => ValueTask.FromResult(context);
}
