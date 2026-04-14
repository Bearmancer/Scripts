namespace CSharpScripts.Orchestrators;

internal sealed class ScrobbleSyncOrchestrator : IDisposable
{
	private readonly SpreadsheetBootstrapper Bootstrapper;
	private readonly CancellationToken Ct;
	private readonly DateTime? ForceFromDate;
	private readonly LastFmService LastFmService;
	private readonly GoogleSheetsService SheetsService;

	private FetchState State;

	private ScrobbleSyncOrchestrator(
		LastFmService lastFmService,
		GoogleSheetsService sheetsService,
		SpreadsheetBootstrapper bootstrapper,
		FetchState state,
		DateTime? forceFromDate,
		CancellationToken ct
	)
	{
		LastFmService = lastFmService;
		SheetsService = sheetsService;
		Bootstrapper = bootstrapper;
		State = state;
		ForceFromDate = forceFromDate;
		Ct = ct;
	}

	public void Dispose()
	{
		SheetsService?.Dispose();
		GC.SuppressFinalize(this);
	}

	public static async Task<ScrobbleSyncOrchestrator> CreateAsync(
		DateTime? forceFromDate,
		CancellationToken ct
	)
	{
		LastFmService lastFmService = new(Secrets.LastFmApiKey, "kanishknishar");
		GoogleSheetsService sheetsService = await GoogleSheetsService.CreateAsync(ct);
		SpreadsheetBootstrapper bootstrapper = new(sheetsService);
		FetchState state = await StateManager.LoadStateAsync<FetchState>(
			StateManager.LastFmSyncFile,
			ct
		);
		return new ScrobbleSyncOrchestrator(
			lastFmService,
			sheetsService,
			bootstrapper,
			state,
			forceFromDate,
			ct
		);
	}

	internal async Task ExecuteAsync()
	{
		UI.Info("Starting Last.fm sync...");
		var spreadsheetId = await GetOrCreateSpreadsheetAsync();

		var deletedCount = 0;
		if (ForceFromDate.HasValue)
			deletedCount = await ExecuteForceResyncAsync(spreadsheetId);
		else if (!State.FetchComplete && State.LastPage > 0)
			await ExecuteResumeFetchAsync();
		else
			await ExecuteIncrementalSyncAsync(spreadsheetId);

		if (Ct.IsCancellationRequested)
		{
			Log.Warning(
				"LastFmFetchInterrupted {Detail}",
				$"Fetched {State.TotalFetched} scrobbles across {State.LastPage} pages"
			);
			return;
		}

		State = StateTransitions.MarkFetchComplete(State);
		await SaveStateAsync();

		List<Scrobble> scrobbles = await LastFmService.LoadScrobblesAsync();

		if (scrobbles.Count == 0)
		{
			UI.Ok("No new scrobbles to sync");
			Log.Information("SyncComplete {Detail}", "No changes detected");
			return;
		}

		List<Scrobble> newScrobbles = await SheetsService.GetNewScrobblesAsync(
			spreadsheetId,
			scrobbles,
			Ct
		);

		if (newScrobbles.Count == 0)
		{
			UI.Ok("Sheet is up to date");
			Log.Information("SyncComplete {Detail}", "No new scrobbles");
			return;
		}

		await WriteToSheetsAsync(newScrobbles, spreadsheetId);

		if (ForceFromDate.HasValue)
		{
			var added = newScrobbles.Count;
			var net = added - deletedCount;
			UI.Info("Deleted: {0} | Added: {1} | Net: {2}", deletedCount, added, net);
			Log.Information(
				"ForceResync_Summary {Deleted} {Added} {Net}",
				deletedCount,
				added,
				net
			);
		}
	}

	private async Task<int> ExecuteForceResyncAsync(string spreadsheetId)
	{
		UI.Info("Force resync from {0}", DateTimeExtensions.ToDisplayDate(ForceFromDate!.Value));
		var deleted = await SheetsService.DeleteScrobblesOnOrAfterAsync(
			spreadsheetId,
			ForceFromDate.Value,
			Ct
		);
		State = StateTransitions.Reset(spreadsheetId);
		await SaveStateAsync();
		LastFmService.DeleteScrobblesCache();
		await FetchScrobblesAsync(ForceFromDate.Value.AddSeconds(-1));
		return deleted;
	}

