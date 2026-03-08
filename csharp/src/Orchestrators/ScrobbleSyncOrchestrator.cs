namespace CSharpScripts.Orchestrators;

internal sealed class ScrobbleSyncOrchestrator : IDisposable
{
	private readonly LastFmService LastFmService;
	private readonly GoogleSheetsService SheetsService;
	private readonly SpreadsheetBootstrapper Bootstrapper;
	private readonly CancellationToken Ct;
	private readonly DateTime? ForceFromDate;

	private FetchState State = StateManager.Load<FetchState>(StateManager.LastFmSyncFile);

	private ScrobbleSyncOrchestrator(
		LastFmService lastFmService,
		GoogleSheetsService sheetsService,
		SpreadsheetBootstrapper bootstrapper,
		DateTime? forceFromDate,
		CancellationToken ct
	)
	{
		LastFmService = lastFmService;
		SheetsService = sheetsService;
		Bootstrapper = bootstrapper;
		ForceFromDate = forceFromDate;
		Ct = ct;
	}

	public static async Task<ScrobbleSyncOrchestrator> CreateAsync(
		DateTime? forceFromDate,
		CancellationToken ct
	)
	{
		LastFmService lastFmService = new(Secrets.LastFmApiKey, "kanishknishar");
		GoogleSheetsService sheetsService = await GoogleSheetsService.CreateAsync(ct);
		SpreadsheetBootstrapper bootstrapper = new(sheetsService);
		return new ScrobbleSyncOrchestrator(
			lastFmService,
			sheetsService,
			bootstrapper,
			forceFromDate,
			ct
		);
	}

	public void Dispose()
	{
		SheetsService?.Dispose();
		GC.SuppressFinalize(this);
	}

	internal async Task ExecuteAsync()
	{
		UI.Info("Starting Last.fm sync...");
		var spreadsheetId = await GetOrCreateSpreadsheetAsync();

		if (ForceFromDate.HasValue)
			await ExecuteForceResyncAsync(spreadsheetId);
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

		State = State.MarkFetchComplete();
		await SaveStateAsync();

		List<Scrobble> scrobbles = LastFmService.LoadScrobbles();

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
	}

	private async Task ExecuteForceResyncAsync(string spreadsheetId)
	{
		UI.Info("Force resync from {0}", ForceFromDate!.Value.ToDisplayDate());
		await SheetsService.DeleteScrobblesOnOrAfterAsync(spreadsheetId, ForceFromDate.Value, Ct);
		State = StateTransitions.Reset(spreadsheetId);
		await SaveStateAsync();
		LastFmService.DeleteScrobblesCache();
		await FetchScrobblesAsync(ForceFromDate.Value.AddSeconds(-1));
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
		List<Scrobble> cachedScrobbles = LastFmService.LoadScrobbles();

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

			if (latestInSheet is not null)
			{
				UI.Info("Latest in sheet: {0}", latestInSheet.Value.ToDisplay());
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
			id =>
			{
				State = State.WithSpreadsheetId(id);
				SaveState();
			},
			Ct
		);

	internal void SaveState() => StateManager.Save(StateManager.LastFmSyncFile, State);

	private async Task SaveStateAsync() =>
		await StateManager.SaveStateAsync(StateManager.LastFmSyncFile, State, Ct);
}
