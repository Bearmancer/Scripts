namespace CSharpScripts.Orchestrators;

public class YouTubePlaylistOrchestrator(CancellationToken ct) : IDisposable
{
    private static readonly IReadOnlyList<object> VideoHeaders =
    [
        "Title",
        "Description",
        "Channel Name",
        "Duration",
    ];

    private readonly GoogleSheetsService sheetsService = new();

    private readonly YouTubeFetchState state = StateManager.Load<YouTubeFetchState>(
        fileName: StateManager.YouTubeSyncFile
    );

    private readonly YouTubeService youtubeService = new();

    public void Dispose()
    {
        youtubeService?.Dispose();
        sheetsService?.Dispose();
        GC.SuppressFinalize(this);
    }

    internal async Task ExecuteAsync()
    {
        Console.Info(message: "Starting YouTube sync...");
        StateManager.MigratePlaylistFiles(snapshots: state.PlaylistSnapshots);

        string spreadsheetId = GetOrCreateSpreadsheet();

        if (state.FetchComplete && state.PlaylistSnapshots.Count > 0)
        {
            await ExecuteOptimizedAsync(spreadsheetId: spreadsheetId);
            return;
        }

        await ExecuteFullSyncAsync(spreadsheetId: spreadsheetId);
    }

    internal async Task ExecuteForPlaylistsAsync(string[] playlistIdentifiers)
    {
        Console.Debug(
            message: "Selective sync initiated for {0} playlist(s)",
            playlistIdentifiers.Length
        );

        string spreadsheetId = GetOrCreateSpreadsheet();
        var resolvedPlaylists = await ResolvePlaylistIdentifiersAsync(
            identifiers: playlistIdentifiers
        );

        if (resolvedPlaylists.Count == 0)
        {
            Console.Error(message: "No matching playlists found for the provided identifiers.");
            Logger.End(success: false, summary: "No matching playlists");
            return;
        }

        Console.Info(message: "Syncing {0} playlist(s):", resolvedPlaylists.Count);
        foreach (var playlist in resolvedPlaylists)
            Console.Info(message: "  • {0}", playlist.Title);

        bool isFirstPlaylist = state.PlaylistSnapshots.Count == 0;
        int processedCount = await ProcessPlaylistsWithProgressAsync(
            playlistsToProcess: resolvedPlaylists,
            spreadsheetId: spreadsheetId,
            isFirstPlaylist: isFirstPlaylist
        );

        if (!ct.IsCancellationRequested)
        {
            Console.Success(message: "Done! Synced {0} playlist(s).", processedCount);
            Logger.End(success: true, $"Synced {processedCount} playlists (selective)");
        }
        else
        {
            Logger.Interrupted(progress: "Interrupted during selective sync");
        }
    }

    private async Task<List<YouTubePlaylist>> ResolvePlaylistIdentifiersAsync(string[] identifiers)
    {
        List<YouTubePlaylist> resolved = [];

        var titleLookup = state.PlaylistSnapshots.Values.ToDictionary(
            s => s.Title,
            s => s,
            comparer: StringComparer.OrdinalIgnoreCase
        );

        foreach (string identifier in identifiers)
        {
            if (ct.IsCancellationRequested)
                break;

            var playlist = await ResolvePlaylistIdentifierAsync(
                identifier: identifier,
                titleLookup: titleLookup
            );
            if (playlist != null)
                resolved.Add(item: playlist);
            else
                Console.Warning(message: "Could not resolve: {0}", identifier);
        }

        return resolved;
    }

