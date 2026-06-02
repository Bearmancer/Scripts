using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Persistence;
using Scripts.Models;
using Scripts.Tests.Attributes;
using System.Text.Json;

namespace Scripts.Tests.ReleaseProgressTests;

[RequiresPgConnStr]
internal sealed class ReleaseProgressServiceTests : IDisposable
{
    private readonly DbContextOptions<ScriptsDbContext> _options;
    private readonly ReleaseProgressService _service;
    private readonly string _releaseId = "test-release-" + Guid.NewGuid().ToString("N")[..8];

    public ReleaseProgressServiceTests()
    {
        var connStr = System.Environment.GetEnvironmentVariable("PGCONNSTR");
        _options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        using var context = new ScriptsDbContext(_options);
        context.Database.EnsureCreated();

        var factory = new TestDbContextFactory(_options);
        _service = new ReleaseProgressService(factory);
    }

    public void Dispose()
    {
        using var context = new ScriptsDbContext(_options);
        context.ReleaseProgress.Where(r => r.ReleaseId == _releaseId).ExecuteDelete();
    }

    [Test]
    public async Task AppendTrackAsync_InsertsTrack()
    {
        var track = new TrackInfo(1, 1, "Test Track", null, null, null, null, null, null, [], null, null, null);

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
        var track1 = new TrackInfo(1, 2, "Track 2", null, null, null, null, null, null, [], null, null, null);
        var track2 = new TrackInfo(1, 1, "Track 1", null, null, null, null, null, null, [], null, null, null);

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
        var track = new TrackInfo(1, 1, "Delete Me", null, null, null, null, null, null, [], null, null, null);
        await _service.AppendTrackAsync(_releaseId, track);

        await _service.DeleteAsync(_releaseId);

        var loaded = await _service.LoadAsync(_releaseId);
        loaded.Should().BeEmpty();
    }
}

internal sealed class TestDbContextFactory(DbContextOptions<ScriptsDbContext> options) : IDbContextFactory<ScriptsDbContext>
{
    public ScriptsDbContext CreateDbContext() => new ScriptsDbContext(options);
}
