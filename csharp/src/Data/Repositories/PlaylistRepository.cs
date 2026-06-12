using Scripts.Data.Entities;

namespace Scripts.Data.Repositories;

internal sealed class PlaylistRepository(
	IDbContextFactory<ScriptsDbContext> contextFactory,
	ResiliencePipeline resiliencePipeline
	)
{
	private readonly IDbContextFactory<ScriptsDbContext> _contextFactory = contextFactory;
	private readonly ResiliencePipeline _resiliencePipeline = resiliencePipeline;

	public async Task<Playlist> AddAsync(Playlist playlist, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				context.Playlists.Add(playlist);
				await context.SaveChangesAsync(token);

				return playlist;
			},
			ct
		);
	}

	public async Task<Playlist?> GetByPlaylistIdAsync(string playlistId, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				return await context
					.Playlists.AsNoTracking()
					.FirstOrDefaultAsync(p => p.PlaylistId == playlistId, cancellationToken: token);
			},
			ct
		);
	}

	public async Task<Playlist> UpsertAsync(Playlist playlist, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				var existing = await context
					.Playlists.AsNoTracking()
					.FirstOrDefaultAsync(p => p.PlaylistId == playlist.PlaylistId, cancellationToken: token);

				if (existing is not null)
				{
					await context
						.Playlists.Where(p => p.PlaylistId == playlist.PlaylistId)
						.ExecuteUpdateAsync(
							setters => setters
								.SetProperty(p => p.Title, playlist.Title)
								.SetProperty(p => p.TitleLower, playlist.TitleLower)
								.SetProperty(p => p.Description, playlist.Description)
								.SetProperty(p => p.ChannelName, playlist.ChannelName)
								.SetProperty(p => p.ChannelNameLower, playlist.ChannelNameLower),
							token
						);

					playlist.Id = existing.Id;
					return playlist;
				}

				context.Playlists.Add(playlist);
				await context.SaveChangesAsync(token);

				return playlist;
			},
			ct
		);
	}
}