    private async Task<YouTubePlaylist?> ResolvePlaylistIdentifierAsync(
        string identifier,
        Dictionary<string, PlaylistSnapshot>? titleLookup = null
    )
    {
        if (state.PlaylistSnapshots.TryGetValue(key: identifier, out var snapshot))
        {
            Console.Debug(message: "Resolved '{0}' from cached snapshot", identifier);
            return new YouTubePlaylist(
                Id: snapshot.PlaylistId,
                Title: snapshot.Title,
                VideoCount: snapshot.ReportedVideoCount,
                await youtubeService.GetPlaylistVideoIdsAsync(
                    playlistId: snapshot.PlaylistId,
                    ct: ct
                ),
                ETag: snapshot.ETag
            );
        }

        PlaylistSnapshot? titleMatch = null;
        if (titleLookup != null)
            titleLookup.TryGetValue(key: identifier, value: out titleMatch);
        else
            titleMatch = state.PlaylistSnapshots.Values.FirstOrDefault(s =>
                s.Title.Equals(
                    value: identifier,
                    comparisonType: StringComparison.OrdinalIgnoreCase
                )
            );

        if (titleMatch != null)
        {
            Console.Debug(message: "Resolved '{0}' by title match", identifier);
            return new YouTubePlaylist(
                Id: titleMatch.PlaylistId,
                Title: titleMatch.Title,
                VideoCount: titleMatch.ReportedVideoCount,
                await youtubeService.GetPlaylistVideoIdsAsync(
                    playlistId: titleMatch.PlaylistId,
                    ct: ct
                ),
                ETag: titleMatch.ETag
            );
        }

        if (
            identifier.StartsWith(value: "PL", comparisonType: StringComparison.Ordinal)
            || identifier.StartsWith(value: "UU", comparisonType: StringComparison.Ordinal)
            || identifier.StartsWith(value: "FL", comparisonType: StringComparison.Ordinal)
        )
        {
            Console.Debug(message: "Fetching playlist by ID: {0}", identifier);
            var videoIds = await youtubeService.GetPlaylistVideoIdsAsync(
                playlistId: identifier,
                ct: ct
            );
            if (videoIds.Count > 0)
                if (
                    await youtubeService.GetPlaylistSummaryAsync(playlistId: identifier, ct: ct) is
                    { } summary
                )
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
        Console.Debug(message: "Last change: {0:yyyy/MM/dd HH:mm:ss}", state.LastUpdated);
        state.LastChecked = DateTime.Now;
        state.LastUpdated = DateTime.Now;
        SaveState();

        List<PlaylistSummary> summaries = [];
        await AnsiConsole
            .Status()
            .Spinner(spinner: Spinner.Known.Dots)
            .StartAsync(
                status: "Fetching playlist metadata...",
                async _ =>
                {
                    summaries = await youtubeService.GetPlaylistSummariesAsync(ct: ct);
                }
            );

        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted(progress: "Interrupted while fetching playlist metadata");
            return;
        }

        var changes = YouTubeChangeDetector.DetectOptimizedChanges(
            currentSummaries: summaries,
            snapshots: state.PlaylistSnapshots
        );

        if (!changes.HasAnyChanges)
        {
            Console.Success(message: "No changes detected.");
            Logger.End(success: true, summary: "No changes detected");
            return;
        }

        YouTubeChangeDetector.LogDetailedChanges(
            changes: changes,
            summaries: summaries,
            snapshots: state.PlaylistSnapshots
        );

        ProcessDeletedPlaylists(deletedIds: changes.DeletedIds, spreadsheetId: spreadsheetId);
        ProcessRenamedPlaylists(renames: changes.Renamed, spreadsheetId: spreadsheetId);
        await ProcessModifiedPlaylistsAsync(
            [.. changes.NewIds, .. changes.ModifiedIds],
            summaries: summaries,
            spreadsheetId: spreadsheetId
        );

        if (ct.IsCancellationRequested)
            Logger.Interrupted(progress: "Interrupted during sync");
    }

    private void ProcessDeletedPlaylists(List<string> deletedIds, string spreadsheetId)
    {
        foreach (string deletedId in deletedIds)
        {
            if (ct.IsCancellationRequested)
                break;

            var snapshot = state.PlaylistSnapshots.GetValueOrDefault(key: deletedId);
            if (snapshot != null)
            {
                ArchiveDeletedPlaylist(snapshot: snapshot);
                sheetsService.DeleteSubsheet(
                    spreadsheetId: spreadsheetId,
                    SanitizeSheetName(name: snapshot.Title)
                );
                Logger.PlaylistDeleted(title: snapshot.Title, videoCount: snapshot.VideoIds.Count);
                state.PlaylistSnapshots.Remove(key: deletedId);
            }
        }

        if (deletedIds.Count > 0)
            SaveState();
    }

