namespace CSharpScripts.Orchestrators;

public class YouTubePlaylistOrchestrator(CancellationToken ct)
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

    internal void Execute()
    {
        StateManager.MigratePlaylistFiles(state.PlaylistSnapshots);

        var spreadsheetId = GetOrCreateSpreadsheet();

        if (state.FetchComplete && state.PlaylistSnapshots.Count > 0)
        {
            ExecuteOptimized(spreadsheetId);
            return;
        }

        ExecuteFullSync(spreadsheetId);
    }

    internal void ExecuteForPlaylists(string[] playlistIdentifiers)
    {
        Console.Debug("Selective sync initiated for {0} playlist(s)", playlistIdentifiers.Length);

        var spreadsheetId = GetOrCreateSpreadsheet();
        var resolvedPlaylists = ResolvePlaylistIdentifiers(playlistIdentifiers);

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
        var processedCount = ProcessPlaylistsWithProgress(
            resolvedPlaylists,
            spreadsheetId,
            isFirstPlaylist
        );

        if (!ct.IsCancellationRequested)
        {
            Console.Success("Done! Synced {0} playlist(s).", processedCount);
            Console.Link(GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId), "Open spreadsheet");
            Logger.End(success: true, $"Synced {processedCount} playlists (selective)");
        }
        else
        {
            Logger.Interrupted("Interrupted during selective sync");
        }
    }

    private List<YouTubePlaylist> ResolvePlaylistIdentifiers(string[] identifiers)
    {
        List<YouTubePlaylist> resolved = [];

        foreach (var identifier in identifiers)
        {
            if (ct.IsCancellationRequested)
                break;

            var playlist = ResolvePlaylistIdentifier(identifier);
            if (playlist != null)
                resolved.Add(playlist);
            else
                Console.Warning("Could not resolve: {0}", identifier);
        }

        return resolved;
    }

    private YouTubePlaylist? ResolvePlaylistIdentifier(string identifier)
    {
        if (state.PlaylistSnapshots.TryGetValue(identifier, out var snapshot))
        {
            Console.Debug("Resolved '{0}' from cached snapshot", identifier);
            return new YouTubePlaylist(
                Id: snapshot.PlaylistId,
                Title: snapshot.Title,
                VideoCount: snapshot.ReportedVideoCount,
                VideoIds: youtubeService.GetPlaylistVideoIds(snapshot.PlaylistId, ct),
                ETag: snapshot.ETag
            );
        }

        var titleMatch = state.PlaylistSnapshots.Values.FirstOrDefault(s =>
            s.Title.Equals(identifier, StringComparison.OrdinalIgnoreCase)
        );

        if (titleMatch != null)
        {
            Console.Debug("Resolved '{0}' by title match", identifier);
            return new YouTubePlaylist(
                Id: titleMatch.PlaylistId,
                Title: titleMatch.Title,
                VideoCount: titleMatch.ReportedVideoCount,
                VideoIds: youtubeService.GetPlaylistVideoIds(titleMatch.PlaylistId, ct),
                ETag: titleMatch.ETag
            );
        }

        if (
            identifier.StartsWith("PL")
            || identifier.StartsWith("UU")
            || identifier.StartsWith("FL")
        )
        {
            Console.Debug("Fetching playlist by ID: {0}", identifier);
            var videoIds = youtubeService.GetPlaylistVideoIds(identifier, ct);
            if (videoIds.Count > 0)
                if (youtubeService.GetPlaylistSummary(identifier, ct) is { } summary)
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

    private void ExecuteOptimized(string spreadsheetId)
    {
        Console.Debug("Last change: {0:yyyy/MM/dd HH:mm:ss}", state.LastUpdated);
        state.LastChecked = DateTime.Now;
        state.LastUpdated = DateTime.Now;
        SaveState();

        var summaries = youtubeService.GetPlaylistSummaries(ct);

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
            Console.Link(GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId), "Open spreadsheet");
            Logger.End(success: true, "No changes detected");
            return;
        }

        YouTubeChangeDetector.LogOptimizedChanges(changes);

        foreach (var deletedId in changes.DeletedIds)
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
                SaveState();
            }
        }

        foreach (var rename in changes.Renamed)
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
                SaveState();
            }
        }

        var playlistsNeedingVideoFetch = changes.NewIds.Concat(changes.ModifiedIds).ToList();

        if (playlistsNeedingVideoFetch.Count > 0)
        {
            Console.Debug(
                "Fetching details for {0} changed playlists...",
                playlistsNeedingVideoFetch.Count
            );

            List<YouTubePlaylist> playlistsToProcess = [];

            Console.Suppress = true;

            AnsiConsole
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
                .Start(ctx =>
                {
                    var task = ctx.AddTask(
                        description: $"[cyan]Fetching video IDs (0/{playlistsNeedingVideoFetch.Count})[/]",
                        maxValue: playlistsNeedingVideoFetch.Count
                    );

                    foreach (var playlistId in playlistsNeedingVideoFetch)
                    {
                        if (ct.IsCancellationRequested)
                            break;

                        var summary = summaries.First(s => s.Id == playlistId);
                        task.Description = $"[cyan]{Markup.Escape(summary.Title)}[/]";

                        var videoIds = youtubeService.GetPlaylistVideoIds(playlistId, ct);

                        if (ct.IsCancellationRequested)
                            break;

                        playlistsToProcess.Add(
                            new YouTubePlaylist(
                                Id: playlistId,
                                Title: summary.Title,
                                VideoCount: summary.VideoCount,
                                VideoIds: videoIds
                            )
                        );

                        task.Increment(1);
                    }
                });

            Console.Suppress = false;

            if (!ct.IsCancellationRequested && playlistsToProcess.Count > 0)
            {
                var isFirstPlaylist = state.PlaylistSnapshots.Count == 0;
                var processedCount = ProcessPlaylistsWithProgress(
                    playlistsToProcess,
                    spreadsheetId,
                    isFirstPlaylist
                );

                foreach (var playlist in playlistsToProcess)
                {
                    var summary = summaries.First(s => s.Id == playlist.Id);
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

                Console.Success("Done! Updated {0} playlists.", processedCount);
                Logger.End(success: true, $"Updated {processedCount} playlists");
            }
        }
        else
        {
            Console.Success("Done! Only metadata changes applied.");
            Logger.End(success: true, "Metadata updates only");
        }

        if (!ct.IsCancellationRequested)
            Console.Link(GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId), "Open spreadsheet");
        else
            Logger.Interrupted("Interrupted during sync");
    }

    private void ExecuteFullSync(string spreadsheetId)
    {
        List<YouTubePlaylist> playlists;

        if (state.CachedPlaylists != null && state.CachedPlaylists.Count > 0)
        {
            playlists = state.CachedPlaylists;
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
        }
        else
        {
            playlists = youtubeService.GetPlaylistMetadata(ct);

            if (ct.IsCancellationRequested)
            {
                Logger.Interrupted("Interrupted while fetching playlist metadata");
                return;
            }

            state.CachedPlaylists = playlists;
            state.VideoIdFetchIndex = 0;
            SaveState();
            Console.Info("[Phase 1/2] Starting video ID fetch for {0} playlists", playlists.Count);
            Console.Debug("Cached playlist metadata for resume capability");
        }

        if (playlists.Count == 0)
        {
            Console.Info("No playlists found.");
            Logger.End(success: true, "No playlists to sync");
            return;
        }

        var needVideoIdFetch = state.VideoIdFetchIndex < playlists.Count;
        if (needVideoIdFetch)
        {
            var interrupted = false;
            var interruptedAt = 0;
            var alreadyFetched = state.VideoIdFetchIndex;

            AnsiConsole
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
                .Start(ctx =>
                {
                    var task = ctx.AddTask(
                        description: $"[cyan]Fetching video IDs ({alreadyFetched}/{playlists.Count} done)[/]",
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
                        task.Description = $"[cyan]{Markup.Escape(playlist.Title)}[/]";

                        var videoIds = youtubeService.GetPlaylistVideoIds(playlist.Id, ct);

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
                        Console.Debug(
                            "Cached: {0} video IDs for '{1}' (resume index: {2})",
                            videoIds.Count,
                            playlist.Title,
                            i + 1
                        );

                        task.Increment(1);
                    }

                    task.Value = task.MaxValue;
                });

            if (interrupted)
            {
                Logger.Interrupted(
                    $"Video ID fetch interrupted at {interruptedAt}/{playlists.Count} playlists"
                );
                return;
            }
        }

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
            Console.Link(GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId), "Open spreadsheet");
            Logger.End(success: true, "No changes detected");
            return;
        }

        YouTubeChangeDetector.LogDetectedChanges(playlistChanges);

        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted("Interrupted before processing playlist changes");
            return;
        }

        foreach (var deletedId in playlistChanges.DeletedPlaylistIds)
        {
            if (ct.IsCancellationRequested)
                break;

            var snapshot = state.PlaylistSnapshots.GetValueOrDefault(deletedId);
            if (snapshot != null)
            {
                ArchiveDeletedPlaylist(snapshot);
                var sheetName = SanitizeSheetName(snapshot.Title);
                sheetsService.DeleteSubsheet(spreadsheetId, sheetName);
                Logger.PlaylistDeleted(snapshot.Title, snapshot.VideoIds.Count);
                state.PlaylistSnapshots.Remove(deletedId);
                SaveState();
            }
        }

        if (ct.IsCancellationRequested)
        {
            Logger.Interrupted("Interrupted after processing deletions");
            return;
        }

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
            processedCount = ProcessPlaylistsWithProgress(
                playlistsToProcess,
                spreadsheetId,
                isFirstPlaylist
            );

        if (!ct.IsCancellationRequested)
        {
            state.FetchComplete = true;
            state.CachedPlaylists = null;
            SaveState();

            if (!IsNullOrEmpty(firstPlaylistTitle))
            {
                var sanitizedFirst = SanitizeSheetName(firstPlaylistTitle);
                sheetsService.RenameSubsheet(
                    spreadsheetId,
                    oldName: "Sheet1",
                    newName: sanitizedFirst
                );
            }
            else
            {
                sheetsService.CleanupDefaultSheet(spreadsheetId);
            }

            sheetsService.ReorderSheetsAlphabetically(spreadsheetId);

            Console.Success(
                "Done! Synced {0} playlists ({1} processed, {2} unchanged).",
                playlists.Count,
                processedCount,
                playlists.Count - processedCount
            );
            Console.Link(GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId), "Open spreadsheet");
            Logger.End(
                success: true,
                $"Synced {playlists.Count} playlists ({processedCount} updated, {playlists.Count - processedCount} unchanged)"
            );
        }
        else
        {
            Logger.Interrupted(
                $"{state.PlaylistSnapshots.Count} playlists completed before interrupt"
            );
        }
    }

    private int ProcessPlaylistsWithProgress(
        List<YouTubePlaylist> playlistsToProcess,
        string spreadsheetId,
        bool isFirstPlaylist
    )
    {
        var processedCount = 0;
        var totalVideos = playlistsToProcess.Sum(p => p.VideoIds.Count);
        var videosProcessed = 0;

        AnsiConsole
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
            .Start(ctx =>
            {
                var overallTask = ctx.AddTask(
                    description: $"[cyan]Syncing {playlistsToProcess.Count} playlists[/]",
                    maxValue: totalVideos
                );

                foreach (var playlist in playlistsToProcess)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    var playlistCount = $"({processedCount + 1}/{playlistsToProcess.Count})";
                    overallTask.Description =
                        $"[dim]{playlistCount}[/] [cyan]{Markup.Escape(playlist.Title)}[/]";

                    ProcessPlaylistWithContext(
                        playlist,
                        spreadsheetId,
                        isFirstPlaylist && processedCount == 0,
                        (count) =>
                        {
                            var currentVideos = videosProcessed + count;
                            overallTask.Value = currentVideos;
                            var videoCount = $"({currentVideos}/{totalVideos})";
                            overallTask.Description =
                                $"[dim]{playlistCount}[/] [cyan]{Markup.Escape(playlist.Title)}[/] [dim]{videoCount}[/]";
                        }
                    );

                    videosProcessed += playlist.VideoIds.Count;
                    overallTask.Value = videosProcessed;
                    processedCount++;
                }

                overallTask.Value = overallTask.MaxValue;
            });

        return processedCount;
    }

    private void ProcessPlaylistWithContext(
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

        var newVideos = youtubeService.GetVideoDetailsForIds(
            videoIds: remainingIds,
            onBatchComplete: (batchVideos) =>
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
            Logger.PlaylistCreated(playlist.Title, videos.Count);
            return;
        }

        if (existingSnapshot == null)
        {
            sheetsService.EnsureSubsheetExists(spreadsheetId, sheetName, VideoHeaders);
            WriteFullPlaylist(sheetName, videos, spreadsheetId);
            Logger.PlaylistCreated(playlist.Title, videos.Count);
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
        CancellationToken ct = default,
        string outputDirectory = "YouTube Playlists"
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

    public static void CountPlaylists(CancellationToken ct = default)
    {
        var youtubeService = new YouTubeService(
            clientId: Config.GoogleClientId,
            clientSecret: Config.GoogleClientSecret
        );

        var playlists = youtubeService.GetPlaylistSummaries(ct: ct);
        Console.Info("Playlists: {0}", playlists.Count);
    }
}
