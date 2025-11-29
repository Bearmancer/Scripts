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

        rootCommand.AddCommand(syncCommand);
        rootCommand.AddCommand(statusCommand);
        rootCommand.AddCommand(clearCommand);
        rootCommand.AddCommand(exportCommand);

        return rootCommand.Invoke(args);
    }

    static Command BuildSyncCommand(Option<bool> verboseOption)
    {
        Command syncCommand = new(
            name: "sync",
            description: """
            Sync data to Google Sheets.

            SUBCOMMANDS:
              yt        Sync YouTube playlists
              lastfm    Sync Last.fm scrobbles

            EXAMPLES:
              dotnet run sync yt
              dotnet run sync lastfm
            """
        );

        Command ytCommand = new(name: "yt", description: "Sync YouTube playlists to Google Sheets");
        ytCommand.AddAlias("youtube");

        ytCommand.SetHandler(
            (verbose) =>
            {
                if (verbose)
                    Logger.CurrentLogLevel = LogLevel.Debug;

                RunYouTube();
            },
            verboseOption
        );

        Command lastfmCommand = new(
            name: "lastfm",
            description: "Sync Last.fm scrobbles to Google Sheets"
        );

        lastfmCommand.SetHandler(
            (verbose) =>
            {
                if (verbose)
                    Logger.CurrentLogLevel = LogLevel.Debug;

                RunLastFm();
            },
            verboseOption
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
              service   yt, lastfm (omit for all)

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
            Clear state, cache, spreadsheets, and rebuild project.

            Clears local state/cache files, spreadsheet content, deletes
            build artifacts (bin/obj), and runs a fresh build.

            USAGE:
              dotnet run clear [service]

            ARGUMENTS:
              service    yt, lastfm, all (default: all)

            EXAMPLES:
              dotnet run clear
              dotnet run clear yt
              dotnet run clear lastfm
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

        clearCommand.AddArgument(serviceArg);

        clearCommand.SetHandler(ClearAndRebuild, serviceArg);

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
            YouTubePlaylistOrchestrator.ExportSheetsAsCSVs(ct: cts.Token);
        });

        csvCommand.AddCommand(ytCsvCommand);
        exportCommand.AddCommand(csvCommand);

        return exportCommand;
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

    static void RunYouTube() =>
        RunWithErrorHandling(
            YouTube,
            () =>
            {
                Logger.Info("Starting YouTube sync...");
                new YouTubePlaylistOrchestrator(cts.Token).Execute();
            }
        );

    static void ClearAndRebuild(string service)
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

            StateManager.DeleteLastFmStates();
            Logger.Success("Last.fm state cleared.");
        }

        if (clearYouTube)
        {
            Logger.Info("Clearing YouTube...");

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
