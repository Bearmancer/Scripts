using CSharpScripts.Data.Entities;
using CSharpScripts.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace CSharpScripts.Data.Repositories;

internal sealed class VideoRepository : IVideoRepository
{
	private readonly IDbContextFactory<ScriptsDbContext> _contextFactory;
	private readonly ResiliencePipeline _resiliencePipeline;

	public VideoRepository(
		IDbContextFactory<ScriptsDbContext> contextFactory,
		ResiliencePipeline resiliencePipeline
	)
	{
		_contextFactory = contextFactory;
		_resiliencePipeline = resiliencePipeline;
	}

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
}
