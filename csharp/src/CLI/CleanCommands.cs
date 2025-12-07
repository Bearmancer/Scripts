using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using CSharpScripts.Services.Sync.Google;
using CSharpScripts.Services.Sync.LastFm;

namespace CSharpScripts.CLI;

/// <summary>
/// NAME
///   clean - Delete state, cache, and remote data
///
/// DESCRIPTION
///   Commands to clean up local state files, cached data, and optionally
///   remote spreadsheet data. Use 'local' to only delete local files,
///   or 'purge' to delete everything including remote data.
///
/// COMMANDS
///   local     Delete local state and cache files only
///   purge     Delete all: state, remote data, CSVs, and builds
///
/// EXAMPLES
///   cli clean local           # Delete all local state
///   cli clean local yt        # Delete YouTube state only
///   cli clean purge           # Full purge including remote
///   cli clean purge lastfm    # Purge Last.fm only
/// </summary>
[Command("clean", Description = "Delete state, cache, and remote data")]
public sealed class CleanGroupCommand : ICommand
{
    public ValueTask ExecuteAsync(IConsole console)
    {
        Console.Rule("Clean Commands");
        Console.NewLine();

        Console.MarkupLine("[blue bold]COMMANDS[/]");
        Console.MarkupLine("  [cyan]local[/]     Delete local state and cache files only");
        Console.MarkupLine("  [cyan]purge[/]     Delete all: state, remote data, CSVs, builds");
        Console.NewLine();

        Console.MarkupLine("[blue bold]ARGUMENTS[/]");
        Console.MarkupLine("  [cyan]service[/]   yt, lastfm, all [grey](default: all)[/]");
        Console.NewLine();

        Console.MarkupLine("[blue bold]EXAMPLES[/]");
        Console.MarkupLine("  [dim]$[/] cli clean local");
        Console.MarkupLine("  [dim]$[/] cli clean local yt");
        Console.MarkupLine("  [dim]$[/] cli clean purge lastfm");

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// NAME
///   clean local - Delete local state and cache files
///
/// DESCRIPTION
///   Deletes local state and cache files for the specified service(s).
///   Does NOT delete remote spreadsheet data. Use this when you want to
///   force a fresh sync without losing your spreadsheet data.
///
/// USAGE
///   cli clean local [service]
///
/// ARGUMENTS
///   service    yt, lastfm, all (default: all)
///
/// EXAMPLES
///   cli clean local           # Delete all local state
///   cli clean local yt        # Delete YouTube state only
///   cli clean local lastfm    # Delete Last.fm state only
/// </summary>
[Command("clean local", Description = "Delete local state and cache files")]
public sealed class CleanLocalCommand : ICommand
{
    [CommandParameter(0, Name = "service", Description = "yt, lastfm, all", IsRequired = false)]
    public string Service { get; init; } = "all";

    public ValueTask ExecuteAsync(IConsole console)
    {
        string normalizedService = Service.ToLowerInvariant();
        bool cleanAll = normalizedService == "all";
        bool cleanLastFm = cleanAll || normalizedService == "lastfm";
        bool cleanYouTube = cleanAll || normalizedService is "youtube" or "yt";

        if (!cleanLastFm && !cleanYouTube)
        {
            Console.Warning("Invalid service: {0}. Use: yt, lastfm, or all", Service);
            return ValueTask.CompletedTask;
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

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// NAME
///   clean purge - Delete all state, remote data, and builds
///
/// DESCRIPTION
///   Performs a complete purge of the specified service(s):
///   - Local state and cache files
///   - Remote spreadsheet data (clears sheets, does not delete spreadsheet)
///   - CSV export files
///   - Build artifacts (bin/, obj/)
///
///   Does NOT delete log files. Use this when you want to start completely
///   fresh or troubleshoot data issues.
///
/// USAGE
///   cli clean purge [service]
///
/// ARGUMENTS
///   service    yt, lastfm, all (default: all)
///
/// EXAMPLES
///   cli clean purge           # Purge everything
///   cli clean purge yt        # Purge YouTube only
///   cli clean purge lastfm    # Purge Last.fm only
/// </summary>
[Command("clean purge", Description = "Delete all state, remote data, and builds")]
public sealed class CleanPurgeCommand : ICommand
{
    [CommandParameter(0, Name = "service", Description = "yt, lastfm, all", IsRequired = false)]
    public string Service { get; init; } = "all";

    public ValueTask ExecuteAsync(IConsole console)
    {
        string normalizedService = Service.ToLowerInvariant();
        bool purgeAll = normalizedService == "all";
        bool purgeLastFm = purgeAll || normalizedService == "lastfm";
        bool purgeYouTube = purgeAll || normalizedService is "youtube" or "yt";

        if (!purgeLastFm && !purgeYouTube)
        {
            Console.Warning("Invalid service: {0}. Use: yt, lastfm, or all", Service);
            return ValueTask.CompletedTask;
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
        Console.Success("Purge complete");

        return ValueTask.CompletedTask;
    }

    private static void PurgeLastFm(ref GoogleSheetsService? sheets)
    {
        Console.Info("Purging Last.fm...");

        FetchState state = StateManager.Load<FetchState>(StateManager.LastFmSyncFile);
        if (!IsNullOrEmpty(state.SpreadsheetId))
        {
            sheets ??= new GoogleSheetsService(Config.GoogleClientId, Config.GoogleClientSecret);
            sheets.ClearSubsheet(state.SpreadsheetId, "Scrobbles");
            Console.Success("  Spreadsheet cleared");
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
            List<string> sheetNames = sheets.GetSubsheetNames(state.SpreadsheetId);
            foreach (string sheet in sheetNames.Where(s => s != "README"))
                sheets.DeleteSubsheet(state.SpreadsheetId, sheet);
            Console.Success("  Spreadsheet cleared");
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
