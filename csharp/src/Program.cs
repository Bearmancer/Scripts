using CSharpScripts.CLI.Commands;

namespace CSharpScripts;

public static class Program
{
    public static CancellationTokenSource Cts { get; } = new();

    public static int Main(string[] args)
    {
        System.Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Cts.Cancel();
            Console.Warning("Cancellation requested...");
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
                        .WithDescription("Search for music");
                    music
                        .AddCommand<MusicLookupCommand>("lookup")
                        .WithDescription("Look up a release");
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
        });

        return app.Run(args);
    }
}
