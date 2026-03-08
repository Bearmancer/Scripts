namespace CSharpScripts.CLI.Sync;

internal sealed class SyncLastFmCommand : BaseAsyncCommand<SyncLastFmCommand.Settings>
{
	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		DateTime? sinceDate = null;
		if (!IsNullOrEmpty(settings.Since))
		{
			if (
				!DateTime.TryParseExact(
					settings.Since,
					"yyyy/MM/dd",
					null,
					DateTimeStyles.None,
					out DateTime parsed
				)
			)
			{
				UI.Error("Invalid date format. Use yyyy/MM/dd (e.g. 2024/01/01)");
				return 1;
			}

			sinceDate = parsed;
			UI.Warn(
				"Will delete existing data on/after {0} and re-sync",
				sinceDate.Value.ToDisplayDate()
			);
			Log.Information("SyncLastFm_SinceDate {SinceDate}", sinceDate.Value.ToDisplayDate());
		}

		return await ExecuteWithErrorHandlingAsync(
			ServiceType.LastFm,
			async () =>
			{
				ScrobbleSyncOrchestrator orchestrator = await ScrobbleSyncOrchestrator.CreateAsync(
					sinceDate,
					Program.Cts.Token
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

		[CommandOption("--since")]
		[Description("Sync from date (yyyy/MM/dd)")]
		public string? Since { get; init; }
	}
}
