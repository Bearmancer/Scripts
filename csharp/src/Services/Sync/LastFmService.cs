using Scrobble = CSharpScripts.Models.Scrobble;
using Hqub.Lastfm;
using Hqub.Lastfm.Entities;

namespace CSharpScripts.Services.Sync.LastFm;

internal sealed class LastFmService(string apiKey, string username)
{
	private const int PerPage = 200;

	private readonly LastfmClient Client = new(apiKey: apiKey);

	internal async Task FetchScrobblesSinceAsync(
		DateTime? fetchAfter,
		FetchState state,
		Action<int, int, TimeSpan, DateTime?, DateTime?> onProgress,
		CancellationToken ct
	)
	{
		Log.Debug("FetchScrobblesSinceAsync entry {FetchAfter}", fetchAfter);
		List<Scrobble> existingScrobbles = await LoadScrobblesAsync();
		List<Scrobble> newScrobbles = [];

		var isIncremental = fetchAfter is not null;
		var page = DetermineStartPage(fetchAfter: fetchAfter, state: state);
		var totalFetched = isIncremental ? 0 : state.TotalFetched;
		var stopwatch = Stopwatch.StartNew();

		while (!ct.IsCancellationRequested)
		{
			List<Scrobble>? batch = await FetchPageAsync(page: page, ct);

			if (ct.IsCancellationRequested || batch is null || batch.Count == 0)
				break;

			batch.RemoveAll(s => !s.PlayedAt.HasValue);

			if (batch.Count == 0)
			{
				page++;
				continue;
			}

			if (fetchAfter is not null)
			{
				var freshScrobbles = new List<Scrobble>(batch.Count);
				// PERFORMANCE: Optimize foreach on List to for loop
				for (var i = 0; i < batch.Count; i++)
				{
					if (batch[i].PlayedAt > fetchAfter)
						freshScrobbles.Add(batch[i]);
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
					DateTime? oldest = Enumerable.Min(newScrobbles, s => s.PlayedAt);
					DateTime? newest = Enumerable.Max(newScrobbles, s => s.PlayedAt);
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
			DateTime? batchOldest = Enumerable.Min(batch, s => s.PlayedAt);
			DateTime? batchNewest = Enumerable.Max(batch, s => s.PlayedAt);
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
				"Fetched {0} new scrobbles in {1:mm\\:ss}",
				newScrobbles.Count,
				stopwatch.Elapsed
			);
		}
		else
			Log.Information("No new scrobbles found");
		Log.Debug("FetchScrobblesSinceAsync exit {Count}", newScrobbles.Count);
	}

	private static async Task SaveMergedScrobblesAsync(
		List<Scrobble> existing,
		List<Scrobble> newOnes
	)
	{
		HashSet<DateTime?> existingTimes = [];
		existingTimes.UnionWith(Enumerable.Select(existing, s => s.PlayedAt));
		var merged = new List<Scrobble>(existing.Count + newOnes.Count);
		// PERFORMANCE: Optimize foreach on List to for loop
		for (var i = 0; i < newOnes.Count; i++)
		{
			if (!existingTimes.Contains(item: newOnes[i].PlayedAt))
				merged.Add(newOnes[i]);
		}

		merged.AddRange(existing);
		await StateManager.SaveStateAsync(StateManager.LastFmScrobblesFile, merged);
	}

	private async Task<List<Scrobble>?> FetchPageAsync(int page, CancellationToken ct)
	{
		Log.Debug("FetchPageAsync entry {Page}", page);
		PagedResponse<Track>? response = await Resilience.ExecuteAsync(
			operation: "LastFm.GetRecentTracks",
			() => Client.User.GetRecentTracksAsync(user: username, limit: PerPage, page: page),
			ct
		);

		if (ct.IsCancellationRequested || response is null)
		{
			Log.Warning("FetchPageAsync exit null (cancelled or no response)");
			return null;
		}

		var result = new List<Scrobble>(response.Count);
		// PERFORMANCE: Optimize foreach to for loop on indexable collection
		for (var i = 0; i < response.Count; i++)
		{
			Track track = response[i];
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
		Log.Debug("FetchPageAsync exit {Count}", result.Count);
		return result;
	}

	internal static async Task<List<Scrobble>> LoadScrobblesAsync() =>
		await StateManager.LoadStateAsync<List<Scrobble>>(StateManager.LastFmScrobblesFile);

	public static void DeleteScrobblesCache() =>
		StateManager.Delete(fileName: StateManager.LastFmScrobblesFile);

	private static int DetermineStartPage(DateTime? fetchAfter, FetchState state) =>
		fetchAfter is not null ? 1
		: state.LastPage > 0 ? state.LastPage + 1
		: 1;
}




