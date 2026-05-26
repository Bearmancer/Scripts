using System.ComponentModel;
using CSharpScripts.Orchestrators;

namespace CSharpScripts.CLI.Sync;

internal sealed class SyncLastFmCommand : BaseAsyncCommand<SyncLastFmCommand.Settings>
{
	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		DateTime? sinceDate = null;
		if (!IsNullOrEmpty(value: settings.Since))
		{
			if (
				!DateTime.TryParseExact(
					s: settings.Since,
					format: "yyyy/MM/dd",
					provider: null,
					style: DateTimeStyles.None,
					out DateTime parsed
				)
			)
			{
				Ui.Error(message: "Invalid date format. Use yyyy/MM/dd (e.g. 2024/01/01)");
				return 1;
			}

			sinceDate = parsed;
			Ui.Warn(
				message: "Will delete existing data on/after {0} and re-sync",
				sinceDate.Value.ToDisplayDate()
			);
			Log.Information(
				messageTemplate: "SyncLastFm_SinceDate {SinceDate}",
				sinceDate.Value.ToDisplayDate()
			);
		}

		return await ExecuteWithErrorHandlingAsync(
			service: ServiceType.LastFm,
			async () =>
			{
				ScrobbleSyncOrchestrator orchestrator = await ScrobbleSyncOrchestrator.CreateAsync(
					forceFromDate: sinceDate,
					ct: Program.Cts.Token
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

		[CommandOption(template: "--since")]
		[Description(description: "Sync from date (yyyy/MM/dd)")]
		public string? Since { get; init; }
	}
}
