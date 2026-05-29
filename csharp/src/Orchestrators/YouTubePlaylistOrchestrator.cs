using CSharpScripts.Services.Sync.YouTube;
using CollectionExtensions = AngleSharp.Dom.CollectionExtensions;

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
		YouTubeService youtubeService = await YouTubeService.CreateAsync(ct: ct);
		YouTubeFetchState state = await StateManager.LoadStateAsync<YouTubeFetchState>(
			fileName: StateManager.YoutubeSyncFile,
			ct: ct
		);
		return new YouTubePlaylistOrchestrator(
			youtubeService: youtubeService,
			state: state,
			previewMode: previewMode,
			ct: ct
		);
	}

	internal async Task ExecuteAsync()
	{
		Ui.Info(message: "Starting YouTube sync...");
		StateManager.MigratePlaylistFiles(snapshots: State.PlaylistSnapshots);

		if (State.FetchComplete && State.PlaylistSnapshots.Count > 0)
		{
			await ExecuteOptimizedAsync();
			return;
		}

		await ExecuteFullSyncAsync();
	}

	internal async Task ExecuteForPlaylistsAsync(string[] playlistIdentifiers)
	{
		Log.Debug(
			messageTemplate: "Selective sync initiated for {0} playlist(s)",
			playlistIdentifiers.Length
		);

		List<YouTubePlaylist> resolvedPlaylists = await ResolvePlaylistIdentifiersAsync(
			identifiers: playlistIdentifiers
		);

		if (resolvedPlaylists.Count == 0)
		{
			Ui.Error(message: "No matching playlists found for the provided identifiers.");
			return;
		}

		Ui.Info(message: "Syncing {0} playlist(s):", resolvedPlaylists.Count);
		foreach (YouTubePlaylist playlist in resolvedPlaylists)
			Ui.Info(message: "  • {0}", playlist.Title);

		var isFirstPlaylist = State.PlaylistSnapshots.Count == 0;
		var processedCount = await ProcessPlaylistsWithProgressAsync(
			playlistsToProcess: resolvedPlaylists,
			isFirstPlaylist: isFirstPlaylist
		);

		if (!Ct.IsCancellationRequested)
			Ui.Complete(message: "Done! Synced {0} playlist(s).", processedCount);
		else
			Log.Warning(
				messageTemplate: "SyncInterrupted {Reason}",
				"Interrupted during selective sync"
			);
	}

	private async Task<List<YouTubePlaylist>> ResolvePlaylistIdentifiersAsync(string[] identifiers)
	{
		List<YouTubePlaylist> resolved = [];

		Dictionary<string, PlaylistSnapshot> titleLookup =
			State.PlaylistSnapshots.Values.ToDictionary(
				s => s.Title,
				s => s,
				comparer: StringComparer.OrdinalIgnoreCase
			);

		foreach (var identifier in identifiers)
		{
			if (Ct.IsCancellationRequested)
				break;

			YouTubePlaylist? playlist = await ResolvePlaylistIdentifierAsync(
				identifier: identifier,
				titleLookup: titleLookup
			);
			if (playlist is { })
				resolved.Add(item: playlist);
			else
				Ui.Warn(message: "Could not resolve: {0}", identifier);
		}

		return resolved;
	}

	private async Task<YouTubePlaylist?> ResolvePlaylistIdentifierAsync(
		string identifier,
		Dictionary<string, PlaylistSnapshot>? titleLookup = null
	)
	{
		if (State.PlaylistSnapshots.TryGetValue(key: identifier, out PlaylistSnapshot? snapshot))
		{
			Log.Debug(messageTemplate: "Resolved '{0}' from cached snapshot", identifier);
			return new YouTubePlaylist(
				Id: snapshot.PlaylistId,
				Title: snapshot.Title,
				VideoCount: snapshot.ReportedVideoCount,
				await YoutubeService.GetPlaylistVideoIdsAsync(snapshot.PlaylistId, Ct),
				ETag: snapshot.ETag
			);
		}

		PlaylistSnapshot? titleMatch =
			titleLookup?.GetValueOrDefault(key: identifier)
			?? State.PlaylistSnapshots.Values.FirstOrDefault(s =>
				s.Title.EqualsIgnoreCase(other: identifier)
			);

		if (titleMatch is { } match)
		{
			Log.Debug(messageTemplate: "Resolved '{0}' by title match", identifier);
			return new YouTubePlaylist(
				Id: match.PlaylistId,
				Title: match.Title,
				VideoCount: match.ReportedVideoCount,
				await YoutubeService.GetPlaylistVideoIdsAsync(match.PlaylistId, Ct),
				ETag: match.ETag
			);
		}

		if (
			identifier.StartsWith(value: "PL")
			|| identifier.StartsWith(value: "UU")
			|| identifier.StartsWith(value: "FL")
		)
		{
			Log.Debug(messageTemplate: "Fetching playlist by ID: {0}", identifier);
			List<string> videoIds = await YoutubeService.GetPlaylistVideoIdsAsync(identifier, Ct);
			if (videoIds.Count > 0)
			{
				if (await YoutubeService.GetPlaylistSummaryAsync(identifier, Ct) is { } summary)
				{
					return new YouTubePlaylist(
						Id: identifier,
						Title: summary.Title,
						VideoCount: summary.VideoCount,
						VideoIds: videoIds,
						ETag: summary.ETag
					);
				}
			}
		}

		return null;
	}

	private async Task ExecuteOptimizedAsync()
	{
		Log.Debug(messageTemplate: "Last change: {0:yyyy/MM/dd HH:mm:ss}", State.LastUpdated);
		State = State.RefreshTimestamps();
		await SaveStateAsync();

		List<PlaylistSummary> summaries = [];
		await AnsiConsole
			.Status()
			.Spinner(spinner: Spinner.Known.Dots)
			.StartAsync(
				status: "Fetching playlist metadata...",
				async _ =>
				{
					summaries = await YoutubeService.GetPlaylistSummariesAsync(ct: Ct);
				}
			);

		if (Ct.IsCancellationRequested)
		{
			Log.Warning(
				messageTemplate: "SyncInterrupted {Reason}",
				"Interrupted while fetching playlist metadata"
			);
			return;
		}

		OptimizedChanges changes = YouTubeChangeDetector.DetectOptimizedChanges(
			currentSummaries: summaries,
			snapshots: State.PlaylistSnapshots
		);

		if (!changes.HasAnyChanges)
		{
			Ui.Complete(message: "No changes detected.");
			return;
		}

		YouTubeChangeDetector.LogDetailedChanges(
			changes: changes,
			summaries: summaries,
			snapshots: State.PlaylistSnapshots
		);

		await ProcessDeletedPlaylistsAsync(deletedIds: changes.DeletedIds);
		await ProcessRenamedPlaylistsAsync(renames: changes.Renamed);
		await ProcessModifiedPlaylistsAsync(
			[.. changes.NewIds, .. changes.ModifiedIds],
			summaries: summaries
		);

		if (Ct.IsCancellationRequested)
			Log.Warning(messageTemplate: "SyncInterrupted {Reason}", "Interrupted during sync");
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
				ArchiveDeletedPlaylist(snapshot: snapshot);

				Log.Information(
					messageTemplate: "PlaylistDeleted {Title} {VideoCount}",
					snapshot.Title,
					snapshot.VideoIds.Count
				);
				State.PlaylistSnapshots.Remove(key: deletedId);
			}
		}

		if (deletedIds.Count > 0)
			await SaveStateAsync();
	}

	private async Task ProcessRenamedPlaylistsAsync(List<PlaylistRename> renames)
	{
		foreach (PlaylistRename rename in renames)
		{
			if (Ct.IsCancellationRequested)
				break;

			Ui.Info(message: "Renaming: {0} → {1}", rename.OldTitle, rename.NewTitle);
			Log.Information(
				messageTemplate: "PlaylistRenamed {OldTitle} {NewTitle}",
				rename.OldTitle,
				rename.NewTitle
			);

			StateManager.RenamePlaylistCache(oldTitle: rename.OldTitle, newTitle: rename.NewTitle);

			if (
				State.PlaylistSnapshots.TryGetValue(
					key: rename.PlaylistId,
					out PlaylistSnapshot? snapshot
				)
			)
			{
				State.PlaylistSnapshots[key: rename.PlaylistId] = snapshot with
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
			Ui.Complete(message: "Done! Only metadata changes applied.");
			return;
		}

		Log.Debug(
			messageTemplate: "Fetching details for {0} changed playlists...",
			playlistIds.Count
		);

		List<YouTubePlaylist> playlistsToProcess = await FetchPlaylistVideoIdsAsync(
			playlistIds: playlistIds,
			summaries: summaries
		);

		if (Ct.IsCancellationRequested || playlistsToProcess.Count == 0)
			return;

		var isFirstPlaylist = State.PlaylistSnapshots.Count == 0;
		var processedCount = await ProcessPlaylistsWithProgressAsync(
			playlistsToProcess: playlistsToProcess,
			isFirstPlaylist: isFirstPlaylist
		);

		UpdateSnapshotsForProcessedPlaylists(playlists: playlistsToProcess, summaries: summaries);

		Ui.Complete(message: "Done! Updated {0} playlists.", processedCount);
	}

	private async Task<List<YouTubePlaylist>> FetchPlaylistVideoIdsAsync(
		List<string> playlistIds,
		List<PlaylistSummary> summaries
	)
	{
		List<YouTubePlaylist> result = [];
		Dictionary<string, PlaylistSummary> summaryLookup = summaries.ToDictionary(s => s.Id);

		Ui.Suppress = true;

		await UI.CreateStandardProgress(DescriptionColumnWidth)
			.StartAsync(async ctx =>
			{
				ProgressTask task = ctx.AddTask(
					Ui.TaskTitle($"Fetching video IDs (0/{playlistIds.Count})"),
					maxValue: playlistIds.Count
				);

				foreach (var playlistId in playlistIds)
				{
					if (Ct.IsCancellationRequested)
						break;

					PlaylistSummary summary = summaryLookup[key: playlistId];
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

					task.Increment(value: 1);
				}
			});

		Ui.Suppress = false;
		return result;
	}

	private void UpdateSnapshotsForProcessedPlaylists(
		List<YouTubePlaylist> playlists,
		List<PlaylistSummary> summaries
	)
	{
		Dictionary<string, PlaylistSummary> summaryLookup = summaries.ToDictionary(s => s.Id);

		foreach (YouTubePlaylist playlist in playlists)
		{
			PlaylistSummary summary = summaryLookup[key: playlist.Id];
			State.PlaylistSnapshots[key: playlist.Id] = new PlaylistSnapshot(
				PlaylistId: playlist.Id,
				Title: playlist.Title,
				VideoIds: playlist.VideoIds,
				LastUpdated: DateTime.UtcNow,
				ReportedVideoCount: playlist.VideoCount,
				ETag: summary.ETag
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
			Ui.Info(message: "No playlists found.");
			return;
		}

		if (!await FetchAllVideoIdsAsync(playlists: playlists))
			return;

		Ui.Info(
			message: "[Phase 1/2] Video ID fetch complete - all {0} playlists ready",
			playlists.Count
		);
		Ui.Info(message: "[Phase 2/2] Starting DB write phase...");

		PlaylistChanges playlistChanges = YouTubeChangeDetector.DetectPlaylistChanges(
			currentPlaylists: playlists,
			snapshots: State.PlaylistSnapshots
		);

		if (!playlistChanges.HasChanges && State.FetchComplete)
		{
			Ui.Complete(message: "No changes detected.");
			return;
		}

		YouTubeChangeDetector.LogDetectedChanges(changes: playlistChanges);

		if (Ct.IsCancellationRequested)
		{
			Log.Warning(
				messageTemplate: "SyncInterrupted {Reason}",
				"Interrupted before processing playlist changes"
			);
			return;
		}

		await ProcessDeletedPlaylistsAsync(deletedIds: playlistChanges.DeletedPlaylistIds);

		if (Ct.IsCancellationRequested)
		{
			Log.Warning(
				messageTemplate: "SyncInterrupted {Reason}",
				"Interrupted after processing deletions"
			);
			return;
		}

		await WritePlaylistsToDbAsync(playlists: playlists, playlistChanges: playlistChanges);
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
					? playlists[index: alreadyHaveVideoIds].Title
					: "(all playlists fetched)";
			Ui.Info(
				message: "[Phase 1/2] Resuming video ID fetch: {0}/{1} ({2}%) - {3}",
				alreadyHaveVideoIds,
				playlistCount,
				progressPercent,
				currentPlaylistTitle
			);
			Log.Debug(
				messageTemplate: "Using cached playlist metadata ({0} playlists)",
				playlistCount
			);
			return playlists;
		}

		List<YouTubePlaylist> freshPlaylists = await YoutubeService.GetPlaylistMetadataAsync(Ct);

		if (Ct.IsCancellationRequested)
		{
			Log.Warning(
				messageTemplate: "SyncInterrupted {Reason}",
				"Interrupted while fetching playlist metadata"
			);
			return null;
		}

		State = State with { CachedPlaylists = freshPlaylists, VideoIdFetchIndex = 0 };
		await SaveStateAsync();
		Ui.Info(
			message: "[Phase 1/2] Starting video ID fetch for {0} playlists",
			freshPlaylists.Count
		);
		Log.Debug(messageTemplate: "Cached playlist metadata for resume capability");
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

		Ui.Suppress = true;

		await UI.CreateStandardProgress(DescriptionColumnWidth)
			.StartAsync(async ctx =>
			{
				ProgressTask task = ctx.AddTask(
					Ui.TaskTitle($"Fetching video IDs ({alreadyFetched}/{playlistCount} done)"),
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

					YouTubePlaylist playlist = playlists[index: i];
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

					playlists[index: i] = playlist with { VideoIds = videoIds };
					State = State with { CachedPlaylists = playlists, VideoIdFetchIndex = i + 1 };
					await SaveStateAsync();

					task.Increment(value: 1);
				}

				task.Value = task.MaxValue;
			});

		Ui.Suppress = false;

		if (interrupted)
		{
			Log.Warning(
				messageTemplate: "YouTubeVideoIdFetchInterrupted {Detail}",
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
		HashSet<string> newPlaylistIds = playlistChanges.NewPlaylistIds.ToHashSet();
		HashSet<string> modifiedPlaylistIds = playlistChanges.ModifiedPlaylistIds.ToHashSet();

		List<YouTubePlaylist> playlistsToProcess = new(capacity: playlists.Count);
		foreach (YouTubePlaylist playlist in playlists)
		{
			if (
				newPlaylistIds.Contains(item: playlist.Id)
				|| modifiedPlaylistIds.Contains(item: playlist.Id)
				|| !State.PlaylistSnapshots.ContainsKey(key: playlist.Id)
			)
				playlistsToProcess.Add(item: playlist);
		}

		var skippedCount = playlists.Count - playlistsToProcess.Count;
		var processedCount = 0;

		if (skippedCount > 0)
			Ui.Info(message: "Skipping {0} unchanged playlists", skippedCount);

		if (playlistsToProcess.Count > 0)
		{
			processedCount = await ProcessPlaylistsWithProgressAsync(
				playlistsToProcess: playlistsToProcess,
				isFirstPlaylist: isFirstPlaylist
			);
		}

		if (Ct.IsCancellationRequested)
		{
			Log.Warning(
				messageTemplate: "YouTubeInterruptedBeforeWrite {Detail}",
				$"{State.PlaylistSnapshots.Count} playlists completed before interrupt"
			);
			return;
		}

		State = State with { FetchComplete = true, CachedPlaylists = null };
		await SaveStateAsync();

		Ui.Complete(
			message: "Done! Synced {0} playlists ({1} processed, {2} unchanged).",
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

		var titleWidth = Math.Min(val1: 40, val2: maxTitleLength);
		var suffixWidth = $"(0/{maxVideoCount} videos)".Length;
		var totalDescriptionWidth = prefixWidth + 1 + titleWidth + 1 + suffixWidth;

		Ui.Suppress = true;

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
						Ui.TaskDescription(
							prefix: playlistCount,
							title: playlist.Title,
							$"(0/{playlistVideoCount} videos)",
							prefixWidth: prefixWidth,
							titleWidth: titleWidth
						),
						maxValue: playlistVideoCount
					);

					await ProcessPlaylistWithContextAsync(
						playlist: playlist,
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

		Ui.Suppress = false;

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
		List<YouTubeVideo> existingCache = StateManager.LoadPlaylistCache(
			playlistTitle: playlist.Title
		);
		List<YouTubeVideo> previousVideos = [.. existingCache];

		if (State.CurrentPlaylistId == playlist.Id && existingCache.Count > 0)
		{
			alreadyFetched = State.CurrentPlaylistVideosFetched;
			videos = existingCache;
			onVideoProgress(obj: alreadyFetched);
			Log.Debug(
				messageTemplate: "Resuming '{0}': {1}/{2} videos already fetched from cache",
				playlist.Title,
				alreadyFetched,
				playlistVideoCount
			);
		}

		List<string> remainingIds = playlist.VideoIds.GetRange(
			index: alreadyFetched,
			playlistVideoCount - alreadyFetched
		);
		var videosFetchedSoFar = alreadyFetched;

		List<YouTubeVideo> newVideos = await YoutubeService.GetVideoDetailsForIdsAsync(
			remainingIds,
			async batchVideos =>
			{
				videos.AddRange(collection: batchVideos);
				videosFetchedSoFar += batchVideos.Count;

				StateManager.SavePlaylistCache(playlistTitle: playlist.Title, videos: videos);

				State = State with
				{
					CurrentPlaylistId = playlist.Id,
					CurrentPlaylistVideosFetched = videosFetchedSoFar,
					LastUpdated = DateTime.UtcNow,
				};
				await SaveStateAsync();
				Log.Debug(
					messageTemplate: "Cached: {0}/{1} video details for '{2}' (batch resume)",
					videosFetchedSoFar,
					playlistVideoCount,
					playlist.Title
				);

				onVideoProgress(obj: videosFetchedSoFar);
				await Task.CompletedTask;
			},
			Ct
		);

		if (Ct.IsCancellationRequested)
			return;

		if (PreviewMode)
		{
			Ui.Suppress = false;
			Ui.Info(message: "Translating {0} videos...", videos.Count);
			videos = await YouTubeTranslationService.TranslateVideosAsync(videos: videos, ct: Ct);

			if (Ct.IsCancellationRequested)
				return;

			Ui.NewLine();
			YouTubeTranslationService.ShowTranslationPreview(videos: videos);
			Ui.Suppress = true;
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
				Log.Debug(
					messageTemplate: "Translating {0} non-English videos...",
					needsTranslationCount
				);
				videos = await YouTubeTranslationService.TranslateVideosAsync(
					videos: videos,
					ct: Ct
				);

				if (Ct.IsCancellationRequested)
					return;
			}
		}

		Log.Information(
			messageTemplate: "SyncComplete {Detail}",
			$"Fetched {videos.Count} videos for DB."
		);

		PlaylistSnapshot snapshot = new(
			PlaylistId: playlist.Id,
			Title: playlist.Title,
			VideoIds: playlist.VideoIds,
			LastUpdated: DateTime.UtcNow,
			ReportedVideoCount: playlist.VideoCount,
			ETag: playlist.ETag
		);
		State.PlaylistSnapshots[key: playlist.Id] = snapshot;
		State = State with
		{
			CurrentPlaylistId = null,
			CurrentPlaylistVideosFetched = 0,
			LastUpdated = DateTime.UtcNow,
		};
		await SaveStateAsync();
	}

	internal async Task SaveStateAsync() =>
		await StateManager.SaveStateAsync(
			fileName: StateManager.YoutubeSyncFile,
			state: State,
			ct: Ct
		);

	private static void ArchiveDeletedPlaylist(PlaylistSnapshot snapshot)
	{
		var archivedPath = StateManager.ArchivePlaylistCache(playlistTitle: snapshot.Title);

		Ui.Warn(message: "Playlist deleted: {0}", snapshot.Title);
		Ui.Info(message: "Archived to: {0}", archivedPath);
	}

	public static Task ExportSheetsAsCsVsAsync(
		string outputDirectory = "YouTube Playlists",
		CancellationToken ct = default
	) => throw new NotImplementedException(message: "Export to CSV requires DB implementation.");

	public static async Task CountPlaylistsAsync(CancellationToken ct = default)
	{
		using YouTubeService youtubeService = await YouTubeService.CreateAsync(ct: ct);

		List<PlaylistSummary> playlists = await youtubeService.GetPlaylistSummariesAsync(ct: ct);
		Ui.Info(message: "Playlists: {0}", playlists.Count);
	}
}
