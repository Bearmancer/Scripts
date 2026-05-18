namespace CSharpScripts.Orchestrators;

internal sealed class YouTubePlaylistOrchestrator : IDisposable
{
	private const int DescriptionColumnWidth = 40;

	private readonly CancellationToken Ct;
	private readonly bool PreviewMode;
	private readonly YouTubeService YoutubeService;

	private YouTubeFetchState State;

	private YouTubePlaylistOrchestrator(
		YouTubeService youtubeService,
		YouTubeFetchState state,
		bool previewMode,
		CancellationToken ct
	)
	{
		YoutubeService = youtubeService;
		State = state;
		Ct = ct;
		PreviewMode = previewMode;
	}

	public void Dispose()
	{
		YoutubeService?.Dispose();
		GC.SuppressFinalize(this);
	}

	public static async Task<YouTubePlaylistOrchestrator> CreateAsync(
		CancellationToken ct,
		bool previewMode = false
	)
	{
		YouTubeService youtubeService = await YouTubeService.CreateAsync(ct);
		YouTubeFetchState state = await StateManager.LoadStateAsync<YouTubeFetchState>(
			StateManager.YoutubeSyncFile,
			ct
		);
		return new YouTubePlaylistOrchestrator(
			youtubeService,
			state,
			previewMode,
			ct
		);
	}

	internal async Task ExecuteAsync()
	{
		UI.Info("Starting YouTube sync...");
		StateManager.MigratePlaylistFiles(State.PlaylistSnapshots);

		if (State.FetchComplete && State.PlaylistSnapshots.Count > 0)
		{
			await ExecuteOptimizedAsync();
			return;
		}

		await ExecuteFullSyncAsync();
	}

	internal async Task ExecuteForPlaylistsAsync(string[] playlistIdentifiers)
	{
		Log.Debug("Selective sync initiated for {0} playlist(s)", playlistIdentifiers.Length);

		List<YouTubePlaylist> resolvedPlaylists = await ResolvePlaylistIdentifiersAsync(
			playlistIdentifiers
		);

		if (resolvedPlaylists.Count == 0)
		{
			UI.Error("No matching playlists found for the provided identifiers.");
			return;
		}

		UI.Info("Syncing {0} playlist(s):", resolvedPlaylists.Count);
		foreach (YouTubePlaylist playlist in resolvedPlaylists)
			UI.Info("  • {0}", playlist.Title);

		var isFirstPlaylist = State.PlaylistSnapshots.Count == 0;
		var processedCount = await ProcessPlaylistsWithProgressAsync(
			resolvedPlaylists,
			isFirstPlaylist
		);

		if (!Ct.IsCancellationRequested)
			UI.Complete("Done! Synced {0} playlist(s).", processedCount);
		else
		{
			Log.Warning("SyncInterrupted {Reason}", "Interrupted during selective sync");
		}
	}

	private async Task<List<YouTubePlaylist>> ResolvePlaylistIdentifiersAsync(string[] identifiers)
	{
		List<YouTubePlaylist> resolved = [];

		var titleLookup = Enumerable.ToDictionary(
			State.PlaylistSnapshots.Values,
			s => s.Title,
			s => s,
			StringComparer.OrdinalIgnoreCase
		);

		foreach (var identifier in identifiers)
		{
			if (Ct.IsCancellationRequested)
				break;

			YouTubePlaylist? playlist = await ResolvePlaylistIdentifierAsync(
				identifier,
				titleLookup
			);
			if (playlist is { })
				resolved.Add(playlist);
			else
				UI.Warn("Could not resolve: {0}", identifier);
		}

		return resolved;
	}

