namespace CSharpScripts.CLI.Music;

internal sealed class DiscogsLookupCommand : BaseAsyncCommand<DiscogsLookupCommand.Settings>
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
				var discogsToken = Secrets.DiscogsToken;
				if (IsNullOrEmpty(discogsToken))
				{
					UI.Error("DISCOGS_USER_TOKEN not set");
					return;
				}

				using DiscogsService discogs = new(discogsToken);
				UI.Info("Looking up Discogs release {0}...", settings.Id!);

				ReleaseData release = await discogs.GetReleaseAsync(
					settings.Id!,
					ct: cancellationToken
				);
				Log.Information(
					"DiscogsLookupComplete {ReleaseId} {TrackCount}",
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
		[Description("Discogs release ID (numeric)")]
		public string? Id { get; init; }

		public override ValidationResult Validate()
		{
			if (IsNullOrEmpty(Id))
				return ValidationResult.Error("--id is required");
			if (!int.TryParse(Id, out _))
				return ValidationResult.Error("--id must be a numeric Discogs release ID");

			return ValidationResult.Success();
		}
	}
}


