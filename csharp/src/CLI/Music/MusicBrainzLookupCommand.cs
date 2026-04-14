namespace CSharpScripts.CLI.Music;

internal sealed class MusicBrainzLookupCommand : BaseAsyncCommand<MusicBrainzLookupCommand.Settings>
{
	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			ServiceType.Music,
			async () =>
			{
				MusicBrainzService mb = new();
				UI.Info("Looking up MusicBrainz release {0}...", settings.Id!);

				ReleaseData release = await mb.GetReleaseAsync(settings.Id!, ct: cancellationToken);
				Log.Information(
					"MBLookupComplete {ReleaseId} {TrackCount}",
					settings.Id,
					release.Tracks.Count
				);

				if (release.Tracks.Count == 0)
				{
					UI.Warn("No release data found.");
					return;
				}

				MusicOutputFormatter.DisplayReleaseData(release);
			}
		);
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandOption("-i|--id")]
		[Description("MusicBrainz release GUID")]
		public string? Id { get; init; }

		[CommandOption("--fresh")]
		[Description("Clear cached state and force fresh API fetch")]
		public bool Fresh { get; init; }

		public override ValidationResult Validate()
		{
			if (IsNullOrEmpty(Id))
				return ValidationResult.Error("--id is required");
			if (!Guid.TryParse(Id, out _))
				return ValidationResult.Error("--id must be a valid GUID");

			return ValidationResult.Success();
		}
	}
}
