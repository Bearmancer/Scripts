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

    private readonly YouTubeService youtubeService = new(
        clientId: Config.GoogleClientId,
        clientSecret: Config.GoogleClientSecret
    );

    private readonly GoogleSheetsService sheetsService = new(
        clientId: Config.GoogleClientId,
        clientSecret: Config.GoogleClientSecret
    );

    private readonly YouTubeFetchState state = StateManager.Load<YouTubeFetchState>(
        StateManager.YouTubeSyncFile
    );

    internal async Task ExecuteAsync()
    {
        Console.Info("Starting YouTube sync...");
        StateManager.MigratePlaylistFiles(state.PlaylistSnapshots);

        var spreadsheetId = GetOrCreateSpreadsheet();

        if (state.FetchComplete && state.PlaylistSnapshots.Count > 0)
        {
            await ExecuteOptimizedAsync(spreadsheetId);
            return;
        }

        await ExecuteFullSyncAsync(spreadsheetId);
    }

    internal async Task ExecuteForPlaylistsAsync(string[] playlistIdentifiers)
    {
        Console.Debug("Selective sync initiated for {0} playlist(s)", playlistIdentifiers.Length);

        var spreadsheetId = GetOrCreateSpreadsheet();
        var resolvedPlaylists = await ResolvePlaylistIdentifiersAsync(playlistIdentifiers);

        if (resolvedPlaylists.Count == 0)
        {
            Console.Error("No matching playlists found for the provided identifiers.");
            Logger.End(success: false, "No matching playlists");
            return;
        }

        Console.Info("Syncing {0} playlist(s):", resolvedPlaylists.Count);
        foreach (var playlist in resolvedPlaylists)
            Console.Info("  • {0}", playlist.Title);

        var isFirstPlaylist = state.PlaylistSnapshots.Count == 0;
        var processedCount = await ProcessPlaylistsWithProgressAsync(
            resolvedPlaylists,
            spreadsheetId,
            isFirstPlaylist
        );

        if (!ct.IsCancellationRequested)
        {
            Console.Success("Done! Synced {0} playlist(s).", processedCount);
            Logger.End(success: true, $"Synced {processedCount} playlists (selective)");
        }
        else
        {
            Logger.Interrupted("Interrupted during selective sync");
        }
    }

    private async Task<List<YouTubePlaylist>> ResolvePlaylistIdentifiersAsync(string[] identifiers)
    {
        List<YouTubePlaylist> resolved = [];

        // Build title lookup once for all identifiers (O(n) instead of O(n*m))
        var titleLookup = state.PlaylistSnapshots.Values.ToDictionary(
            s => s.Title,
            s => s,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var identifier in identifiers)
        {
            if (ct.IsCancellationRequested)
                break;

            var playlist = await ResolvePlaylistIdentifierAsync(identifier, titleLookup);
            if (playlist != null)
                resolved.Add(playlist);
            else
                Console.Warning("Could not resolve: {0}", identifier);
        }

        return resolved;
    }

    private async Task<YouTubePlaylist?> ResolvePlaylistIdentifierAsync(
        string identifier,
        Dictionary<string, PlaylistSnapshot>? titleLookup = null
    )
    {
        if (state.PlaylistSnapshots.TryGetValue(identifier, out var snapshot))
        {
            Console.Debug("Resolved '{0}' from cached snapshot", identifier);
            return new YouTubePlaylist(
                Id: snapshot.PlaylistId,
                Title: snapshot.Title,
                VideoCount: snapshot.ReportedVideoCount,
                VideoIds: await youtubeService.GetPlaylistVideoIdsAsync(snapshot.PlaylistId, ct),
                ETag: snapshot.ETag
            );
        }

        // Use provided lookup or fall back to linear search
        PlaylistSnapshot? titleMatch = null;
        if (titleLookup != null)
            titleLookup.TryGetValue(identifier, out titleMatch);
        else
            titleMatch = state.PlaylistSnapshots.Values.FirstOrDefault(s =>
                s.Title.Equals(identifier, StringComparison.OrdinalIgnoreCase)
            );

        if (titleMatch != null)
        {
            Console.Debug("Resolved '{0}' by title match", identifier);
            return new YouTubePlaylist(
                Id: titleMatch.PlaylistId,
                Title: titleMatch.Title,
                VideoCount: titleMatch.ReportedVideoCount,
                VideoIds: await youtubeService.GetPlaylistVideoIdsAsync(titleMatch.PlaylistId, ct),
                ETag: titleMatch.ETag
            );
        }

        if (
            identifier.StartsWith("PL", StringComparison.Ordinal)
            || identifier.StartsWith("UU", StringComparison.Ordinal)
            || identifier.StartsWith("FL", StringComparison.Ordinal)
        )
        {
            Console.Debug("Fetching playlist by ID: {0}", identifier);
            var videoIds = await youtubeService.GetPlaylistVideoIdsAsync(identifier, ct);
            if (videoIds.Count > 0)
                if (await youtubeService.GetPlaylistSummaryAsync(identifier, ct) is { } summary)
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
        Console.Debug("Last change: {0:yyyy/MM/dd HH:mm:ss}", state.LastUpdated);
        state.LastChecked = DateTime.Now;
        state.LastUpdated = DateTime.Now;
        SaveState();

        List<PlaylistSummary> summaries = [];
        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                "Fetching playlist metadata...",
                async _ =>
                {
                    summaries = await youtubeService.GetPlaylistSummariesAsync(ct);
                }
            );

        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted("Interrupted while fetching playlist metadata");
            return;
        }

        var changes = YouTubeChangeDetector.DetectOptimizedChanges(
            summaries,
            state.PlaylistSnapshots
        );

        if (!changes.HasAnyChanges)
        {
            Console.Success("No changes detected.");
            Logger.End(success: true, "No changes detected");
            return;
        }

        YouTubeChangeDetector.LogDetailedChanges(changes, summaries, state.PlaylistSnapshots);

        ProcessDeletedPlaylists(changes.DeletedIds, spreadsheetId);
        ProcessRenamedPlaylists(changes.Renamed, spreadsheetId);
        await ProcessModifiedPlaylistsAsync(
            [.. changes.NewIds, .. changes.ModifiedIds],
            summaries,
            spreadsheetId
        );

        if (ct.IsCancellationRequested)
            Logger.Interrupted("Interrupted during sync");
    }

    private void ProcessDeletedPlaylists(List<string> deletedIds, string spreadsheetId)
    {
        foreach (var deletedId in deletedIds)
        {
            if (ct.IsCancellationRequested)
                break;

            var snapshot = state.PlaylistSnapshots.GetValueOrDefault(deletedId);
            if (snapshot != null)
            {
                ArchiveDeletedPlaylist(snapshot);
                sheetsService.DeleteSubsheet(spreadsheetId, SanitizeSheetName(snapshot.Title));
                Logger.PlaylistDeleted(snapshot.Title, snapshot.VideoIds.Count);
                state.PlaylistSnapshots.Remove(deletedId);
            }
        }

        // Save state once at end instead of after each deletion
        if (deletedIds.Count > 0)
            SaveState();
    }

    private void ProcessRenamedPlaylists(List<PlaylistRename> renames, string spreadsheetId)
    {
        foreach (var rename in renames)
        {
            if (ct.IsCancellationRequested)
                break;

            Console.Info("Renaming: {0} → {1}", rename.OldTitle, rename.NewTitle);
            Logger.PlaylistRenamed(rename.OldTitle, rename.NewTitle);

            sheetsService.RenameSubsheet(
                spreadsheetId,
                oldName: SanitizeSheetName(rename.OldTitle),
                newName: SanitizeSheetName(rename.NewTitle)
            );

            StateManager.RenamePlaylistCache(rename.OldTitle, rename.NewTitle);

            if (state.PlaylistSnapshots.TryGetValue(rename.PlaylistId, out var snapshot))
            {
                state.PlaylistSnapshots[rename.PlaylistId] = snapshot with
                {
                    Title = rename.NewTitle,
                };
            }
        }

        // Save state once at end instead of after each rename
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
            Console.Success("Done! Only metadata changes applied.");
            Logger.End(success: true, "Metadata updates only");
            return;
        }

        Console.Debug("Fetching details for {0} changed playlists...", playlistIds.Count);

        var playlistsToProcess = await FetchPlaylistVideoIdsAsync(playlistIds, summaries);

        if (ct.IsCancellationRequested || playlistsToProcess.Count == 0)
            return;

        var isFirstPlaylist = state.PlaylistSnapshots.Count == 0;
        var processedCount = await ProcessPlaylistsWithProgressAsync(
            playlistsToProcess,
            spreadsheetId,
            isFirstPlaylist
        );

        UpdateSnapshotsForProcessedPlaylists(playlistsToProcess, summaries);

        Console.Success("Done! Updated {0} playlists.", processedCount);
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
            .AutoClear(true)
            .HideCompleted(false)
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
                    description: Console.TaskTitle($"Fetching video IDs (0/{playlistIds.Count})"),
                    maxValue: playlistIds.Count
                );

                foreach (var playlistId in playlistIds)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    var summary = summaryLookup[playlistId];
                    task.Description = Console.TaskTitle(summary.Title);

                    var videoIds = await youtubeService.GetPlaylistVideoIdsAsync(playlistId, ct);

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

                    task.Increment(1);
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
            var summary = summaryLookup[playlist.Id];
            state.PlaylistSnapshots[playlist.Id] = new PlaylistSnapshot(
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
            Console.Info("No playlists found.");
            Logger.End(success: true, "No playlists to sync");
            return;
        }

        if (!await FetchAllVideoIdsAsync(playlists))
            return;

        Console.Info(
            "[Phase 1/2] Video ID fetch complete - all {0} playlists ready",
            playlists.Count
        );
        Console.Info("[Phase 2/2] Starting sheet write phase (processes alphabetically)...");

        var playlistChanges = YouTubeChangeDetector.DetectPlaylistChanges(
            playlists,
            state.PlaylistSnapshots
        );

        if (!playlistChanges.HasChanges && state.FetchComplete)
        {
            Console.Success("No changes detected.");
            Logger.End(success: true, "No changes detected");
            return;
        }

        YouTubeChangeDetector.LogDetectedChanges(playlistChanges);

        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted("Interrupted before processing playlist changes");
            return;
        }

        ProcessDeletedPlaylists(playlistChanges.DeletedPlaylistIds, spreadsheetId);

        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted("Interrupted after processing deletions");
            return;
        }

        await WritePlaylistsToSheetsAsync(playlists, playlistChanges, spreadsheetId);
    }

    private async Task<List<YouTubePlaylist>?> GetOrFetchPlaylistMetadataAsync()
    {
        if (state.CachedPlaylists != null && state.CachedPlaylists.Count > 0)
        {
            var playlists = state.CachedPlaylists;
            var alreadyHaveVideoIds = state.VideoIdFetchIndex;
            var progressPercent = (int)((alreadyHaveVideoIds / (double)playlists.Count) * 100);
            var currentPlaylistTitle =
                alreadyHaveVideoIds < playlists.Count
                    ? playlists[alreadyHaveVideoIds].Title
                    : "(all playlists fetched)";
            Console.Info(
                "[Phase 1/2] Resuming video ID fetch: {0}/{1} ({2}%) - {3}",
                alreadyHaveVideoIds,
                playlists.Count,
                progressPercent,
                currentPlaylistTitle
            );
            Console.Debug("Using cached playlist metadata ({0} playlists)", playlists.Count);
            return playlists;
        }

        var freshPlaylists = await youtubeService.GetPlaylistMetadataAsync(ct);

        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted("Interrupted while fetching playlist metadata");
            return null;
        }

        state.CachedPlaylists = freshPlaylists;
        state.VideoIdFetchIndex = 0;
        SaveState();
        Console.Info("[Phase 1/2] Starting video ID fetch for {0} playlists", freshPlaylists.Count);
        Console.Debug("Cached playlist metadata for resume capability");
        return freshPlaylists;
    }

    private async Task<bool> FetchAllVideoIdsAsync(List<YouTubePlaylist> playlists)
    {
        if (state.VideoIdFetchIndex >= playlists.Count)
            return true;

        var interrupted = false;
        var interruptedAt = 0;
        var alreadyFetched = state.VideoIdFetchIndex;

        Console.Suppress = true;

        await AnsiConsole
            .Progress()
            .AutoClear(true)
            .HideCompleted(false)
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn(),
            ])
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(
                    description: Console.TaskTitle(
                        $"Fetching video IDs ({alreadyFetched}/{playlists.Count} done)"
                    ),
                    maxValue: playlists.Count
                );
                task.Value = alreadyFetched;

                for (var i = state.VideoIdFetchIndex; i < playlists.Count; i++)
                {
                    if (ct.IsCancellationRequested)
                    {
                        interrupted = true;
                        interruptedAt = i;
                        return;
                    }

                    var playlist = playlists[i];
                    task.Description = Console.TaskTitle(playlist.Title);

                    var videoIds = await youtubeService.GetPlaylistVideoIdsAsync(playlist.Id, ct);

                    if (ct.IsCancellationRequested)
                    {
                        interrupted = true;
                        interruptedAt = i;
                        return;
                    }

                    playlists[i] = playlist with { VideoIds = videoIds };
                    state.CachedPlaylists = playlists;
                    state.VideoIdFetchIndex = i + 1;
                    SaveState();

                    task.Increment(1);
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
        var isFirstPlaylist = state.PlaylistSnapshots.Count == 0;
        var firstPlaylistTitle = playlists.FirstOrDefault()?.Title;

        var playlistsToProcess = playlists
            .Where(p =>
                playlistChanges.NewPlaylistIds.Contains(p.Id)
                || playlistChanges.ModifiedPlaylistIds.Contains(p.Id)
                || !state.PlaylistSnapshots.ContainsKey(p.Id)
            )
            .ToList();

        var skippedCount = playlists.Count - playlistsToProcess.Count;
        var processedCount = 0;

        if (skippedCount > 0)
            Console.Info("Skipping {0} unchanged playlists", skippedCount);

        if (playlistsToProcess.Count > 0)
            processedCount = await ProcessPlaylistsWithProgressAsync(
                playlistsToProcess,
                spreadsheetId,
                isFirstPlaylist
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

        FinalizeSpreadsheet(spreadsheetId, firstPlaylistTitle);

        Console.Success(
            "Done! Synced {0} playlists ({1} processed, {2} unchanged).",
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
        if (!IsNullOrEmpty(firstPlaylistTitle))
        {
            var sanitizedFirst = SanitizeSheetName(firstPlaylistTitle);
            sheetsService.RenameSubsheet(spreadsheetId, oldName: "Sheet1", newName: sanitizedFirst);
        }
        else
        {
            sheetsService.CleanupDefaultSheet(spreadsheetId);
        }

        sheetsService.ReorderSheetsAlphabetically(spreadsheetId);
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
            .AutoClear(true)
            .HideCompleted(false)
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn(),
            ])
            .StartAsync(async ctx =>
            {
                foreach (var playlist in playlistsToProcess)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    var playlistCount = $"({processedCount + 1}/{playlistsToProcess.Count})";
                    var playlistVideoCount = playlist.VideoIds.Count;

                    var task = ctx.AddTask(
                        description: Console.TaskDescription(
                            playlistCount,
                            playlist.Title,
                            $"(0/{playlistVideoCount} videos)"
                        ),
                        maxValue: playlistVideoCount
                    );

                    await ProcessPlaylistWithContextAsync(
                        playlist,
                        spreadsheetId,
                        isFirstPlaylist && processedCount == 0,
                        (count) =>
                        {
                            task.Value = count;
                            task.Description = Console.TaskDescription(
                                playlistCount,
                                playlist.Title,
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
        var existingCache = StateManager.LoadPlaylistCache(playlist.Title);
        List<YouTubeVideo> previousVideos = [.. existingCache];

        if (state.CurrentPlaylistId == playlist.Id && existingCache.Count > 0)
        {
            alreadyFetched = state.CurrentPlaylistVideosFetched;
            videos = existingCache;
            onVideoProgress(alreadyFetched);
            Console.Debug(
                "Resuming '{0}': {1}/{2} videos already fetched from cache",
                playlist.Title,
                alreadyFetched,
                playlist.VideoIds.Count
            );
        }

        var remainingIds = playlist.VideoIds.Skip(alreadyFetched).ToList();
        var videosFetchedSoFar = alreadyFetched;

        var newVideos = await youtubeService.GetVideoDetailsForIdsAsync(
            videoIds: remainingIds,
            onBatchComplete: async (batchVideos) =>
            {
                videos.AddRange(batchVideos);
                videosFetchedSoFar += batchVideos.Count;

                StateManager.SavePlaylistCache(playlist.Title, videos);

                state.UpdatePlaylistProgress(
                    playlistId: playlist.Id,
                    videosFetched: videosFetchedSoFar
                );
                SaveState();
                Console.Debug(
                    "Cached: {0}/{1} video details for '{2}' (batch resume)",
                    videosFetchedSoFar,
                    playlist.VideoIds.Count,
                    playlist.Title
                );

                onVideoProgress(videosFetchedSoFar);
                await Task.CompletedTask;
            },
            ct: ct
        );

        if (ct.IsCancellationRequested)
            return;

        WritePlaylist(playlist, videos, previousVideos, spreadsheetId, isFirstPlaylist);

        PlaylistSnapshot snapshot = new(
            PlaylistId: playlist.Id,
            Title: playlist.Title,
            VideoIds: playlist.VideoIds,
            LastUpdated: DateTime.Now,
            ReportedVideoCount: playlist.VideoCount,
            ETag: playlist.ETag
        );
        state.PlaylistSnapshots[playlist.Id] = snapshot;
        state.ClearCurrentProgress();
        SaveState();
    }

    private string GetOrCreateSpreadsheet() =>
        sheetsService.GetOrCreateSpreadsheet(
            currentSpreadsheetId: state.SpreadsheetId,
            defaultSpreadsheetId: Config.YouTubeSpreadsheetId,
            spreadsheetTitle: Config.YouTubeSpreadsheetTitle,
            onSpreadsheetResolved: id =>
            {
                state.SpreadsheetId = id;
                SaveState();
            }
        );

    internal void SaveState() => StateManager.Save(StateManager.YouTubeSyncFile, state);

    private static void ArchiveDeletedPlaylist(PlaylistSnapshot snapshot)
    {
        var archivedPath = StateManager.ArchivePlaylistCache(snapshot.Title);

        Console.Warning("Playlist deleted: {0}", snapshot.Title);
        Console.Info("Archived to: {0}", archivedPath);
    }

    private void WritePlaylist(
        YouTubePlaylist playlist,
        List<YouTubeVideo> videos,
        List<YouTubeVideo> previousVideos,
        string spreadsheetId,
        bool isFirstPlaylist
    )
    {
        var sheetName = SanitizeSheetName(playlist.Title);
        var existingSnapshot = state.PlaylistSnapshots.GetValueOrDefault(playlist.Id);

        if (isFirstPlaylist)
        {
            sheetsService.RenameSubsheet(spreadsheetId, oldName: "Sheet1", newName: sheetName);
            WriteFullPlaylist(sheetName, videos, spreadsheetId);
            return;
        }

        if (existingSnapshot == null)
        {
            sheetsService.EnsureSubsheetExists(spreadsheetId, sheetName, VideoHeaders);
            WriteFullPlaylist(sheetName, videos, spreadsheetId);
            return;
        }

        var videoChanges = YouTubeChangeDetector.DetectVideoChanges(
            playlist.VideoIds,
            existingSnapshot.VideoIds
        );

        var removedTitles = videoChanges
            .RemovedVideoIds.Select(id =>
                previousVideos.FirstOrDefault(v => v.VideoId == id)?.Title
            )
            .Where(t => t != null)
            .Cast<string>()
            .ToList();

        var addedVideos = videos
            .Where(v => videoChanges.AddedVideoIds.Contains(v.VideoId))
            .ToList();
        var addedTitles = addedVideos.Select(v => v.Title).ToList();

        if (videoChanges.RequiresFullRewrite)
        {
            Console.Debug("Order changed in '{0}', full rewrite required", playlist.Title);
            WriteFullPlaylist(sheetName, videos, spreadsheetId);
            Logger.PlaylistUpdated(
                playlist.Title,
                addedTitles.Count,
                removedTitles.Count,
                addedTitles,
                removedTitles,
                videoChanges.RemovedVideoIds
            );
            return;
        }

        if (!videoChanges.HasChanges)
        {
            Console.Debug("No video changes in '{0}'", playlist.Title);
            Console.Debug("Skipped playlist: " + playlist.Title);
            return;
        }

        var removedSet = videoChanges.RemovedVideoIds.ToHashSet();

        Logger.PlaylistUpdated(
            playlist.Title,
            addedTitles.Count,
            removedTitles.Count,
            addedTitles,
            removedTitles,
            videoChanges.RemovedVideoIds
        );

        if (videoChanges.RemovedRowIndices.Count > 0)
            sheetsService.DeleteRowsFromSubsheet(
                spreadsheetId,
                sheetName,
                videoChanges.RemovedRowIndices
            );

        if (addedVideos.Count > 0)
            sheetsService.AppendRecords(spreadsheetId, sheetName, addedVideos, MapVideoToRow);

        var updatedVideos = previousVideos
            .Where(v => !removedSet.Contains(v.VideoId))
            .Concat(videos.Where(v => videoChanges.AddedVideoIds.Contains(v.VideoId)))
            .ToList();
        StateManager.SavePlaylistCache(playlist.Title, updatedVideos);
    }

    private void WriteFullPlaylist(
        string sheetName,
        List<YouTubeVideo> videos,
        string spreadsheetId
    )
    {
        Console.Debug("Full write: {0} videos to '{1}'", videos.Count, sheetName);

        sheetsService.WriteRecords(spreadsheetId, sheetName, VideoHeaders, videos, MapVideoToRow);
    }

    private static IList<object> MapVideoToRow(YouTubeVideo v) =>
        [
            $"=HYPERLINK(\"{v.VideoUrl}\", \"{EscapeFormulaString(v.Title)}\")",
            v.Description,
            $"=HYPERLINK(\"{v.ChannelUrl}\", \"{EscapeFormulaString(v.ChannelName)}\")",
            v.FormattedDuration,
        ];

    private static string SanitizeSheetName(string name) =>
        name.Replace(":", " -")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("?", "")
            .Replace("*", "")
            .Replace("[", "(")
            .Replace("]", ")");

    private static string EscapeFormulaString(string value) => value.Replace("\"", "\"\"");

    public static void ExportSheetsAsCSVs(
        string outputDirectory = "YouTube Playlists",
        CancellationToken ct = default
    )
    {
        var state = StateManager.Load<YouTubeFetchState>(StateManager.YouTubeSyncFile);

        if (IsNullOrEmpty(state.SpreadsheetId))
            throw new InvalidOperationException(
                "No YouTube spreadsheet found. Run sync first to create it."
            );

        var desktopPath = GetFolderPath(SpecialFolder.Desktop);
        var fullOutputPath = Combine(desktopPath, outputDirectory);

        var sheetsService = new GoogleSheetsService(
            clientId: Config.GoogleClientId,
            clientSecret: Config.GoogleClientSecret
        );

        var exported = sheetsService.ExportEachSheetAsCSV(
            spreadsheetId: state.SpreadsheetId,
            outputDirectory: fullOutputPath,
            ct: ct
        );

        if (exported > 0)
            Console.Success(
                "Exported {0} playlists to: {1}",
                exported,
                GetFullPath(fullOutputPath)
            );
        else
            Console.Info("All playlists already exported to: {0}", GetFullPath(fullOutputPath));
    }

    public static async Task CountPlaylistsAsync(CancellationToken ct = default)
    {
        var youtubeService = new YouTubeService(
            clientId: Config.GoogleClientId,
            clientSecret: Config.GoogleClientSecret
        );

        var playlists = await youtubeService.GetPlaylistSummariesAsync(ct: ct);
        Console.Info("Playlists: {0}", playlists.Count);
    }

    public void Dispose()
    {
        youtubeService?.Dispose();
        sheetsService?.Dispose();
        GC.SuppressFinalize(this);
    }
}
