using CSharpScripts.Data.Entities;
using CSharpScripts.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace CSharpScripts.Data.Repositories;

internal sealed class ScrobbleRepository : IScrobbleRepository
{
	private readonly IDbContextFactory<ScriptsDbContext> _contextFactory;
	private readonly ResiliencePipeline _resiliencePipeline;

	public ScrobbleRepository(
		IDbContextFactory<ScriptsDbContext> contextFactory,
		ResiliencePipeline resiliencePipeline
	)
	{
		_contextFactory = contextFactory;
		_resiliencePipeline = resiliencePipeline;
	}

	public async Task<int> UpsertAsync(
		IEnumerable<Entities.Scrobble> scrobbles,
		CancellationToken ct = default
	)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				var count = 0;
				foreach (var scrobble in scrobbles)
				{
					var updated = await context
						.Scrobbles.Where(s =>
							s.TrackId == scrobble.TrackId && s.ScrobbledAt == scrobble.ScrobbledAt
						)
						.ExecuteUpdateAsync(
							setters => setters.SetProperty(s => s.Platform, scrobble.Platform),
							token
						);

					if (updated == 0)
					{
						context.Scrobbles.Add(scrobble);
						await context.SaveChangesAsync(token);
					}

					count += Math.Max(updated, 1);
				}

				return count;
			},
			ct
		);
	}

	public async Task<int> DeleteByTrackIdAsync(int trackId, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				return await context
					.Scrobbles.Where(s => s.TrackId == trackId)
					.ExecuteDeleteAsync(token);
			},
			ct
		);
	}

	public async Task<IReadOnlyList<Entities.Scrobble>> GetByTrackIdAsync(
		int trackId,
		CancellationToken ct = default
	)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				return await context
					.Scrobbles.AsNoTracking()
					.Where(s => s.TrackId == trackId)
					.OrderByDescending(s => s.ScrobbledAt)
					.ToListAsync(token);
			},
			ct
		);
	}

	public async Task<IReadOnlyList<Entities.Scrobble>> GetByPlatformAsync(
		string platform,
		CancellationToken ct = default
	)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				return await context
					.Scrobbles.AsNoTracking()
					.Where(s => s.Platform == platform)
					.OrderByDescending(s => s.ScrobbledAt)
					.ToListAsync(token);
			},
			ct
		);
	}
}