	private async Task<YouTubePlaylist?> ResolvePlaylistIdentifierAsync(
		string identifier,
		Dictionary<string, PlaylistSnapshot>? titleLookup = null
	)
	{
		if (State.PlaylistSnapshots.TryGetValue(identifier, out PlaylistSnapshot? snapshot))
		{
			Log.Debug("Resolved '{0}' from cached snapshot", identifier);
			return new YouTubePlaylist(
				snapshot.PlaylistId,
				snapshot.Title,
				snapshot.ReportedVideoCount,
				await YoutubeService.GetPlaylistVideoIdsAsync(snapshot.PlaylistId, Ct),
				snapshot.ETag
			);
		}

		PlaylistSnapshot? titleMatch =
			titleLookup?.GetValueOrDefault(identifier)
			?? Enumerable.FirstOrDefault(
				State.PlaylistSnapshots.Values,
				s => s.Title.EqualsIgnoreCase(identifier)
			);

		if (titleMatch is { } match)
		{
			Log.Debug("Resolved '{0}' by title match", identifier);
			return new YouTubePlaylist(
				match.PlaylistId,
				match.Title,
				match.ReportedVideoCount,
				await YoutubeService.GetPlaylistVideoIdsAsync(match.PlaylistId, Ct),
				match.ETag
			);
		}

		if (
			identifier.StartsWith("PL")
			|| identifier.StartsWith("UU")
			|| identifier.StartsWith("FL")
		)
		{
			Log.Debug("Fetching playlist by ID: {0}", identifier);
			List<string> videoIds = await YoutubeService.GetPlaylistVideoIdsAsync(identifier, Ct);
			if (videoIds.Count > 0)
			{
				if (await YoutubeService.GetPlaylistSummaryAsync(identifier, Ct) is { } summary)
				{
					return new YouTubePlaylist(
						identifier,
						summary.Title,
						summary.VideoCount,
						videoIds,
						summary.ETag
					);
				}
			}
		}

		return null;
	}

	private async Task ExecuteOptimizedAsync()
	{
		Log.Debug("Last change: {0:yyyy/MM/dd HH:mm:ss}", State.LastUpdated);
		State = StateTransitions.RefreshTimestamps(State);
		await SaveStateAsync();

		List<PlaylistSummary> summaries = [];
		await StatusExtensions
			.Spinner(AnsiConsole.Status(), Spinner.Known.Dots)
			.StartAsync(
				"Fetching playlist metadata...",
				async _ =>
				{
					summaries = await YoutubeService.GetPlaylistSummariesAsync(Ct);
				}
			);

		if (Ct.IsCancellationRequested)
		{
			Log.Warning("SyncInterrupted {Reason}", "Interrupted while fetching playlist metadata");
			return;
		}

		OptimizedChanges changes = YouTubeChangeDetector.DetectOptimizedChanges(
			summaries,
			State.PlaylistSnapshots
		);

		if (!changes.HasAnyChanges)
		{
			UI.Complete("No changes detected.");
			return;
		}

		YouTubeChangeDetector.LogDetailedChanges(changes, summaries, State.PlaylistSnapshots);

		await ProcessDeletedPlaylistsAsync(changes.DeletedIds);
		await ProcessRenamedPlaylistsAsync(changes.Renamed);
		await ProcessModifiedPlaylistsAsync(
			[.. changes.NewIds, .. changes.ModifiedIds],
			summaries
		);

		if (Ct.IsCancellationRequested)
			Log.Warning("SyncInterrupted {Reason}", "Interrupted during sync");
	}

	private async Task ProcessDeletedPlaylistsAsync(List<string> deletedIds)
	{
		foreach (var deletedId in deletedIds)
		{
			if (Ct.IsCancellationRequested)
				break;

			if (
				CollectionExtensions.GetValueOrDefault(State.PlaylistSnapshots, deletedId) is
				{ } snapshot
			)
			{
				ArchiveDeletedPlaylist(snapshot);
				
				Log.Information(
					"PlaylistDeleted {Title} {VideoCount}",
					snapshot.Title,
					snapshot.VideoIds.Count
				);
				State.PlaylistSnapshots.Remove(deletedId);
			}
		}

		if (deletedIds.Count > 0)
			await SaveStateAsync();
	}

	private async Task ProcessRenamedPlaylistsAsync(
		List<PlaylistRename> renames
	)
	{
		foreach (PlaylistRename rename in renames)
		{
			if (Ct.IsCancellationRequested)
				break;

			UI.Info("Renaming: {0} → {1}", rename.OldTitle, rename.NewTitle);
			Log.Information(
				"PlaylistRenamed {OldTitle} {NewTitle}",
				rename.OldTitle,
				rename.NewTitle
			);

			StateManager.RenamePlaylistCache(rename.OldTitle, rename.NewTitle);

			if (
				State.PlaylistSnapshots.TryGetValue(
					rename.PlaylistId,
					out PlaylistSnapshot? snapshot
				)
			)
			{
				State.PlaylistSnapshots[rename.PlaylistId] = snapshot with
				{
					Title = rename.NewTitle,
				};
			}
		}

		if (renames.Count > 0)
			await SaveStateAsync();
	}

