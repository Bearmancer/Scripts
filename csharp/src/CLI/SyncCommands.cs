namespace CSharpScripts.CLI.Commands;

#region SyncAllCommand

public sealed class SyncAllCommand : AsyncCommand<SyncAllCommand.Settings>
{
    public override async Task<int> ExecuteAsync(
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
            Console.Info(message: "Clearing local cache...");
            StateManager.DeleteLastFmStates();
            StateManager.DeleteAllYouTubeStates();
            Console.Success(message: "Cache cleared");
        }

        Console.Rule(text: "YouTube Sync");
        int ytResult = await RunYouTubeSyncAsync();

        Console.NewLine();
        Console.Rule(text: "Last.fm Sync");
        int lfResult = await RunLastFmSyncAsync();

        Console.NewLine();
        if (ytResult == 0 && lfResult == 0)
            Console.Success(message: "All syncs complete!");
        else
            Console.Warning(
                message: "Completed with errors (YouTube: {0}, Last.fm: {1})",
                ytResult,
                lfResult
            );

        return ytResult != 0 ? ytResult : lfResult;
    }

    private static async Task<int> RunYouTubeSyncAsync()
    {
        Logger.Start(service: ServiceType.YouTube);
        return await SyncYouTubeCommand.ExecuteWithErrorHandlingAsync(async () =>
            await new YouTubePlaylistOrchestrator(ct: Program.Cts.Token).ExecuteAsync()
        );
    }

    private static async Task<int> RunLastFmSyncAsync()
    {
        Logger.Start(service: ServiceType.LastFm);
        return await SyncYouTubeCommand.ExecuteWithErrorHandlingAsync(async () =>
            await new ScrobbleSyncOrchestrator(
                forceFromDate: null,
                ct: Program.Cts.Token
            ).ExecuteAsync()
        );
    }

    public sealed class Settings : CommandSettings
    {
        [CommandOption(template: "-v|--verbose")]
        [Description(description: "Debug logging")]
        public bool Verbose { get; init; }

        [CommandOption(template: "-r|--reset")]
        [Description(description: "Clear cache first")]
        public bool Reset { get; init; }
    }
}

#endregion

#region SyncYouTubeCommand

