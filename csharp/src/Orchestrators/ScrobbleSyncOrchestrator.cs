namespace CSharpScripts.Orchestrators;

public class ScrobbleSyncOrchestrator(DateTime? forceFromDate, CancellationToken ct) : IDisposable
{
    private readonly LastFmService lastFmService = new(
        apiKey: Config.LastFmApiKey,
        username: Config.LastFmUsername
    );

    private readonly GoogleSheetsService sheetsService = new();

    private FetchState state = StateManager.Load<FetchState>(StateManager.LastFmSyncFile);

    internal async Task ExecuteAsync()
    {
        Console.Info("Starting Last.fm sync...");
        var spreadsheetId = GetOrCreateSpreadsheet();

        if (forceFromDate.HasValue)
        {
            Console.Info("Force resync from {0}", forceFromDate.Value.ToString("yyyy/MM/dd"));
            sheetsService.DeleteScrobblesOnOrAfter(spreadsheetId, forceFromDate.Value);
            state = new() { SpreadsheetId = spreadsheetId };
            SaveState();
            LastFmService.DeleteScrobblesCache();
            await FetchScrobblesAsync(forceFromDate.Value.AddSeconds(-1));
        }
        else if (!state.FetchComplete && state.LastPage > 0)
        {
            Console.Warning(
                "Resuming full sync from page {0} ({1} scrobbles fetched)",
                state.LastPage + 1,
                state.TotalFetched
            );

            await FetchScrobblesAsync(fetchAfter: null);
        }
        else
        {
            // Check local cache first
            var cachedScrobbles = LastFmService.LoadScrobbles();

            if (cachedScrobbles.Count > 0)
            {
                // Use first/last since scrobbles are sorted (newest first)
                DateTime? newestCached = cachedScrobbles[0].PlayedAt;
                DateTime? oldestCached = cachedScrobbles[^1].PlayedAt;

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
                await FetchScrobblesAsync(newestCached);
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
                    await FetchScrobblesAsync(latestInSheet);
                }
                else
                {
                    Console.Info("No existing data. Full sync...");
                    await FetchScrobblesAsync(null);
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

    private async Task FetchScrobblesAsync(DateTime? fetchAfter)
    {
        Console.Info("Fetching from Last.fm...");
        var saveStateCounter = 0;
        const int SAVE_STATE_INTERVAL = 10;

        try
        {
            await lastFmService.FetchScrobblesSinceAsync(
                fetchAfter: fetchAfter,
                state: state,
                onProgress: (page, total, elapsed, oldest, newest) =>
                {
                    state.Update(page, total, oldest, newest);
                    saveStateCounter++;

                    // Batch state saves every N pages
                    if (saveStateCounter >= SAVE_STATE_INTERVAL)
                    {
                        SaveState();
                        saveStateCounter = 0;
                    }

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
            // Always save final state
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
            (a, b) => b.PlayedAt.GetValueOrDefault().CompareTo(a.PlayedAt.GetValueOrDefault())
        );

        sheetsService.EnsureSheetExists(spreadsheetId);
        sheetsService.WriteScrobbles(spreadsheetId, scrobbles);

        Console.Success("Wrote {0} scrobbles.", scrobbles.Count);
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

    public void Dispose()
    {
        sheetsService?.Dispose();
        GC.SuppressFinalize(this);
    }
}
