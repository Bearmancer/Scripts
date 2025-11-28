using static CSharpScripts.Infrastructure.Logger;

namespace CSharpScripts.Orchestration;

internal class ScrobbleSyncOrchestrator(CancellationToken ct)
{
    readonly LastFmService lastFmService = new(
        apiKey: AuthenticationConfig.LastFmApiKey,
        username: AuthenticationConfig.LastFmUsername
    );

    readonly GoogleSheetsService sheetsService = new(
        clientId: AuthenticationConfig.GoogleClientId,
        clientSecret: AuthenticationConfig.GoogleClientSecret
    );

    FetchState state = StateManager.Load<FetchState>(StateManager.FetchStateFile);

    void LogResumeInfo()
    {
        if (state.LastPage > 0 && !state.FetchComplete)
            Warning(
                "Resuming from page {0} ({1} scrobbles fetched)",
                state.LastPage,
                state.TotalFetched
            );
    }

    internal void Execute()
    {
        Info("Sync initiated");

        var spreadsheetId = GetOrCreateSpreadsheet();
        var fetchAfter = sheetsService.GetLatestScrobbleTime(spreadsheetId);

        if (fetchAfter != null)
        {
            Info("Latest in sheet: {0}", fetchAfter.Value.ToString("yyyy/MM/dd HH:mm:ss"));
            Info("Incremental sync from {0}", fetchAfter.Value.ToString("yyyy/MM/dd HH:mm:ss"));
            state = new();
            SaveState();
            LastFmService.DeleteScrobblesCache();
        }
        else
        {
            Info("No existing scrobbles. Fetching all...");
            Info("Full sync requested (no existing data)");
            LogResumeInfo();
        }

        if (!state.FetchComplete)
        {
            FetchScrobbles(fetchAfter);

            if (ct.IsCancellationRequested)
            {
                Interrupted(
                    $"Fetched {state.TotalFetched} scrobbles across {state.LastPage} pages"
                );
                return;
            }

            state.FetchComplete = true;
            SaveState();
            NewLine();
            Info(
                "Fetch complete: {0} scrobbles from {1} pages",
                state.TotalFetched,
                state.LastPage
            );
        }
        else
        {
            Debug("Using cached scrobbles");
        }

        var scrobbles = LastFmService.LoadScrobbles();

        if (scrobbles.Count == 0)
        {
            Info("Sheet is up to date.");
            End(success: true, summary: "Already up to date");
            return;
        }

        WriteToSheets(scrobbles, spreadsheetId);
    }

    void FetchScrobbles(DateTime? fetchAfter)
    {
        Info("Fetching from Last.fm...");

        lastFmService.FetchScrobblesSince(
            fetchAfter: fetchAfter,
            state: state,
            onProgress: (page, total, elapsed) =>
            {
                state.Update(page, total);
                SaveState();
                Progress(
                    "Page: {0} | Tracks: {1} | Elapsed: {2}",
                    page,
                    total,
                    elapsed.ToString(@"hh\:mm\:ss")
                );
            },
            ct: ct
        );

        NewLine();

        if (ct.IsCancellationRequested)
            Warning("Stopped at page {0} ({1} scrobbles)", state.LastPage, state.TotalFetched);
    }

    void WriteToSheets(List<Scrobble> scrobbles, string spreadsheetId)
    {
        scrobbles.Sort(
            (a, b) => (b.PlayedAt ?? DateTime.MinValue).CompareTo(a.PlayedAt ?? DateTime.MinValue)
        );

        sheetsService.EnsureSheetExists(spreadsheetId);

        Info("Writing {0} scrobbles...", scrobbles.Count);
        sheetsService.WriteScrobbles(spreadsheetId, scrobbles);

        Success("Done! Wrote {0} scrobbles.", scrobbles.Count);
        Link(GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId), "Open spreadsheet");
        End(success: true, summary: $"Wrote {scrobbles.Count} scrobbles to sheet");
    }

    string GetOrCreateSpreadsheet() =>
        sheetsService.GetOrCreateSpreadsheet(
            currentSpreadsheetId: state.SpreadsheetId,
            defaultSpreadsheetId: SpreadsheetConfig.LastFmSpreadsheetId,
            spreadsheetTitle: SpreadsheetConfig.LastFmSpreadsheetTitle,
            onSpreadsheetResolved: id =>
            {
                state.SpreadsheetId = id;
                SaveState();
            }
        );

    internal void SaveState() => StateManager.Save(StateManager.FetchStateFile, state);
}
