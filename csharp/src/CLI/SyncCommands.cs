using System.ComponentModel;
using Spectre.Console.Cli;

namespace CSharpScripts.CLI.Commands;

public sealed class SyncAllCommand : Command<SyncAllCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-v|--verbose")]
        [Description("Debug logging")]
        public bool Verbose { get; init; }

        [CommandOption("-r|--reset")]
        [Description("Clear cache first")]
        public bool Reset { get; init; }
    }

    public override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        if (settings.Verbose)
        {
            Console.Level = LogLevel.Debug;
            Logger.FileLevel = LogLevel.Debug;
        }

        if (settings.Reset)
        {
            Console.Info("Clearing local cache...");
            StateManager.DeleteLastFmStates();
            StateManager.DeleteAllYouTubeStates();
            Console.Success("Cache cleared");
        }

        Console.Rule("YouTube Sync");
        int ytResult = RunYouTubeSync(settings.Verbose);

        Console.NewLine();
        Console.Rule("Last.fm Sync");
        int lfResult = RunLastFmSync(settings.Verbose);

        Console.NewLine();
        if (ytResult == 0 && lfResult == 0)
            Console.Success("All syncs complete!");
        else
            Console.Warning(
                "Completed with errors (YouTube: {0}, Last.fm: {1})",
                ytResult,
                lfResult
            );

        return ytResult != 0 ? ytResult : lfResult;
    }

    private static int RunYouTubeSync(bool verbose)
    {
        Logger.Start(ServiceType.YouTube);
        return SyncYouTubeCommand.ExecuteWithErrorHandling(() =>
        {
            new YouTubePlaylistOrchestrator(Program.Cts.Token).Execute();
        });
    }

    private static int RunLastFmSync(bool verbose)
    {
        Logger.Start(ServiceType.LastFm);
        return SyncYouTubeCommand.ExecuteWithErrorHandling(() =>
        {
            Console.Info("Starting Last.fm sync...");
            new ScrobbleSyncOrchestrator(Program.Cts.Token, forceFromDate: null).Execute();
        });
    }
}

public sealed class SyncYouTubeCommand : Command<SyncYouTubeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-v|--verbose")]
        [Description("Debug logging")]
        [DefaultValue(false)]
        public bool Verbose { get; init; }

        [CommandOption("-r|--reset")]
        [Description("Clear cache first")]
        [DefaultValue(false)]
        public bool Reset { get; init; }

        [CommandOption("-i|--session-id")]
        [Description("Show session ID")]
        [DefaultValue(false)]
        public bool ShowSessionId { get; init; }
    }

    public override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        if (settings.Verbose)
        {
            Console.Level = LogLevel.Debug;
            Logger.FileLevel = LogLevel.Debug;
        }

        Logger.Start(ServiceType.YouTube);

        return ExecuteWithErrorHandling(() =>
        {
            if (settings.Reset)
            {
                Console.Info("Clearing YouTube cache...");
                StateManager.DeleteAllYouTubeStates();
                Console.Success("Cache cleared");
            }

            if (settings.ShowSessionId)
                Console.Info("Session ID: {0}", Logger.CurrentSessionId);

            new YouTubePlaylistOrchestrator(Program.Cts.Token).Execute();
        });
    }

    internal static int ExecuteWithErrorHandling(Action action)
    {
        try
        {
            action();
            return 0;
        }
        catch (DailyQuotaExceededException ex)
        {
            Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
            Console.Error(
                "Try again tomorrow or request quota increase from Google Cloud Console."
            );
            if (ex.InnerException != null)
                Console.Error("Inner: {0}", ex.InnerException.Message);
            Logger.End(success: false, summary: $"DailyQuotaExceededException: {ex.Message}");
            return 1;
        }
        catch (RetryExhaustedException ex)
        {
            Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
            Console.Error("Wait 15-30 minutes and try again. Progress has been saved.");
            if (ex.InnerException != null)
                Console.Error(
                    "Inner: {0}: {1}",
                    ex.InnerException.GetType().Name,
                    ex.InnerException.Message
                );
            Logger.End(success: false, summary: $"RetryExhaustedException: {ex.Message}");
            return 1;
        }
        catch (AggregateException aex)
        {
            foreach (Exception ex in aex.InnerExceptions)
            {
                Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
                if (ex.InnerException != null)
                    Console.Error(
                        "  Inner: {0}: {1}",
                        ex.InnerException.GetType().Name,
                        ex.InnerException.Message
                    );
            }
            Exception firstError = aex.InnerExceptions[0];
            string summary =
                $"AggregateException ({aex.InnerExceptions.Count} errors): {firstError.GetType().Name}: {firstError.Message}";
            Logger.End(success: false, summary: summary);
            return 1;
        }
        catch (OperationCanceledException)
        {
            Console.Warning("Operation cancelled by user");
            Logger.Interrupted("Cancelled by Ctrl+C");
            return 130;
        }
        catch (Exception ex)
        {
            Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
            if (ex.InnerException != null)
                Console.Error(
                    "Inner: {0}: {1}",
                    ex.InnerException.GetType().Name,
                    ex.InnerException.Message
                );
            if (ex.StackTrace != null)
            {
                string firstStackLine = ex.StackTrace.Split('\n')[0].Trim();
                Console.Dim($"Stack: {firstStackLine}");
            }

            string summary =
                ex.InnerException != null
                    ? $"{ex.GetType().Name}: {ex.Message} (Inner: {ex.InnerException.Message})"
                    : $"{ex.GetType().Name}: {ex.Message}";

            Logger.End(success: false, summary: summary);
            return 1;
        }
    }
}

