using CSharpScripts.Data.Entities;

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
		CancellationToken ct = default
	)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(
			cancellationToken: ct
		);

		await context
			.Scrobbles.Where(s => s.Id == id)
			.ExecuteUpdateAsync(
				scrobble =>
					scrobble
						.SetProperty(s => s.TrackId, valueExpression: trackId)
						.SetProperty(s => s.ScrobbledAt, valueExpression: timestamp)
						.SetProperty(s => s.Platform, valueExpression: platform),
				cancellationToken: ct
			);
	}

	internal async Task BulkInsertTracksAsync(
		IEnumerable<Track> tracks,
		CancellationToken ct = default
	)
	{
		await using ScriptsDbContext context = await contextFactory.CreateDbContextAsync(
			cancellationToken: ct
		);

		context.Tracks.AddRange(entities: tracks);
		await context.SaveChangesAsync(cancellationToken: ct);
	}
}