    private void ProcessRenamedPlaylists(List<PlaylistRename> renames, string spreadsheetId)
    {
        foreach (var rename in renames)
        {
            if (ct.IsCancellationRequested)
                break;

            Console.Info(message: "Renaming: {0} → {1}", rename.OldTitle, rename.NewTitle);
            Logger.PlaylistRenamed(oldTitle: rename.OldTitle, newTitle: rename.NewTitle);

            sheetsService.RenameSubsheet(
                spreadsheetId: spreadsheetId,
                SanitizeSheetName(name: rename.OldTitle),
                SanitizeSheetName(name: rename.NewTitle)
            );

            StateManager.RenamePlaylistCache(oldTitle: rename.OldTitle, newTitle: rename.NewTitle);

            if (state.PlaylistSnapshots.TryGetValue(key: rename.PlaylistId, out var snapshot))
                state.PlaylistSnapshots[key: rename.PlaylistId] = snapshot with
                {
                    Title = rename.NewTitle,
                };
        }

        if (renames.Count > 0)
            SaveState();
    }

    private async Task ProcessModifiedPlaylistsAsync(
        List<string> playlistIds,
        List<PlaylistSummary> summaries,
        string spreadsheetId
    )
    {
        if (playlistIds.Count == 0)
        {
            Console.Success(message: "Done! Only metadata changes applied.");
            Logger.End(success: true, summary: "Metadata updates only");
            return;
        }

        Console.Debug(message: "Fetching details for {0} changed playlists...", playlistIds.Count);

        var playlistsToProcess = await FetchPlaylistVideoIdsAsync(
            playlistIds: playlistIds,
            summaries: summaries
        );

        if (ct.IsCancellationRequested || playlistsToProcess.Count == 0)
            return;

        bool isFirstPlaylist = state.PlaylistSnapshots.Count == 0;
        int processedCount = await ProcessPlaylistsWithProgressAsync(
            playlistsToProcess: playlistsToProcess,
            spreadsheetId: spreadsheetId,
            isFirstPlaylist: isFirstPlaylist
        );

        UpdateSnapshotsForProcessedPlaylists(playlists: playlistsToProcess, summaries: summaries);

        Console.Success(message: "Done! Updated {0} playlists.", processedCount);
        Logger.End(success: true, $"Updated {processedCount} playlists");
    }

    private async Task<List<YouTubePlaylist>> FetchPlaylistVideoIdsAsync(
        List<string> playlistIds,
        List<PlaylistSummary> summaries
    )
    {
        List<YouTubePlaylist> result = [];
        var summaryLookup = summaries.ToDictionary(s => s.Id);

        Console.Suppress = true;

        await AnsiConsole
            .Progress()
            .AutoClear(enabled: true)
            .HideCompleted(enabled: false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            )
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(
                    Console.TaskTitle($"Fetching video IDs (0/{playlistIds.Count})"),
                    maxValue: playlistIds.Count
                );

                foreach (string playlistId in playlistIds)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    var summary = summaryLookup[key: playlistId];
                    task.Description = Console.TaskTitle(title: summary.Title);

                    var videoIds = await youtubeService.GetPlaylistVideoIdsAsync(
                        playlistId: playlistId,
                        ct: ct
                    );

                    if (ct.IsCancellationRequested)
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

        Console.Suppress = false;
        return result;
    }

    private void UpdateSnapshotsForProcessedPlaylists(
        List<YouTubePlaylist> playlists,
        List<PlaylistSummary> summaries
    )
    {
        var summaryLookup = summaries.ToDictionary(s => s.Id);

        foreach (var playlist in playlists)
        {
            var summary = summaryLookup[key: playlist.Id];
            state.PlaylistSnapshots[key: playlist.Id] = new PlaylistSnapshot(
                PlaylistId: playlist.Id,
                Title: playlist.Title,
                VideoIds: playlist.VideoIds,
                LastUpdated: DateTime.Now,
                ReportedVideoCount: playlist.VideoCount,
                ETag: summary.ETag
            );
        }
        SaveState();
    }

    private async Task ExecuteFullSyncAsync(string spreadsheetId)
    {
        var playlists = await GetOrFetchPlaylistMetadataAsync();
        if (playlists == null)
            return;

        if (playlists.Count == 0)
        {
            Console.Info(message: "No playlists found.");
            Logger.End(success: true, summary: "No playlists to sync");
            return;
        }

        if (!await FetchAllVideoIdsAsync(playlists: playlists))
            return;

        Console.Info(
            message: "[Phase 1/2] Video ID fetch complete - all {0} playlists ready",
            playlists.Count
        );
        Console.Info(
            message: "[Phase 2/2] Starting sheet write phase (processes alphabetically)..."
        );

        var playlistChanges = YouTubeChangeDetector.DetectPlaylistChanges(
            currentPlaylists: playlists,
            snapshots: state.PlaylistSnapshots
        );

        if (!playlistChanges.HasChanges && state.FetchComplete)
        {
            Console.Success(message: "No changes detected.");
            Logger.End(success: true, summary: "No changes detected");
            return;
        }

        YouTubeChangeDetector.LogDetectedChanges(changes: playlistChanges);

        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted(progress: "Interrupted before processing playlist changes");
            return;
        }

