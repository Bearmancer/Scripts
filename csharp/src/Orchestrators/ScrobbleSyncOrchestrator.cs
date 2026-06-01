using Scripts.Services.Sync.LastFm;

namespace Scripts.Orchestrators;

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

	public void Dispose() => GC.SuppressFinalize(this);

	public static async Task<ScrobbleSyncOrchestrator> CreateAsync(
		DateTime? forceFromDate,
		CancellationToken ct
	)
	{
		LastFmService lastFmService = new(apiKey: Secrets.LastFmApiKey, username: "kanishknishar");
		FetchState state = await StateManager.LoadStateAsync<FetchState>(
			fileName: StateManager.LastFmSyncFile,
			ct: ct
		);
		return new ScrobbleSyncOrchestrator(
			lastFmService: lastFmService,
			state: state,
			forceFromDate: forceFromDate,
			ct: ct
		);
	}

	internal async Task ExecuteAsync()
	{
		Ui.Info(message: "Starting Last.fm sync...");

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
				messageTemplate: "LastFmFetchInterrupted {Detail}",
				$"Fetched {State.TotalFetched} scrobbles across {State.LastPage} pages"
			);
			return;
		}

		State = State.MarkFetchComplete();
		await SaveStateAsync();

		List<Scrobble> scrobbles = await this.LastFmService.LoadScrobblesAsync();

		if (scrobbles.Count == 0)
		{
			Ui.Ok(message: "No new scrobbles to sync");
			Log.Information(messageTemplate: "SyncComplete {Detail}", "No changes detected");
			return;
		}

		Ui.Ok(message: "Fetched {0} scrobbles ready for DB.", scrobbles.Count);
	}

	private async Task<int> ExecuteForceResyncAsync()
	{
		Ui.Info(message: "Force resync from {0}", ForceFromDate!.Value.ToDisplayDate());
		var deleted = 0;
		State = StateTransitions.Reset(spreadsheetId: "");
		await SaveStateAsync();
		this.LastFmService.DeleteScrobblesCache();
		await FetchScrobblesAsync(ForceFromDate.Value.AddSeconds(value: -1));
		return deleted;
	}

	private async Task ExecuteResumeFetchAsync()
	{
		Ui.Warn(
			message: "Resuming full sync from page {0} ({1} scrobbles fetched)",
			State.LastPage + 1,
			State.TotalFetched
		);
		await FetchScrobblesAsync(fetchAfter: null);
	}

	private async Task ExecuteIncrementalSyncAsync()
	{
		List<Scrobble> cachedScrobbles = await this.LastFmService.LoadScrobblesAsync();

		if (cachedScrobbles.Count > 0)
		{
			DateTime? newestCached = cachedScrobbles[index: 0].PlayedAt;
			DateTime? oldestCached = cachedScrobbles[^1].PlayedAt;

			if (
				State.OldestScrobble.HasValue
				&& State.NewestScrobble.HasValue
				&& oldestCached.HasValue
				&& newestCached.HasValue
			)
				await FetchScrobblesAsync(fetchAfter: newestCached);
		}
		else
		{
			DateTime? latestInDb = null;

			if (latestInDb is { })
			{
				Ui.Info(message: "Latest in db: {0}", latestInDb.Value.ToDisplay());
				await FetchScrobblesAsync(fetchAfter: latestInDb);
			}
			else
			{
				Ui.Info(message: "No existing data. Full sync...");
				await FetchScrobblesAsync(fetchAfter: null);
			}
		}
	}

	private async Task FetchScrobblesAsync(DateTime? fetchAfter)
	{
		var saveStateCounter = 0;
		const int saveStateInterval = 10;

		try
		{
			await LastFmService.FetchScrobblesSinceAsync(
				fetchAfter: fetchAfter,
				state: State,
				(page, total, elapsed, oldest, newest) =>
				{
					State = State.WithUpdate(
						page: page,
						total: total,
						oldest: oldest,
						newest: newest
					);
					saveStateCounter++;

					if (saveStateCounter >= saveStateInterval)
					{
						SaveState();
						saveStateCounter = 0;
					}

					Ui.Progress(
						message: "Page: {0} | Tracks: {1} | Elapsed: {2}",
						page,
						total,
						elapsed.ToString(format: @"hh\:mm\:ss")
					);
				},
				ct: Ct
			);
		}
		finally
		{
			await SaveStateAsync();
		}

		if (Ct.IsCancellationRequested)
			Ui.Warn(
				message: "Stopped at page {0} ({1} scrobbles)",
				State.LastPage,
				State.TotalFetched
			);
	}

	private void SaveState() =>
		_ = StateManager.SaveStateAsync(
			fileName: StateManager.LastFmSyncFile,
			state: State,
			ct: Ct
		);

	internal Task SaveStateAsync() =>
		StateManager.SaveStateAsync(fileName: StateManager.LastFmSyncFile, state: State, ct: Ct);
}
