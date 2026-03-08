namespace CSharpScripts.CLI.Sync;

internal sealed class SyncYouTubeCommand : BaseAsyncCommand<SyncYouTubeCommand.Settings>
{
	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			ServiceType.YouTube,
			async () =>
			{
				YouTubePlaylistOrchestrator orchestrator =
					await YouTubePlaylistOrchestrator.CreateAsync(
						Program.Cts.Token,
						previewMode: settings.PreviewTranslations
					);
				await orchestrator.ExecuteAsync();
			}
		);
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandOption("-v|--verbose")]
		[Description("Debug logging")]
		public bool Verbose { get; init; }

		[CommandOption("-i|--session-id")]
		[Description("Show session ID")]
		public bool ShowSessionId { get; init; }

		[CommandOption("-p|--preview")]
		[Description("Preview translations before applying")]
		public bool PreviewTranslations { get; init; }
	}
}
