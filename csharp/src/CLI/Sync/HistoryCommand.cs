namespace CSharpScripts.CLI.Sync;

internal sealed class HistoryCommand : AsyncCommand<HistoryCommand.Settings>
{
	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		var checkLastFm = IsNullOrEmpty(settings.Service) || settings.Service.Equals("lastfm");
		var checkYouTube =
			IsNullOrEmpty(settings.Service)
			|| settings.Service.Equals("yt")
			|| settings.Service.Equals("youtube");

		if (checkLastFm)
			await ShowLastFmStatusAsync(cancellationToken);

		if (checkYouTube)
			ShowYouTubeStatus();

		return 0;
	}

	private static async Task ShowLastFmStatusAsync(CancellationToken ct)
	{
		UI.Info("=== Last.fm ===");
		var stateFile = Path.Combine(Paths.StateDirectory, StateManager.LastFmSyncFile);
		var hasState = File.Exists(stateFile);
		var spreadsheetUrl =
			$"https://docs.google.com/spreadsheets/d/{Secrets.LastFmSpreadsheetId}";

		if (hasState)
		{
			var json = await File.ReadAllTextAsync(stateFile, ct);
			FetchState state =
				JsonSerializer.Deserialize<FetchState>(json, StateManager.JsonIndented)
				?? new FetchState();
			UI.Info("Scrobbles: {0}", state.TotalFetched);
			UI.Info("Cached: Yes");
			UI.Info("Last sync: {0}", state.LastUpdated.ToDisplay());
			UI.Link(spreadsheetUrl, "Spreadsheet");
		}
		else
		{
			using GoogleSheetsService sheets = await GoogleSheetsService.CreateAsync(ct);
			var scrobbleCount = await sheets.GetScrobbleCountAsync(Secrets.LastFmSpreadsheetId, ct);
			UI.Info("Scrobbles: {0}", scrobbleCount);
			UI.Info("Cached: No");
			UI.Link(spreadsheetUrl, "Spreadsheet");
		}

		UI.NewLine();
	}

	private static void ShowYouTubeStatus()
	{
		UI.Info("=== YouTube ===");
		var stateFile = Path.Combine(Paths.StateDirectory, StateManager.YoutubeSyncFile);
		var cached = File.Exists(stateFile);

		if (cached)
		{
			var json = File.ReadAllText(stateFile);
			YouTubeFetchState state =
				JsonSerializer.Deserialize<YouTubeFetchState>(json, StateManager.JsonIndented)
				?? new YouTubeFetchState();
			var totalVideos = state.PlaylistSnapshots.Values.Sum(s => s.VideoIds.Count);
			var spreadsheetUrl = $"https://docs.google.com/spreadsheets/d/{state.SpreadsheetId}";

			if (!state.FetchComplete)
				UI.Warn("Fetch incomplete - run sync to resume");

			UI.Info("Playlists: {0} | Videos: {1}", state.PlaylistSnapshots.Count, totalVideos);
			UI.Info("Cached: Yes");
			UI.Info("Last sync: {0}", state.LastUpdated.ToDisplay());
			UI.Link(spreadsheetUrl, "Spreadsheet");
		}
		else
		{
			UI.Info("Cached: No");
		}

		UI.NewLine();
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(0, "[service]")]
		[Description("yt, lastfm (omit for all)")]
		[AllowedValues("yt", "youtube", "lastfm", "all")]
		public string Service { get; init; } = "all";
	}
}
