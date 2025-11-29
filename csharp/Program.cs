global using System.Collections.Frozen;
global using System.CommandLine;
global using System.Diagnostics;
global using static System.Environment;
global using static System.IO.Directory;
global using static System.IO.File;
global using static System.IO.Path;
global using static System.String;
global using System.Text.Json;
global using System.Xml;
global using CSharpScripts.Configuration;
global using CSharpScripts.Infrastructure;
global using CSharpScripts.Models;
global using CSharpScripts.Orchestration;
global using CSharpScripts.Orchestration.YouTube;
global using CSharpScripts.Services;
global using Google.Apis.Auth.OAuth2;
global using Google.Apis.Drive.v3;
global using Google.Apis.Services;
global using Google.Apis.Sheets.v4;
global using Google.Apis.Sheets.v4.Data;
global using Hqub.Lastfm;
global using Polly;
global using Spectre.Console;
using static CSharpScripts.Infrastructure.LogLevel;
using static CSharpScripts.Infrastructure.ServiceType;

namespace CSharpScripts;

internal class Program
{
    static readonly CancellationTokenSource cts = new();

    static int Main(string[] args)
    {
        Logger.CurrentLogLevel = Info;

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Logger.Error("Shutdown requested...");
        };

        Option<bool> verboseOption = new(
            aliases: ["--verbose", "-v"],
            description: "Enable debug logging"
        );

        RootCommand rootCommand = new(
            description: "Sync music data to Google Sheets. Use <command> --help for details."
        );
        rootCommand.AddGlobalOption(verboseOption);

        var syncCommand = BuildSyncCommand(verboseOption);
        var statusCommand = BuildStatusCommand();
        var clearCommand = BuildClearCommand();
        var exportCommand = BuildExportCommand();
        var dupesCommand = BuildDupesCommand();

        rootCommand.AddCommand(syncCommand);
        rootCommand.AddCommand(statusCommand);
        rootCommand.AddCommand(clearCommand);
        rootCommand.AddCommand(exportCommand);
        rootCommand.AddCommand(dupesCommand);

