namespace CSharpScripts.Orchestrators;

internal sealed class YouTubePlaylistOrchestrator : IDisposable
{
	private const int DescriptionColumnWidth = 40;

	private static readonly IReadOnlyList<object> VideoHeaders =
	[
		"Title",
		"Description",
		"Channel Name",
		"Duration",
	];
	private readonly GoogleSheetsService SheetsService;
	private readonly SpreadsheetBootstrapper Bootstrapper;
	private readonly YouTubeService YoutubeService;
	private readonly CancellationToken Ct;
	private readonly bool PreviewMode;

	private YouTubeFetchState State = StateManager.Load<YouTubeFetchState>(
		fileName: StateManager.YoutubeSyncFile
	);

	private YouTubePlaylistOrchestrator(
		GoogleSheetsService sheetsService,
		SpreadsheetBootstrapper bootstrapper,
		YouTubeService youtubeService,
		bool previewMode,
		CancellationToken ct
	)
	{
		SheetsService = sheetsService;
		Bootstrapper = bootstrapper;
		YoutubeService = youtubeService;
		Ct = ct;
		PreviewMode = previewMode;
	}

	public static async Task<YouTubePlaylistOrchestrator> CreateAsync(
		CancellationToken ct,
		bool previewMode = false
	)
	{
		GoogleSheetsService sheetsService = await GoogleSheetsService.CreateAsync(ct);
		SpreadsheetBootstrapper bootstrapper = new(sheetsService);
		YouTubeService youtubeService = await YouTubeService.CreateAsync(ct);
		return new YouTubePlaylistOrchestrator(
			sheetsService,
			bootstrapper,
			youtubeService,
			previewMode,
			ct
		);
	}

	public void Dispose()
	{
		YoutubeService?.Dispose();
		SheetsService?.Dispose();
		GC.SuppressFinalize(this);
	}

	internal async Task ExecuteAsync()
	{
		UI.Info("Starting YouTube sync...");
		StateManager.MigratePlaylistFiles(State.PlaylistSnapshots);

		var spreadsheetId = await GetOrCreateSpreadsheetAsync();

		if (State.FetchComplete && State.PlaylistSnapshots.Count > 0)
		{
			await ExecuteOptimizedAsync(spreadsheetId);
			return;
		}

		await ExecuteFullSyncAsync(spreadsheetId);
	}

	internal async Task ExecuteForPlaylistsAsync(string[] playlistIdentifiers)
	{
		Log.Debug("Selective sync initiated for {0} playlist(s)", playlistIdentifiers.Length);

		var spreadsheetId = await GetOrCreateSpreadsheetAsync();
		List<YouTubePlaylist> resolvedPlaylists = await ResolvePlaylistIdentifiersAsync(
			playlistIdentifiers
		);

		if (resolvedPlaylists.Count == 0)
		{
			UI.Error("No matching playlists found for the provided identifiers.");
			// session ends via IDisposable
			return;
		}

		UI.Info("Syncing {0} playlist(s):", resolvedPlaylists.Count);
		foreach (YouTubePlaylist playlist in resolvedPlaylists)
			UI.Info("  • {0}", playlist.Title);

		var isFirstPlaylist = State.PlaylistSnapshots.Count == 0;
		var processedCount = await ProcessPlaylistsWithProgressAsync(
			resolvedPlaylists,
			spreadsheetId,
			isFirstPlaylist
		);

		if (!Ct.IsCancellationRequested)
		{
			UI.Complete("Done! Synced {0} playlist(s).", processedCount);
		}
		else
		{
			Log.Warning("SyncInterrupted {Reason}", "Interrupted during selective sync");
		}
	}

