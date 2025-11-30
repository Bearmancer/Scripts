namespace CSharpScripts.Services;

internal class LastFmService(string apiKey, string username)
{
    const int PerPage = 200;

    readonly LastfmClient client = new(apiKey);

    internal void FetchScrobblesSince(
        DateTime? fetchAfter,
        FetchState state,
        Action<int, int, TimeSpan> onProgress,
        CancellationToken ct
    )
    {
        Logger.Debug("FetchScrobblesSince: fetchAfter={0}", fetchAfter?.ToString() ?? "null");

        List<Scrobble> scrobbles = [];
        var page = state.LastPage > 0 ? state.LastPage + 1 : 1;
        var totalFetched = state.TotalFetched;
        var stopwatch = Stopwatch.StartNew();

        Logger.Debug("Starting from page {0}, {1} already fetched", page, totalFetched);

        while (!ct.IsCancellationRequested)
        {
            Logger.Debug("Fetching page {0}", page);
            var batch = FetchPage(page, ct);

            if (ct.IsCancellationRequested || batch is null || batch.Count == 0)
            {
                if (batch is null || batch.Count == 0)
                    Logger.Debug("No more tracks to fetch");
                break;
            }

            if (fetchAfter is not null)
            {
                var newScrobbles = batch.Where(s => s.PlayedAt > fetchAfter).ToList();

                foreach (var s in newScrobbles)
                    Logger.Debug("  New: {0} at {1:yyyy/MM/dd HH:mm:ss}", s.TrackName, s.PlayedAt);

                if (newScrobbles.Count == 0)
                {
                    var firstExisting = batch.First();
                    Logger.Debug(
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
                    Logger.Debug(
                        "  Exists: {0} at {1:yyyy/MM/dd HH:mm:ss}",
                        firstExisting.TrackName,
                        firstExisting.PlayedAt
                    );
                    StateManager.Save(StateManager.ScrobblesFile, scrobbles);
                    onProgress(page, totalFetched, stopwatch.Elapsed);
                    break;
                }
            }
            else
            {
                scrobbles.AddRange(batch);
                totalFetched += batch.Count;
            }

            StateManager.Save(StateManager.ScrobblesFile, scrobbles);
            onProgress(page, totalFetched, stopwatch.Elapsed);

            if (batch.Count < PerPage)
            {
                Logger.Debug("Last page reached ({0} tracks)", batch.Count);
                break;
            }

            page++;
            ApiConfig.Delay(ServiceType.LastFm);
        }

        stopwatch.Stop();
        Logger.Debug(
            "Fetch complete: {0} scrobbles in {1:mm\\:ss}",
            scrobbles.Count,
            stopwatch.Elapsed
        );
    }

    List<Scrobble>? FetchPage(int page, CancellationToken ct)
    {
        var response = ApiConfig.ExecuteWithRetry(
            operationName: "LastFm.GetRecentTracks",
            action: () =>
                client.User.GetRecentTracksAsync(username, limit: PerPage, page: page).Result,
            postAction: () => ApiConfig.Delay(ServiceType.LastFm),
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
        StateManager.Load<List<Scrobble>>(StateManager.ScrobblesFile);

    internal static void DeleteScrobblesCache() => StateManager.Delete(StateManager.ScrobblesFile);
}
