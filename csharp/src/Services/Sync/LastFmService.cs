using Hqub.Lastfm;
using Hqub.Lastfm.Entities;
using Scrobble = CSharpScripts.Models.Scrobble;

namespace CSharpScripts.Services.Sync.LastFm;

internal sealed class LastFmService(string apiKey, string username, IDbContextFactory<ScriptsDbContext> contextFactory)
{
	private const int PerPage = 200;

	private readonly LastfmClient Client = new(apiKey: apiKey);

	internal async Task FetchScrobblesSinceAsync(
		DateTime? fetchAfter,
		FetchState state,
		Action<int, int, TimeSpan, DateTimeOffset?, DateTimeOffset?> onProgress,
		CancellationToken ct
	)
	{
		Log.Debug(messageTemplate: "FetchScrobblesSinceAsync entry {FetchAfter}", fetchAfter);
		List<Scrobble> existingScrobbles = await LoadScrobblesAsync();
		List<Scrobble> newScrobbles = [];

		var isIncremental = fetchAfter is { };
		var page = DetermineStartPage(fetchAfter: fetchAfter, state: state);
		var totalFetched = isIncremental ? 0 : state.TotalFetched;
		Stopwatch stopwatch = Stopwatch.StartNew();

		while (!ct.IsCancellationRequested)
		{
			List<Scrobble>? batch = await FetchPageAsync(page: page, ct: ct);

			if (ct.IsCancellationRequested || batch is null || batch.Count == 0)
				break;

			batch.RemoveAll(s => !s.PlayedAt.HasValue);

			if (batch.Count == 0)
			{
				page++;
				continue;
			}

			if (fetchAfter is { })
			{
				List<Scrobble> freshScrobbles = [];
				foreach (var track in batch)
				{
					if (track.PlayedAt > fetchAfter)
						freshScrobbles.Add(track);
				}

				if (freshScrobbles.Count == 0)
					break;

				newScrobbles.AddRange(collection: freshScrobbles);
				totalFetched += freshScrobbles.Count;

				if (freshScrobbles.Count < batch.Count)
				{
					await SaveMergedScrobblesAsync(
						existing: existingScrobbles,
						newOnes: newScrobbles
					);
					DateTimeOffset? oldest = newScrobbles.Min(s => s.PlayedAt);
					DateTimeOffset? newest = newScrobbles.Max(s => s.PlayedAt);
					onProgress(
						arg1: page,
						arg2: totalFetched,
						arg3: stopwatch.Elapsed,
						arg4: oldest,
						arg5: newest
					);
					break;
				}
			}
			else
			{
				newScrobbles.AddRange(collection: batch);
				totalFetched += batch.Count;
			}

			await SaveMergedScrobblesAsync(existing: existingScrobbles, newOnes: newScrobbles);
			DateTimeOffset? batchOldest = batch.Min(s => s.PlayedAt);
			DateTimeOffset? batchNewest = batch.Max(s => s.PlayedAt);
			onProgress(
				arg1: page,
				arg2: totalFetched,
				arg3: stopwatch.Elapsed,
				arg4: batchOldest,
				arg5: batchNewest
			);

			if (batch.Count < PerPage)
				break;

			page++;
		}

		stopwatch.Stop();

		if (newScrobbles.Count > 0)
		{
			Log.Information(
				messageTemplate: "Fetched {0} new scrobbles in {1:mm\\:ss}",
				newScrobbles.Count,
				stopwatch.Elapsed
			);
		}
		else
			Log.Information(messageTemplate: "No new scrobbles found");
		Log.Debug(messageTemplate: "FetchScrobblesSinceAsync exit {Count}", newScrobbles.Count);
	}

	private static async Task SaveMergedScrobblesAsync(
		List<Scrobble> existing,
		List<Scrobble> newOnes
	)
	{
		HashSet<DateTimeOffset?> existingTimes = [];
		existingTimes.UnionWith(existing.Select(s => s.PlayedAt));
		List<Scrobble> merged = [];
		foreach (var newScrobble in newOnes)
		{
			if (!existingTimes.Contains(item: newScrobble.PlayedAt))
				merged.Add(newScrobble);
		}

		merged.AddRange(collection: existing);
		await StateManager.SaveStateAsync(
			fileName: StateManager.LastFmScrobblesFile,
			state: merged
		);
	}

	private async Task<List<Scrobble>?> FetchPageAsync(int page, CancellationToken ct)
	{
		Log.Debug(messageTemplate: "FetchPageAsync entry {Page}", page);
		PagedResponse<Track>? response = await Resilience.ExecuteAsync(
			operation: "LastFm.GetRecentTracks",
			() => Client.User.GetRecentTracksAsync(user: username, limit: PerPage, page: page),
			ct: ct
		);

		if (ct.IsCancellationRequested || response is null)
		{
			Log.Warning(messageTemplate: "FetchPageAsync exit null (cancelled or no response)");
			return null;
		}

		List<Scrobble> result = [];
		foreach (var track in response)
		{
			result.Add(
				new Scrobble(
					track.Name
						?? throw new InvalidOperationException($"{nameof(track.Name)} is null"),
					track.Artist?.Name ?? "",
					track.Album?.Name ?? "",
					PlayedAt: track.Date
				)
			);
		}
		Log.Debug(messageTemplate: "FetchPageAsync exit {Count}", result.Count);
		return result;
	}

	internal static async Task<List<Scrobble>> LoadScrobblesAsync() =>
		await StateManager.LoadStateAsync<List<Scrobble>>(
			fileName: StateManager.LastFmScrobblesFile
		);

	public static void DeleteScrobblesCache() =>
		StateManager.Delete(fileName: StateManager.LastFmScrobblesFile);

	private static int DetermineStartPage(DateTimeOffset? fetchAfter, FetchState state) =>
		fetchAfter is { } ? 1
		: state.LastPage > 0 ? state.LastPage + 1
		: 1;

	internal async Task<CSharpScripts.Data.Entities.Artist?> FindArtistByNameAsync(string name, CancellationToken ct = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Artists
			.AsNoTracking()
			.FirstOrDefaultAsync(a => EF.Functions.ILike(a.Name, name), ct);
	}
}
