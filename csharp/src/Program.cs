using CSharpScripts.CLI.Commands;
using Spectre.Console.Cli;

namespace CSharpScripts;

public static class Program
{
    public static readonly CancellationTokenSource Cts = new();

    public static int Main(string[] args)
    {
        System.Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Cts.Cancel();
            AnsiConsole.MarkupLine("[red]Shutdown requested...[/]");
        };

        // Special case: direct Ormandy invocation (internal use)
        if (args.Length > 0 && args[0].Equals("--ormandy", StringComparison.OrdinalIgnoreCase))
        {
            return RunOrmandy().GetAwaiter().GetResult();
        }

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("cli");
            config.SetApplicationVersion("1.0.0");

            config.AddBranch(
                "sync",
                sync =>
                {
                    sync.SetDescription("Sync data to Google Sheets");
                    sync.SetDefaultCommand<SyncAllCommand>();
                    sync.AddCommand<SyncAllCommand>("all")
                        .WithDescription("Sync all services (YouTube + Last.fm)");
                    sync.AddCommand<SyncYouTubeCommand>("yt")
                        .WithDescription("Sync YouTube playlists to Google Sheets");
                    sync.AddCommand<SyncLastFmCommand>("lastfm")
                        .WithDescription("Sync Last.fm scrobbles to Google Sheets");
                }
            );

            config.AddBranch(
                "clean",
                clean =>
                {
                    clean.SetDescription("Delete state, cache, and remote data");
                    clean
                        .AddCommand<CleanLocalCommand>("local")
                        .WithDescription("Delete local state and cache files only");
                    clean
                        .AddCommand<CleanPurgeCommand>("purge")
                        .WithDescription("Delete all: state, remote data, CSVs, and builds");
                }
            );

            config.AddBranch(
                "music",
                music =>
                {
                    music.SetDescription("Search Discogs and MusicBrainz");
                    music
                        .AddCommand<MusicSearchCommand>("search")
                        .WithDescription("Search for releases across Discogs and MusicBrainz");
                    music
                        .AddCommand<MusicLookupCommand>("lookup")
                        .WithDescription("Get detailed release information by ID");
                }
            );

            config.AddBranch(
                "mail",
                mail =>
                {
                    mail.SetDescription("Temporary email management");
                    mail.AddCommand<MailCreateCommand>("create")
                        .WithDescription("Create temporary email account");
                    mail.AddCommand<MailCheckCommand>("check")
                        .WithDescription("Check inbox for messages");
                    mail.AddCommand<MailDeleteCommand>("delete")
                        .WithDescription("Delete temporary email account");
                }
            );

            config
                .AddCommand<StatusCommand>("status")
                .WithDescription("Show sync state and cache info");
        });

        return app.Run(args);
    }

    static async Task<int> RunOrmandy()
    {
        try
        {
            // Enable debug logging for detailed output
            Console.Level = LogLevel.Debug;

            OrmandyBoxParser parser = new();
            List<OrmandyTrack> tracks = await parser.ParseAsync();
            parser.Display(tracks);

            Console.NewLine();
            parser.CreateAndWriteToSheet(tracks);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
            if (ex.InnerException != null)
                Console.Error("Inner: {0}", ex.InnerException.Message);
            return 1;
        }
    }
}
