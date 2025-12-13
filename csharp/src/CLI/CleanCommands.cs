namespace CSharpScripts.CLI.Commands;

public sealed class CleanLocalCommand : Command<CleanLocalCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[service]")]
        [Description("yt, lastfm, all (default: all)")]
        [DefaultValue("all")]
        public string Service { get; init; } = "all";
    }

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
            Console.Warning("Invalid service: {0}. Use: yt, lastfm, or all", settings.Service);
            return 1;
        }

        Console.Rule("Clean Local");

        if (cleanLastFm)
        {
            Console.Info("Cleaning Last.fm local state...");
            StateManager.DeleteLastFmStates();
            Console.Success("  State files deleted");
        }

        if (cleanYouTube)
        {
            Console.Info("Cleaning YouTube local state...");
            StateManager.DeleteAllYouTubeStates();
            Console.Success("  State files deleted");
        }

        Console.NewLine();
        Console.Success("Clean complete");

        return 0;
    }
}

public sealed class CleanPurgeCommand : Command<CleanPurgeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[service]")]
        [Description("yt, lastfm, all (default: all)")]
        [DefaultValue("all")]
        public string Service { get; init; } = "all";
    }

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
            Console.Warning("Invalid service: {0}. Use: yt, lastfm, or all", settings.Service);
            return 1;
        }

        Console.Rule("Clean Purge");

        GoogleSheetsService? sheets = null;

        if (purgeLastFm)
            PurgeLastFm(ref sheets);

        if (purgeYouTube)
            PurgeYouTube(ref sheets);

        PurgeCsvExports();
        PurgeBuildArtifacts();

        Console.NewLine();
        Console.Success("Purge complete - terminal will close in 2 seconds...");

        Thread.Sleep(2000);
        Environment.Exit(0);

        return 0;
    }

    private static void PurgeLastFm(ref GoogleSheetsService? sheets)
    {
        Console.Info("Purging Last.fm...");

        FetchState state = StateManager.Load<FetchState>(StateManager.LastFmSyncFile);
        if (!IsNullOrEmpty(state.SpreadsheetId))
        {
            sheets ??= new GoogleSheetsService(Config.GoogleClientId, Config.GoogleClientSecret);
            sheets.DeleteSpreadsheet(state.SpreadsheetId);
        }

        StateManager.DeleteLastFmStates();
        Console.Success("  State files deleted");
    }

    private static void PurgeYouTube(ref GoogleSheetsService? sheets)
    {
        Console.Info("Purging YouTube...");

        YouTubeFetchState state = StateManager.Load<YouTubeFetchState>(
            StateManager.YouTubeSyncFile
        );
        if (!IsNullOrEmpty(state.SpreadsheetId))
        {
            sheets ??= new GoogleSheetsService(Config.GoogleClientId, Config.GoogleClientSecret);
            sheets.DeleteSpreadsheet(state.SpreadsheetId);
        }

        StateManager.DeleteAllYouTubeStates();
        Console.Success("  State files deleted");
    }

    private static void PurgeCsvExports()
    {
        Console.Info("Purging CSV exports...");

        string csvDir = Combine(Paths.ProjectRoot, "exports");
        if (Directory.Exists(csvDir))
        {
            Delete(csvDir, recursive: true);
            Console.Success("  exports/ deleted");
        }
        else
        {
            Console.Dim("  No exports/ directory found");
        }
    }

    private static void PurgeBuildArtifacts()
    {
        Console.Info("Purging build artifacts...");

        string binDir = Combine(Paths.ProjectRoot, "csharp", "bin");
        string objDir = Combine(Paths.ProjectRoot, "csharp", "obj");

        try
        {
            if (Directory.Exists(binDir))
            {
                Delete(binDir, recursive: true);
                Console.Success("  bin/ deleted");
            }

            if (Directory.Exists(objDir))
            {
                Delete(objDir, recursive: true);
                Console.Success("  obj/ deleted");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ScheduleDeferredCleanup(binDir, objDir);
            return;
        }

        RebuildProject();
    }

    private static void ScheduleDeferredCleanup(string binDir, string objDir)
    {
        Console.Warning("  Build artifacts locked - scheduling deferred cleanup...");

        string csprojDir = Combine(Paths.ProjectRoot, "csharp");
        string script = $$"""
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
                Arguments = $"-Command \"{script.Replace("\"", "\\\"")}\"",
                UseShellExecute = true,
                CreateNoWindow = false,
            }
        );

        Console.Success("  Cleanup scheduled - will run after this process exits");
    }

    private static void RebuildProject()
    {
        Console.NewLine();
        Console.Info("Rebuilding...");

        string csprojDir = Combine(Paths.ProjectRoot, "csharp");
        Process? process = Process.Start(
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
            Console.Success("Build complete");
        else
            Console.Error("Build failed. Run 'dotnet build' manually.");
    }
}
