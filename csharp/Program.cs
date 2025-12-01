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
global using CSharpScripts.Commands;
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

namespace CSharpScripts;

internal class Program
{
    static readonly CancellationTokenSource Cts = new();

    static int Main(string[] args)
    {
        Logger.ConsoleLevel = Info;

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Cts.Cancel();
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

        rootCommand.AddCommand(BuildSyncCommand(verboseOption));
        rootCommand.AddCommand(BuildStatusCommand());
        rootCommand.AddCommand(BuildClearCommand());
        rootCommand.AddCommand(BuildCleanCommand());
        rootCommand.AddCommand(BuildResetCommand());
        rootCommand.AddCommand(BuildExportCommand());

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
            """
        );

        Command ytCommand = new(name: "yt", description: "Sync YouTube playlists to Google Sheets");
        ytCommand.AddAlias("youtube");

        Option<bool> forceOption = new(
            aliases: ["--force", "-f"],
            description: "Clear cache and re-fetch all data from YouTube API"
        );
        ytCommand.AddOption(forceOption);

        ytCommand.SetHandler(
            (verbose, force) =>
            {
                if (verbose)
                {
                    Logger.ConsoleLevel = LogLevel.Debug;
                    Logger.FileLevel = LogLevel.Debug;
                }
                SyncHandler.YouTube(ct: Cts.Token, force: force);
            },
            verboseOption,
            forceOption
        );

        Command lastfmCommand = new(
            name: "lastfm",
            description: "Sync Last.fm scrobbles to Google Sheets"
        );

        lastfmCommand.SetHandler(
            (verbose) =>
            {
                if (verbose)
                {
                    Logger.ConsoleLevel = LogLevel.Debug;
                    Logger.FileLevel = LogLevel.Debug;
                }
                SyncHandler.LastFm(ct: Cts.Token);
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

            ARGUMENTS:
              service   yt, lastfm (omit for all)
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
        statusCommand.SetHandler(StatusHandler.Execute, serviceArg);

        return statusCommand;
    }

    static Command BuildClearCommand()
    {
        Command clearCommand = new(
            name: "clear",
            description: """
            Clear state and spreadsheet data (does not rebuild).

            ARGUMENTS:
              service    yt, lastfm, all (default: all)
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

        Option<bool> localOnlyOption = new(
            aliases: ["--local-only"],
            description: "Clear only local state files, keep spreadsheets"
        );

        Option<bool> remoteOnlyOption = new(
            aliases: ["--remote-only"],
            description: "Clear only spreadsheet contents, keep local state files"
        );

        clearCommand.AddArgument(serviceArg);
        clearCommand.AddOption(localOnlyOption);
        clearCommand.AddOption(remoteOnlyOption);

        clearCommand.SetHandler(
            ClearHandler.Execute,
            serviceArg,
            localOnlyOption,
            remoteOnlyOption
        );

        return clearCommand;
    }

    static Command BuildCleanCommand()
    {
        Command cleanCommand = new(
            name: "clean",
            description: "Delete bin/obj folders and rebuild the project."
        );

        cleanCommand.SetHandler(CleanHandler.Execute);

        return cleanCommand;
    }

    static Command BuildResetCommand()
    {
        Command resetCommand = new(
            name: "reset",
            description: """
            Full reset: clear all state/spreadsheets AND rebuild project.

            ARGUMENTS:
              service    yt, lastfm, all (default: all)
            """
        );

        Argument<string> serviceArg = new(
            name: "service",
            description: "Service to reset: yt, lastfm, all"
        )
        {
            Arity = ArgumentArity.ZeroOrOne,
        };
        serviceArg.SetDefaultValue("all");

        resetCommand.AddArgument(serviceArg);
        resetCommand.SetHandler(ResetHandler.Execute, serviceArg);

        return resetCommand;
    }

    static Command BuildExportCommand()
    {
        Command exportCommand = new(
            name: "export",
            description: """
            Export data to files.

            SUBCOMMANDS:
              csv yt      Export YouTube playlists as CSV files
            """
        );

        Command csvCommand = new(name: "csv", description: "Export data as CSV files");
        Command ytCsvCommand = new(
            name: "yt",
            description: "Export YouTube playlists as individual CSV files"
        );

        ytCsvCommand.SetHandler(() => ExportHandler.YouTubeCsv(ct: Cts.Token));

        csvCommand.AddCommand(ytCsvCommand);
        exportCommand.AddCommand(csvCommand);

        return exportCommand;
    }
}