	private async Task ProcessModifiedPlaylistsAsync(
		List<string> playlistIds,
		List<PlaylistSummary> summaries
	)
	{
		if (playlistIds.Count == 0)
		{
			UI.Complete("Done! Only metadata changes applied.");
			return;
		}

		Log.Debug("Fetching details for {0} changed playlists...", playlistIds.Count);

		List<YouTubePlaylist> playlistsToProcess = await FetchPlaylistVideoIdsAsync(
			playlistIds,
			summaries
		);

		if (Ct.IsCancellationRequested || playlistsToProcess.Count == 0)
			return;

		var isFirstPlaylist = State.PlaylistSnapshots.Count == 0;
		var processedCount = await ProcessPlaylistsWithProgressAsync(
			playlistsToProcess,
			isFirstPlaylist
		);

		UpdateSnapshotsForProcessedPlaylists(playlistsToProcess, summaries);

		UI.Complete("Done! Updated {0} playlists.", processedCount);
	}

	private async Task<List<YouTubePlaylist>> FetchPlaylistVideoIdsAsync(
		List<string> playlistIds,
		List<PlaylistSummary> summaries
	)
	{
		List<YouTubePlaylist> result = [];
		var summaryLookup = Enumerable.ToDictionary(summaries, s => s.Id);

		UI.Suppress = true;

		await UI.CreateStandardProgress(DescriptionColumnWidth)
			.StartAsync(async ctx =>
			{
				ProgressTask task = ctx.AddTask(
					UI.TaskTitle($"Fetching video IDs (0/{playlistIds.Count})"),
					maxValue: playlistIds.Count
				);

				foreach (var playlistId in playlistIds)
				{
					if (Ct.IsCancellationRequested)
						break;

					PlaylistSummary summary = summaryLookup[playlistId];
					task.Description = UI.TaskTitle(summary.Title);

					List<string> videoIds = await YoutubeService.GetPlaylistVideoIdsAsync(
						playlistId,
						Ct
					);

					if (Ct.IsCancellationRequested)
						break;

					result.Add(
						new YouTubePlaylist(
							playlistId,
							summary.Title,
							summary.VideoCount,
							videoIds,
							summary.ETag
						)
					);

					task.Increment(1);
				}
			});

		UI.Suppress = false;
		return result;
	}

	private void UpdateSnapshotsForProcessedPlaylists(
		List<YouTubePlaylist> playlists,
		List<PlaylistSummary> summaries
	)
	{
		var summaryLookup = Enumerable.ToDictionary(summaries, s => s.Id);

		foreach (YouTubePlaylist playlist in playlists)
		{
			PlaylistSummary summary = summaryLookup[playlist.Id];
			State.PlaylistSnapshots[playlist.Id] = new PlaylistSnapshot(
				playlist.Id,
				playlist.Title,
				playlist.VideoIds,
				DateTime.UtcNow,
				playlist.VideoCount,
				summary.ETag
			);
		}
		_ = SaveStateAsync();
	}

	private async Task ExecuteFullSyncAsync()
	{
		List<YouTubePlaylist>? playlists = await GetOrFetchPlaylistMetadataAsync();
		if (playlists is null)
			return;

		if (playlists.Count == 0)
		{
			UI.Info("No playlists found.");
			return;
		}

		if (!await FetchAllVideoIdsAsync(playlists))
			return;

		UI.Info("[Phase 1/2] Video ID fetch complete - all {0} playlists ready", playlists.Count);
		UI.Info("[Phase 2/2] Starting DB write phase...");

		PlaylistChanges playlistChanges = YouTubeChangeDetector.DetectPlaylistChanges(
			playlists,
			State.PlaylistSnapshots
		);

		if (!playlistChanges.HasChanges && State.FetchComplete)
		{
			UI.Complete("No changes detected.");
			return;
		}

		YouTubeChangeDetector.LogDetectedChanges(playlistChanges);

		if (Ct.IsCancellationRequested)
		{
			Log.Warning(
				"SyncInterrupted {Reason}",
				"Interrupted before processing playlist changes"
			);
			return;
		}

		await ProcessDeletedPlaylistsAsync(playlistChanges.DeletedPlaylistIds);

		if (Ct.IsCancellationRequested)
		{
			Log.Warning("SyncInterrupted {Reason}", "Interrupted after processing deletions");
			return;
		}

		await WritePlaylistsToDbAsync(playlists, playlistChanges);
	}

