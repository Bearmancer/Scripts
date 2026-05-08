using CSharpScripts.CLI.Clean;
using CSharpScripts.CLI.Cloud;
using CSharpScripts.CLI.Mail;
using CSharpScripts.CLI.Music;
using CSharpScripts.CLI.Read;
using CSharpScripts.CLI.Sync;
using Npgsql;

namespace CSharpScripts;

internal static class Program
{
	private static volatile bool Cancelled;
	public static CancellationTokenSource Cts { get; } = new();
	private static ServiceProvider? _serviceProvider;

	public static int Main(string[] args)
	{
		Serilog.Log.Logger = Log.BuildAppLogger("app.jsonl");

		try
		{
			Console.CancelKeyPress += (_, e) =>
			{
				e.Cancel = true;
				if (!Cancelled)
				{
					Cancelled = true;
					Cts.Cancel();
					UI.Warn(message: "Cancellation requested, stopping gracefully...");
				}
			};

			var services = new ServiceCollection();
			ConfigureServices(services);
			_serviceProvider = services.BuildServiceProvider();

			var registrar = new SpectreTypeRegistrar(_serviceProvider);
			CommandApp app = new(registrar);

			app.Configure(config =>
			{
				config.SetApplicationName("tools");

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
						sync.AddCommand<HistoryCommand>("history")
							.WithDescription("Show sync history");
					}
				);

				config.AddBranch(
					"clean",
					clean =>
					{
						clean.SetDescription("Clean local state");
						clean
							.AddCommand<CleanCacheCommand>("cache")
							.WithDescription("Clean local cache files");
						clean
							.AddCommand<CleanResetCommand>("reset")
							.WithDescription("Reset all state and spreadsheets");
					}
				);

				config.AddBranch(
					"music",
					music =>
					{
						music.SetDescription("Music metadata commands");
						music
							.AddCommand<MusicSearchCommand>("search")
							.WithDescription("Search for a music release");
						music
							.AddCommand<MusicEnrichCommand>("enrich")
							.WithDescription(
								"Enrich CSV with missing metadata from MusicBrainz and Discogs"
							);
						music
							.AddCommand<MusicNotesCommand>("notes")
							.WithDescription("Parse and display Discogs release notes by ID");
						music
							.AddCommand<MusicTranslateCommand>("translate")
							.WithDescription(
								"Translate non-English titles in a CSV using Azure Translator"
							);
						music.AddBranch(
							"lookup",
							lookup =>
							{
								lookup.SetDescription("Lookup a release by ID");
								lookup
									.AddCommand<DiscogsLookupCommand>("discogs")
									.WithDescription("Lookup a Discogs release by ID");
								lookup
									.AddCommand<MusicBrainzLookupCommand>("mb")
									.WithDescription("Lookup a MusicBrainz release by GUID");
							}
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
						mail.AddCommand<MailCheckCommand>("check")
							.WithDescription("Check for incoming messages");
						mail.AddCommand<MailDeleteCommand>("delete")
							.WithDescription("Delete temporary email account");
					}
				);

				config
					.AddCommand<ReadCommand>("read")
					.WithDescription("Extract an article from a URL to EPUB");

				config.AddBranch(
					"cloud",
					cloud =>
					{
						cloud.SetDescription("Cloud service management");
						cloud
							.AddCommand<CloudUsageCommand>("usage")
							.WithDescription(
								"Show Azure free tier usage for current billing period"
							);
					}
				);
			});

			return app.Run(args: args);
		}
		catch (OperationCanceledException)
		{
			Console.Error.WriteLine(value: "Fatal: Operation canceled.");
			return 130;
		}
		catch (FileNotFoundException ex)
		{
			Console.Error.WriteLine($"Fatal: {ex.Message}");
			return 1;
		}
		catch (UnauthorizedAccessException ex)
		{
			Console.Error.WriteLine($"Fatal: {ex.Message}");
			return 1;
		}
		catch (InvalidOperationException ex)
		{
			Console.Error.WriteLine($"Fatal: {ex.Message}");
			return 1;
		}
		catch (IOException ex)
		{
			Console.Error.WriteLine($"Fatal: {ex.Message}");
			return 1;
		}
		catch (HttpRequestException ex)
		{
			Console.Error.WriteLine($"Fatal: {ex.Message}");
			return 1;
		}
		finally
		{
			Serilog.Log.CloseAndFlush();
		}
	}
}
