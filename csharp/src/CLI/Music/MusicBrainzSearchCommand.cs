namespace CSharpScripts.CLI.Music;

internal sealed class MusicBrainzSearchCommand : BaseAsyncCommand<MusicBrainzSearchCommand.Settings>
{
	internal sealed class Settings : CommandSettings
	{
		[CommandOption("-q|--query")]
		[Description("Free-text search query")]
		public string? Query { get; init; }

		[CommandOption("-n|--limit")]
		[Description("Max results (default 25)")]
		[DefaultValue(25)]
		public int Limit { get; init; } = 25;

		public override ValidationResult Validate()
		{
			if (IsNullOrEmpty(Query))
				return ValidationResult.Error("--query is required");
			return ValidationResult.Success();
		}
	}

	public override async Task<int> ExecuteAsync(
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
				UI.Info("Searching MusicBrainz for '{0}'...", settings.Query!);

				List<SearchResult> results = await mb.SearchAsync(
					settings.Query!,
					maxResults: settings.Limit,
					ct: cancellationToken
				);
				Log.Information(
					"MBSearchComplete {Query} {ResultCount}",
					settings.Query,
					results.Count
				);

				if (results.Count == 0)
				{
					UI.Warn("No results found.");
					return;
				}

				MusicOutputFormatter.DisplaySearchResults(results);
			}
		);
	}
}
