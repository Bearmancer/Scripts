using System.ComponentModel;
using CSharpScripts.Services.Music;

namespace CSharpScripts.CLI.Music;

internal sealed class MusicBrainzSearchCommand : BaseAsyncCommand<MusicBrainzSearchCommand.Settings>
{
	protected async override Task<int> ExecuteAsync(
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
				Ui.Info(message: "Searching MusicBrainz for '{0}'...", settings.Query!);

				List<SearchResult> results = await mb.SearchAsync(
					settings.Query!,
					maxResults: settings.Limit,
					ct: cancellationToken
				);
				Log.Information(
					messageTemplate: "MBSearchComplete {Query} {ResultCount}",
					settings.Query,
					results.Count
				);

				if (results.Count == 0)
				{
					Ui.Warn(message: "No results found.");
					return;
				}

				MusicOutputFormatter.DisplaySearchResults(results);
			}
		);
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandOption(template: "-q|--query")]
		[Description(description: "Free-text search query")]
		public string? Query { get; init; }

		[CommandOption(template: "-n|--limit")]
		[Description(description: "Max results (default 25)")]
		[DefaultValue(value: 25)]
		public int Limit { get; init; } = 25;

		public override ValidationResult Validate()
		{
			if (IsNullOrEmpty(value: Query))
				return ValidationResult.Error(message: "--query is required");

			return ValidationResult.Success();
		}
	}
}
