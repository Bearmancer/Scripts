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

    internal void Update(int page, int total)
    {
        LastPage = page;
        TotalFetched = total;
        LastUpdated = DateTime.Now;
    }
}

public class LastFmService(string apiKey, string username)
{
    private const int PerPage = 200;

    private readonly LastfmClient client = new(apiKey);

    internal void FetchScrobblesSince(
        DateTime? fetchAfter,
        FetchState state,
        Action<int, int, TimeSpan> onProgress,
        CancellationToken ct
    )
    {
        Console.Debug("FetchScrobblesSince: fetchAfter={0}", fetchAfter?.ToString() ?? "null");

        List<Scrobble> scrobbles = [];
        var page = state.LastPage > 0 ? state.LastPage + 1 : 1;
        var totalFetched = state.TotalFetched;
        var stopwatch = Stopwatch.StartNew();

        Console.Debug("Starting from page {0}, {1} already fetched", page, totalFetched);

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
                var newScrobbles = batch.Where(s => s.PlayedAt > fetchAfter).ToList();

                foreach (var s in newScrobbles)
                    Console.Debug("  New: {0} at {1:yyyy/MM/dd HH:mm:ss}", s.TrackName, s.PlayedAt);

                if (newScrobbles.Count == 0)
                {
                    var firstExisting = batch.First();
                    Console.Debug(
                        "  Exists: {0} at {1:yyyy/MM/dd HH:mm:ss}",
                        firstExisting.TrackName,
                        firstExisting.PlayedAt
                    );
                    break;
                }

                scrobbles.AddRange(newScrobbles);
                totalFetched += newScrobbles.Count;

                if (newScrobbles.Count < batch.Count)
                {
                    var firstExisting = batch.First(s => s.PlayedAt <= fetchAfter);
                    Console.Debug(
                        "  Exists: {0} at {1:yyyy/MM/dd HH:mm:ss}",
                        firstExisting.TrackName,
                        firstExisting.PlayedAt
                    );
                    StateManager.Save(StateManager.LastFmScrobblesFile, scrobbles);
                    onProgress(page, totalFetched, stopwatch.Elapsed);
                    break;
                }
            }
            else
            {
                scrobbles.AddRange(batch);
                totalFetched += batch.Count;
            }

            StateManager.Save(StateManager.LastFmScrobblesFile, scrobbles);
            onProgress(page, totalFetched, stopwatch.Elapsed);

            if (batch.Count < PerPage)
            {
                Console.Debug("Last page reached ({0} tracks)", batch.Count);
                break;
            }

            page++;
            Resilience.Delay(ServiceType.LastFm);
        }

        stopwatch.Stop();
        Console.Debug(
            "Fetch complete: {0} scrobbles in {1:mm\\:ss}",
            scrobbles.Count,
            stopwatch.Elapsed
        );
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

        return response
            .Select(track => new Scrobble(
                TrackName: track.Name
                    ?? throw new InvalidOperationException($"{nameof(track.Name)} is null"),
                ArtistName: track.Artist?.Name ?? "",
                AlbumName: track.Album?.Name ?? "",
                PlayedAt: track.Date
            ))
            .ToList();
    }

    internal static List<Scrobble> LoadScrobbles() =>
        StateManager.Load<List<Scrobble>>(StateManager.LastFmScrobblesFile);

    public static void DeleteScrobblesCache() =>
        StateManager.Delete(StateManager.LastFmScrobblesFile);
}