	private async Task<List<YouTubePlaylist>?> GetOrFetchPlaylistMetadataAsync()
	{
		if (State.CachedPlaylists is { } && State.CachedPlaylists.Count > 0)
		{
			List<YouTubePlaylist> playlists = State.CachedPlaylists;
			var playlistCount = playlists.Count;
			var alreadyHaveVideoIds = State.VideoIdFetchIndex;
			var progressPercent = (int)(alreadyHaveVideoIds / (double)playlistCount * 100);
			var currentPlaylistTitle =
				alreadyHaveVideoIds < playlistCount
					? playlists[alreadyHaveVideoIds].Title
					: "(all playlists fetched)";
			UI.Info(
				"[Phase 1/2] Resuming video ID fetch: {0}/{1} ({2}%) - {3}",
				alreadyHaveVideoIds,
				playlistCount,
				progressPercent,
				currentPlaylistTitle
			);
			Log.Debug("Using cached playlist metadata ({0} playlists)", playlistCount);
			return playlists;
		}

		List<YouTubePlaylist> freshPlaylists = await YoutubeService.GetPlaylistMetadataAsync(Ct);

		if (Ct.IsCancellationRequested)
		{
			Log.Warning("SyncInterrupted {Reason}", "Interrupted while fetching playlist metadata");
			return null;
		}

		State = State with { CachedPlaylists = freshPlaylists, VideoIdFetchIndex = 0 };
		await SaveStateAsync();
		UI.Info("[Phase 1/2] Starting video ID fetch for {0} playlists", freshPlaylists.Count);
		Log.Debug("Cached playlist metadata for resume capability");
		return freshPlaylists;
	}

	private async Task<bool> FetchAllVideoIdsAsync(List<YouTubePlaylist> playlists)
	{
		var playlistCount = playlists.Count;
		var startIndex = State.VideoIdFetchIndex;

		if (startIndex >= playlistCount)
			return true;

		var interrupted = false;
		var interruptedAt = 0;
		var alreadyFetched = startIndex;

		UI.Suppress = true;

		await UI.CreateStandardProgress(DescriptionColumnWidth)
			.StartAsync(async ctx =>
			{
				ProgressTask task = ctx.AddTask(
					UI.TaskTitle($"Fetching video IDs ({alreadyFetched}/{playlistCount} done)"),
					maxValue: playlistCount
				);
				task.Value = alreadyFetched;

				for (var i = startIndex; i < playlistCount; i++)
				{
					if (Ct.IsCancellationRequested)
					{
						interrupted = true;
						interruptedAt = i;
						return;
					}

					YouTubePlaylist playlist = playlists[i];
					task.Description = UI.TaskTitle(playlist.Title);

					List<string> videoIds = await YoutubeService.GetPlaylistVideoIdsAsync(
						playlist.Id,
						Ct
					);

					if (Ct.IsCancellationRequested)
					{
						interrupted = true;
						interruptedAt = i;
						return;
					}

					playlists[i] = playlist with { VideoIds = videoIds };
					State = State with { CachedPlaylists = playlists, VideoIdFetchIndex = i + 1 };
					await SaveStateAsync();

					task.Increment(1);
				}

				task.Value = task.MaxValue;
			});

		UI.Suppress = false;

		if (interrupted)
		{
			Log.Warning(
				"YouTubeVideoIdFetchInterrupted {Detail}",
				$"Video ID fetch interrupted at {interruptedAt}/{playlists.Count} playlists"
			);
			return false;
		}

		return true;
	}

