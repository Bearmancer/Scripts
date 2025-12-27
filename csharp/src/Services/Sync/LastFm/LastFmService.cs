namespace CSharpScripts.Services.Sync.LastFm;

#region Models

public record Scrobble(string TrackName, string ArtistName, string AlbumName, DateTime? PlayedAt)
{
    public string FormattedDate =>
        PlayedAt?.ToString(format: "yyyy/MM/dd HH:mm:ss", provider: CultureInfo.InvariantCulture)
        ?? "";
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

    private readonly LastfmClient client = new(apiKey: apiKey);

    internal async Task FetchScrobblesSinceAsync(
        DateTime? fetchAfter,
        FetchState state,
        Action<int, int, TimeSpan, DateTime?, DateTime?> onProgress,
        CancellationToken ct
    )
    {
        Console.Debug(
            message: "FetchScrobblesSince: fetchAfter={0}",
            fetchAfter?.ToString() ?? "null"
        );

        var existingScrobbles = LoadScrobbles();
        List<Scrobble> newScrobbles = [];

        bool isIncremental = fetchAfter is { };
        int page =
            isIncremental ? 1
            : state.LastPage > 0 ? state.LastPage + 1
            : 1;
        int totalFetched = isIncremental ? 0 : state.TotalFetched;
        var stopwatch = Stopwatch.StartNew();

        Console.Debug(
            message: "Mode: {0}, starting from page {1}, {2} already fetched, {3} in cache",
            isIncremental ? "incremental" : "full",
            page,
            totalFetched,
            existingScrobbles.Count
        );

        while (!ct.IsCancellationRequested)
        {
            Console.Debug(message: "Fetching page {0}", page);
            var batch = await FetchPageAsync(page: page, ct: ct);

            if (ct.IsCancellationRequested || batch is null || batch.Count == 0)
            {
                if (batch is null || batch.Count == 0)
                    Console.Debug(message: "No more tracks to fetch");
                break;
            }

            if (fetchAfter is { })
            {
                List<Scrobble> freshScrobbles = [.. batch.Where(s => s.PlayedAt > fetchAfter)];

                foreach (var s in freshScrobbles)
                    Console.Debug(
                        message: "New: \"{0}\" at {1:yyyy/MM/dd HH:mm:ss}",
                        s.TrackName,
                        s.PlayedAt
                    );

                if (freshScrobbles.Count == 0)
                {
                    var firstExisting = batch.First();
                    Console.Debug(
                        message: "Exists: \"{0}\" at {1:yyyy/MM/dd HH:mm:ss}",
                        firstExisting.TrackName,
                        firstExisting.PlayedAt
                    );
                    break;
                }

                newScrobbles.AddRange(collection: freshScrobbles);
                totalFetched += freshScrobbles.Count;

                if (freshScrobbles.Count < batch.Count)
                {
                    var firstExisting = batch.First(s => s.PlayedAt <= fetchAfter);
                    Console.Debug(
                        message: "Exists: \"{0}\" at {1:yyyy/MM/dd HH:mm:ss}",
                        firstExisting.TrackName,
                        firstExisting.PlayedAt
                    );
                    SaveMergedScrobbles(existing: existingScrobbles, newOnes: newScrobbles);
                    var oldest = newScrobbles.Min(s => s.PlayedAt);
                    var newest = newScrobbles.Max(s => s.PlayedAt);
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

            SaveMergedScrobbles(existing: existingScrobbles, newOnes: newScrobbles);
            var batchOldest = batch.Min(s => s.PlayedAt);
            var batchNewest = batch.Max(s => s.PlayedAt);
            onProgress(
                arg1: page,
                arg2: totalFetched,
                arg3: stopwatch.Elapsed,
                arg4: batchOldest,
                arg5: batchNewest
            );

            if (batch.Count < PerPage)
            {
                Console.Debug(message: "Last page reached ({0} tracks)", batch.Count);
                break;
            }

            page++;
        }

        stopwatch.Stop();

        if (newScrobbles.Count > 0)
            Console.Info(
                message: "Fetched {0} new scrobbles in {1:mm\\:ss}",
                newScrobbles.Count,
                stopwatch.Elapsed
            );
        else
            Console.Info(message: "No new scrobbles found");
    }

    private static void SaveMergedScrobbles(List<Scrobble> existing, List<Scrobble> newOnes)
    {
        HashSet<DateTime?> existingTimes = [.. existing.Select(s => s.PlayedAt)];
        List<Scrobble> merged =
        [
            .. newOnes.Where(s => !existingTimes.Contains(item: s.PlayedAt)),
            .. existing,
        ];
        StateManager.Save(fileName: StateManager.LastFmScrobblesFile, state: merged);
    }

    private async Task<List<Scrobble>?> FetchPageAsync(int page, CancellationToken ct)
    {
        var response = await Resilience.ExecuteAsync(
            operation: "LastFm.GetRecentTracks",
            () => client.User.GetRecentTracksAsync(user: username, limit: PerPage, page: page),
            ct: ct
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
        StateManager.Load<List<Scrobble>>(fileName: StateManager.LastFmScrobblesFile);

    public static void DeleteScrobblesCache() =>
        StateManager.Delete(fileName: StateManager.LastFmScrobblesFile);
}

#endregion
