using System.Text.Json;
using CSharpScripts.Data.Entities;
using CSharpScripts.Models;
using Microsoft.EntityFrameworkCore;

namespace CSharpScripts.Data.Persistence;

internal sealed class ReleaseProgressService(IDbContextFactory<ScriptsDbContext> contextFactory)
{
	public async Task AppendTrackAsync(
		string releaseId,
		TrackInfo track,
		CancellationToken ct = default
	)
	{
		await using var context = await contextFactory.CreateDbContextAsync(ct);

		var entity = new ReleaseProgress
		{
			ReleaseId = releaseId,
			DiscNumber = track.DiscNumber,
			TrackNumber = track.TrackNumber,
			Title = track.Title,
			Duration = track.Duration?.ToString(),
			RecordingYear = track.RecordingYear,
			Composer = track.Composer,
			WorkName = track.WorkName,
			Conductor = track.Conductor,
			Orchestra = track.Orchestra,
			Soloists =
				track.Soloists.Count > 0
					? JsonSerializer.SerializeToDocument(track.Soloists)
					: null,
			Artist = track.Artist,
			RecordingVenue = track.RecordingVenue,
			RecordingId = track.RecordingId,
		};

		context.ReleaseProgress.Add(entity);
		await context.SaveChangesAsync(ct);
	}

	public async Task<List<TrackInfo>> LoadAsync(string releaseId, CancellationToken ct = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(ct);

		var rows = await context
			.ReleaseProgress.AsNoTracking()
			.Where(r => r.ReleaseId == releaseId)
			.OrderBy(r => r.DiscNumber)
			.ThenBy(r => r.TrackNumber)
			.ToListAsync(ct);

		return rows.Select(MapToTrackInfo).ToList();
	}

	public async Task<int> DeleteAsync(string releaseId, CancellationToken ct = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(ct);
		return await context
			.ReleaseProgress.Where(r => r.ReleaseId == releaseId)
			.ExecuteDeleteAsync(cancellationToken: ct);
	}

	private static TrackInfo MapToTrackInfo(ReleaseProgress r)
	{
		List<string> soloists = [];
		if (r.Soloists is not null)
		{
			try
			{
				soloists =
					JsonSerializer.Deserialize<List<string>>(r.Soloists.RootElement.GetRawText())
					?? [];
			}
			catch (JsonException ex)
			{
				Log.Warning("Failed to deserialize soloists JSON: {Message}", ex.Message);
			}
		}

		TimeSpan? duration = null;
		if (r.Duration is not null && TimeSpan.TryParse(r.Duration, out var ts))
			duration = ts;

		return new TrackInfo(
			r.DiscNumber,
			r.TrackNumber,
			r.Title,
			duration,
			r.RecordingYear,
			r.Composer,
			r.WorkName,
			r.Conductor,
			r.Orchestra,
			soloists,
			r.Artist,
			r.RecordingVenue,
			r.RecordingId
		);
	}
}
