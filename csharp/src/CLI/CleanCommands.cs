namespace CSharpScripts.CLI.Commands;

#region CleanLocalCommand

public sealed class CleanLocalCommand : Command<CleanLocalCommand.Settings>
{
    public override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        string normalizedService = settings.Service.ToLowerInvariant();
        bool cleanAll = normalizedService == "all";
        bool cleanLastFm = cleanAll || normalizedService == "lastfm";
        bool cleanYouTube = cleanAll || normalizedService is "youtube" or "yt";

        if (!cleanLastFm && !cleanYouTube)
        {
            Console.Warning(
                message: "Invalid service: {0}. Use: yt, lastfm, or all",
                settings.Service
            );
            return 1;
        }

        Console.Rule(text: "Clean Local");

        if (cleanLastFm)
        {
            Console.Info(message: "Cleaning Last.fm local state...");
            StateManager.DeleteLastFmStates();
            Console.Success(message: "  State files deleted");
        }

        if (cleanYouTube)
        {
            Console.Info(message: "Cleaning YouTube local state...");
            StateManager.DeleteAllYouTubeStates();
            Console.Success(message: "  State files deleted");
        }

        Console.NewLine();
        Console.Success(message: "Clean complete");

        return 0;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(position: 0, template: "[service]")]
        [Description(description: "yt, lastfm, all (default: all)")]
        [DefaultValue(value: "all")]
        public string Service { get; init; } = "all";
    }
}

#endregion

#region CleanPurgeCommand

public sealed class CleanPurgeCommand : Command<CleanPurgeCommand.Settings>
{
    public override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        string normalizedService = settings.Service.ToLowerInvariant();
        bool purgeAll = normalizedService == "all";
        bool purgeLastFm = purgeAll || normalizedService == "lastfm";
        bool purgeYouTube = purgeAll || normalizedService is "youtube" or "yt";

        if (!purgeLastFm && !purgeYouTube)
        {
            Console.Warning(
                message: "Invalid service: {0}. Use: yt, lastfm, or all",
                settings.Service
            );
            return 1;
        }

        Console.Rule(text: "Clean Purge");

        GoogleSheetsService? sheets = null;

        if (purgeLastFm)
            PurgeLastFm(sheets: ref sheets);

        if (purgeYouTube)
            PurgeYouTube(sheets: ref sheets);

        PurgeCsvExports();
        PurgeBuildArtifacts();

        Console.NewLine();
        Console.Success(message: "Purge complete - terminal will close in 2 seconds...");

        Thread.Sleep(millisecondsTimeout: 2000);
        Exit(exitCode: 0);

        return 0;
    }

    private static void PurgeLastFm(ref GoogleSheetsService? sheets)
    {
        Console.Info(message: "Purging Last.fm...");

        var state = StateManager.Load<FetchState>(fileName: StateManager.LastFmSyncFile);
        if (!IsNullOrEmpty(value: state.SpreadsheetId))
        {
            sheets ??= new GoogleSheetsService();
            sheets.DeleteSpreadsheet(spreadsheetId: state.SpreadsheetId);
        }

        StateManager.DeleteLastFmStates();
        Console.Success(message: "  State files deleted");
    }

    private static void PurgeYouTube(ref GoogleSheetsService? sheets)
    {
        Console.Info(message: "Purging YouTube...");

        var state = StateManager.Load<YouTubeFetchState>(fileName: StateManager.YouTubeSyncFile);
        if (!IsNullOrEmpty(value: state.SpreadsheetId))
        {
            sheets ??= new GoogleSheetsService();
            sheets.DeleteSpreadsheet(spreadsheetId: state.SpreadsheetId);
        }

        StateManager.DeleteAllYouTubeStates();
        Console.Success(message: "  State files deleted");
    }

    private static void PurgeCsvExports()
    {
        Console.Info(message: "Purging CSV exports...");

        string csvDir = Combine(path1: Paths.ProjectRoot, path2: "exports");
        if (Directory.Exists(path: csvDir))
        {
            Delete(path: csvDir, recursive: true);
            Console.Success(message: "  exports/ deleted");
        }
        else
        {
            Console.Dim(text: "  No exports/ directory found");
        }
    }

    private static void PurgeBuildArtifacts()
    {
        Console.Info(message: "Purging build artifacts...");

        string binDir = Combine(path1: Paths.ProjectRoot, path2: "csharp", path3: "bin");
        string objDir = Combine(path1: Paths.ProjectRoot, path2: "csharp", path3: "obj");

        try
        {
            if (Directory.Exists(path: binDir))
            {
                Delete(path: binDir, recursive: true);
                Console.Success(message: "  bin/ deleted");
            }

            if (Directory.Exists(path: objDir))
            {
                Delete(path: objDir, recursive: true);
                Console.Success(message: "  obj/ deleted");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ScheduleDeferredCleanup(binDir: binDir, objDir: objDir);
            return;
        }

        RebuildProject();
    }

    private static void ScheduleDeferredCleanup(string binDir, string objDir)
    {
        Console.Warning(message: "  Build artifacts locked - scheduling deferred cleanup...");

        string csprojDir = Combine(path1: Paths.ProjectRoot, path2: "csharp");
        var script = $$"""
            Start-Sleep -Seconds 2
            if (Test-Path '{{binDir}}') { Remove-Item -Recurse -Force '{{binDir}}' }
            if (Test-Path '{{objDir}}') { Remove-Item -Recurse -Force '{{objDir}}' }
            Set-Location '{{csprojDir}}'
            dotnet build
            """;

        Process.Start(
            new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments = $"-Command \"{script.Replace(oldValue: "\"", newValue: "\\\"")}\"",
                UseShellExecute = true,
                CreateNoWindow = false,
            }
        );

        Console.Success(message: "  Cleanup scheduled - will run after this process exits");
    }

    private static void RebuildProject()
    {
        Console.NewLine();
        Console.Info(message: "Rebuilding...");

        string csprojDir = Combine(path1: Paths.ProjectRoot, path2: "csharp");
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
            Console.Success(message: "Build complete");
        else
            Console.Error(message: "Build failed. Run 'dotnet build' manually.");
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(position: 0, template: "[service]")]
        [Description(description: "yt, lastfm, all (default: all)")]
        [DefaultValue(value: "all")]
        public string Service { get; init; } = "all";
    }
}

#endregion
