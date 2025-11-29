namespace CSharpScripts.Commands;

internal static class ClearHandler
{
    internal static void Execute(string service, bool noRebuild, bool localOnly, bool remoteOnly)
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

        if (!noRebuild)
            RebuildProject();
    }

    static void ClearLastFm(ref GoogleSheetsService? sheets, bool clearLocal, bool clearRemote)
    {
        Logger.Info("Clearing Last.fm...");

        var state = StateManager.Load<FetchState>(StateManager.FetchStateFile);
        if (clearRemote && !IsNullOrEmpty(state.SpreadsheetId))
        {
            sheets ??= new GoogleSheetsService(
                clientId: AuthenticationConfig.GoogleClientId,
                clientSecret: AuthenticationConfig.GoogleClientSecret
            );
            sheets.ClearSubsheet(state.SpreadsheetId, "Scrobbles");
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

        var state = StateManager.Load<YouTubeFetchState>(StateManager.YouTubeStateFile);
        if (clearRemote && !IsNullOrEmpty(state.SpreadsheetId))
        {
            sheets ??= new GoogleSheetsService(
                clientId: AuthenticationConfig.GoogleClientId,
                clientSecret: AuthenticationConfig.GoogleClientSecret
            );
            var sheetNames = sheets.GetSubsheetNames(state.SpreadsheetId);
            foreach (var sheet in sheetNames.Where(s => s != "README"))
                sheets.DeleteSubsheet(state.SpreadsheetId, sheet);
            Logger.Success("YouTube spreadsheet content cleared.");
        }

        if (clearLocal)
        {
            StateManager.DeleteYouTubeStates();
            Logger.Success("YouTube state cleared.");
        }
    }

    static void RebuildProject()
    {
        var binDir = Combine(Paths.ProjectRoot, "csharp", "bin");
        var objDir = Combine(Paths.ProjectRoot, "csharp", "obj");

        var hadBin = Directory.Exists(binDir);
        var hadObj = Directory.Exists(objDir);

        if (hadBin)
        {
            Delete(binDir, recursive: true);
            Logger.Info("Deleted bin/");
        }

        if (hadObj)
        {
            Delete(objDir, recursive: true);
            Logger.Info("Deleted obj/");
        }

        if (!hadBin && !hadObj)
        {
            Logger.Info("No build artifacts to clean. Skipping rebuild.");
            return;
        }

        Logger.NewLine();
        Logger.Info("Rebuilding...");

        var csprojDir = Combine(Paths.ProjectRoot, "csharp");
        var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build",
                WorkingDirectory = csprojDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        );

        process?.WaitForExit();

        if (process?.ExitCode == 0)
            Logger.Success("Clear complete. Project rebuilt successfully.");
        else
            Logger.Error("Build failed. Run 'dotnet build' manually to see errors.");
    }
}
