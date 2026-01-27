namespace CSharpScripts.Services.Sync.LastFm;

#region Models

public record Scrobble(string TrackName, string ArtistName, string AlbumName, DateTime? PlayedAt)
{
	public string FormattedDate =>
		PlayedAt?.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
}

public record FetchState
{
	public int LastPage { get; set; }
	public int TotalFetched { get; set; }
	public DateTime LastUpdated { get; set; } = DateTime.Now;
	public string? SpreadsheetId { get; set; }
	public bool FetchComplete { get; set; }
	public DateTime? OldestScrobble { get; set; }
	public DateTime? NewestScrobble { get; set; }

	internal void Update(int page, int total, DateTime? oldest = null, DateTime? newest = null)
	{
		LastPage = page;
		TotalFetched = total;
		LastUpdated = DateTime.Now;
		if (oldest.HasValue && (!OldestScrobble.HasValue || oldest < OldestScrobble))
			OldestScrobble = oldest;
		if (newest.HasValue && (!NewestScrobble.HasValue || newest > NewestScrobble))
			NewestScrobble = newest;
	}
}

#endregion

#region Service

public class LastFmService(string apiKey, string username)
{
	private const int PerPage = 200;

	private readonly LastfmClient client = new(apiKey);

	internal async Task FetchScrobblesSinceAsync(
		DateTime? fetchAfter,
		FetchState state,
		Action<int, int, TimeSpan, DateTime?, DateTime?> onProgress,
		CancellationToken ct
	)
	{
		List<Scrobble> existingScrobbles = LoadScrobbles();
		List<Scrobble> newScrobbles = [];

		var isIncremental = fetchAfter is { };
		var page =
			isIncremental ? 1
			: state.LastPage > 0 ? state.LastPage + 1
			: 1;
		var totalFetched = isIncremental ? 0 : state.TotalFetched;
		var stopwatch = Stopwatch.StartNew();

		while (!ct.IsCancellationRequested)
		{
			List<Scrobble>? batch = await FetchPageAsync(page, ct);

			if (ct.IsCancellationRequested || batch is null || batch.Count == 0)
				break;

			// Filter out "now playing" tracks (they have null PlayedAt and aren't real scrobbles yet)
			batch = [.. batch.Where(s => s.PlayedAt.HasValue)];

			if (batch.Count == 0)
			{
				page++;
				continue;
			}

			if (fetchAfter is { })
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
			Console.Info(
				"Fetched {0} new scrobbles in {1:mm\\:ss}",
				newScrobbles.Count,
				stopwatch.Elapsed
			);
		else
			Console.Info("No new scrobbles found");
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
		PagedResponse<Hqub.Lastfm.Entities.Track>? response = await Resilience.ExecuteAsync(
			"LastFm.GetRecentTracks",
			() => client.User.GetRecentTracksAsync(user: username, limit: PerPage, page: page),
			ct
		);

		if (ct.IsCancellationRequested || response is null)
			return null;

		return
		[
			.. response.Select(track => new Scrobble(
				track.Name ?? throw new InvalidOperationException($"{nameof(track.Name)} is null"),
				track.Artist?.Name ?? "",
				track.Album?.Name ?? "",
				PlayedAt: track.Date
			)),
		];
	}

	internal static List<Scrobble> LoadScrobbles() =>
		StateManager.Load<List<Scrobble>>(StateManager.LastFmScrobblesFile);

	public static void DeleteScrobblesCache() =>
		StateManager.Delete(StateManager.LastFmScrobblesFile);
}

#endregion
