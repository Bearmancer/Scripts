namespace CSharpScripts.Orchestrators;

#region ScrobbleSyncOrchestrator

public class ScrobbleSyncOrchestrator(DateTime? forceFromDate, CancellationToken ct) : IDisposable
{
    private readonly LastFmService lastFmService = new(
        apiKey: Config.LastFmApiKey,
        username: Config.LastFmUsername
    );

    private readonly GoogleSheetsService sheetsService = new();

    private FetchState state = StateManager.Load<FetchState>(fileName: StateManager.LastFmSyncFile);

    public void Dispose()
    {
        sheetsService?.Dispose();
        GC.SuppressFinalize(this);
    }

    internal async Task ExecuteAsync()
    {
        Console.Info(message: "Starting Last.fm sync...");
        string spreadsheetId = GetOrCreateSpreadsheet();

        if (forceFromDate.HasValue)
        {
            Console.Info(
                message: "Force resync from {0}",
                forceFromDate.Value.ToString(format: "yyyy/MM/dd")
            );
            sheetsService.DeleteScrobblesOnOrAfter(
                spreadsheetId: spreadsheetId,
                fromDate: forceFromDate.Value
            );
            state = new FetchState { SpreadsheetId = spreadsheetId };
            SaveState();
            LastFmService.DeleteScrobblesCache();
            await FetchScrobblesAsync(forceFromDate.Value.AddSeconds(value: -1));
        }
        else if (!state.FetchComplete && state.LastPage > 0)
        {
            Console.Warning(
                message: "Resuming full sync from page {0} ({1} scrobbles fetched)",
                state.LastPage + 1,
                state.TotalFetched
            );

            await FetchScrobblesAsync(fetchAfter: null);
        }
        else
        {
            var cachedScrobbles = LastFmService.LoadScrobbles();

            if (cachedScrobbles.Count > 0)
            {
                var newestCached = cachedScrobbles[index: 0].PlayedAt;
                var oldestCached = cachedScrobbles[^1].PlayedAt;

                Console.Debug(message: "Cache: {0} scrobbles", cachedScrobbles.Count);

                if (
                    state.OldestScrobble.HasValue
                    && state.NewestScrobble.HasValue
                    && oldestCached.HasValue
                    && newestCached.HasValue
                )
                    await FetchScrobblesAsync(fetchAfter: newestCached);
            }
            else
            {
                var latestInSheet = sheetsService.GetLatestScrobbleTime(
                    spreadsheetId: spreadsheetId
                );

                if (latestInSheet != null)
                {
                    Console.Info(
                        message: "Latest in sheet: {0}",
                        latestInSheet.Value.ToString(format: "yyyy/MM/dd HH:mm:ss")
                    );
                    await FetchScrobblesAsync(fetchAfter: latestInSheet);
                }
                else
                {
                    Console.Info(message: "No existing data. Full sync...");
                    await FetchScrobblesAsync(fetchAfter: null);
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
            Console.Success(message: "No new scrobbles to sync");
            Logger.End(success: true, summary: "No changes detected");
            return;
        }

        var newScrobbles = sheetsService.GetNewScrobbles(
            spreadsheetId: spreadsheetId,
            allScrobbles: scrobbles
        );

        if (newScrobbles.Count == 0)
        {
            Console.Success(message: "Sheet is up to date");
            Logger.End(success: true, summary: "No new scrobbles");
            return;
        }

        WriteToSheets(scrobbles: newScrobbles, spreadsheetId: spreadsheetId);
    }

    private async Task FetchScrobblesAsync(DateTime? fetchAfter)
    {
        var saveStateCounter = 0;
        const int saveStateInterval = 10;

        try
        {
            await lastFmService.FetchScrobblesSinceAsync(
                fetchAfter: fetchAfter,
                state: state,
                (page, total, elapsed, oldest, newest) =>
                {
                    state.Update(page: page, total: total, oldest: oldest, newest: newest);
                    saveStateCounter++;

                    if (saveStateCounter >= saveStateInterval)
                    {
                        SaveState();
                        saveStateCounter = 0;
                    }

                    Console.Progress(
                        message: "Page: {0} | Tracks: {1} | Elapsed: {2}",
                        page,
                        total,
                        elapsed.ToString(format: @"hh\:mm\:ss")
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
                message: "Stopped at page {0} ({1} scrobbles)",
                state.LastPage,
                state.TotalFetched
            );
    }

    private void WriteToSheets(List<Scrobble> scrobbles, string spreadsheetId)
    {
        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted(progress: "Interrupted before writing to sheets");
            return;
        }

        scrobbles.Sort(
            (a, b) => b.PlayedAt.GetValueOrDefault().CompareTo(a.PlayedAt.GetValueOrDefault())
        );

        sheetsService.EnsureSheetExists(spreadsheetId: spreadsheetId);
        sheetsService.WriteScrobbles(spreadsheetId: spreadsheetId, scrobbles: scrobbles);

        Console.Success(message: "Wrote {0} scrobbles.", scrobbles.Count);
        Logger.End(success: true, $"Wrote {scrobbles.Count} scrobbles to sheet");
    }

    private string GetOrCreateSpreadsheet() =>
        sheetsService.GetOrCreateSpreadsheet(
            currentSpreadsheetId: state.SpreadsheetId,
            defaultSpreadsheetId: Config.LastFmSpreadsheetId,
            spreadsheetTitle: Config.LastFmSpreadsheetTitle,
            id =>
            {
                state.SpreadsheetId = id;
                SaveState();
            }
        );

    internal void SaveState() =>
        StateManager.Save(fileName: StateManager.LastFmSyncFile, state: state);
}

#endregion
