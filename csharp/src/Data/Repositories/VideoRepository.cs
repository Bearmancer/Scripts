using Scripts.Data.Entities;

namespace Scripts.Data.Repositories;

internal sealed class VideoRepository(
	IDbContextFactory<ScriptsDbContext> contextFactory,
	ResiliencePipeline resiliencePipeline
	)
{
	private readonly IDbContextFactory<ScriptsDbContext> _contextFactory = contextFactory;
	private readonly ResiliencePipeline _resiliencePipeline = resiliencePipeline;

	public async Task<Video> AddAsync(Video video, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				context.Videos.Add(video);
				await context.SaveChangesAsync(token);

				return video;
			},
			ct
		);
	}

	public async Task<Video?> GetByUrlAsync(string url, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				return await context
					.Videos.AsNoTracking()
					.FirstOrDefaultAsync(v => v.Url == url, cancellationToken: token);
			},
			ct
		);
	}

	public async Task<IReadOnlyList<Video>> GetByChannelAsync(
		string channelName,
		CancellationToken ct = default
	)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				return await context
					.Videos.AsNoTracking()
					.Where(v => v.ChannelName == channelName)
					.OrderByDescending(v => v.UploadDate)
					.ToListAsync(token);
			},
			ct
		);
	}

	public async Task<Video?> GetByVideoIdAsync(string videoId, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				return await context
					.Videos.AsNoTracking()
					.FirstOrDefaultAsync(v => v.VideoId == videoId, cancellationToken: token);
			},
			ct
		);
	}

	public async Task<Video> UpsertAsync(Video video, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				var existing = await context
					.Videos.AsNoTracking()
					.FirstOrDefaultAsync(v => v.VideoId == video.VideoId, cancellationToken: token);

				if (existing is not null)
				{
					await context
						.Videos.Where(v => v.VideoId == video.VideoId)
						.ExecuteUpdateAsync(
							setters => setters
								.SetProperty(v => v.Title, video.Title)
								.SetProperty(v => v.TitleLower, video.TitleLower)
								.SetProperty(v => v.Description, video.Description)
								.SetProperty(v => v.ChannelName, video.ChannelName)
								.SetProperty(v => v.ChannelNameLower, video.ChannelNameLower)
								.SetProperty(v => v.TranslatedTitle, video.TranslatedTitle)
								.SetProperty(v => v.TranslatedDescription, video.TranslatedDescription)
								.SetProperty(v => v.Metadata, video.Metadata)
								.SetProperty(v => v.SyncedAt, video.SyncedAt),
							token
						);

					video.Id = existing.Id;
					return video;
				}

				context.Videos.Add(video);
				await context.SaveChangesAsync(token);

				return video;
			},
			ct
		);
	}
}