	private async Task WritePlaylistsToDbAsync(
		List<YouTubePlaylist> playlists,
		PlaylistChanges playlistChanges
	)
	{
		var isFirstPlaylist = State.PlaylistSnapshots.Count == 0;
		var newPlaylistIds = Enumerable.ToHashSet(playlistChanges.NewPlaylistIds);
		var modifiedPlaylistIds = Enumerable.ToHashSet(playlistChanges.ModifiedPlaylistIds);

		var playlistsToProcess = new List<YouTubePlaylist>(playlists.Count);
		foreach (YouTubePlaylist playlist in playlists)
		{
			if (
				newPlaylistIds.Contains(playlist.Id)
				|| modifiedPlaylistIds.Contains(playlist.Id)
				|| !State.PlaylistSnapshots.ContainsKey(playlist.Id)
			)
			{
				playlistsToProcess.Add(playlist);
			}
		}

		var skippedCount = playlists.Count - playlistsToProcess.Count;
		var processedCount = 0;

		if (skippedCount > 0)
			UI.Info("Skipping {0} unchanged playlists", skippedCount);

		if (playlistsToProcess.Count > 0)
		{
			processedCount = await ProcessPlaylistsWithProgressAsync(
				playlistsToProcess,
				isFirstPlaylist
			);
		}

		if (Ct.IsCancellationRequested)
		{
			Log.Warning(
				"YouTubeInterruptedBeforeWrite {Detail}",
				$"{State.PlaylistSnapshots.Count} playlists completed before interrupt"
			);
			return;
		}

		State = State with { FetchComplete = true, CachedPlaylists = null };
		await SaveStateAsync();

		UI.Complete(
			"Done! Synced {0} playlists ({1} processed, {2} unchanged).",
			playlists.Count,
			processedCount,
			playlists.Count - processedCount
		);
	}

	private async Task<int> ProcessPlaylistsWithProgressAsync(
		List<YouTubePlaylist> playlistsToProcess,
		bool isFirstPlaylist
	)
	{
		var processedCount = 0;
		var totalPlaylists = playlistsToProcess.Count;

		var prefixWidth = $"({totalPlaylists}/{totalPlaylists})".Length;
		var maxTitleLength = 0;
		var maxVideoCount = 0;

		foreach (YouTubePlaylist playlist in playlistsToProcess)
		{
			if (playlist.Title.Length > maxTitleLength)
				maxTitleLength = playlist.Title.Length;

			if (playlist.VideoIds.Count > maxVideoCount)
				maxVideoCount = playlist.VideoIds.Count;
		}

		var titleWidth = Math.Min(40, maxTitleLength);
		var suffixWidth = $"(0/{maxVideoCount} videos)".Length;
		var totalDescriptionWidth = prefixWidth + 1 + titleWidth + 1 + suffixWidth;

		UI.Suppress = true;

		await UI.CreateStandardProgress(totalDescriptionWidth)
			.StartAsync(async ctx =>
			{
				foreach (YouTubePlaylist playlist in playlistsToProcess)
				{
					if (Ct.IsCancellationRequested)
						break;

					var playlistCount = $"({processedCount + 1}/{totalPlaylists})";
					var playlistVideoCount = playlist.VideoIds.Count;

					ProgressTask task = ctx.AddTask(
						UI.TaskDescription(
							playlistCount,
							playlist.Title,
							$"(0/{playlistVideoCount} videos)",
							prefixWidth,
							titleWidth
						),
						maxValue: playlistVideoCount
					);

					await ProcessPlaylistWithContextAsync(
						playlist,
						isFirstPlaylist && processedCount == 0,
						count =>
						{
							task.Value = count;
							task.Description = UI.TaskDescription(
								playlistCount,
								playlist.Title,
								$"({count}/{playlistVideoCount} videos)",
								prefixWidth,
								titleWidth
							);
						}
					);

					task.Value = task.MaxValue;
					processedCount++;
				}
			});

		UI.Suppress = false;

		return processedCount;
	}

