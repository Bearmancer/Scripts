using System.ComponentModel;
using CSharpScripts.Orchestrators;

namespace CSharpScripts.CLI.Sync;

internal sealed class SyncAllCommand : BaseAsyncCommand<SyncAllCommand.Settings>
{
	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		Ui.Rule(text: "YouTube Sync");
		var ytResult = await RunYouTubeSyncAsync();

		Ui.NewLine();
		Ui.Rule(text: "Last.fm Sync");
		var lfResult = await RunLastFmSyncAsync();

		Ui.NewLine();
		if (ytResult == 0 && lfResult == 0)
		{
			Ui.Ok(message: "Sync complete! Everything's up to date.");
			Log.Information(messageTemplate: "SyncAll_Success");
		}
		else
		{
			Ui.Warn(
				message: "Completed with errors (YouTube: {0}, Last.fm: {1})",
				ytResult,
				lfResult
			);
			Log.Warning(
				messageTemplate: "SyncAll_CompletedWithErrors {YouTubeResult} {LastFmResult}",
				ytResult,
				lfResult
			);
		}

		return ytResult != 0 ? ytResult : lfResult;
	}

	private static async Task<int> RunYouTubeSyncAsync() =>
		await ExecuteWithErrorHandlingAsync(
			service: ServiceType.YouTube,
			async () =>
			{
#pragma warning disable CA2000
				YouTubePlaylistOrchestrator orchestrator =
					await YouTubePlaylistOrchestrator.CreateAsync(
						ct: Program.Cts.Token,
						previewMode: false
					);
#pragma warning restore CA2000
				await orchestrator.ExecuteAsync();
			}
		);

	private static async Task<int> RunLastFmSyncAsync() =>
		await ExecuteWithErrorHandlingAsync(
			service: ServiceType.LastFm,
			async () =>
			{
#pragma warning disable CA2000
				ScrobbleSyncOrchestrator orchestrator = await ScrobbleSyncOrchestrator.CreateAsync(
					forceFromDate: null,
					ct: Program.Cts.Token
				);
#pragma warning restore CA2000
				await orchestrator.ExecuteAsync();
			}
		);

	internal sealed class Settings : CommandSettings
	{
		[CommandOption(template: "-v|--verbose")]
		[Description(description: "Debug logging")]
		public bool Verbose { get; init; }
	}
}
