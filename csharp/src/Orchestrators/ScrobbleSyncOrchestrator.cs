namespace CSharpScripts.Orchestrators;

public class ScrobbleSyncOrchestrator(CancellationToken ct, DateTime? forceFromDate = null)
{
    private readonly LastFmService lastFmService = new(
        apiKey: Config.LastFmApiKey,
        username: Config.LastFmUsername
    );

    private readonly GoogleSheetsService sheetsService = new(
        clientId: Config.GoogleClientId,
        clientSecret: Config.GoogleClientSecret
    );

    private FetchState state = StateManager.Load<FetchState>(StateManager.LastFmSyncFile);

    private void LogResumeInfo()
    {
        if (state.LastPage > 0 && !state.FetchComplete)
            Console.Warning(
                "Resuming from page {0} ({1} scrobbles fetched)",
                state.LastPage,
                state.TotalFetched
            );
    }

    internal void Execute()
    {
        Console.Info("Starting Last.fm sync...");
        var spreadsheetId = GetOrCreateSpreadsheet();
        Console.Info("Authenticated");

        if (forceFromDate.HasValue)
        {
            Console.Info("Force resync from {0}", forceFromDate.Value.ToString("yyyy/MM/dd"));
            sheetsService.DeleteScrobblesOnOrAfter(spreadsheetId, forceFromDate.Value);
            state = new() { SpreadsheetId = spreadsheetId };
            SaveState();
            LastFmService.DeleteScrobblesCache();
            FetchScrobbles(forceFromDate.Value.AddSeconds(-1));
        }
        else if (!state.FetchComplete && state.LastPage > 0)
        {
            Console.Warning(
                "Resuming full sync from page {0} ({1} scrobbles fetched)",
                state.LastPage + 1,
                state.TotalFetched
            );

            FetchScrobbles(fetchAfter: null);
        }
        else
        {
            // Check local cache first
            var cachedScrobbles = LastFmService.LoadScrobbles();

            if (cachedScrobbles.Count > 0)
            {
                DateTime? oldestCached = cachedScrobbles.Min(s => s.PlayedAt);
                DateTime? newestCached = cachedScrobbles.Max(s => s.PlayedAt);

                Console.Debug("Cache: {0} scrobbles", cachedScrobbles.Count);

                // Validate cache against tracked state
                if (
                    state.OldestScrobble.HasValue
                    && state.NewestScrobble.HasValue
                    && oldestCached.HasValue
                    && newestCached.HasValue
                )
                {
                    bool cacheComplete =
                        oldestCached <= state.OldestScrobble
                        && newestCached >= state.NewestScrobble;

                    if (!cacheComplete)
                    {
                        Console.Warning(
                            "Cache incomplete! Expected {0} → {1}",
                            state.OldestScrobble?.ToString("yyyy/MM/dd HH:mm:ss") ?? "?",
                            state.NewestScrobble?.ToString("yyyy/MM/dd HH:mm:ss") ?? "?"
                        );
                    }
                }

                // Fetch only new scrobbles since latest cached
                FetchScrobbles(newestCached);
            }
            else
            {
                var latestInSheet = sheetsService.GetLatestScrobbleTime(spreadsheetId);

                if (latestInSheet != null)
                {
                    Console.Info(
                        "Latest in sheet: {0}",
                        latestInSheet.Value.ToString("yyyy/MM/dd HH:mm:ss")
                    );
                    FetchScrobbles(latestInSheet);
                }
                else
                {
                    Console.Info("No existing data. Full sync...");
                    FetchScrobbles(null);
                }
            }
        }

        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted(
                $"Fetched {state.TotalFetched} scrobbles across {state.LastPage} pages"
            );
            return;
        }

        state.FetchComplete = true;
        SaveState();

        var scrobbles = LastFmService.LoadScrobbles();

        if (scrobbles.Count == 0)
        {
            Console.Success("No new scrobbles to sync");
            Logger.End(true, "No changes detected");
            return;
        }

        var newScrobbles = sheetsService.GetNewScrobbles(spreadsheetId, scrobbles);

        if (newScrobbles.Count == 0)
        {
            Console.Success("Sheet is up to date");
            Logger.End(true, "No new scrobbles");
            return;
        }

        WriteToSheets(newScrobbles, spreadsheetId);
    }

    private void FetchScrobbles(DateTime? fetchAfter)
    {
        Console.Info("Fetching from Last.fm...");

        try
        {
            lastFmService.FetchScrobblesSince(
                fetchAfter: fetchAfter,
                state: state,
                onProgress: (page, total, elapsed, oldest, newest) =>
                {
                    state.Update(page, total, oldest, newest);
                    SaveState();
                    Console.Progress(
                        "Page: {0} | Tracks: {1} | Elapsed: {2}",
                        page,
                        total,
                        elapsed.ToString(@"hh\:mm\:ss")
                    );
                },
                ct: ct
            );
        }
        finally
        {
            SaveState();
        }

        if (ct.IsCancellationRequested)
            Console.Warning(
                "Stopped at page {0} ({1} scrobbles)",
                state.LastPage,
                state.TotalFetched
            );
    }

    private void WriteToSheets(List<Scrobble> scrobbles, string spreadsheetId)
    {
        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted("Interrupted before writing to sheets");
            return;
        }

        scrobbles.Sort(
            (a, b) => (b.PlayedAt ?? DateTime.MinValue).CompareTo(a.PlayedAt ?? DateTime.MinValue)
        );

        sheetsService.EnsureSheetExists(spreadsheetId);
        sheetsService.WriteScrobbles(spreadsheetId, scrobbles);

        Console.Success("Wrote {0} scrobbles.", scrobbles.Count);
        Console.Link(GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId), "Open spreadsheet");
        Logger.End(true, $"Wrote {scrobbles.Count} scrobbles to sheet");
    }

    private string GetOrCreateSpreadsheet() =>
        sheetsService.GetOrCreateSpreadsheet(
            currentSpreadsheetId: state.SpreadsheetId,
            defaultSpreadsheetId: Config.LastFmSpreadsheetId,
            spreadsheetTitle: Config.LastFmSpreadsheetTitle,
            onSpreadsheetResolved: id =>
            {
                state.SpreadsheetId = id;
                SaveState();
            }
        );

    internal void SaveState() => StateManager.Save(StateManager.LastFmSyncFile, state);
}
