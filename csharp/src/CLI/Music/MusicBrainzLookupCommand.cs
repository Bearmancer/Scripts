using System.ComponentModel;
using Scripts.Services.Music;

namespace Scripts.CLI.Music;

internal sealed class MusicBrainzLookupCommand : BaseAsyncCommand<MusicBrainzLookupCommand.Settings>
{
	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			service: ServiceType.Music,
			async () =>
			{
				MusicBrainzService mb = new();
				Ui.Info(message: "Looking up MusicBrainz release {0}...", settings.Id!);

				ReleaseData release = await mb.GetReleaseAsync(settings.Id!, ct: cancellationToken);
				Log.Information(
					messageTemplate: "MBLookupComplete {ReleaseId} {TrackCount}",
					settings.Id,
					release.Tracks.Count
				);

				if (release.Tracks.Count == 0)
				{
					Ui.Warn(message: "No release data found.");
					return;
				}

				MusicOutputFormatter.DisplayReleaseData(release);
			}
		);
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandOption(template: "-i|--id")]
		[Description(description: "MusicBrainz release GUID")]
		public string? Id { get; init; }

		[CommandOption(template: "--fresh")]
		[Description(description: "Clear cached state and force fresh API fetch")]
		public bool Fresh { get; init; }

		public override ValidationResult Validate()
		{
			if (IsNullOrEmpty(value: Id))
				return ValidationResult.Error(message: "--id is required");
			if (!Guid.TryParse(input: Id, result: out _))
				return ValidationResult.Error(message: "--id must be a valid GUID");

			return ValidationResult.Success();
		}
	}
}
