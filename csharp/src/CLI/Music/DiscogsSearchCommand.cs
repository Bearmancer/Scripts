namespace CSharpScripts.CLI.Music;

internal sealed class DiscogsSearchCommand : BaseAsyncCommand<DiscogsSearchCommand.Settings>
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
				UI.Info("Searching Discogs for '{0}'...", settings.Query!);

				List<SearchResult> results = await discogs.SearchAsync(
					settings.Query!,
					settings.Limit,
					cancellationToken
				);
				Log.Information(
					"DiscogsSearchComplete {Query} {ResultCount}",
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
}
