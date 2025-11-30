namespace CSharpScripts.Commands;

internal static class StatusHandler
{
    internal static void Execute(string? service)
    {
        var checkLastFm =
            IsNullOrEmpty(service) || service.Equals("lastfm", StringComparison.OrdinalIgnoreCase);
        var checkYouTube =
            IsNullOrEmpty(service)
            || service.Equals("yt", StringComparison.OrdinalIgnoreCase)
            || service.Equals("youtube", StringComparison.OrdinalIgnoreCase);

        if (!checkLastFm && !checkYouTube)
        {
            Logger.Warning("Unknown service: {0}. Use: yt, lastfm", service);
            return;
        }

        if (checkLastFm)
            ShowLastFmStatus();

        if (checkYouTube)
            ShowYouTubeStatus();
    }

    static void ShowLastFmStatus()
    {
        Logger.Info("=== Last.fm ===");
        var stateFile = Combine(Paths.StateDirectory, StateManager.LastFmSyncFile);
        var hasState = File.Exists(stateFile);
        var spreadsheetUrl =
            $"https://docs.google.com/spreadsheets/d/{SpreadsheetConfig.LastFmSpreadsheetId}";

        if (hasState)
        {
            var json = ReadAllText(stateFile);
            var state =
                JsonSerializer.Deserialize<FetchState>(json, StateManager.JsonOptions)
                ?? new FetchState();
            Logger.Info("Scrobbles: {0}", state.TotalFetched);
            Logger.Info("Cached: Yes");
            Logger.Info("Last sync: {0}", state.LastUpdated.ToString("yyyy/MM/dd HH:mm:ss"));
            Logger.Link(spreadsheetUrl, "Spreadsheet");
        }
        else
        {
            GoogleSheetsService sheets = new(
                clientId: AuthenticationConfig.GoogleClientId,
                clientSecret: AuthenticationConfig.GoogleClientSecret
            );
            var scrobbleCount = sheets.GetScrobbleCount(SpreadsheetConfig.LastFmSpreadsheetId);
            Logger.Info("Scrobbles: {0}", scrobbleCount);
            Logger.Info("Cached: No");
            Logger.Link(spreadsheetUrl, "Spreadsheet");
        }

        Logger.NewLine();
    }

    static void ShowYouTubeStatus()
    {
        Logger.Info("=== YouTube ===");
        var stateFile = Combine(Paths.StateDirectory, StateManager.YouTubeSyncFile);
        var cached = File.Exists(stateFile);

        if (cached)
        {
            var json = ReadAllText(stateFile);
            var state =
                JsonSerializer.Deserialize<YouTubeFetchState>(json, StateManager.JsonOptions)
                ?? new YouTubeFetchState();
            var totalVideos = state.PlaylistSnapshots.Values.Sum(s => s.VideoIds.Count);
            var spreadsheetUrl = $"https://docs.google.com/spreadsheets/d/{state.SpreadsheetId}";

            if (!state.FetchComplete)
                Logger.Warning("Fetch incomplete - run sync to resume");

            Logger.Info("Playlists: {0} | Videos: {1}", state.PlaylistSnapshots.Count, totalVideos);
            Logger.Info("Cached: Yes");
            Logger.Info("Last sync: {0}", state.LastUpdated.ToString("yyyy/MM/dd HH:mm:ss"));
            Logger.Link(spreadsheetUrl, "Spreadsheet");
        }
        else
        {
            Logger.Info("Cached: No");
        }

        Logger.NewLine();
    }
}