public sealed class SyncLastFmCommand : Command<SyncLastFmCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-v|--verbose")]
        [Description("Debug logging")]
        public bool Verbose { get; init; }

        [CommandOption("-r|--reset")]
        [Description("Clear cache first")]
        public bool Reset { get; init; }

        [CommandOption("--since")]
        [Description("Sync from date (yyyy/MM/dd)")]
        public string? Since { get; init; }
    }

    public override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        if (settings.Verbose)
        {
            Console.Level = LogLevel.Debug;
            Logger.FileLevel = LogLevel.Debug;
        }

        if (settings.Reset)
        {
            Console.Info("Clearing Last.fm local cache...");
            StateManager.DeleteLastFmStates();
            Console.Success("Cache cleared");
        }

        DateTime? sinceDate = null;
        if (!IsNullOrEmpty(settings.Since))
        {
            if (
                !DateTime.TryParseExact(
                    settings.Since,
                    "yyyy/MM/dd",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime parsed
                )
            )
            {
                Console.Error("Invalid date format. Use yyyy/MM/dd (e.g. 2024/01/01)");
                return 1;
            }

            sinceDate = parsed;
            Console.Warning(
                "Will delete existing data on/after {0} and re-sync",
                sinceDate.Value.ToString("yyyy/MM/dd")
            );
        }

        Logger.Start(ServiceType.LastFm);

        return SyncYouTubeCommand.ExecuteWithErrorHandling(() =>
        {
            Console.Info("Starting Last.fm sync...");
            new ScrobbleSyncOrchestrator(Program.Cts.Token, sinceDate).Execute();
        });
    }
}

public sealed class StatusCommand : Command<StatusCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[service]")]
        [Description("yt, lastfm (omit for all)")]
        public string? Service { get; init; }
    }

    public override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        bool checkLastFm =
            IsNullOrEmpty(settings.Service)
            || settings.Service.Equals("lastfm", StringComparison.OrdinalIgnoreCase);
        bool checkYouTube =
            IsNullOrEmpty(settings.Service)
            || settings.Service.Equals("yt", StringComparison.OrdinalIgnoreCase)
            || settings.Service.Equals("youtube", StringComparison.OrdinalIgnoreCase);

        if (!checkLastFm && !checkYouTube)
        {
            Console.Warning("Unknown service: {0}. Use: yt, lastfm", settings.Service);
            return 1;
        }

        if (checkLastFm)
            ShowLastFmStatus();

        if (checkYouTube)
            ShowYouTubeStatus();

        return 0;
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
