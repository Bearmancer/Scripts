namespace CSharpScripts.Services.Sync.LastFm;

public record Scrobble(string TrackName, string ArtistName, string AlbumName, DateTime? PlayedAt)
{
    internal string FormattedDate => PlayedAt?.ToString("yyyy/MM/dd HH:mm:ss") ?? "";
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

public class LastFmService(string apiKey, string username)
{
    private const int PerPage = 200;

    private readonly LastfmClient client = new(apiKey);

    internal void FetchScrobblesSince(
        DateTime? fetchAfter,
        FetchState state,
        Action<int, int, TimeSpan, DateTime?, DateTime?> onProgress,
        CancellationToken ct
    )
    {
        Console.Debug("FetchScrobblesSince: fetchAfter={0}", fetchAfter?.ToString() ?? "null");

        // Load existing cache to append to
        List<Scrobble> existingScrobbles = LoadScrobbles();
        List<Scrobble> newScrobbles = [];

        var isIncremental = fetchAfter is not null;
        var page = isIncremental ? 1 : (state.LastPage > 0 ? state.LastPage + 1 : 1);
        var totalFetched = isIncremental ? 0 : state.TotalFetched;
        var stopwatch = Stopwatch.StartNew();

        Console.Debug(
            "Mode: {0}, starting from page {1}, {2} already fetched, {3} in cache",
            isIncremental ? "incremental" : "full",
            page,
            totalFetched,
            existingScrobbles.Count
        );

        while (!ct.IsCancellationRequested)
        {
            Console.Debug("Fetching page {0}", page);
            var batch = FetchPage(page, ct);

            if (ct.IsCancellationRequested || batch is null || batch.Count == 0)
            {
                if (batch is null || batch.Count == 0)
                    Console.Debug("No more tracks to fetch");
                break;
            }

            if (fetchAfter is not null)
            {
                var freshScrobbles = batch.Where(s => s.PlayedAt > fetchAfter).ToList();

                foreach (var s in freshScrobbles)
                    Console.Debug(
                        "New: \"{0}\" at {1:yyyy/MM/dd HH:mm:ss}",
                        s.TrackName,
                        s.PlayedAt
                    );

                if (freshScrobbles.Count == 0)
                {
                    var firstExisting = batch.First();
                    Console.Debug(
                        "Exists: \"{0}\" at {1:yyyy/MM/dd HH:mm:ss}",
                        firstExisting.TrackName,
                        firstExisting.PlayedAt
                    );
                    break;
                }

                newScrobbles.AddRange(freshScrobbles);
                totalFetched += freshScrobbles.Count;

                if (freshScrobbles.Count < batch.Count)
                {
                    var firstExisting = batch.First(s => s.PlayedAt <= fetchAfter);
                    Console.Debug(
                        "Exists: \"{0}\" at {1:yyyy/MM/dd HH:mm:ss}",
                        firstExisting.TrackName,
                        firstExisting.PlayedAt
                    );
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
            {
                Console.Debug("Last page reached ({0} tracks)", batch.Count);
                break;
            }

            page++;
            Resilience.Delay(ServiceType.LastFm);
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

    private List<Scrobble>? FetchPage(int page, CancellationToken ct)
    {
        var response = Resilience.Execute(
            operationName: "LastFm.GetRecentTracks",
            action: () =>
                client.User.GetRecentTracksAsync(username, limit: PerPage, page: page).Result,
            postAction: () => Resilience.Delay(ServiceType.LastFm),
            ct: ct
        );

        if (ct.IsCancellationRequested || response is null)
            return null;

        return
        [
            .. response.Select(track => new Scrobble(
                TrackName: track.Name
                    ?? throw new InvalidOperationException($"{nameof(track.Name)} is null"),
                ArtistName: track.Artist?.Name ?? "",
                AlbumName: track.Album?.Name ?? "",
                PlayedAt: track.Date
            )),
        ];
    }

    internal static List<Scrobble> LoadScrobbles() =>
        StateManager.Load<List<Scrobble>>(StateManager.LastFmScrobblesFile);

    public static void DeleteScrobblesCache() =>
        StateManager.Delete(StateManager.LastFmScrobblesFile);
}
