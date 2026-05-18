namespace CSharpScripts.Orchestrators;

internal sealed class ScrobbleSyncOrchestrator : IDisposable
{
	private readonly CancellationToken Ct;
	private readonly DateTime? ForceFromDate;
	private readonly LastFmService LastFmService;

	private FetchState State;

	private ScrobbleSyncOrchestrator(
		LastFmService lastFmService,
		FetchState state,
		DateTime? forceFromDate,
		CancellationToken ct
	)
	{
		LastFmService = lastFmService;
		State = state;
		ForceFromDate = forceFromDate;
		Ct = ct;
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	public static async Task<ScrobbleSyncOrchestrator> CreateAsync(
		DateTime? forceFromDate,
		CancellationToken ct
	)
	{
		LastFmService lastFmService = new(Secrets.LastFmApiKey, "kanishknishar");
		FetchState state = await StateManager.LoadStateAsync<FetchState>(
			StateManager.LastFmSyncFile,
			ct
		);
		return new ScrobbleSyncOrchestrator(
			lastFmService,
			state,
			forceFromDate,
			ct
		);
	}

	internal async Task ExecuteAsync()
	{
		UI.Info("Starting Last.fm sync...");

		var deletedCount = 0;
		if (ForceFromDate.HasValue)
			deletedCount = await ExecuteForceResyncAsync();
		else if (!State.FetchComplete && State.LastPage > 0)
			await ExecuteResumeFetchAsync();
		else
			await ExecuteIncrementalSyncAsync();

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
        
        // TODO: Database saving logic will go here
        UI.Ok("Fetched {0} scrobbles ready for DB.", scrobbles.Count);
	}

	private async Task<int> ExecuteForceResyncAsync()
	{
		UI.Info("Force resync from {0}", DateTimeExtensions.ToDisplayDate(ForceFromDate!.Value));
        // TODO: DB delete logic will go here
		var deleted = 0; 
		State = StateTransitions.Reset(""); // Empty spreadsheet ID for now
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

	private async Task ExecuteIncrementalSyncAsync()
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
            // TODO: Query DB for latest scrobble
			DateTime? latestInDb = null;

			if (latestInDb is { })
			{
				UI.Info("Latest in db: {0}", DateTimeExtensions.ToDisplay(latestInDb.Value));
				await FetchScrobblesAsync(latestInDb);
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

	private void SaveState() =>
		_ = StateManager.SaveStateAsync(StateManager.LastFmSyncFile, State, Ct);

	internal Task SaveStateAsync() =>
		StateManager.SaveStateAsync(StateManager.LastFmSyncFile, State, Ct);
}

