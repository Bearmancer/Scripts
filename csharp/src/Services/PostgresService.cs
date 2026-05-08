using Microsoft.EntityFrameworkCore;

namespace CSharpScripts.Services;

internal sealed class PostgresService(IDbContextFactory<Data.ScriptsDbContext> contextFactory)
{
    /// <summary>
    /// Upserts a scrobble row using ON CONFLICT DO UPDATE.
    /// Scrobble PK (bigint id) is the conflict target.
    /// </summary>
    internal async Task UpsertScrobbleAsync(
        long id,
        Guid trackId,
        DateTimeOffset timestamp,
        string platform,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        await context.Scrobbles
            .ExecuteUpdateAsync(
                scrobble => scrobble
                    .SetProperty(s => s.TrackId, trackId)
                    .SetProperty(s => s.Timestamp, timestamp)
                    .SetProperty(s => s.Platform, platform),
                ct);
    }

    /// <summary>
    /// Inserts a batch of tracks in a single round-trip.
    /// </summary>
    internal async Task BulkInsertTracksAsync(
        IEnumerable<Data.Entities.Track> tracks,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        context.Tracks.AddRange(tracks);
        await context.SaveChangesAsync(ct);
    }
}
