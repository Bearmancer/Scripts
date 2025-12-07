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
        Console.Debug("Sync initiated");

        var spreadsheetId = GetOrCreateSpreadsheet();
        DateTime? fetchAfter;

        if (forceFromDate.HasValue)
        {
            Console.Info("Force resync from {0}", forceFromDate.Value.ToString("yyyy/MM/dd"));
            sheetsService.DeleteScrobblesOnOrAfter(spreadsheetId, forceFromDate.Value);
            state = new() { SpreadsheetId = spreadsheetId };
            SaveState();
            LastFmService.DeleteScrobblesCache();
            fetchAfter = forceFromDate.Value.AddSeconds(-1);
        }
        else
        {
            fetchAfter = sheetsService.GetLatestScrobbleTime(spreadsheetId);

            if (fetchAfter != null)
            {
                Console.Info(
                    "Latest in sheet: {0}",
                    fetchAfter.Value.ToString("yyyy/MM/dd HH:mm:ss")
                );
                Console.Info(
                    "Incremental sync from {0}",
                    fetchAfter.Value.ToString("yyyy/MM/dd HH:mm:ss")
                );
                state = new() { SpreadsheetId = spreadsheetId };
                SaveState();
                LastFmService.DeleteScrobblesCache();
            }
            else
            {
                Console.Info("No existing scrobbles. Fetching all...");
                Console.Info("Full sync requested (no existing data)");
                LogResumeInfo();
            }
        }

        if (!state.FetchComplete)
        {
            FetchScrobbles(fetchAfter);

            if (ct.IsCancellationRequested)
            {
                Logger.Interrupted(
                    $"Fetched {state.TotalFetched} scrobbles across {state.LastPage} pages"
                );
                return;
            }

            state.FetchComplete = true;
            SaveState();
            Console.Info(
                "Fetch complete: {0} scrobbles from {1} pages",
                state.TotalFetched,
                state.LastPage
            );
        }
        else
        {
            Console.Debug("Using cached scrobbles");
        }

        var scrobbles = LastFmService.LoadScrobbles();

        if (scrobbles.Count == 0)
        {
            Logger.End(true, "No changes detected");
            return;
        }

        WriteToSheets(scrobbles, spreadsheetId);
    }

    private void FetchScrobbles(DateTime? fetchAfter)
    {
        Console.Info("Fetching from Last.fm...");

        lastFmService.FetchScrobblesSince(
            fetchAfter: fetchAfter,
            state: state,
            onProgress: (page, total, elapsed) =>
            {
                state.Update(page, total);
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

        if (ct.IsCancellationRequested)
            Console.Warning(
                "Stopped at page {0} ({1} scrobbles)",
                state.LastPage,
                state.TotalFetched
            );
    }

    private void WriteToSheets(List<Scrobble> scrobbles, string spreadsheetId)
    {
        scrobbles.Sort(
            (a, b) => (b.PlayedAt ?? DateTime.MinValue).CompareTo(a.PlayedAt ?? DateTime.MinValue)
        );

        sheetsService.EnsureSheetExists(spreadsheetId);

        Console.Info("Writing {0} scrobbles...", scrobbles.Count);
        sheetsService.WriteScrobbles(spreadsheetId, scrobbles);

        Console.Success("Done! Wrote {0} scrobbles.", scrobbles.Count);
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
