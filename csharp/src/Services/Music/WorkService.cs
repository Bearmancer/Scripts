using Scripts.Data;
using Scripts.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Scripts.Services.Music;

internal sealed class WorkService
{
	private readonly IDbContextFactory<ScriptsDbContext> _contextFactory;

	public WorkService(IDbContextFactory<ScriptsDbContext> contextFactory)
	{
		_contextFactory = contextFactory;
	}

	public async Task<int> GetOrCreateWorkAsync(string title, string? composer, CancellationToken ct = default)
	{
		await using var db = await _contextFactory.CreateDbContextAsync(ct);

		var work = await db.MusicWorks
			.FirstOrDefaultAsync(w => 
				w.Title.Equals(title, StringComparison.OrdinalIgnoreCase) && 
				(composer == null || w.Composer == null || w.Composer.Equals(composer, StringComparison.OrdinalIgnoreCase)), 
				ct);

		if (work != null)
			return work.Id;

		var newWork = new MusicWork
		{
			Title = title,
			Composer = composer
		};

		db.MusicWorks.Add(newWork);
		await db.SaveChangesAsync(ct);

		return newWork.Id;
	}
}