	private async Task<List<YouTubePlaylist>> ResolvePlaylistIdentifiersAsync(string[] identifiers)
	{
		List<YouTubePlaylist> resolved = [];

		var titleLookup = State.PlaylistSnapshots.Values.ToDictionary(
			s => s.Title,
			s => s,
			comparer: StringComparer.OrdinalIgnoreCase
		);

		foreach (var identifier in identifiers)
		{
			if (Ct.IsCancellationRequested)
				break;

			YouTubePlaylist? playlist = await ResolvePlaylistIdentifierAsync(
				identifier,
				titleLookup
			);
			if (playlist is not null)
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
				Id: snapshot.PlaylistId,
				Title: snapshot.Title,
				VideoCount: snapshot.ReportedVideoCount,
				await YoutubeService.GetPlaylistVideoIdsAsync(snapshot.PlaylistId, Ct),
				ETag: snapshot.ETag
			);
		}

		PlaylistSnapshot? titleMatch =
			titleLookup?.GetValueOrDefault(identifier)
			?? State.PlaylistSnapshots.Values.FirstOrDefault(s =>
				s.Title.EqualsIgnoreCase(identifier)
			);

		if (titleMatch is { } match)
		{
			Log.Debug("Resolved '{0}' by title match", identifier);
			return new YouTubePlaylist(
				Id: match.PlaylistId,
				Title: match.Title,
				VideoCount: match.ReportedVideoCount,
				await YoutubeService.GetPlaylistVideoIdsAsync(match.PlaylistId, Ct),
				ETag: match.ETag
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
				if (await YoutubeService.GetPlaylistSummaryAsync(identifier, Ct) is { } summary)
					return new YouTubePlaylist(
						Id: identifier,
						Title: summary.Title,
						VideoCount: summary.VideoCount,
						VideoIds: videoIds,
						ETag: summary.ETag
					);
		}

		return null;
	}

	private async Task ExecuteOptimizedAsync(string spreadsheetId)
	{
		Log.Debug("Last change: {0:yyyy/MM/dd HH:mm:ss}", State.LastUpdated);
		State = State.RefreshTimestamps();
		await SaveStateAsync();

		List<PlaylistSummary> summaries = [];
		await AnsiConsole
			.Status()
			.Spinner(Spinner.Known.Dots)
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
			// session ends via IDisposable
			return;
		}

		YouTubeChangeDetector.LogDetailedChanges(changes, summaries, State.PlaylistSnapshots);

		await ProcessDeletedPlaylistsAsync(changes.DeletedIds, spreadsheetId);
		await ProcessRenamedPlaylistsAsync(changes.Renamed, spreadsheetId);
		await ProcessModifiedPlaylistsAsync(
			[.. changes.NewIds, .. changes.ModifiedIds],
			summaries,
			spreadsheetId
		);

