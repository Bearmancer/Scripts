namespace CSharpScripts.CLI.Sync;

internal sealed class HistoryCommand : AsyncCommand<HistoryCommand.Settings>
{
	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		var checkLastFm =
			IsNullOrEmpty(value: settings.Service) || settings.Service.Equals(value: "lastfm");
		var checkYouTube =
			IsNullOrEmpty(value: settings.Service)
			|| settings.Service.Equals(value: "yt")
			|| settings.Service.Equals(value: "youtube");

		if (checkLastFm)
			await ShowLastFmStatusAsync(cancellationToken);

		if (checkYouTube)
			ShowYouTubeStatus();

		return 0;
	}

	private static async Task ShowLastFmStatusAsync(CancellationToken ct)
	{
		UI.Info(message: "=== Last.fm ===");
		var stateFile = Path.Combine(
			path1: Paths.StateDirectory,
			path2: StateManager.LastFmSyncFile
		);
		var hasState = File.Exists(path: stateFile);
		var spreadsheetUrl =
			$"https://docs.google.com/spreadsheets/d/{Secrets.LastFmSpreadsheetId}";

		if (hasState)
		{
			var json = await File.ReadAllTextAsync(stateFile, ct);
			FetchState state =
				JsonSerializer.Deserialize<FetchState>(
					json: json,
					options: StateManager.JsonIndented
				) ?? new FetchState();
			UI.Info(message: "Scrobbles: {0}", state.TotalFetched);
			UI.Info(message: "Cached: Yes");
			UI.Info(message: "Last sync: {0}", DateTimeExtensions.ToDisplay(state.LastUpdated));
			UI.Link(url: spreadsheetUrl, text: "Spreadsheet");
		}
		else
		{
			using GoogleSheetsService sheets = await GoogleSheetsService.CreateAsync(ct);
			var scrobbleCount = await sheets.GetScrobbleCountAsync(
				spreadsheetId: Secrets.LastFmSpreadsheetId,
				ct
			);
			UI.Info(message: "Scrobbles: {0}", scrobbleCount);
			UI.Info(message: "Cached: No");
			UI.Link(url: spreadsheetUrl, text: "Spreadsheet");
		}

		UI.NewLine();
	}

	private static void ShowYouTubeStatus()
	{
		UI.Info(message: "=== YouTube ===");
		var stateFile = Path.Combine(
			path1: Paths.StateDirectory,
			path2: StateManager.YoutubeSyncFile
		);
		var cached = File.Exists(path: stateFile);

		if (cached)
		{
			var json = File.ReadAllText(path: stateFile);
			YouTubeFetchState state =
				JsonSerializer.Deserialize<YouTubeFetchState>(
					json: json,
					options: StateManager.JsonIndented
				) ?? new YouTubeFetchState();
			var totalVideos = Enumerable.Sum(state.PlaylistSnapshots.Values, s => s.VideoIds.Count);
			var spreadsheetUrl = $"https://docs.google.com/spreadsheets/d/{state.SpreadsheetId}";

			if (!state.FetchComplete)
				UI.Warn(message: "Fetch incomplete - run sync to resume");

			UI.Info(
				message: "Playlists: {0} | Videos: {1}",
				state.PlaylistSnapshots.Count,
				totalVideos
			);
			UI.Info(message: "Cached: Yes");
			UI.Info(message: "Last sync: {0}", DateTimeExtensions.ToDisplay(state.LastUpdated));
			UI.Link(url: spreadsheetUrl, text: "Spreadsheet");
		}
		else
			UI.Info(message: "Cached: No");

		UI.NewLine();
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(position: 0, template: "[service]")]
		[Description(description: "yt, lastfm (omit for all)")]
		[AllowedValues("yt", "youtube", "lastfm", "all")]
		public string Service { get; init; } = "all";
	}
}
