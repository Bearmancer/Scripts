using CSharpScripts.Data.Entities;
using CSharpScripts.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace CSharpScripts.Data.Repositories;

internal sealed class TrackRepository : ITrackRepository
{
	private readonly IDbContextFactory<ScriptsDbContext> _contextFactory;
	private readonly ResiliencePipeline _resiliencePipeline;

	public TrackRepository(
		IDbContextFactory<ScriptsDbContext> contextFactory,
		ResiliencePipeline resiliencePipeline
	)
	{
		_contextFactory = contextFactory;
		_resiliencePipeline = resiliencePipeline;
	}

	public async Task<int> BulkInsertAsync(
		IEnumerable<Track> tracks,
		CancellationToken ct = default
	)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				var trackList = tracks.ToList();
				context.Tracks.AddRange(trackList);
				await context.SaveChangesAsync(token);

				return trackList.Count;
			},
			ct
		);
	}

	public async Task<Track?> GetByArtistAndTitleAsync(
		int artistId,
		string title,
		CancellationToken ct = default
	)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				return await context
					.Tracks.AsNoTracking()
					.FirstOrDefaultAsync(
						t => t.ArtistId == artistId && t.Title == title,
						cancellationToken: token
					);
			},
			ct
		);
	}
}