	private async Task ProcessPlaylistWithContextAsync(
		YouTubePlaylist playlist,
		bool isFirstPlaylist,
		Action<int> onVideoProgress
	)
	{
		var alreadyFetched = 0;
		var playlistVideoCount = playlist.VideoIds.Count;
		List<YouTubeVideo> videos = [];
		List<YouTubeVideo> existingCache = StateManager.LoadPlaylistCache(playlist.Title);
		List<YouTubeVideo> previousVideos = [.. existingCache];

		if (State.CurrentPlaylistId == playlist.Id && existingCache.Count > 0)
		{
			alreadyFetched = State.CurrentPlaylistVideosFetched;
			videos = existingCache;
			onVideoProgress(alreadyFetched);
			Log.Debug(
				"Resuming '{0}': {1}/{2} videos already fetched from cache",
				playlist.Title,
				alreadyFetched,
				playlistVideoCount
			);
		}

		List<string> remainingIds = playlist.VideoIds.GetRange(
			alreadyFetched,
			playlistVideoCount - alreadyFetched
		);
		var videosFetchedSoFar = alreadyFetched;

		List<YouTubeVideo> newVideos = await YoutubeService.GetVideoDetailsForIdsAsync(
			remainingIds,
			async batchVideos =>
			{
				videos.AddRange(batchVideos);
				videosFetchedSoFar += batchVideos.Count;

				StateManager.SavePlaylistCache(playlist.Title, videos);

				State = State with
				{
					CurrentPlaylistId = playlist.Id,
					CurrentPlaylistVideosFetched = videosFetchedSoFar,
					LastUpdated = DateTime.UtcNow,
				};
				await SaveStateAsync();
				Log.Debug(
					"Cached: {0}/{1} video details for '{2}' (batch resume)",
					videosFetchedSoFar,
					playlistVideoCount,
					playlist.Title
				);

				onVideoProgress(videosFetchedSoFar);
				await Task.CompletedTask;
			},
			Ct
		);

		if (Ct.IsCancellationRequested)
			return;

		if (PreviewMode)
		{
			UI.Suppress = false;
			UI.Info("Translating {0} videos...", videos.Count);
			videos = await YouTubeTranslationService.TranslateVideosAsync(videos, Ct);

			if (Ct.IsCancellationRequested)
				return;

			UI.NewLine();
			YouTubeTranslationService.ShowTranslationPreview(videos);
			UI.Suppress = true;
		}
		else
		{
			var needsTranslationCount = 0;
			foreach (YouTubeVideo video in videos)
			{
				if (video.NeedsTranslation)
					needsTranslationCount++;
			}

			if (needsTranslationCount > 0)
			{
				Log.Debug("Translating {0} non-English videos...", needsTranslationCount);
				videos = await YouTubeTranslationService.TranslateVideosAsync(videos, Ct);

				if (Ct.IsCancellationRequested)
					return;
			}
		}

        // TODO: Database logic will go here
		Log.Information("SyncComplete {Detail}", $"Fetched {videos.Count} videos for DB.");

		PlaylistSnapshot snapshot = new(
			playlist.Id,
			playlist.Title,
			playlist.VideoIds,
			DateTime.UtcNow,
			playlist.VideoCount,
			playlist.ETag
		);
		State.PlaylistSnapshots[playlist.Id] = snapshot;
		State = State with
		{
			CurrentPlaylistId = null,
			CurrentPlaylistVideosFetched = 0,
			LastUpdated = DateTime.UtcNow,
		};
		await SaveStateAsync();
	}

	internal async Task SaveStateAsync() =>
		await StateManager.SaveStateAsync(StateManager.YoutubeSyncFile, State, Ct);

	private static void ArchiveDeletedPlaylist(PlaylistSnapshot snapshot)
	{
		var archivedPath = StateManager.ArchivePlaylistCache(snapshot.Title);

		UI.Warn("Playlist deleted: {0}", snapshot.Title);
		UI.Info("Archived to: {0}", archivedPath);
	}

	public static Task ExportSheetsAsCSVsAsync(
		string outputDirectory = "YouTube Playlists",
		CancellationToken ct = default
	)
	{
        // TODO: Implement export from DB
        throw new NotImplementedException("Export to CSV requires DB implementation.");
	}

	public static async Task CountPlaylistsAsync(CancellationToken ct = default)
	{
		using YouTubeService youtubeService = await YouTubeService.CreateAsync(ct);

		List<PlaylistSummary> playlists = await youtubeService.GetPlaylistSummariesAsync(ct);
		UI.Info("Playlists: {0}", playlists.Count);
	}
}


