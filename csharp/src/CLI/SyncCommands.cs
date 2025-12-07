using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using CSharpScripts.Orchestrators;
using CSharpScripts.Services.Sync.Google;

namespace CSharpScripts.CLI;

/// <summary>
/// NAME
///   sync - Sync data to Google Sheets
///
/// DESCRIPTION
///   Commands to synchronize YouTube playlists and Last.fm scrobbles
///   to Google Sheets for tracking and analysis.
///
/// COMMANDS
///   yt        Sync YouTube playlists to Google Sheets
///   lastfm    Sync Last.fm scrobbles to Google Sheets
///
/// EXAMPLES
///   cli sync yt
///   cli sync yt --force --verbose
///   cli sync lastfm
///   cli sync lastfm --since 2024/06/15
/// </summary>
[Command("sync", Description = "Sync data to Google Sheets")]
public sealed class SyncGroupCommand : ICommand
{
    public ValueTask ExecuteAsync(IConsole console)
    {
        Console.Rule("Sync Commands");
        Console.NewLine();

        Console.MarkupLine("[blue bold]COMMANDS[/]");
        Console.MarkupLine("  [cyan]yt[/]        Sync YouTube playlists → Google Sheets");
        Console.MarkupLine("  [cyan]lastfm[/]    Sync Last.fm scrobbles → Google Sheets");
        Console.NewLine();

        Console.MarkupLine("[blue bold]OPTIONS[/]");
        Console.MarkupLine("  [cyan]-v, --verbose[/]     Enable debug logging");
        Console.MarkupLine(
            "  [cyan]-f, --force[/]       Clear cache and re-fetch all data [grey](yt only)[/]"
        );
        Console.MarkupLine("  [cyan]--since[/]           Re-sync from date [grey](lastfm only)[/]");
        Console.MarkupLine("  [cyan]--session-id[/]      Show session ID in output");
        Console.NewLine();

        Console.MarkupLine("[blue bold]EXAMPLES[/]");
        Console.MarkupLine("  [dim]$[/] cli sync yt");
        Console.MarkupLine("  [dim]$[/] cli sync yt --force --verbose");
        Console.MarkupLine("  [dim]$[/] cli sync lastfm --since 2024/06/15");

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// NAME
///   sync yt - Sync YouTube playlists to Google Sheets
///
/// DESCRIPTION
///   Fetches all YouTube playlists and their videos, then syncs them to a
///   Google Sheets spreadsheet. Uses incremental sync by default - only
///   fetching changes since last run. Progress is saved so interrupted
///   syncs can resume.
///
/// USAGE
///   cli sync yt [options]
///
/// OPTIONS
///   -v, --verbose     Enable debug logging to console and log file
///   -f, --force       Clear all cached state and re-fetch everything
///   --session-id      Display the session ID for log correlation
///
/// EXAMPLES
///   cli sync yt                    # Normal incremental sync
///   cli sync yt --force            # Full re-sync from scratch
///   cli sync yt -v --session-id    # Debug mode with session tracking
/// </summary>
[Command("sync yt", Description = "Sync YouTube playlists → Google Sheets")]
public sealed class SyncYouTubeCommand : ICommand
{
    [CommandOption("verbose", 'v', Description = "Enable debug logging")]
    public bool Verbose { get; init; }

    [CommandOption("force", 'f', Description = "Clear cache and re-fetch all data")]
    public bool Force { get; init; }

    [CommandOption("session-id", Description = "Show session ID in output")]
    public bool ShowSessionId { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        if (Verbose)
        {
            Console.Level = LogLevel.Debug;
            Logger.FileLevel = LogLevel.Debug;
        }

        Logger.Start(ServiceType.YouTube);

        ExecuteWithErrorHandling(() =>
        {
            if (Force)
            {
                Console.Info("Force sync: clearing cache and re-fetching all data...");
                StateManager.DeleteAllYouTubeStates();
            }

            if (ShowSessionId)
                Console.Info("Session ID: {0}", Logger.CurrentSessionId);

            new YouTubePlaylistOrchestrator(Program.Cts.Token).Execute();
        });

        return ValueTask.CompletedTask;
    }

    private static void ExecuteWithErrorHandling(Action action)
    {
        try
        {
            action();
        }
        catch (DailyQuotaExceededException ex)
        {
            Console.Error(ex.Message);
            Console.Error(
                "Try again tomorrow or request quota increase from Google Cloud Console."
            );
            Logger.End(success: false, summary: "Daily quota exceeded");
            throw new CommandException("Sync failed: daily quota exceeded", 1);
        }
        catch (RetryExhaustedException ex)
        {
            Console.Error(ex.Message);
            Console.Error("Wait 15-30 minutes and try again. Progress has been saved.");
            Logger.End(success: false, summary: "Retry limit reached");
            throw new CommandException("Sync failed: retry limit reached", 1);
        }
        catch (AggregateException aex)
        {
            foreach (Exception ex in aex.InnerExceptions)
                Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
            Logger.End(
                success: false,
                summary: $"Failed with {aex.InnerExceptions.Count} error(s)"
            );
            throw new CommandException($"Sync failed with {aex.InnerExceptions.Count} error(s)", 1);
        }
        catch (OperationCanceledException)
        {
            Console.Warning("Operation cancelled by user");
            Logger.Interrupted("Cancelled by Ctrl+C");
            throw new CommandException("Sync cancelled", 130);
        }
        catch (CommandException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
            Logger.End(success: false, summary: ex.Message);
            throw new CommandException($"Sync failed: {ex.Message}", 1);
        }
    }
}

/// <summary>
/// NAME
///   sync lastfm - Sync Last.fm scrobbles to Google Sheets
///
/// DESCRIPTION
///   Fetches all scrobbles from Last.fm and syncs them to a Google Sheets
///   spreadsheet. Uses incremental sync by default - only fetching new
///   scrobbles since last run. Use --since to re-sync from a specific date.
///
/// USAGE
///   cli sync lastfm [options]
///
/// OPTIONS
///   -v, --verbose     Enable debug logging to console and log file
///   --since           Re-sync from date (yyyy/MM/dd). Deletes existing
///                     data on/after this date before syncing.
///
/// EXAMPLES
///   cli sync lastfm                    # Normal incremental sync
///   cli sync lastfm --since 2024/06/15 # Re-sync from June 15, 2024
///   cli sync lastfm -v                 # Debug mode
/// </summary>
[Command("sync lastfm", Description = "Sync Last.fm scrobbles → Google Sheets")]
public sealed class SyncLastFmCommand : ICommand
{
    [CommandOption("verbose", 'v', Description = "Enable debug logging")]
    public bool Verbose { get; init; }

    [CommandOption("since", Description = "Re-sync from date (yyyy/MM/dd)")]
    public string? Since { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        if (Verbose)
        {
            Console.Level = LogLevel.Debug;
            Logger.FileLevel = LogLevel.Debug;
        }

        DateTime? sinceDate = null;
        if (!IsNullOrEmpty(Since))
        {
            if (
                !DateTime.TryParseExact(
                    Since,
                    "yyyy/MM/dd",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime parsed
                )
            )
            {
                Console.Error("Invalid date format. Use yyyy/MM/dd (e.g. 2024/01/01)");
                return ValueTask.CompletedTask;
            }
            sinceDate = parsed;
            Console.Warning(
                "Will delete existing data on/after {0} and re-sync",
                sinceDate.Value.ToString("yyyy/MM/dd")
            );
        }

        Logger.Start(ServiceType.LastFm);

        try
        {
            Console.Info("Starting Last.fm sync...");
            new ScrobbleSyncOrchestrator(Program.Cts.Token, sinceDate).Execute();
        }
        catch (DailyQuotaExceededException ex)
        {
            Console.Error(ex.Message);
            Console.Error(
                "Try again tomorrow or request quota increase from Google Cloud Console."
            );
            Logger.End(success: false, summary: "Daily quota exceeded");
            throw new CommandException("Sync failed: daily quota exceeded", 1);
        }
        catch (RetryExhaustedException ex)
        {
            Console.Error(ex.Message);
            Console.Error("Wait 15-30 minutes and try again. Progress has been saved.");
            Logger.End(success: false, summary: "Retry limit reached");
            throw new CommandException("Sync failed: retry limit reached", 1);
        }
        catch (AggregateException aex)
        {
            foreach (Exception ex in aex.InnerExceptions)
                Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
            Logger.End(
                success: false,
                summary: $"Failed with {aex.InnerExceptions.Count} error(s)"
            );
            throw new CommandException($"Sync failed with {aex.InnerExceptions.Count} error(s)", 1);
        }
        catch (OperationCanceledException)
        {
            Console.Warning("Operation cancelled by user");
            Logger.Interrupted("Cancelled by Ctrl+C");
            throw new CommandException("Sync cancelled", 130);
        }
        catch (CommandException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
            Logger.End(success: false, summary: ex.Message);
            throw new CommandException($"Sync failed: {ex.Message}", 1);
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// NAME
///   status - Show sync state and cache info
///
/// DESCRIPTION
///   Displays the current sync state for YouTube and/or Last.fm, including
///   cached data counts, last sync time, and spreadsheet links.
///
/// USAGE
///   cli status [service]
///
/// ARGUMENTS
///   service    yt, lastfm (omit for all)
///
/// EXAMPLES
///   cli status           # Show all services
///   cli status yt        # YouTube only
///   cli status lastfm    # Last.fm only
/// </summary>
[Command("status", Description = "Show sync state and cache info")]
public sealed class StatusCommand : ICommand
{
    [CommandParameter(
        0,
        Name = "service",
        Description = "yt, lastfm (omit for all)",
        IsRequired = false
    )]
    public string? Service { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        bool checkLastFm =
            IsNullOrEmpty(Service) || Service.Equals("lastfm", StringComparison.OrdinalIgnoreCase);
        bool checkYouTube =
            IsNullOrEmpty(Service)
            || Service.Equals("yt", StringComparison.OrdinalIgnoreCase)
            || Service.Equals("youtube", StringComparison.OrdinalIgnoreCase);

        if (!checkLastFm && !checkYouTube)
        {
            Console.Warning("Unknown service: {0}. Use: yt, lastfm", Service);
            return ValueTask.CompletedTask;
        }

        if (checkLastFm)
            ShowLastFmStatus();

        if (checkYouTube)
            ShowYouTubeStatus();

        return ValueTask.CompletedTask;
    }

    private static void ShowLastFmStatus()
    {
        Console.Info("=== Last.fm ===");
        string stateFile = Combine(Paths.StateDirectory, StateManager.LastFmSyncFile);
        bool hasState = File.Exists(stateFile);
        string spreadsheetUrl =
            $"https://docs.google.com/spreadsheets/d/{Config.LastFmSpreadsheetId}";

        if (hasState)
        {
            string json = ReadAllText(stateFile);
            FetchState state =
                JsonSerializer.Deserialize<FetchState>(json, StateManager.JsonIndented)
                ?? new FetchState();
            Console.Info("Scrobbles: {0}", state.TotalFetched);
            Console.Info("Cached: Yes");
            Console.Info("Last sync: {0}", state.LastUpdated.ToString("yyyy/MM/dd HH:mm:ss"));
            Console.Link(spreadsheetUrl, "Spreadsheet");
        }
        else
        {
            GoogleSheetsService sheets = new(Config.GoogleClientId, Config.GoogleClientSecret);
            int scrobbleCount = sheets.GetScrobbleCount(Config.LastFmSpreadsheetId);
            Console.Info("Scrobbles: {0}", scrobbleCount);
            Console.Info("Cached: No");
            Console.Link(spreadsheetUrl, "Spreadsheet");
        }

        Console.NewLine();
    }

    private static void ShowYouTubeStatus()
    {
        Console.Info("=== YouTube ===");
        string stateFile = Combine(Paths.StateDirectory, StateManager.YouTubeSyncFile);
        bool cached = File.Exists(stateFile);

        if (cached)
        {
            string json = ReadAllText(stateFile);
            YouTubeFetchState state =
                JsonSerializer.Deserialize<YouTubeFetchState>(json, StateManager.JsonIndented)
                ?? new YouTubeFetchState();
            int totalVideos = state.PlaylistSnapshots.Values.Sum(s => s.VideoIds.Count);
            string spreadsheetUrl = $"https://docs.google.com/spreadsheets/d/{state.SpreadsheetId}";

            if (!state.FetchComplete)
                Console.Warning("Fetch incomplete - run sync to resume");

            Console.Info(
                "Playlists: {0} | Videos: {1}",
                state.PlaylistSnapshots.Count,
                totalVideos
            );
            Console.Info("Cached: Yes");
            Console.Info("Last sync: {0}", state.LastUpdated.ToString("yyyy/MM/dd HH:mm:ss"));
            Console.Link(spreadsheetUrl, "Spreadsheet");
        }
        else
        {
            Console.Info("Cached: No");
        }

        Console.NewLine();
    }
}
