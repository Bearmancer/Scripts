using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Services.Sync.LastFm;

namespace Scripts.Tests.SyncService;

internal sealed class SyncServiceTests
{
    [Test]
    public void LastFmService_Constructor_AcceptsDbContextFactory()
    {
        var connStr = System.Environment.GetEnvironmentVariable("PGCONNSTR");
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        using var context = new ScriptsDbContext(options);
        var factory = new TestDbContextFactory(context);

        var service = new LastFmService("test-api-key", "test-user", factory);
        service.Should().NotBeNull();
    }

    [Test]
    public async Task ILike_Lookup_FindsArtist_CaseInsensitive()
    {
        await using var fixture = new Scripts.Tests.DbContext.DatabaseTestFixture();
        await fixture.InitializeAsync();
        await using var context = fixture.GetContext();

        // Insert a test artist
        var artistName = "ILikeTest_" + Guid.NewGuid().ToString("N")[..8];
        context.Artists.Add(new Scripts.Data.Entities.Artist { Name = artistName });
        await context.SaveChangesAsync();

        // Case-insensitive lookup via EF.Functions.ILike
        var found = await context.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => EF.Functions.ILike(a.Name, artistName.ToUpper()));

        found.Should().NotBeNull();
        found!.Name.Should().Be(artistName);
    }

    [Test]
    public async Task ExecuteDeleteAsync_DeletesScrobbles_ByPlatform()
    {
        await using var fixture = new Scripts.Tests.DbContext.DatabaseTestFixture();
        await fixture.InitializeAsync();
        await using var context = fixture.GetContext();

        var artist = new Scripts.Data.Entities.Artist { Name = "SyncArtist" };
        context.Artists.Add(artist);
        await context.SaveChangesAsync();
        
        var album = new Scripts.Data.Entities.Album { ArtistId = artist.Id, Title = "SyncAlbum", ReleaseDate = new DateOnly(2024, 1, 1) };
        context.Albums.Add(album);
        await context.SaveChangesAsync();
        
        var track = new Scripts.Data.Entities.Track { AlbumId = album.Id, ArtistId = artist.Id, Title = "SyncTrack", DurationSeconds = 120 };
        context.Tracks.Add(track);
        await context.SaveChangesAsync();

        var testPlatform = "del_test_" + Guid.NewGuid().ToString("N")[..6];
        var scrobble = new Scripts.Data.Entities.Scrobble
        {
            TrackId = track.Id,
            ScrobbledAt = DateTimeOffset.UtcNow,
            Platform = testPlatform
        };
        context.Scrobbles.Add(scrobble);
        await context.SaveChangesAsync();

        var deleted = await context.Scrobbles
            .Where(s => s.Platform == testPlatform)
            .ExecuteDeleteAsync();

        deleted.Should().Be(1);
    }
}

internal sealed class TestDbContextFactory(ScriptsDbContext context) : IDbContextFactory<ScriptsDbContext>
{
    public ScriptsDbContext CreateDbContext() => context;
}
