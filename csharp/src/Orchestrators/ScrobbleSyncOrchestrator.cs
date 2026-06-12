using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Repositories;
using Scripts.Services.Sync.LastFm;

namespace Scripts.Orchestrators;

internal sealed class ScrobbleSyncOrchestrator : IDisposable
{
	private readonly CancellationToken Ct;
	private readonly DateTime? ForceFromDate;
	private readonly LastFmService LastFmService;
	private readonly TrackRepository TrackRepository;
	private readonly ArtistRepository ArtistRepository;
	private readonly AlbumRepository AlbumRepository;
	private readonly ScrobbleRepository ScrobbleRepository;
	private readonly PurgeService PurgeService;

	private FetchState State;

	private ScrobbleSyncOrchestrator(
		LastFmService lastFmService,
		TrackRepository trackRepository,
		ArtistRepository artistRepository,
		AlbumRepository albumRepository,
		ScrobbleRepository scrobbleRepository,
		PurgeService purgeService,
		FetchState state,
		DateTime? forceFromDate,
		CancellationToken ct
	)
	{
		LastFmService = lastFmService;
		TrackRepository = trackRepository;
		ArtistRepository = artistRepository;
		AlbumRepository = albumRepository;
		ScrobbleRepository = scrobbleRepository;
		PurgeService = purgeService;
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
		IDbContextFactory<ScriptsDbContext> contextFactory =
			DbContextRegistration.CreateContextFactory();
		ResiliencePipeline pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();

		ArtistRepository artistRepository = new(contextFactory, pipeline);
		AlbumRepository albumRepository = new(contextFactory, pipeline);
		TrackRepository trackRepository = new(contextFactory, pipeline);
		ScrobbleRepository scrobbleRepository = new(contextFactory, pipeline);
		PurgeService purgeService = new(contextFactory);

		LastFmService lastFmService = new(
			apiKey: Secrets.LastFmApiKey,
			username: "kanishknishar",
			artistRepository: artistRepository
		);
		FetchState state = await StateManager.LoadStateAsync<FetchState>(
			fileName: StateManager.LastFmSyncFile,
			ct: ct
		);
		return new ScrobbleSyncOrchestrator(
			lastFmService: lastFmService,
			trackRepository: trackRepository,
			artistRepository: artistRepository,
			albumRepository: albumRepository,
			scrobbleRepository: scrobbleRepository,
			purgeService: purgeService,
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

		List<LastFmScrobble> scrobbles = await this.LastFmService.LoadScrobblesAsync();

		if (scrobbles.Count == 0)
		{
			Ui.Ok(message: "No new scrobbles to sync");
			Log.Information(messageTemplate: "SyncComplete {Detail}", "No changes detected");
			return;
		}

		Ui.Ok(message: "Fetched {0} scrobbles ready for DB.", scrobbles.Count);

		int ingestedCount = await IngestScrobblesAsync(scrobbles);
		Ui.Complete(message: "Ingested {0} scrobbles into DB.", ingestedCount);
	}

	private async Task<int> ExecuteForceResyncAsync()
	{
		Ui.Info(message: "Force resync from {0}", ForceFromDate!.Value.ToDisplayDate());
		var deleted = 0;
		State = StateTransitions.Reset(spreadsheetId: "");
		await SaveStateAsync();
		this.LastFmService.DeleteScrobblesCache();
		await FetchScrobblesAsync(ForceFromDate.Value.AddSeconds(value: -1));

		Ui.Info(message: "Purging orphans...");
		var purgeResult = await PurgeService.PurgeOrphansAsync(Ct);
		Ui.Ok(
			message: "Purged {0} tracks, {1} albums, {2} artists.",
			purgeResult.TracksPurged,
			purgeResult.AlbumsPurged,
			purgeResult.ArtistsPurged
		);

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
		List<LastFmScrobble> cachedScrobbles = await this.LastFmService.LoadScrobblesAsync();

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

	private async Task<int> IngestScrobblesAsync(List<LastFmScrobble> scrobbles)
	{
		var ingestedCount = 0;
		List<Entities.Scrobble> scrobbleEntities = [];

		foreach (var scrobble in scrobbles)
		{
			if (Ct.IsCancellationRequested)
				break;

			var artist = await ArtistRepository.GetByNameAsync(scrobble.ArtistName, Ct);
			if (artist == null)
			{
				artist = await ArtistRepository.AddAsync(
					new Artist { Name = scrobble.ArtistName },
					Ct
				);
			}

			var album = await AlbumRepository.GetByArtistAndTitleAsync(
				artist.Id,
				scrobble.AlbumName,
				Ct
			);
			if (album == null)
			{
				album = await AlbumRepository.AddAsync(
					new Album { ArtistId = artist.Id, Title = scrobble.AlbumName },
					Ct
				);
			}

			var track = await TrackRepository.GetByArtistAndTitleAsync(
				artist.Id,
				scrobble.TrackName,
				Ct
			);
			int trackId;
			if (track == null)
			{
				var newTrack = new Track
				{
					ArtistId = artist.Id,
					AlbumId = album.Id,
					Title = scrobble.TrackName,
				};
				await TrackRepository.BulkInsertAsync([newTrack], Ct);
				track = await TrackRepository.GetByArtistAndTitleAsync(
					artist.Id,
					scrobble.TrackName,
					Ct
				);
				trackId = track!.Id;
			}
			else
			{
				trackId = track.Id;
			}

			scrobbleEntities.Add(
				new Entities.Scrobble
				{
					TrackId = trackId,
					ScrobbledAt = scrobble.PlayedAt ?? DateTimeOffset.UtcNow,
					Platform = "Last.fm",
				}
			);

			ingestedCount++;
		}

		if (scrobbleEntities.Count > 0)
		{
			await ScrobbleRepository.UpsertAsync(scrobbleEntities, Ct);
		}

		return ingestedCount;
	}
}
