namespace CSharpScripts.CLI.Sync;

internal sealed class SyncAllCommand : BaseAsyncCommand<SyncAllCommand.Settings>
{
	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		UI.Rule("YouTube Sync");
		var ytResult = await RunYouTubeSyncAsync();

		UI.NewLine();
		UI.Rule("Last.fm Sync");
		var lfResult = await RunLastFmSyncAsync();

		UI.NewLine();
		if (ytResult == 0 && lfResult == 0)
		{
			UI.Ok("Sync complete! Everything's up to date.");
			Log.Information("SyncAll_Success");
		}
		else
		{
			UI.Warn("Completed with errors (YouTube: {0}, Last.fm: {1})", ytResult, lfResult);
			Log.Warning(
				"SyncAll_CompletedWithErrors {YouTubeResult} {LastFmResult}",
				ytResult,
				lfResult
			);
		}

		return ytResult != 0 ? ytResult : lfResult;
	}

	private static async Task<int> RunYouTubeSyncAsync() =>
		await ExecuteWithErrorHandlingAsync(
			ServiceType.YouTube,
			async () =>
			{
#pragma warning disable CA2000
				YouTubePlaylistOrchestrator orchestrator =
					await YouTubePlaylistOrchestrator.CreateAsync(
						Program.Cts.Token,
						previewMode: false
					);
#pragma warning restore CA2000
				await orchestrator.ExecuteAsync();
			}
		);

	private static async Task<int> RunLastFmSyncAsync() =>
		await ExecuteWithErrorHandlingAsync(
			ServiceType.LastFm,
			async () =>
			{
#pragma warning disable CA2000
				ScrobbleSyncOrchestrator orchestrator = await ScrobbleSyncOrchestrator.CreateAsync(
					null,
					Program.Cts.Token
				);
#pragma warning restore CA2000
				await orchestrator.ExecuteAsync();
			}
		);

	internal sealed class Settings : CommandSettings
	{
		[CommandOption("-v|--verbose")]
		[Description("Debug logging")]
		public bool Verbose { get; init; }
	}
}
