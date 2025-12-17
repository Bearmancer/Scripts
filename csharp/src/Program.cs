using CSharpScripts.CLI.Commands;

namespace CSharpScripts;

public static class Program
{
    public static CancellationTokenSource Cts { get; } = new();
    private static bool cancelled;

    public static int Main(string[] args)
    {
        // Global verbose flag - affects all Console.Debug() calls project-wide
        if (args.Contains("-v") || args.Contains("--verbose"))
        {
            Console.Level = LogLevel.Debug;
            Logger.FileLevel = LogLevel.Debug;
        }

        System.Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            if (!cancelled)
            {
                cancelled = true;
                Cts.Cancel();
                Console.Warning("Cancellation requested, stopping gracefully...");
            }
        };

        CommandApp app = new();

        app.Configure(config =>
        {
            config.SetApplicationName("scripts");

            config.AddBranch(
                "sync",
                sync =>
                {
                    sync.SetDescription("Sync data from various services");
                    sync.AddCommand<SyncAllCommand>("all")
                        .WithDescription("Sync YouTube and Last.fm");
                    sync.AddCommand<SyncYouTubeCommand>("yt")
                        .WithDescription("Sync YouTube playlists");
                    sync.AddCommand<SyncLastFmCommand>("lastfm")
                        .WithDescription("Sync Last.fm scrobbles");
                    sync.AddCommand<StatusCommand>("status").WithDescription("Show sync status");
                }
            );

            config.AddBranch(
                "clean",
                clean =>
                {
                    clean.SetDescription("Clean local state");
                    clean
                        .AddCommand<CleanLocalCommand>("local")
                        .WithDescription("Clean local state files");
                    clean
                        .AddCommand<CleanPurgeCommand>("purge")
                        .WithDescription("Purge all state and spreadsheets");
                }
            );

            config.AddBranch(
                "music",
                music =>
                {
                    music.SetDescription("Music metadata commands");
                    music
                        .AddCommand<MusicSearchCommand>("search")
                        .WithDescription("Search or lookup a music release");
                    music
                        .AddCommand<MusicSchemaCommand>("schema")
                        .WithDescription("List all metadata fields from MusicBrainz and Discogs");
                }
            );

            config.AddBranch(
                "mail",
                mail =>
                {
                    mail.SetDescription("Temporary email commands");
                    mail.AddCommand<MailCreateCommand>("create")
                        .WithDescription("Create a temporary email");
                }
            );

            config.AddBranch(
                "completion",
                completion =>
                {
                    completion.SetDescription("Tab completion support");
                    completion
                        .AddCommand<CompletionInstallCommand>("install")
                        .WithDescription("Install PowerShell tab completion to your profile");
                    completion
                        .AddCommand<CompletionSuggestCommand>("suggest")
                        .WithDescription("Get completion suggestions (used internally)")
                        .IsHidden();
                }
            );
        });

        return app.Run(args);
    }
}