        return rootCommand.Invoke(args);
    }

    static Command BuildSyncCommand(Option<bool> verboseOption)
    {
        Command syncCommand = new(
            name: "sync",
            description: """
            Sync data to Google Sheets.

            SUBCOMMANDS:
              yt [ids]    Sync YouTube playlists
              lastfm      Sync Last.fm scrobbles

            OPTIONS:
              -r, --refresh       Clear local cache before sync
              -c, --clear-sheet   Clear spreadsheet content before sync

            EXAMPLES:
              dotnet run sync yt
              dotnet run sync yt PLxxxxxx
              dotnet run sync yt -r
              dotnet run sync lastfm
              dotnet run sync lastfm -c
            """
        );

        Option<bool> refreshOption = new(
            aliases: ["--refresh", "-r"],
            description: "Clear local cache before syncing"
        );

        Option<bool> clearSheetOption = new(
            aliases: ["--clear-sheet", "-c"],
            description: "Clear spreadsheet content before syncing"
        );

        Command ytCommand = new(name: "yt", description: "Sync YouTube playlists to Google Sheets");
        ytCommand.AddAlias("youtube");

        Argument<string[]> playlistIdsArg = new(
            name: "playlist-ids",
            description: "Playlist ID(s) or title(s) to sync (omit for all)"
        )
        {
            Arity = ArgumentArity.ZeroOrMore,
        };

        ytCommand.AddArgument(playlistIdsArg);
        ytCommand.AddOption(refreshOption);
        ytCommand.AddOption(clearSheetOption);

        ytCommand.SetHandler(
            (verbose, refresh, clearSheet, playlistIds) =>
            {
                if (verbose)
                    Logger.CurrentLogLevel = LogLevel.Debug;

                if (refresh)
                    StateManager.DeleteYouTubeStates();

                if (clearSheet)
                    ClearYouTubeSheetContent();

                RunYouTube(playlistIds);
            },
            verboseOption,
            refreshOption,
            clearSheetOption,
            playlistIdsArg
        );

        Command lastfmCommand = new(
            name: "lastfm",
            description: "Sync Last.fm scrobbles to Google Sheets"
        );

        Option<bool> lastfmRefreshOption = new(
            aliases: ["--refresh", "-r"],
            description: "Clear local cache before syncing"
        );

        Option<bool> lastfmClearSheetOption = new(
            aliases: ["--clear-sheet", "-c"],
            description: "Clear spreadsheet content before syncing"
        );

        lastfmCommand.AddOption(lastfmRefreshOption);
        lastfmCommand.AddOption(lastfmClearSheetOption);

        lastfmCommand.SetHandler(
            (verbose, refresh, clearSheet) =>
            {
                if (verbose)
                    Logger.CurrentLogLevel = LogLevel.Debug;

                if (refresh)
                    StateManager.DeleteLastFmStates();

                if (clearSheet)
                    ClearLastFmSheetContent();

                RunLastFm();
            },
            verboseOption,
            lastfmRefreshOption,
            lastfmClearSheetOption
        );

        syncCommand.AddCommand(ytCommand);
        syncCommand.AddCommand(lastfmCommand);

        return syncCommand;
    }

    static Command BuildStatusCommand()
    {
        Command statusCommand = new(
            name: "status",
            description: """
            Show cache and state information.

            USAGE:
              dotnet run status [service]

            ARGUMENTS:
              service    yt, lastfm (omit for all)

            EXAMPLES:
              dotnet run status
              dotnet run status yt
              dotnet run status lastfm
            """
        );

        Argument<string> serviceArg = new(
            name: "service",
            description: "Service to check: yt, lastfm (omit for all)"
        )
        {
            Arity = ArgumentArity.ZeroOrOne,
        };

        statusCommand.AddArgument(serviceArg);
        statusCommand.SetHandler(CheckStatus, serviceArg);

        return statusCommand;
    }

    static Command BuildClearCommand()
    {
        Command clearCommand = new(
            name: "clear",
            description: """
            Clear state, cache, and rebuild project.

            Clears local state/cache files, optionally clears spreadsheet content,
            deletes build artifacts (bin/obj), and runs a fresh build.

            USAGE:
              dotnet run clear [service]

            ARGUMENTS:
              service    yt, lastfm, all (default: all)

            OPTIONS:
              --sheet    Also clear spreadsheet content (keeps the spreadsheet)

            EXAMPLES:
              dotnet run clear
              dotnet run clear yt
              dotnet run clear lastfm --sheet
              dotnet run clear all --sheet
            """
        );

        Argument<string> serviceArg = new(
            name: "service",
            description: "Service to clear: yt, lastfm, all"
        )
        {
            Arity = ArgumentArity.ZeroOrOne,
        };
        serviceArg.SetDefaultValue("all");

        Option<bool> clearSheetOption = new(
            aliases: ["--sheet"],
            description: "Also clear spreadsheet content (keeps the spreadsheet)"
        );

        clearCommand.AddArgument(serviceArg);
        clearCommand.AddOption(clearSheetOption);

        clearCommand.SetHandler(
            (service, clearSheet) =>
            {
                ClearAndRebuild(service, clearSheet);
            },
            serviceArg,
            clearSheetOption
        );

        return clearCommand;
    }

    static Command BuildExportCommand()
    {
        Command exportCommand = new(
            name: "export",
            description: """
            Export data to files.

            SUBCOMMANDS:
              csv yt      Export YouTube playlists as CSV files

            EXAMPLES:
              dotnet run export csv yt
            """
        );

        Command csvCommand = new(name: "csv", description: "Export data as CSV files");

        Command ytCsvCommand = new(
            name: "yt",
            description: "Export YouTube playlists as individual CSV files"
        );

        ytCsvCommand.SetHandler(() =>
        {
            YouTubePlaylistOrchestrator.ExportSheetsAsCSVs();
        });

        csvCommand.AddCommand(ytCsvCommand);
        exportCommand.AddCommand(csvCommand);

        return exportCommand;
    }

    static Command BuildDupesCommand()
    {
        Command dupesCommand = new(
            name: "dupes",
            description: """
            Check for and manage duplicate spreadsheets in Google Drive.

            Searches for spreadsheets with matching names and allows you to
            delete duplicates interactively.

            USAGE:
              dotnet run dupes [service]

            ARGUMENTS:
              service    yt, lastfm, all (default: all)

            EXAMPLES:
              dotnet run dupes
              dotnet run dupes yt
            """
        );

        Argument<string> serviceArg = new(
            name: "service",
            description: "Service to check: yt, lastfm, all"
        )
        {
            Arity = ArgumentArity.ZeroOrOne,
        };
        serviceArg.SetDefaultValue("all");

        dupesCommand.AddArgument(serviceArg);
        dupesCommand.SetHandler(CheckAndManageDuplicates, serviceArg);

        return dupesCommand;
    }

    static void RunWithErrorHandling(ServiceType service, Action action)
    {
        Logger.Start(service);

        try
        {
            action();
        }
        catch (DailyQuotaExceededException ex)
        {
            Logger.Error(ex.Message);
            Logger.Error("Try again tomorrow or request quota increase from Google Cloud Console.");
            Logger.End(success: false, summary: "Daily quota exceeded");
        }
        catch (RetryExhaustedException ex)
        {
            Logger.Error(ex.Message);
            Logger.Error("Wait 15-30 minutes and try again. Progress has been saved.");
            Logger.End(success: false, summary: "Retry limit reached");
        }
        catch (AggregateException aex)
        {
            foreach (var ex in aex.InnerExceptions)
            {
                Logger.Error("{0}: {1}", ex.GetType().Name, ex.Message);
                Logger.FileError(ex.Message, ex);
            }
            Logger.End(
                success: false,
                summary: $"Failed with {aex.InnerExceptions.Count} error(s)"
            );
        }
        catch (OperationCanceledException)
        {
            Logger.Warning("Operation cancelled by user");
            Logger.Interrupted("Cancelled by Ctrl+C");
        }
        catch (Exception ex)
        {
            Logger.Error("{0}: {1}", ex.GetType().Name, ex.Message);
            Logger.FileError(ex.Message, ex);
            Logger.End(success: false, summary: ex.Message);
        }
    }

    static void RunLastFm() =>
        RunWithErrorHandling(
            LastFm,
            () =>
            {
                Logger.Info("Starting Last.fm sync...");
                new ScrobbleSyncOrchestrator(cts.Token).Execute();
            }
        );

    static void RunYouTube(string[] playlistIds) =>
        RunWithErrorHandling(
            YouTube,
            () =>
            {
                Logger.Info("Starting YouTube sync...");
                var orchestrator = new YouTubePlaylistOrchestrator(cts.Token);

                if (playlistIds.Length > 0)
                    orchestrator.ExecuteForPlaylists(playlistIds);
                else
                    orchestrator.Execute();
            }
        );

    static void ClearLastFmSheetContent()
    {
        var state = StateManager.Load<FetchState>(StateManager.FetchStateFile);
        if (IsNullOrEmpty(state.SpreadsheetId))
            return;

        var sheets = new GoogleSheetsService(
            clientId: AuthenticationConfig.GoogleClientId,
            clientSecret: AuthenticationConfig.GoogleClientSecret
        );
        sheets.ClearSubsheet(state.SpreadsheetId, "Scrobbles");
        Logger.Info("Cleared Last.fm spreadsheet content.");
    }

    static void ClearYouTubeSheetContent()
    {
        var state = StateManager.Load<YouTubeFetchState>(StateManager.YouTubeStateFile);
        if (IsNullOrEmpty(state.SpreadsheetId))
            return;

        var sheets = new GoogleSheetsService(
            clientId: AuthenticationConfig.GoogleClientId,
            clientSecret: AuthenticationConfig.GoogleClientSecret
        );

        var sheetNames = sheets.GetSubsheetNames(state.SpreadsheetId);
        foreach (var sheet in sheetNames.Where(s => s != "README"))
            sheets.DeleteSubsheet(state.SpreadsheetId, sheet);

        Logger.Info("Cleared YouTube spreadsheet content.");
    }

    static void ClearAndRebuild(string service, bool clearSheet)
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

        GoogleSheetsService? sheets = null;

        if (clearLastFm)
        {
            Logger.Info("Clearing Last.fm...");

            if (clearSheet)
            {
                var state = StateManager.Load<FetchState>(StateManager.FetchStateFile);
                if (!IsNullOrEmpty(state.SpreadsheetId))
                {
                    sheets ??= new GoogleSheetsService(
                        clientId: AuthenticationConfig.GoogleClientId,
                        clientSecret: AuthenticationConfig.GoogleClientSecret
                    );
                    sheets.ClearSubsheet(state.SpreadsheetId, "Scrobbles");
                    Logger.Success("Last.fm spreadsheet content cleared.");
                }
            }

            StateManager.DeleteLastFmStates();
            Logger.Success("Last.fm state cleared.");
        }

        if (clearYouTube)
        {
            Logger.Info("Clearing YouTube...");

            if (clearSheet)
            {
                var state = StateManager.Load<YouTubeFetchState>(StateManager.YouTubeStateFile);
                if (!IsNullOrEmpty(state.SpreadsheetId))
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
            }

            StateManager.DeleteYouTubeStates();
            Logger.Success("YouTube state cleared.");
        }

        var binDir = Combine(Paths.ProjectRoot, "csharp", "bin");
        var objDir = Combine(Paths.ProjectRoot, "csharp", "obj");

        if (Directory.Exists(binDir))
        {
            Delete(binDir, recursive: true);
            Logger.Info("Deleted bin/");
        }

        if (Directory.Exists(objDir))
        {
            Delete(objDir, recursive: true);
            Logger.Info("Deleted obj/");
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

    static void CheckAndManageDuplicates(string service)
    {
        var normalizedService = service.ToLowerInvariant();
        var checkAll = normalizedService == "all";
        var checkLastFm = checkAll || normalizedService == "lastfm";
        var checkYouTube = checkAll || normalizedService is "youtube" or "yt";

        if (!checkLastFm && !checkYouTube)
        {
            Logger.Warning("Invalid service: {0}. Use: yt, lastfm, or all", service);
            return;
        }

        var sheets = new GoogleSheetsService(
            clientId: AuthenticationConfig.GoogleClientId,
            clientSecret: AuthenticationConfig.GoogleClientSecret
        );

        List<(string Title, List<(string Id, string Url)> Duplicates)> allDupes = [];

        if (checkLastFm)
        {
            var dupes = sheets.FindDuplicateSpreadsheets(SpreadsheetConfig.LastFmSpreadsheetTitle);
            if (dupes.Count > 1)
                allDupes.Add((SpreadsheetConfig.LastFmSpreadsheetTitle, dupes));
        }

        if (checkYouTube)
        {
            var dupes = sheets.FindDuplicateSpreadsheets(SpreadsheetConfig.YouTubeSpreadsheetTitle);
            if (dupes.Count > 1)
                allDupes.Add((SpreadsheetConfig.YouTubeSpreadsheetTitle, dupes));
        }

        if (allDupes.Count == 0)
        {
            Logger.Success("No duplicate spreadsheets found.");
            return;
        }

        foreach (var (title, dupes) in allDupes)
        {
            Logger.Warning("Found {0} spreadsheets named \"{1}\":", dupes.Count, title);
            Logger.NewLine();

            for (var i = 0; i < dupes.Count; i++)
            {
                var (id, url) = dupes[i];
                Logger.Info("  [{0}] {1}", i + 1, url);
            }

            Logger.NewLine();
            Logger.Info(
                "Enter numbers to DELETE (comma-separated, e.g., 1,3) or press Enter to skip:"
            );

            var input = Console.ReadLine()?.Trim();
            if (IsNullOrEmpty(input))
            {
                Logger.Info("Skipped.");
                continue;
            }

            var toDelete = input
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var n) ? n : -1)
                .Where(n => n >= 1 && n <= dupes.Count)
                .Distinct()
                .ToList();

            if (toDelete.Count == 0)
            {
                Logger.Warning("No valid selections.");
                continue;
            }

            Logger.Warning("Will delete {0} spreadsheet(s).", toDelete.Count);
            if (!AnsiConsole.Confirm("Confirm deletion?", defaultValue: false))
            {
                Logger.Info("Cancelled.");
                continue;
            }

            foreach (var idx in toDelete)
            {
                var (id, _) = dupes[idx - 1];
                sheets.DeleteSpreadsheet(id);
                Logger.Success("Deleted spreadsheet {0}", id);
            }
        }
    }

    static void CheckStatus(string? service)
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
        {
            Logger.Info("=== Last.fm ===");
            var stateFile = Combine(Paths.StateDirectory, StateManager.FetchStateFile);
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
                var sheets = new GoogleSheetsService(
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

        if (checkYouTube)
        {
            Logger.Info("=== YouTube ===");
            var stateFile = Combine(Paths.StateDirectory, StateManager.YouTubeStateFile);
            var cached = File.Exists(stateFile);

            if (cached)
            {
                var json = ReadAllText(stateFile);
                var state =
                    JsonSerializer.Deserialize<YouTubeFetchState>(json, StateManager.JsonOptions)
                    ?? new YouTubeFetchState();
                var totalVideos = state.PlaylistSnapshots.Values.Sum(s => s.VideoIds.Count);
                var spreadsheetUrl =
                    $"https://docs.google.com/spreadsheets/d/{state.SpreadsheetId}";

                if (!state.FetchComplete)
                    Logger.Warning("Fetch incomplete - run sync to resume");

                Logger.Info(
                    "Playlists: {0} | Videos: {1}",
                    state.PlaylistSnapshots.Count,
                    totalVideos
                );
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
}
