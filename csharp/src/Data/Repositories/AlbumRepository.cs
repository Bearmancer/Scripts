using Scripts.Data.Entities;
using Scripts.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Scripts.Data.Repositories;

internal sealed class AlbumRepository : IAlbumRepository
{
	private readonly IDbContextFactory<ScriptsDbContext> _contextFactory;
	private readonly ResiliencePipeline _resiliencePipeline;

	public AlbumRepository(
		IDbContextFactory<ScriptsDbContext> contextFactory,
		ResiliencePipeline resiliencePipeline
	)
	{
		_contextFactory = contextFactory;
		_resiliencePipeline = resiliencePipeline;
	}

	public async Task<Album?> GetByArtistAndTitleAsync(
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
					.Albums.AsNoTracking()
					.FirstOrDefaultAsync(
						a => a.ArtistId == artistId && a.Title == title,
						cancellationToken: token
					);
			},
			ct
		);
	}

	public async Task<Album> AddAsync(Album album, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				context.Albums.Add(album);
				await context.SaveChangesAsync(token);

				return album;
			},
			ct
		);
	}

	public async Task<Album?> GetCachedByArtistAndTitleAsync(
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
					.Albums.AsNoTracking()
					.FirstOrDefaultAsync(
						a => a.ArtistId == artistId && a.Title == title,
						cancellationToken: token
					);
			},
			ct
		);
	}

	public async Task<int> UpsertCacheAsync(Album album, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				var updated = await context
					.Albums.Where(a => a.ArtistId == album.ArtistId && a.Title == album.Title)
					.ExecuteUpdateAsync(
						setters => setters.SetProperty(a => a.ReleaseDate, album.ReleaseDate),
						token
					);

				if (updated == 0)
				{
					context.Albums.Add(album);
					await context.SaveChangesAsync(token);
					return 1;
				}

				return updated;
			},
			ct
		);
	}
}