        ProcessDeletedPlaylists(
            deletedIds: playlistChanges.DeletedPlaylistIds,
            spreadsheetId: spreadsheetId
        );

        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted(progress: "Interrupted after processing deletions");
            return;
        }

        await WritePlaylistsToSheetsAsync(
            playlists: playlists,
            playlistChanges: playlistChanges,
            spreadsheetId: spreadsheetId
        );
    }

    private async Task<List<YouTubePlaylist>?> GetOrFetchPlaylistMetadataAsync()
    {
        if (state.CachedPlaylists != null && state.CachedPlaylists.Count > 0)
        {
            var playlists = state.CachedPlaylists;
            int alreadyHaveVideoIds = state.VideoIdFetchIndex;
            var progressPercent = (int)(alreadyHaveVideoIds / (double)playlists.Count * 100);
            string currentPlaylistTitle =
                alreadyHaveVideoIds < playlists.Count
                    ? playlists[index: alreadyHaveVideoIds].Title
                    : "(all playlists fetched)";
            Console.Info(
                message: "[Phase 1/2] Resuming video ID fetch: {0}/{1} ({2}%) - {3}",
                alreadyHaveVideoIds,
                playlists.Count,
                progressPercent,
                currentPlaylistTitle
            );
            Console.Debug(
                message: "Using cached playlist metadata ({0} playlists)",
                playlists.Count
            );
            return playlists;
        }

        var freshPlaylists = await youtubeService.GetPlaylistMetadataAsync(ct: ct);

        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted(progress: "Interrupted while fetching playlist metadata");
            return null;
        }

        state.CachedPlaylists = freshPlaylists;
        state.VideoIdFetchIndex = 0;
        SaveState();
        Console.Info(
            message: "[Phase 1/2] Starting video ID fetch for {0} playlists",
            freshPlaylists.Count
        );
        Console.Debug(message: "Cached playlist metadata for resume capability");
        return freshPlaylists;
    }

    private async Task<bool> FetchAllVideoIdsAsync(List<YouTubePlaylist> playlists)
    {
        if (state.VideoIdFetchIndex >= playlists.Count)
            return true;

        var interrupted = false;
        var interruptedAt = 0;
        int alreadyFetched = state.VideoIdFetchIndex;

        Console.Suppress = true;

        await AnsiConsole
            .Progress()
            .AutoClear(enabled: true)
            .HideCompleted(enabled: false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            )
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(
                    Console.TaskTitle(
                        $"Fetching video IDs ({alreadyFetched}/{playlists.Count} done)"
                    ),
                    maxValue: playlists.Count
                );
                task.Value = alreadyFetched;

                for (int i = state.VideoIdFetchIndex; i < playlists.Count; i++)
                {
                    if (ct.IsCancellationRequested)
                    {
                        interrupted = true;
                        interruptedAt = i;
                        return;
                    }

                    var playlist = playlists[index: i];
                    task.Description = Console.TaskTitle(title: playlist.Title);

                    var videoIds = await youtubeService.GetPlaylistVideoIdsAsync(
                        playlistId: playlist.Id,
                        ct: ct
                    );

                    if (ct.IsCancellationRequested)
                    {
                        interrupted = true;
                        interruptedAt = i;
                        return;
                    }

                    playlists[index: i] = playlist with { VideoIds = videoIds };
                    state.CachedPlaylists = playlists;
                    state.VideoIdFetchIndex = i + 1;
                    SaveState();

                    task.Increment(value: 1);
                }

                task.Value = task.MaxValue;
            });

        Console.Suppress = false;

        if (interrupted)
        {
            Logger.Interrupted(
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
        bool isFirstPlaylist = state.PlaylistSnapshots.Count == 0;
        string? firstPlaylistTitle = playlists.FirstOrDefault()?.Title;

        var playlistsToProcess = playlists
            .Where(p =>
                playlistChanges.NewPlaylistIds.Contains(item: p.Id)
                || playlistChanges.ModifiedPlaylistIds.Contains(item: p.Id)
                || !state.PlaylistSnapshots.ContainsKey(key: p.Id)
            )
            .ToList();

        int skippedCount = playlists.Count - playlistsToProcess.Count;
        var processedCount = 0;

        if (skippedCount > 0)
            Console.Info(message: "Skipping {0} unchanged playlists", skippedCount);

        if (playlistsToProcess.Count > 0)
            processedCount = await ProcessPlaylistsWithProgressAsync(
                playlistsToProcess: playlistsToProcess,
                spreadsheetId: spreadsheetId,
                isFirstPlaylist: isFirstPlaylist
            );

        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted(
                $"{state.PlaylistSnapshots.Count} playlists completed before interrupt"
            );
            return;
        }

        state.FetchComplete = true;
        state.CachedPlaylists = null;
        SaveState();

        FinalizeSpreadsheet(spreadsheetId: spreadsheetId, firstPlaylistTitle: firstPlaylistTitle);

        Console.Success(
            message: "Done! Synced {0} playlists ({1} processed, {2} unchanged).",
            playlists.Count,
            processedCount,
            playlists.Count - processedCount
        );
        Logger.End(
            success: true,
            $"Synced {playlists.Count} playlists ({processedCount} updated, {playlists.Count - processedCount} unchanged)"
        );
    }

    private void FinalizeSpreadsheet(string spreadsheetId, string? firstPlaylistTitle)
    {
        if (!IsNullOrEmpty(value: firstPlaylistTitle))
        {
            string sanitizedFirst = SanitizeSheetName(name: firstPlaylistTitle);
            sheetsService.RenameSubsheet(
                spreadsheetId: spreadsheetId,
                oldName: "Sheet1",
                newName: sanitizedFirst
            );
        }
        else
        {
            sheetsService.CleanupDefaultSheet(spreadsheetId: spreadsheetId);
        }

        sheetsService.ReorderSheetsAlphabetically(spreadsheetId: spreadsheetId);
    }

    private async Task<int> ProcessPlaylistsWithProgressAsync(
        List<YouTubePlaylist> playlistsToProcess,
        string spreadsheetId,
        bool isFirstPlaylist
    )
    {
        var processedCount = 0;

        Console.Suppress = true;

        await AnsiConsole
            .Progress()
            .AutoClear(enabled: true)
            .HideCompleted(enabled: false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            )
            .StartAsync(async ctx =>
            {
                foreach (var playlist in playlistsToProcess)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    var playlistCount = $"({processedCount + 1}/{playlistsToProcess.Count})";
                    int playlistVideoCount = playlist.VideoIds.Count;

                    var task = ctx.AddTask(
                        Console.TaskDescription(
                            prefix: playlistCount,
                            title: playlist.Title,
                            $"(0/{playlistVideoCount} videos)"
                        ),
                        maxValue: playlistVideoCount
                    );

                    await ProcessPlaylistWithContextAsync(
                        playlist: playlist,
                        spreadsheetId: spreadsheetId,
                        isFirstPlaylist && processedCount == 0,
                        count =>
                        {
                            task.Value = count;
                            task.Description = Console.TaskDescription(
                                prefix: playlistCount,
                                title: playlist.Title,
                                $"({count}/{playlistVideoCount} videos)"
                            );
                        }
                    );

                    task.Value = task.MaxValue;
                    processedCount++;
                }
            });

        Console.Suppress = false;

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
        var existingCache = StateManager.LoadPlaylistCache(playlistTitle: playlist.Title);
        List<YouTubeVideo> previousVideos = [.. existingCache];

        if (state.CurrentPlaylistId == playlist.Id && existingCache.Count > 0)
        {
            alreadyFetched = state.CurrentPlaylistVideosFetched;
            videos = existingCache;
            onVideoProgress(obj: alreadyFetched);
            Console.Debug(
                message: "Resuming '{0}': {1}/{2} videos already fetched from cache",
                playlist.Title,
                alreadyFetched,
                playlist.VideoIds.Count
            );
        }

        var remainingIds = playlist.VideoIds.Skip(count: alreadyFetched).ToList();
        int videosFetchedSoFar = alreadyFetched;

        var newVideos = await youtubeService.GetVideoDetailsForIdsAsync(
            videoIds: remainingIds,
            async batchVideos =>
            {
                videos.AddRange(collection: batchVideos);
                videosFetchedSoFar += batchVideos.Count;

                StateManager.SavePlaylistCache(playlistTitle: playlist.Title, videos: videos);

                state.UpdatePlaylistProgress(
                    playlistId: playlist.Id,
                    videosFetched: videosFetchedSoFar
                );
                SaveState();
                Console.Debug(
                    message: "Cached: {0}/{1} video details for '{2}' (batch resume)",
                    videosFetchedSoFar,
                    playlist.VideoIds.Count,
                    playlist.Title
                );

                onVideoProgress(obj: videosFetchedSoFar);
                await Task.CompletedTask;
            },
            ct: ct
        );

        if (ct.IsCancellationRequested)
            return;

        WritePlaylist(
            playlist: playlist,
            videos: videos,
            previousVideos: previousVideos,
            spreadsheetId: spreadsheetId,
            isFirstPlaylist: isFirstPlaylist
        );

        PlaylistSnapshot snapshot = new(
            PlaylistId: playlist.Id,
            Title: playlist.Title,
            VideoIds: playlist.VideoIds,
            LastUpdated: DateTime.Now,
            ReportedVideoCount: playlist.VideoCount,
            ETag: playlist.ETag
        );
        state.PlaylistSnapshots[key: playlist.Id] = snapshot;
        state.ClearCurrentProgress();
        SaveState();
    }

    private string GetOrCreateSpreadsheet() =>
        sheetsService.GetOrCreateSpreadsheet(
            currentSpreadsheetId: state.SpreadsheetId,
            defaultSpreadsheetId: Config.YouTubeSpreadsheetId,
            spreadsheetTitle: Config.YouTubeSpreadsheetTitle,
            id =>
            {
                state.SpreadsheetId = id;
                SaveState();
            }
        );

    internal void SaveState() =>
        StateManager.Save(fileName: StateManager.YouTubeSyncFile, state: state);

    private static void ArchiveDeletedPlaylist(PlaylistSnapshot snapshot)
    {
        string archivedPath = StateManager.ArchivePlaylistCache(playlistTitle: snapshot.Title);

        Console.Warning(message: "Playlist deleted: {0}", snapshot.Title);
        Console.Info(message: "Archived to: {0}", archivedPath);
    }

    private void WritePlaylist(
        YouTubePlaylist playlist,
        List<YouTubeVideo> videos,
        List<YouTubeVideo> previousVideos,
        string spreadsheetId,
        bool isFirstPlaylist
    )
    {
        string sheetName = SanitizeSheetName(name: playlist.Title);
        var existingSnapshot = state.PlaylistSnapshots.GetValueOrDefault(key: playlist.Id);

        if (isFirstPlaylist)
        {
            sheetsService.RenameSubsheet(
                spreadsheetId: spreadsheetId,
                oldName: "Sheet1",
                newName: sheetName
            );
            WriteFullPlaylist(sheetName: sheetName, videos: videos, spreadsheetId: spreadsheetId);
            return;
        }

        if (existingSnapshot == null)
        {
            sheetsService.EnsureSubsheetExists(
                spreadsheetId: spreadsheetId,
                sheetName: sheetName,
                headers: VideoHeaders
            );
            WriteFullPlaylist(sheetName: sheetName, videos: videos, spreadsheetId: spreadsheetId);
            return;
        }

        var videoChanges = YouTubeChangeDetector.DetectVideoChanges(
            currentVideoIds: playlist.VideoIds,
            storedVideoIds: existingSnapshot.VideoIds
        );

        var removedTitles = videoChanges
            .RemovedVideoIds.Select(id =>
                previousVideos.FirstOrDefault(v => v.VideoId == id)?.Title
            )
            .Where(t => t != null)
            .Cast<string>()
            .ToList();

        var addedVideos = videos
            .Where(v => videoChanges.AddedVideoIds.Contains(item: v.VideoId))
            .ToList();
        var addedTitles = addedVideos.Select(v => v.Title).ToList();

        if (videoChanges.RequiresFullRewrite)
        {
            Console.Debug(message: "Order changed in '{0}', full rewrite required", playlist.Title);
            WriteFullPlaylist(sheetName: sheetName, videos: videos, spreadsheetId: spreadsheetId);
            Logger.PlaylistUpdated(
                title: playlist.Title,
                added: addedTitles.Count,
                removed: removedTitles.Count,
                addedTitles: addedTitles,
                removedTitles: removedTitles,
                removedVideoIds: videoChanges.RemovedVideoIds
            );
            return;
        }

        if (!videoChanges.HasChanges)
        {
            Console.Debug(message: "No video changes in '{0}'", playlist.Title);
            Console.Debug("Skipped playlist: " + playlist.Title);
            return;
        }

        var removedSet = videoChanges.RemovedVideoIds.ToHashSet();

        Logger.PlaylistUpdated(
            title: playlist.Title,
            added: addedTitles.Count,
            removed: removedTitles.Count,
            addedTitles: addedTitles,
            removedTitles: removedTitles,
            removedVideoIds: videoChanges.RemovedVideoIds
        );

        if (videoChanges.RemovedRowIndices.Count > 0)
            sheetsService.DeleteRowsFromSubsheet(
                spreadsheetId: spreadsheetId,
                sheetName: sheetName,
                rowIndices: videoChanges.RemovedRowIndices
            );

        if (addedVideos.Count > 0)
            sheetsService.AppendRecords(
                spreadsheetId: spreadsheetId,
                sheetName: sheetName,
                records: addedVideos,
                rowMapper: MapVideoToRow
            );

        var updatedVideos = previousVideos
            .Where(v => !removedSet.Contains(item: v.VideoId))
            .Concat(videos.Where(v => videoChanges.AddedVideoIds.Contains(item: v.VideoId)))
            .ToList();
        StateManager.SavePlaylistCache(playlistTitle: playlist.Title, videos: updatedVideos);
    }

    private void WriteFullPlaylist(
        string sheetName,
        List<YouTubeVideo> videos,
        string spreadsheetId
    )
    {
        Console.Debug(message: "Full write: {0} videos to '{1}'", videos.Count, sheetName);

        sheetsService.WriteRecords(
            spreadsheetId: spreadsheetId,
            sheetName: sheetName,
            headers: VideoHeaders,
            records: videos,
            rowMapper: MapVideoToRow
        );
    }

    private static IList<object> MapVideoToRow(YouTubeVideo v) =>
        [
            $"=HYPERLINK(\"{v.VideoUrl}\", \"{EscapeFormulaString(value: v.Title)}\")",
            v.Description,
            $"=HYPERLINK(\"{v.ChannelUrl}\", \"{EscapeFormulaString(value: v.ChannelName)}\")",
            v.FormattedDuration,
        ];

    private static string SanitizeSheetName(string name) =>
        name.Replace(oldValue: ":", newValue: " -")
            .Replace(oldValue: "/", newValue: "-")
            .Replace(oldValue: "\\", newValue: "-")
            .Replace(oldValue: "?", newValue: "")
            .Replace(oldValue: "*", newValue: "")
            .Replace(oldValue: "[", newValue: "(")
            .Replace(oldValue: "]", newValue: ")");

    private static string EscapeFormulaString(string value) =>
        value.Replace(oldValue: "\"", newValue: "\"\"");

    public static void ExportSheetsAsCSVs(
        string outputDirectory = "YouTube Playlists",
        CancellationToken ct = default
    )
    {
        var state = StateManager.Load<YouTubeFetchState>(fileName: StateManager.YouTubeSyncFile);

        if (IsNullOrEmpty(value: state.SpreadsheetId))
            throw new InvalidOperationException(
                message: "No YouTube spreadsheet found. Run sync first to create it."
            );

        string desktopPath = GetFolderPath(folder: SpecialFolder.Desktop);
        string fullOutputPath = Combine(path1: desktopPath, path2: outputDirectory);

        var sheetsService = new GoogleSheetsService();

        int exported = sheetsService.ExportEachSheetAsCSV(
            spreadsheetId: state.SpreadsheetId,
            outputDirectory: fullOutputPath,
            ct: ct
        );

        if (exported > 0)
            Console.Success(
                message: "Exported {0} playlists to: {1}",
                exported,
                GetFullPath(path: fullOutputPath)
            );
        else
            Console.Info(
                message: "All playlists already exported to: {0}",
                GetFullPath(path: fullOutputPath)
            );
    }

    public static async Task CountPlaylistsAsync(CancellationToken ct = default)
    {
        var youtubeService = new YouTubeService();

        var playlists = await youtubeService.GetPlaylistSummariesAsync(ct: ct);
        Console.Info(message: "Playlists: {0}", playlists.Count);
    }
}
