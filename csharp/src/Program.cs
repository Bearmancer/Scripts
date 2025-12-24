namespace CSharpScripts;

public static class Program
{
    private static bool cancelled;
    public static CancellationTokenSource Cts { get; } = new();

    public static int Main(string[] args)
    {
        if (args.Contains(value: "-v") || args.Contains(value: "--verbose"))
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
                Console.Warning(message: "Cancellation requested, stopping gracefully...");
            }
        };

        CommandApp app = new();

        app.Configure(config =>
        {
            config.SetApplicationName(name: "scripts");

            config.AddBranch(
                name: "sync",
                sync =>
                {
                    sync.SetDescription(description: "Sync data from various services");
                    sync.AddCommand<SyncAllCommand>(name: "all")
                        .WithDescription(description: "Sync YouTube and Last.fm");
                    sync.AddCommand<SyncYouTubeCommand>(name: "yt")
                        .WithDescription(description: "Sync YouTube playlists");
                    sync.AddCommand<SyncLastFmCommand>(name: "lastfm")
                        .WithDescription(description: "Sync Last.fm scrobbles");
                    sync.AddCommand<StatusCommand>(name: "status")
                        .WithDescription(description: "Show sync status");
                }
            );

            config.AddBranch(
                name: "clean",
                clean =>
                {
                    clean.SetDescription(description: "Clean local state");
                    clean
                        .AddCommand<CleanLocalCommand>(name: "local")
                        .WithDescription(description: "Clean local state files");
                    clean
                        .AddCommand<CleanPurgeCommand>(name: "purge")
                        .WithDescription(description: "Purge all state and spreadsheets");
                }
            );

            config.AddBranch(
                name: "music",
                music =>
                {
                    music.SetDescription(description: "Music metadata commands");
                    music
                        .AddCommand<MusicSearchCommand>(name: "search")
                        .WithDescription(description: "Search or lookup a music release");
                    music
                        .AddCommand<MusicFillCommand>(name: "fill")
                        .WithDescription(
                            description: "Fill missing fields in TSV/CSV using MB and Discogs"
                        );
                }
            );

            config.AddBranch(
                name: "mail",
                mail =>
                {
                    mail.SetDescription(description: "Temporary email commands");
                    mail.AddCommand<MailCreateCommand>(name: "create")
                        .WithDescription(description: "Create a temporary email");
                }
            );

            config.AddBranch(
                name: "completion",
                completion =>
                {
                    completion.SetDescription(description: "Tab completion support");
                    completion
                        .AddCommand<CompletionInstallCommand>(name: "install")
                        .WithDescription(
                            description: "Install PowerShell tab completion to your profile"
                        );
                    completion
                        .AddCommand<CompletionSuggestCommand>(name: "suggest")
                        .WithDescription(
                            description: "Get completion suggestions (used internally)"
                        )
                        .IsHidden();
                }
            );
        });

        return app.Run(args: args);
    }
}
