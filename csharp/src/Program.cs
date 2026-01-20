namespace CSharpScripts;

public static class Program
{
	private static bool cancelled;
	public static CancellationTokenSource cts { get; } = new();

	public static int Main(string[] args)
	{
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
				cts.Cancel();
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
					sync.AddCommand<StatusCommand>("status")
						.WithDescription("Show sync status");
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
						.AddCommand<MusicFillCommand>("fill")
						.WithDescription(
							"Fill missing fields in TSV/CSV using MB and Discogs"
						);
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
