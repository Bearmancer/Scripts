using System.ComponentModel;
using CSharpScripts.Orchestrators;

namespace CSharpScripts.CLI.Sync;

internal sealed class SyncYouTubeCommand : BaseAsyncCommand<SyncYouTubeCommand.Settings>
{
	protected async override Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			service: ServiceType.YouTube,
			async () =>
			{
				YouTubePlaylistOrchestrator orchestrator =
					await YouTubePlaylistOrchestrator.CreateAsync(
						ct: Program.Cts.Token,
						previewMode: settings.PreviewTranslations
					);
				await orchestrator.ExecuteAsync();
			}
		);
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandOption(template: "-v|--verbose")]
		[Description(description: "Debug logging")]
		public bool Verbose { get; init; }

		[CommandOption(template: "-i|--session-id")]
		[Description(description: "Show session ID")]
		public bool ShowSessionId { get; init; }

		[CommandOption(template: "-p|--preview")]
		[Description(description: "Preview translations before applying")]
		public bool PreviewTranslations { get; init; }
	}
}
