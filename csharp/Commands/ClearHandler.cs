namespace CSharpScripts.Commands;

internal static class ClearHandler
{
    internal static void Execute(string service, bool localOnly, bool remoteOnly)
    {
        var normalizedService = service.ToLowerInvariant();
        var clearAll = normalizedService == "all";
        var clearLastFm = clearAll || normalizedService == "lastfm";
        var clearYouTube = clearAll || normalizedService is "youtube" or "yt";

        if (!clearLastFm && !clearYouTube)
        {
            Logger.Warning("Invalid service: {0}. Use: yt, lastfm, or all", service);
            return;
        }

        if (localOnly && remoteOnly)
        {
            Logger.Error("Cannot specify both --local-only and --remote-only.");
            throw new InvalidOperationException("Conflicting clear options.");
        }

        var clearLocal = !remoteOnly;
        var clearRemote = !localOnly;

        GoogleSheetsService? sheets = null;

        if (clearLastFm)
            ClearLastFm(ref sheets, clearLocal, clearRemote);

        if (clearYouTube)
            ClearYouTube(ref sheets, clearLocal, clearRemote);

        Logger.Success("Clear complete.");
    }

    static GoogleSheetsService GetOrCreateSheetsService(ref GoogleSheetsService? sheets)
    {
        return sheets ??= new GoogleSheetsService(
            clientId: AuthenticationConfig.GoogleClientId,
            clientSecret: AuthenticationConfig.GoogleClientSecret
        );
    }

    static void ClearLastFm(ref GoogleSheetsService? sheets, bool clearLocal, bool clearRemote)
    {
        Logger.Info("Clearing Last.fm...");

        var state = StateManager.Load<FetchState>(StateManager.LastFmSyncFile);
        if (clearRemote && !IsNullOrEmpty(state.SpreadsheetId))
        {
            var service = GetOrCreateSheetsService(ref sheets);
            service.ClearSubsheet(state.SpreadsheetId, "Scrobbles");
            Logger.Success("Last.fm spreadsheet content cleared.");
        }

        if (clearLocal)
        {
            StateManager.DeleteLastFmStates();
            Logger.Success("Last.fm state cleared.");
        }
    }

    static void ClearYouTube(ref GoogleSheetsService? sheets, bool clearLocal, bool clearRemote)
    {
        Logger.Info("Clearing YouTube...");

        var state = StateManager.Load<YouTubeFetchState>(StateManager.YouTubeSyncFile);
        if (clearRemote && !IsNullOrEmpty(state.SpreadsheetId))
        {
            var service = GetOrCreateSheetsService(ref sheets);
            var sheetNames = service.GetSubsheetNames(state.SpreadsheetId);
            foreach (var sheet in sheetNames.Where(s => s != "README"))
                service.DeleteSubsheet(state.SpreadsheetId, sheet);
            Logger.Success("YouTube spreadsheet content cleared.");
        }

        if (clearLocal)
        {
            StateManager.DeleteYouTubeStates();
            Logger.Success("YouTube state cleared.");
        }
    }
}
