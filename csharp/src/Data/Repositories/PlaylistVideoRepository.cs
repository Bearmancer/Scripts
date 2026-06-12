using Scripts.Data.Entities;

namespace Scripts.Data.Repositories;

internal sealed class PlaylistVideoRepository(
	IDbContextFactory<ScriptsDbContext> contextFactory,
	ResiliencePipeline resiliencePipeline
	)
{
	private readonly IDbContextFactory<ScriptsDbContext> _contextFactory = contextFactory;
	private readonly ResiliencePipeline _resiliencePipeline = resiliencePipeline;

	public async Task<int> BulkInsertAsync(
		IEnumerable<PlaylistVideo> playlistVideos,
		CancellationToken ct = default
	)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				var playlistVideoList = playlistVideos.ToList();
				context.PlaylistVideos.AddRange(playlistVideoList);
				await context.SaveChangesAsync(token);

				return playlistVideoList.Count;
			},
			ct
		);
	}

	public async Task<int> DeleteByPlaylistIdAsync(int playlistId, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				return await context
					.PlaylistVideos.Where(pv => pv.PlaylistId == playlistId)
					.ExecuteDeleteAsync(token);
			},
			ct
		);
	}

	public async Task<IReadOnlyList<PlaylistVideo>> GetByPlaylistIdAsync(
		int playlistId,
		CancellationToken ct = default
	)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				return await context
					.PlaylistVideos.AsNoTracking()
					.Where(pv => pv.PlaylistId == playlistId)
					.ToListAsync(token);
			},
			ct
		);
	}
}