		if (Ct.IsCancellationRequested)
			Log.Warning("SyncInterrupted {Reason}", "Interrupted during sync");
	}

	private async Task ProcessDeletedPlaylistsAsync(List<string> deletedIds, string spreadsheetId)
	{
		foreach (var deletedId in deletedIds)
		{
			if (Ct.IsCancellationRequested)
				break;

			if (State.PlaylistSnapshots.GetValueOrDefault(deletedId) is { } snapshot)
			{
				ArchiveDeletedPlaylist(snapshot);
				await SheetsService.DeleteSubsheetAsync(
					spreadsheetId,
					SheetNameHelper.Sanitize(snapshot.Title),
					Ct
				);
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
		List<PlaylistRename> renames,
		string spreadsheetId
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

			await SheetsService.RenameSubsheetAsync(
				spreadsheetId,
				SheetNameHelper.Sanitize(rename.OldTitle),
				SheetNameHelper.Sanitize(rename.NewTitle),
				Ct
			);

			StateManager.RenamePlaylistCache(rename.OldTitle, rename.NewTitle);

			if (
				State.PlaylistSnapshots.TryGetValue(
					rename.PlaylistId,
					out PlaylistSnapshot? snapshot
				)
			)
				State.PlaylistSnapshots[rename.PlaylistId] = snapshot with
				{
					Title = rename.NewTitle,
				};
		}

		if (renames.Count > 0)
			await SaveStateAsync();
	}

	private async Task ProcessModifiedPlaylistsAsync(
		List<string> playlistIds,
		List<PlaylistSummary> summaries,
		string spreadsheetId
	)
	{
		if (playlistIds.Count == 0)
		{
			UI.Complete("Done! Only metadata changes applied.");
			// session ends via IDisposable
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
			spreadsheetId,
			isFirstPlaylist
		);

		UpdateSnapshotsForProcessedPlaylists(playlistsToProcess, summaries);

		UI.Complete("Done! Updated {0} playlists.", processedCount);
		// session ends via IDisposable
	}

	private async Task<List<YouTubePlaylist>> FetchPlaylistVideoIdsAsync(
		List<string> playlistIds,
		List<PlaylistSummary> summaries
	)
	{
		List<YouTubePlaylist> result = [];
		var summaryLookup = summaries.ToDictionary(s => s.Id);

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
							Id: playlistId,
							Title: summary.Title,
							VideoCount: summary.VideoCount,
							VideoIds: videoIds,
							ETag: summary.ETag
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
		var summaryLookup = summaries.ToDictionary(s => s.Id);

		foreach (YouTubePlaylist playlist in playlists)
		{
			PlaylistSummary summary = summaryLookup[playlist.Id];
			State.PlaylistSnapshots[playlist.Id] = new PlaylistSnapshot(
				PlaylistId: playlist.Id,
				Title: playlist.Title,
				VideoIds: playlist.VideoIds,
				LastUpdated: DateTime.UtcNow,
				ReportedVideoCount: playlist.VideoCount,
				ETag: summary.ETag
			);
		}
		SaveState();
	}

	private async Task ExecuteFullSyncAsync(string spreadsheetId)
	{
		List<YouTubePlaylist>? playlists = await GetOrFetchPlaylistMetadataAsync();
		if (playlists is null)
			return;

		if (playlists.Count == 0)
		{
			UI.Info("No playlists found.");
			// session ends via IDisposable
			return;
		}

		if (!await FetchAllVideoIdsAsync(playlists))
			return;

		UI.Info("[Phase 1/2] Video ID fetch complete - all {0} playlists ready", playlists.Count);
		UI.Info("[Phase 2/2] Starting sheet write phase (processes alphabetically)...");

		PlaylistChanges playlistChanges = YouTubeChangeDetector.DetectPlaylistChanges(
			playlists,
			State.PlaylistSnapshots
		);

		if (!playlistChanges.HasChanges && State.FetchComplete)
		{
			UI.Complete("No changes detected.");
			// session ends via IDisposable
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

		await ProcessDeletedPlaylistsAsync(playlistChanges.DeletedPlaylistIds, spreadsheetId);

		if (Ct.IsCancellationRequested)
		{
			Log.Warning("SyncInterrupted {Reason}", "Interrupted after processing deletions");
			return;
		}

		await WritePlaylistsToSheetsAsync(playlists, playlistChanges, spreadsheetId);
	}

	private async Task<List<YouTubePlaylist>?> GetOrFetchPlaylistMetadataAsync()
	{
		if (State.CachedPlaylists is not null && State.CachedPlaylists.Count > 0)
		{
			List<YouTubePlaylist> playlists = State.CachedPlaylists;
			var alreadyHaveVideoIds = State.VideoIdFetchIndex;
			var progressPercent = (int)(alreadyHaveVideoIds / (double)playlists.Count * 100);
			var currentPlaylistTitle =
				alreadyHaveVideoIds < playlists.Count
					? playlists[alreadyHaveVideoIds].Title
					: "(all playlists fetched)";
			UI.Info(
				"[Phase 1/2] Resuming video ID fetch: {0}/{1} ({2}%) - {3}",
				alreadyHaveVideoIds,
				playlists.Count,
				progressPercent,
				currentPlaylistTitle
			);
			Log.Debug("Using cached playlist metadata ({0} playlists)", playlists.Count);
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
		if (State.VideoIdFetchIndex >= playlists.Count)
			return true;

		var interrupted = false;
		var interruptedAt = 0;
		var alreadyFetched = State.VideoIdFetchIndex;

		UI.Suppress = true;

		await UI.CreateStandardProgress(DescriptionColumnWidth)
			.StartAsync(async ctx =>
			{
				ProgressTask task = ctx.AddTask(
					UI.TaskTitle($"Fetching video IDs ({alreadyFetched}/{playlists.Count} done)"),
					maxValue: playlists.Count
				);
				task.Value = alreadyFetched;

				for (var i = State.VideoIdFetchIndex; i < playlists.Count; i++)
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

	private async Task WritePlaylistsToSheetsAsync(
		List<YouTubePlaylist> playlists,
		PlaylistChanges playlistChanges,
		string spreadsheetId
	)
	{
		var isFirstPlaylist = State.PlaylistSnapshots.Count == 0;
		var firstPlaylistTitle = playlists.FirstOrDefault()?.Title;

		var playlistsToProcess = playlists
			.Where(p =>
				playlistChanges.NewPlaylistIds.Contains(p.Id)
				|| playlistChanges.ModifiedPlaylistIds.Contains(p.Id)
				|| !State.PlaylistSnapshots.ContainsKey(p.Id)
			)
			.ToList();

		var skippedCount = playlists.Count - playlistsToProcess.Count;
		var processedCount = 0;

		if (skippedCount > 0)
			UI.Info("Skipping {0} unchanged playlists", skippedCount);

		if (playlistsToProcess.Count > 0)
			processedCount = await ProcessPlaylistsWithProgressAsync(
				playlistsToProcess,
				spreadsheetId,
				isFirstPlaylist
			);

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

		await FinalizeSpreadsheetAsync(spreadsheetId, firstPlaylistTitle);

		UI.Complete(
			"Done! Synced {0} playlists ({1} processed, {2} unchanged).",
			playlists.Count,
			processedCount,
			playlists.Count - processedCount
		);
	}

	private async Task FinalizeSpreadsheetAsync(string spreadsheetId, string? firstPlaylistTitle)
	{
		if (!IsNullOrEmpty(firstPlaylistTitle))
		{
			var sanitizedFirst = SheetNameHelper.Sanitize(firstPlaylistTitle);
			await SheetsService.RenameSubsheetAsync(spreadsheetId, "Sheet1", sanitizedFirst, Ct);
		}
		else
		{
			await SheetsService.CleanupDefaultSheetAsync(spreadsheetId, Ct);
		}

		await SheetsService.ReorderSheetsAlphabeticallyAsync(spreadsheetId, Ct);
	}

	private async Task<int> ProcessPlaylistsWithProgressAsync(
		List<YouTubePlaylist> playlistsToProcess,
		string spreadsheetId,
		bool isFirstPlaylist
	)
	{
		var processedCount = 0;
		var totalPlaylists = playlistsToProcess.Count;

		// Calculate widths for vertical alignment
		var prefixWidth = $"({totalPlaylists}/{totalPlaylists})".Length;
		var titleWidth = Math.Min(40, playlistsToProcess.Max(p => p.Title.Length));
		var suffixWidth = $"(0/{playlistsToProcess.Max(p => p.VideoIds.Count)} videos)".Length;
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
						spreadsheetId,
						isFirstPlaylist && processedCount == 0,
						count =>
						{
							task.Value = count;
							task.Description = UI.TaskDescription(
								playlistCount,
								title: playlist.Title,
								suffix: $"({count}/{playlistVideoCount} videos)",
								prefixWidth: prefixWidth,
								titleWidth: titleWidth
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
		string spreadsheetId,
		bool isFirstPlaylist,
		Action<int> onVideoProgress
	)
	{
		var alreadyFetched = 0;
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
				playlist.VideoIds.Count
			);
		}

		var remainingIds = playlist.VideoIds.Skip(alreadyFetched).ToList();
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
					playlist.VideoIds.Count,
					playlist.Title
				);

				onVideoProgress(videosFetchedSoFar);
				await Task.CompletedTask;
			},
			Ct
		);

		if (Ct.IsCancellationRequested)
			return;

		// Translate videos if needed
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
			// Auto-translate without preview
			var needsTranslation = videos.Where(v => v.NeedsTranslation).ToList();
			if (needsTranslation.Count > 0)
			{
				Log.Debug("Translating {0} non-English videos...", needsTranslation.Count);
				videos = await YouTubeTranslationService.TranslateVideosAsync(videos, Ct);

				if (Ct.IsCancellationRequested)
					return;
			}
		}

		await WritePlaylistAsync(playlist, videos, previousVideos, spreadsheetId, isFirstPlaylist);

		PlaylistSnapshot snapshot = new(
			PlaylistId: playlist.Id,
			Title: playlist.Title,
			VideoIds: playlist.VideoIds,
			LastUpdated: DateTime.UtcNow,
			ReportedVideoCount: playlist.VideoCount,
			ETag: playlist.ETag
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

	private async Task<string> GetOrCreateSpreadsheetAsync() =>
		await Bootstrapper.GetOrCreateAsync(
			State.SpreadsheetId,
			Secrets.YouTubeSpreadsheetId,
			"YouTube Playlists",
			id =>
			{
				State = State with { SpreadsheetId = id };
				SaveState();
			},
			Ct
		);

	internal async Task SaveStateAsync() =>
		await StateManager.SaveStateAsync(StateManager.YoutubeSyncFile, State, Ct);

	internal void SaveState() => StateManager.Save(StateManager.YoutubeSyncFile, State);

	private static void ArchiveDeletedPlaylist(PlaylistSnapshot snapshot)
	{
		var archivedPath = StateManager.ArchivePlaylistCache(snapshot.Title);

		UI.Warn("Playlist deleted: {0}", snapshot.Title);
		UI.Info("Archived to: {0}", archivedPath);
	}

	private async Task WritePlaylistAsync(
		YouTubePlaylist playlist,
		List<YouTubeVideo> videos,
		List<YouTubeVideo> previousVideos,
		string spreadsheetId,
		bool isFirstPlaylist
	)
	{
		var sheetName = SheetNameHelper.Sanitize(playlist.Title);
		PlaylistSnapshot? existingSnapshot = State.PlaylistSnapshots.GetValueOrDefault(playlist.Id);

		if (isFirstPlaylist)
		{
			await SheetsService.RenameSubsheetAsync(spreadsheetId, "Sheet1", sheetName, Ct);
			await WriteFullPlaylistAsync(sheetName, videos, spreadsheetId);
			return;
		}

		if (existingSnapshot is null)
		{
			await SheetsService.EnsureSubsheetExistsAsync(
				spreadsheetId,
				sheetName,
				VideoHeaders,
				Ct
			);
			await WriteFullPlaylistAsync(sheetName, videos, spreadsheetId);
			return;
		}

		VideoChanges videoChanges = YouTubeChangeDetector.DetectVideoChanges(
			playlist.VideoIds,
			existingSnapshot.VideoIds
		);

		var removedTitles = videoChanges
			.RemovedVideoIds.Select(id =>
				previousVideos.FirstOrDefault(v => v.VideoId == id)?.Title
			)
			.Where(t => t is not null)
			.Cast<string>()
			.ToList();

		var addedVideos = videos
			.Where(v => videoChanges.AddedVideoIds.Contains(v.VideoId))
			.ToList();
		var addedTitles = addedVideos.Select(v => v.Title).ToList();

		if (videoChanges.RequiresFullRewrite)
		{
			Log.Debug("Order changed in '{0}', full rewrite required", playlist.Title);
			await WriteFullPlaylistAsync(sheetName, videos, spreadsheetId);
			Log.PlaylistUpdated(
				playlist.Title,
				addedTitles.Count,
				removedTitles.Count,
				addedTitles
			);
			return;
		}

		if (!videoChanges.HasChanges)
		{
			Log.Debug("No video changes in '{0}'", playlist.Title);
			Log.Debug("Skipped playlist: " + playlist.Title);
			return;
		}

		var removedSet = videoChanges.RemovedVideoIds.ToHashSet();

		Log.PlaylistUpdated(playlist.Title, addedTitles.Count, removedTitles.Count, addedTitles);

		if (videoChanges.RemovedRowIndices.Count > 0)
			await SheetsService.DeleteRowsFromSubsheetAsync(
				spreadsheetId,
				sheetName,
				videoChanges.RemovedRowIndices,
				Ct
			);

		if (addedVideos.Count > 0)
			await SheetsService.AppendRecordsAsync(
				spreadsheetId,
				sheetName,
				addedVideos,
				MapVideoToRow,
				Ct
			);

		var updatedVideos = previousVideos
			.Where(v => !removedSet.Contains(v.VideoId))
			.Concat(videos.Where(v => videoChanges.AddedVideoIds.Contains(v.VideoId)))
			.ToList();
		StateManager.SavePlaylistCache(playlist.Title, updatedVideos);
	}

	private async Task WriteFullPlaylistAsync(
		string sheetName,
		List<YouTubeVideo> videos,
		string spreadsheetId
	)
	{
		Log.Debug("Full write: {0} videos to '{1}'", videos.Count, sheetName);

		await SheetsService.WriteRecordsAsync(
			spreadsheetId,
			sheetName,
			VideoHeaders,
			videos,
			MapVideoToRow,
			Ct
		);
	}

	private static IList<object> MapVideoToRow(YouTubeVideo v) =>
		[
			$"=HYPERLINK(\"{v.VideoUrl}\", \"{EscapeFormulaString(v.Title)}\")",
			v.Description,
			$"=HYPERLINK(\"{v.ChannelUrl}\", \"{EscapeFormulaString(v.ChannelName)}\")",
			v.FormattedDuration,
		];

	private static string EscapeFormulaString(string value) => value.Replace("\"", "\"\"");

	public static async Task ExportSheetsAsCSVsAsync(
		string outputDirectory = "YouTube Playlists",
		CancellationToken ct = default
	)
	{
		YouTubeFetchState state = StateManager.Load<YouTubeFetchState>(
			StateManager.YoutubeSyncFile
		);

		if (IsNullOrEmpty(state.SpreadsheetId))
			throw new InvalidOperationException(
				"No YouTube spreadsheet found. Run sync first to create it."
			);

		var desktopPath = GetFolderPath(SpecialFolder.Desktop);
		var fullOutputPath = Path.Combine(desktopPath, outputDirectory);

		using GoogleSheetsService sheetsService = await GoogleSheetsService.CreateAsync(ct);

		var exported = await sheetsService.ExportEachSheetAsCSVAsync(
			state.SpreadsheetId,
			fullOutputPath,
			ct
		);

		if (exported > 0)
			UI.Complete(
				"Exported {0} playlists to: {1}",
				exported,
				Path.GetFullPath(fullOutputPath)
			);
		else
			UI.Info("All playlists already exported to: {0}", Path.GetFullPath(fullOutputPath));
	}

	public static async Task CountPlaylistsAsync(CancellationToken ct = default)
	{
		using YouTubeService youtubeService = await YouTubeService.CreateAsync(ct);

		List<PlaylistSummary> playlists = await youtubeService.GetPlaylistSummariesAsync(ct);
		UI.Info("Playlists: {0}", playlists.Count);
	}
}
