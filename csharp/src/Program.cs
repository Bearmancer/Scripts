using CSharpScripts.CLI.Clean;
using CSharpScripts.CLI.Cloud;
using CSharpScripts.CLI.Music;
using CSharpScripts.CLI.Read;
using CSharpScripts.CLI.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpScripts;

internal static class Program
{
	private static volatile bool Cancelled;
	private static ServiceProvider? ServiceProvider;
	public static CancellationTokenSource Cts { get; } = new();

	public static int Main(string[] args)
	{
		Serilog.Log.Logger = Log.BuildAppLogger(filename: "app.jsonl");

		try
		{
			Console.CancelKeyPress += (_, e) =>
			{
				e.Cancel = true;
				if (!Cancelled)
				{
					Cancelled = true;
					Cts.Cancel();
					Ui.Warn(message: "Cancellation requested, stopping gracefully...");
				}
			};

			ServiceCollection services = new();
			ServiceProvider = services.BuildServiceProvider();

			SpectreTypeRegistrar registrar = new(serviceProvider: ServiceProvider);
			CommandApp app = new(registrar: registrar);

			app.Configure(config =>
			{
				config.SetApplicationName(name: "tools");

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
						sync.AddCommand<HistoryCommand>(name: "history")
							.WithDescription(description: "Show sync history");
					}
				);

				config.AddBranch(
					name: "clean",
					clean =>
					{
						clean.SetDescription(description: "Clean local state");
						clean
							.AddCommand<CleanCacheCommand>(name: "cache")
							.WithDescription(description: "Clean local cache files");
						clean
							.AddCommand<CleanResetCommand>(name: "reset")
							.WithDescription(description: "Reset all state and spreadsheets");
					}
				);

				config.AddBranch(
					name: "music",
					music =>
					{
						music.SetDescription(description: "Music metadata commands");
						music
							.AddCommand<MusicSearchCommand>(name: "search")
							.WithDescription(description: "Search for a music release");
						music
							.AddCommand<MusicEnrichCommand>(name: "enrich")
							.WithDescription(
								description: "Enrich CSV with missing metadata from MusicBrainz and Discogs"
							);
						music
							.AddCommand<MusicNotesCommand>(name: "notes")
							.WithDescription(description: "Parse and display Discogs release notes by ID");
						music
							.AddCommand<MusicTranslateCommand>(name: "translate")
							.WithDescription(
								description: "Translate non-English titles in a CSV using Azure Translator"
							);
						music.AddBranch(
							name: "lookup",
							lookup =>
							{
								lookup.SetDescription(description: "Lookup a release by ID");
								lookup
									.AddCommand<DiscogsLookupCommand>(name: "discogs")
									.WithDescription(description: "Lookup a Discogs release by ID");
								lookup
									.AddCommand<MusicBrainzLookupCommand>(name: "mb")
									.WithDescription(description: "Lookup a MusicBrainz release by GUID");
							}
						);
					}
				);


				config
					.AddCommand<ReadCommand>(name: "read")
					.WithDescription(description: "Extract an article from a URL to EPUB");

				config.AddBranch(
					name: "cloud",
					cloud =>
					{
						cloud.SetDescription(description: "Cloud service management");
						cloud
							.AddCommand<CloudUsageCommand>(name: "usage")
							.WithDescription(
								description: "Show Azure free tier usage for current billing period"
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
