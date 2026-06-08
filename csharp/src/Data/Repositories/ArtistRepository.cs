using Scripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Scripts.Data.Repositories;

internal sealed class ArtistRepository
{
	private readonly IDbContextFactory<ScriptsDbContext> _contextFactory;
	private readonly ResiliencePipeline _resiliencePipeline;

	public ArtistRepository(
		IDbContextFactory<ScriptsDbContext> contextFactory,
		ResiliencePipeline resiliencePipeline
	)
	{
		_contextFactory = contextFactory;
		_resiliencePipeline = resiliencePipeline;
	}

	public async Task<Artist?> GetByNameAsync(string name, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				return await context
					.Artists.AsNoTracking()
					.FirstOrDefaultAsync(a => a.Name == name, cancellationToken: token);
			},
			ct
		);
	}

	public async Task<Artist?> GetByFuzzyNameAsync(string name, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				return await context
					.Artists.AsNoTracking()
					.FirstOrDefaultAsync(a => EF.Functions.ILike(a.Name, name), cancellationToken: token);
			},
			ct
		);
	}

	public async Task<Artist> AddAsync(Artist artist, CancellationToken ct = default)
	{
		return await _resiliencePipeline.ExecuteAsync(
			async token =>
			{
				await using var context = await _contextFactory.CreateDbContextAsync(token);

				context.Artists.Add(artist);
				await context.SaveChangesAsync(token);

				return artist;
			},
			ct
		);
	}
}