public sealed class SyncYouTubeCommand : AsyncCommand<SyncYouTubeCommand.Settings>
{
    public override async Task<int> ExecuteAsync(
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

        Logger.Start(service: ServiceType.YouTube);

        return await ExecuteWithErrorHandlingAsync(async () =>
        {
            if (settings.Reset)
            {
                Console.Info(message: "Clearing YouTube cache...");
                StateManager.DeleteAllYouTubeStates();
                Console.Success(message: "Cache cleared");
            }

            if (settings.ShowSessionId)
                Console.Info(message: "Session ID: {0}", Logger.CurrentSessionId);

            await new YouTubePlaylistOrchestrator(ct: Program.Cts.Token).ExecuteAsync();
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
            Console.Error(message: "{0}: {1}", ex.GetType().Name, ex.Message);
            Console.Error(
                message: "Try again tomorrow or request quota increase from Google Cloud Console."
            );
            if (ex.InnerException != null)
                Console.Error(message: "Inner: {0}", ex.InnerException.Message);
            Logger.End(success: false, $"DailyQuotaExceededException: {ex.Message}", exception: ex);
            return 1;
        }
        catch (RetryExhaustedException ex)
        {
            Console.Error(message: "{0}: {1}", ex.GetType().Name, ex.Message);
            Console.Error(message: "Wait 15-30 minutes and try again. Progress has been saved.");
            if (ex.InnerException != null)
                Console.Error(
                    message: "Inner: {0}: {1}",
                    ex.InnerException.GetType().Name,
                    ex.InnerException.Message
                );
            Logger.End(success: false, $"RetryExhaustedException: {ex.Message}", exception: ex);
            return 1;
        }
        catch (AggregateException aex)
        {
            foreach (var ex in aex.InnerExceptions)
            {
                Console.Error(message: "{0}: {1}", ex.GetType().Name, ex.Message);
                if (ex.InnerException != null)
                    Console.Error(
                        message: "  Inner: {0}: {1}",
                        ex.InnerException.GetType().Name,
                        ex.InnerException.Message
                    );
            }
            var firstError = aex.InnerExceptions[index: 0];
            var summary =
                $"AggregateException ({aex.InnerExceptions.Count} errors): {firstError.GetType().Name}: {firstError.Message}";
            Logger.End(success: false, summary: summary, exception: aex);
            return 1;
        }
        catch (OperationCanceledException)
        {
            Console.Warning(message: "Operation cancelled by user");
            Logger.Interrupted(progress: "Cancelled by Ctrl+C");
            return 130;
        }
        catch (Exception ex)
        {
            Console.Error(message: "{0}: {1}", ex.GetType().Name, ex.Message);
            if (ex.InnerException != null)
                Console.Error(
                    message: "Inner: {0}: {1}",
                    ex.InnerException.GetType().Name,
                    ex.InnerException.Message
                );
            if (ex.StackTrace != null)
            {
                string firstStackLine = ex.StackTrace.Split(separator: '\n')[0].Trim();
                Console.Dim($"Stack: {firstStackLine}");
            }

            string summary =
                ex.InnerException != null
                    ? $"{ex.GetType().Name}: {ex.Message} (Inner: {ex.InnerException.Message})"
                    : $"{ex.GetType().Name}: {ex.Message}";

            Logger.End(success: false, summary: summary, exception: ex);
            return 1;
        }
    }

    internal static async Task<int> ExecuteWithErrorHandlingAsync(Func<Task> action)
    {
        try
        {
            await action();
            return 0;
        }
        catch (DailyQuotaExceededException ex)
        {
            Console.Error(message: "{0}: {1}", ex.GetType().Name, ex.Message);
            Console.Error(
                message: "Try again tomorrow or request quota increase from Google Cloud Console."
            );
            if (ex.InnerException != null)
                Console.Error(message: "Inner: {0}", ex.InnerException.Message);
            Logger.End(success: false, $"DailyQuotaExceededException: {ex.Message}", exception: ex);
            return 1;
        }
        catch (RetryExhaustedException ex)
        {
            Console.Error(message: "{0}: {1}", ex.GetType().Name, ex.Message);
            Console.Error(message: "Wait 15-30 minutes and try again. Progress has been saved.");
            if (ex.InnerException != null)
                Console.Error(
                    message: "Inner: {0}: {1}",
                    ex.InnerException.GetType().Name,
                    ex.InnerException.Message
                );
            Logger.End(success: false, $"RetryExhaustedException: {ex.Message}", exception: ex);
            return 1;
        }
        catch (OperationCanceledException)
        {
            Console.Warning(message: "Operation cancelled by user");
            Logger.Interrupted(progress: "Cancelled by Ctrl+C");
            return 130;
        }
        catch (Exception ex)
        {
            Console.Error(message: "{0}: {1}", ex.GetType().Name, ex.Message);
            if (ex.InnerException != null)
                Console.Error(
                    message: "Inner: {0}: {1}",
                    ex.InnerException.GetType().Name,
                    ex.InnerException.Message
                );
            if (ex.StackTrace != null)
            {
                string firstStackLine = ex.StackTrace.Split(separator: '\n')[0].Trim();
                Console.Dim($"Stack: {firstStackLine}");
            }

            string summary =
                ex.InnerException != null
                    ? $"{ex.GetType().Name}: {ex.Message} (Inner: {ex.InnerException.Message})"
                    : $"{ex.GetType().Name}: {ex.Message}";

            Logger.End(success: false, summary: summary, exception: ex);
            return 1;
        }
    }

    public sealed class Settings : CommandSettings
    {
        [CommandOption(template: "-v|--verbose")]
        [Description(description: "Debug logging")]
        [DefaultValue(value: false)]
        public bool Verbose { get; init; }

        [CommandOption(template: "-r|--reset")]
        [Description(description: "Clear cache first")]
        [DefaultValue(value: false)]
        public bool Reset { get; init; }

        [CommandOption(template: "-i|--session-id")]
        [Description(description: "Show session ID")]
        [DefaultValue(value: false)]
        public bool ShowSessionId { get; init; }
    }
}

#endregion

#region SyncLastFmCommand

public sealed class SyncLastFmCommand : AsyncCommand<SyncLastFmCommand.Settings>
{
    public override async Task<int> ExecuteAsync(
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
            Console.Info(message: "Clearing Last.fm local cache...");
            StateManager.DeleteLastFmStates();
            Console.Success(message: "Cache cleared");
        }

        DateTime? sinceDate = null;
        if (!IsNullOrEmpty(value: settings.Since))
        {
            if (
                !DateTime.TryParseExact(
                    s: settings.Since,
                    format: "yyyy/MM/dd",
                    provider: null,
                    style: DateTimeStyles.None,
                    out var parsed
                )
            )
            {
                Console.Error(message: "Invalid date format. Use yyyy/MM/dd (e.g. 2024/01/01)");
                return 1;
            }

            sinceDate = parsed;
            Console.Warning(
                message: "Will delete existing data on/after {0} and re-sync",
                sinceDate.Value.ToString(format: "yyyy/MM/dd")
            );
        }

