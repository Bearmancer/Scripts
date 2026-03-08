using Hqub.Lastfm;

namespace CSharpScripts.Services.Sync.LastFm;

internal sealed class LastFmService(string apiKey, string username)
{
	private const int PerPage = 200;

	private readonly LastfmClient Client = new(apiKey);

	internal async Task FetchScrobblesSinceAsync(
		DateTime? fetchAfter,
		FetchState state,
		Action<int, int, TimeSpan, DateTime?, DateTime?> onProgress,
		CancellationToken ct
	)
	{
		Log.Debug("FetchScrobblesSinceAsync entry {FetchAfter}", fetchAfter);
		List<Scrobble> existingScrobbles = LoadScrobbles();
		List<Scrobble> newScrobbles = [];

		var isIncremental = fetchAfter is not null;
		var page = DetermineStartPage(fetchAfter, state);
		var totalFetched = isIncremental ? 0 : state.TotalFetched;
		var stopwatch = Stopwatch.StartNew();

		while (!ct.IsCancellationRequested)
		{
			List<Scrobble>? batch = await FetchPageAsync(page, ct);

			if (ct.IsCancellationRequested || batch is null || batch.Count == 0)
				break;

			batch = [.. batch.Where(s => s.PlayedAt.HasValue)];

			if (batch.Count == 0)
			{
				page++;
				continue;
			}

			if (fetchAfter is not null)
			{
				List<Scrobble> freshScrobbles = [.. batch.Where(s => s.PlayedAt > fetchAfter)];

				if (freshScrobbles.Count == 0)
					break;

				newScrobbles.AddRange(freshScrobbles);
				totalFetched += freshScrobbles.Count;

				if (freshScrobbles.Count < batch.Count)
				{
					SaveMergedScrobbles(existingScrobbles, newScrobbles);
					DateTime? oldest = newScrobbles.Min(s => s.PlayedAt);
					DateTime? newest = newScrobbles.Max(s => s.PlayedAt);
					onProgress(page, totalFetched, stopwatch.Elapsed, oldest, newest);
					break;
				}
			}
			else
			{
				newScrobbles.AddRange(batch);
				totalFetched += batch.Count;
			}

			SaveMergedScrobbles(existingScrobbles, newScrobbles);
			DateTime? batchOldest = batch.Min(s => s.PlayedAt);
			DateTime? batchNewest = batch.Max(s => s.PlayedAt);
			onProgress(page, totalFetched, stopwatch.Elapsed, batchOldest, batchNewest);

			if (batch.Count < PerPage)
				break;

			page++;
		}

		stopwatch.Stop();

		if (newScrobbles.Count > 0)
			Log.Information(
				"Fetched {0} new scrobbles in {1:mm\\:ss}",
				newScrobbles.Count,
				stopwatch.Elapsed
			);
		else
			Log.Information("No new scrobbles found");
		Log.Debug("FetchScrobblesSinceAsync exit {Count}", newScrobbles.Count);
	}

	private static void SaveMergedScrobbles(List<Scrobble> existing, List<Scrobble> newOnes)
	{
		HashSet<DateTime?> existingTimes = [.. existing.Select(s => s.PlayedAt)];
		List<Scrobble> merged =
		[
			.. newOnes.Where(s => !existingTimes.Contains(s.PlayedAt)),
			.. existing,
		];
		StateManager.Save(StateManager.LastFmScrobblesFile, merged);
	}

	private async Task<List<Scrobble>?> FetchPageAsync(int page, CancellationToken ct)
	{
		Log.Debug("FetchPageAsync entry {Page}", page);
		PagedResponse<Hqub.Lastfm.Entities.Track>? response = await Resilience.ExecuteAsync(
			"LastFm.GetRecentTracks",
			() => Client.User.GetRecentTracksAsync(user: username, limit: PerPage, page: page),
			ct
		);

		if (ct.IsCancellationRequested || response is null)
		{
			Log.Warning("FetchPageAsync exit null (cancelled or no response)");
			return null;
		}

		List<Scrobble> result =
		[
			.. response.Select(track => new Scrobble(
				track.Name ?? throw new InvalidOperationException($"{nameof(track.Name)} is null"),
				track.Artist?.Name ?? "",
				track.Album?.Name ?? "",
				PlayedAt: track.Date
			)),
		];
		Log.Debug("FetchPageAsync exit {Count}", result.Count);
		return result;
	}

	internal static List<Scrobble> LoadScrobbles() =>
		StateManager.Load<List<Scrobble>>(StateManager.LastFmScrobblesFile);

	public static void DeleteScrobblesCache() =>
		StateManager.Delete(StateManager.LastFmScrobblesFile);

	private static int DetermineStartPage(DateTime? fetchAfter, FetchState state) =>
		fetchAfter is not null ? 1
		: state.LastPage > 0 ? state.LastPage + 1
		: 1;
}