	private async Task ExecuteResumeFetchAsync()
	{
		UI.Warn(
			"Resuming full sync from page {0} ({1} scrobbles fetched)",
			State.LastPage + 1,
			State.TotalFetched
		);
		await FetchScrobblesAsync(null);
	}

	private async Task ExecuteIncrementalSyncAsync(string spreadsheetId)
	{
		List<Scrobble> cachedScrobbles = await LastFmService.LoadScrobblesAsync();

		if (cachedScrobbles.Count > 0)
		{
			DateTime? newestCached = cachedScrobbles[0].PlayedAt;
			DateTime? oldestCached = cachedScrobbles[^1].PlayedAt;

			if (
				State.OldestScrobble.HasValue
				&& State.NewestScrobble.HasValue
				&& oldestCached.HasValue
				&& newestCached.HasValue
			)
				await FetchScrobblesAsync(newestCached);
		}
		else
		{
			DateTime? latestInSheet = await SheetsService.GetLatestScrobbleTimeAsync(
				spreadsheetId,
				Ct
			);

			if (latestInSheet is { })
			{
				UI.Info("Latest in sheet: {0}", DateTimeExtensions.ToDisplay(latestInSheet.Value));
				await FetchScrobblesAsync(latestInSheet);
			}
			else
			{
				UI.Info("No existing data. Full sync...");
				await FetchScrobblesAsync(null);
			}
		}
	}

	private async Task FetchScrobblesAsync(DateTime? fetchAfter)
	{
		var saveStateCounter = 0;
		const int SaveStateInterval = 10;

		try
		{
			await LastFmService.FetchScrobblesSinceAsync(
				fetchAfter,
				State,
				(page, total, elapsed, oldest, newest) =>
				{
					State = State.WithUpdate(page, total, oldest, newest);
					saveStateCounter++;

					if (saveStateCounter >= SaveStateInterval)
					{
						SaveState();
						saveStateCounter = 0;
					}

					UI.Progress(
						"Page: {0} | Tracks: {1} | Elapsed: {2}",
						page,
						total,
						elapsed.ToString(@"hh\:mm\:ss")
					);
				},
				Ct
			);
		}
		finally
		{
			await SaveStateAsync();
		}

		if (Ct.IsCancellationRequested)
			UI.Warn("Stopped at page {0} ({1} scrobbles)", State.LastPage, State.TotalFetched);
	}

	private async Task WriteToSheetsAsync(List<Scrobble> scrobbles, string spreadsheetId)
	{
		if (Ct.IsCancellationRequested)
		{
			Log.Warning("WriteInterrupted {Detail}", "Interrupted before writing to sheets");
			return;
		}

		scrobbles.Sort(
			(a, b) => b.PlayedAt.GetValueOrDefault().CompareTo(a.PlayedAt.GetValueOrDefault())
		);

		await SheetsService.EnsureSheetExistsAsync(spreadsheetId, Ct);
		await SheetsService.WriteScrobblesAsync(spreadsheetId, scrobbles, Ct);

		UI.Ok("Wrote {0} scrobbles.", scrobbles.Count);
		Log.Information("SyncComplete {Detail}", $"Wrote {scrobbles.Count} scrobbles to sheet");
	}

	private async Task<string> GetOrCreateSpreadsheetAsync() =>
		await Bootstrapper.GetOrCreateAsync(
			State.SpreadsheetId,
			Secrets.LastFmSpreadsheetId,
			"Last.fm Scrobbles",
			async id =>
			{
				State = StateTransitions.WithSpreadsheetId(State, id);
				await SaveStateAsync();
			},
			Ct
		);

	private void SaveState() =>
		_ = StateManager.SaveStateAsync(StateManager.LastFmSyncFile, State, Ct);

	internal Task SaveStateAsync() =>
		StateManager.SaveStateAsync(StateManager.LastFmSyncFile, State, Ct);
}
