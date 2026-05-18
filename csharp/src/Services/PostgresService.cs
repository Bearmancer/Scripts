#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
#pragma warning disable IDE0060
namespace CSharpScripts.Services;

internal sealed class PostgresService(IDbContextFactory<ScriptsDbContext> contextFactory)
{
	internal async Task UpsertScrobbleAsync(
			long id,
			int trackId,
			DateTimeOffset timestamp,
			string platform,
			CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);

		await context.Scrobbles
				.ExecuteUpdateAsync(
						scrobble => scrobble
								.SetProperty(s => s.TrackId, trackId)
								.SetProperty(s => s.ScrobbledAt, timestamp)
								.SetProperty(s => s.Platform, platform),
						ct);
	}

	internal async Task BulkInsertTracksAsync(
			IEnumerable<Data.Entities.Track> tracks,
			CancellationToken ct = default)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(ct);

		context.Tracks.AddRange(tracks);
		await context.SaveChangesAsync(ct);
	}
}