        Logger.Start(service: ServiceType.LastFm);

        return await SyncYouTubeCommand.ExecuteWithErrorHandlingAsync(async () =>
            await new ScrobbleSyncOrchestrator(
                forceFromDate: sinceDate,
                ct: Program.Cts.Token
            ).ExecuteAsync()
        );
    }

    public sealed class Settings : CommandSettings
    {
        [CommandOption(template: "-v|--verbose")]
        [Description(description: "Debug logging")]
        public bool Verbose { get; init; }

        [CommandOption(template: "-r|--reset")]
        [Description(description: "Clear cache first")]
        public bool Reset { get; init; }

        [CommandOption(template: "--since")]
        [Description(description: "Sync from date (yyyy/MM/dd)")]
        public string? Since { get; init; }
    }
}

#endregion

#region StatusCommand

public sealed class StatusCommand : Command<StatusCommand.Settings>
{
    public override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        bool checkLastFm =
            IsNullOrEmpty(value: settings.Service)
            || settings.Service.Equals(
                value: "lastfm",
                comparisonType: StringComparison.OrdinalIgnoreCase
            );
        bool checkYouTube =
            IsNullOrEmpty(value: settings.Service)
            || settings.Service.Equals(
                value: "yt",
                comparisonType: StringComparison.OrdinalIgnoreCase
            )
            || settings.Service.Equals(
                value: "youtube",
                comparisonType: StringComparison.OrdinalIgnoreCase
            );

        if (checkLastFm)
            ShowLastFmStatus();

        if (checkYouTube)
            ShowYouTubeStatus();

        return 0;
    }

    private static void ShowLastFmStatus()
    {
        Console.Info(message: "=== Last.fm ===");
        string stateFile = Combine(path1: Paths.StateDirectory, path2: StateManager.LastFmSyncFile);
        bool hasState = File.Exists(path: stateFile);
        var spreadsheetUrl = $"https://docs.google.com/spreadsheets/d/{Config.LastFmSpreadsheetId}";

        if (hasState)
        {
            string json = ReadAllText(path: stateFile);
            var state =
                JsonSerializer.Deserialize<FetchState>(
                    json: json,
                    options: StateManager.JsonIndented
                ) ?? new FetchState();
            Console.Info(message: "Scrobbles: {0}", state.TotalFetched);
            Console.Info(message: "Cached: Yes");
            Console.Info(
                message: "Last sync: {0}",
                state.LastUpdated.ToString(format: "yyyy/MM/dd HH:mm:ss")
            );
            Console.Link(url: spreadsheetUrl, text: "Spreadsheet");
        }
        else
        {
            GoogleSheetsService sheets = new();
            int scrobbleCount = sheets.GetScrobbleCount(spreadsheetId: Config.LastFmSpreadsheetId);
            Console.Info(message: "Scrobbles: {0}", scrobbleCount);
            Console.Info(message: "Cached: No");
            Console.Link(url: spreadsheetUrl, text: "Spreadsheet");
        }

        Console.NewLine();
    }

    private static void ShowYouTubeStatus()
    {
        Console.Info(message: "=== YouTube ===");
        string stateFile = Combine(
            path1: Paths.StateDirectory,
            path2: StateManager.YouTubeSyncFile
        );
        bool cached = File.Exists(path: stateFile);

        if (cached)
        {
            string json = ReadAllText(path: stateFile);
            var state =
                JsonSerializer.Deserialize<YouTubeFetchState>(
                    json: json,
                    options: StateManager.JsonIndented
                ) ?? new YouTubeFetchState();
            int totalVideos = state.PlaylistSnapshots.Values.Sum(s => s.VideoIds.Count);
            var spreadsheetUrl = $"https://docs.google.com/spreadsheets/d/{state.SpreadsheetId}";

            if (!state.FetchComplete)
                Console.Warning(message: "Fetch incomplete - run sync to resume");

            Console.Info(
                message: "Playlists: {0} | Videos: {1}",
                state.PlaylistSnapshots.Count,
                totalVideos
            );
            Console.Info(message: "Cached: Yes");
            Console.Info(
                message: "Last sync: {0}",
                state.LastUpdated.ToString(format: "yyyy/MM/dd HH:mm:ss")
            );
            Console.Link(url: spreadsheetUrl, text: "Spreadsheet");
        }
        else
        {
            Console.Info(message: "Cached: No");
        }

        Console.NewLine();
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(position: 0, template: "[service]")]
        [Description(description: "yt, lastfm (omit for all)")]
        [AllowedValues("yt", "youtube", "lastfm", "all")]
        public string Service { get; init; } = "all";
    }
}

#endregion
