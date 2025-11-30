namespace CSharpScripts.Orchestration.YouTube;

internal class YouTubePlaylistOrchestrator(CancellationToken ct)
{
    static readonly IReadOnlyList<object> VideoHeaders =
    [
        "Title",
        "Description",
        "Duration",
        "Channel",
        "Video URL",
    ];

    readonly YouTubeService youtubeService = new(
        clientId: AuthenticationConfig.GoogleClientId,
        clientSecret: AuthenticationConfig.GoogleClientSecret
    );

    readonly GoogleSheetsService sheetsService = new(
        clientId: AuthenticationConfig.GoogleClientId,
        clientSecret: AuthenticationConfig.GoogleClientSecret
    );

    readonly YouTubeFetchState state = StateManager.Load<YouTubeFetchState>(
        StateManager.YouTubeStateFile
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
        Logger.Info("Selective sync initiated for {0} playlist(s)", playlistIdentifiers.Length);

        var spreadsheetId = GetOrCreateSpreadsheet();
        var resolvedPlaylists = ResolvePlaylistIdentifiers(playlistIdentifiers);

        if (resolvedPlaylists.Count == 0)
        {
            Logger.Error("No matching playlists found for the provided identifiers.");
            Logger.End(success: false, "No matching playlists");
            return;
        }

        Logger.Info("Syncing {0} playlist(s):", resolvedPlaylists.Count);
        foreach (var playlist in resolvedPlaylists)
            Logger.Info("  • {0}", playlist.Title);

        var isFirstPlaylist = state.PlaylistSnapshots.Count == 0;
        var processedCount = ProcessPlaylistsWithProgress(
            resolvedPlaylists,
            spreadsheetId,
            isFirstPlaylist
        );

        if (!ct.IsCancellationRequested)
        {
            Logger.Success("Done! Synced {0} playlist(s).", processedCount);
            Logger.Link(GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId), "Open spreadsheet");
            Logger.End(success: true, $"Synced {processedCount} playlists (selective)");
        }
        else
        {
            Logger.Interrupted("Interrupted during selective sync");
        }
    }

    List<YouTubePlaylist> ResolvePlaylistIdentifiers(string[] identifiers)
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
                Logger.Warning("Could not resolve: {0}", identifier);
        }

        return resolved;
    }

    YouTubePlaylist? ResolvePlaylistIdentifier(string identifier)
    {
        if (state.PlaylistSnapshots.TryGetValue(identifier, out var snapshot))
        {
            Logger.Debug("Resolved '{0}' from cached snapshot", identifier);
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
            Logger.Debug("Resolved '{0}' by title match", identifier);
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
            Logger.Debug("Fetching playlist by ID: {0}", identifier);
            var videoIds = youtubeService.GetPlaylistVideoIds(identifier, ct);
            if (videoIds.Count > 0)
            {
                if (youtubeService.GetPlaylistSummary(identifier, ct) is { } summary)
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

    void ExecuteOptimized(string spreadsheetId)
    {
        Logger.Info("Checking for changes since {0:yyyy/MM/dd HH:mm:ss}", state.LastUpdated);

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
            Logger.Info("No changes detected. Everything is up to date.");
            Logger.Link(GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId), "Open spreadsheet");
            Logger.End(success: true, "Already up to date (optimized check)");
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
                state.PlaylistSnapshots.Remove(deletedId);
                SaveState();
            }
        }

        foreach (var rename in changes.Renamed)
        {
            if (ct.IsCancellationRequested)
                break;

            Logger.Info("Renaming: {0} → {1}", rename.OldTitle, rename.NewTitle);

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
            Logger.Info(
                "Fetching details for {0} changed playlists...",
                playlistsNeedingVideoFetch.Count
            );

            List<YouTubePlaylist> playlistsToProcess = [];

            Logger.SuppressConsole = true;

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

            Logger.SuppressConsole = false;

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

                Logger.Success("Done! Updated {0} playlists.", processedCount);
                Logger.End(success: true, $"Updated {processedCount} playlists (optimized)");
            }
        }
        else
        {
            Logger.Success("Done! Only metadata changes applied.");
            Logger.End(success: true, "Metadata updates only");
        }

        if (!ct.IsCancellationRequested)
        {
            Logger.Link(GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId), "Open spreadsheet");
        }
        else
        {
            Logger.Interrupted("Interrupted during optimized sync");
        }
    }

    void ExecuteFullSync(string spreadsheetId)
    {
        List<YouTubePlaylist> playlists;

        if (state.CachedPlaylists != null && state.CachedPlaylists.Count > 0)
        {
            playlists = state.CachedPlaylists;
            var alreadyHaveVideoIds = state.VideoIdFetchIndex;
            Logger.Info(
                "Resuming with {0} cached playlists ({1} have video IDs)",
                playlists.Count,
                alreadyHaveVideoIds
            );
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
            Logger.Info("Fetched {0} playlists from YouTube API", playlists.Count);
        }

        if (playlists.Count == 0)
        {
            Logger.Info("No playlists found.");
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

        var playlistChanges = YouTubeChangeDetector.DetectPlaylistChanges(
            playlists,
            state.PlaylistSnapshots
        );

        if (!playlistChanges.HasChanges && state.FetchComplete)
        {
            Logger.Info("All playlists already synced. No changes detected.");
            Logger.Link(GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId), "Open spreadsheet");
            Logger.End(success: true, "Already up to date");
            return;
        }

        YouTubeChangeDetector.LogDetectedChanges(playlistChanges);

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
                state.PlaylistSnapshots.Remove(deletedId);
                SaveState();
            }
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
            Logger.Info("Skipping {0} unchanged playlists", skippedCount);

        if (playlistsToProcess.Count > 0)
        {
            processedCount = ProcessPlaylistsWithProgress(
                playlistsToProcess,
                spreadsheetId,
                isFirstPlaylist
            );
        }

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

            Logger.Success(
                "Done! Synced {0} playlists ({1} processed, {2} unchanged).",
                playlists.Count,
                processedCount,
                playlists.Count - processedCount
            );
            Logger.Link(GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId), "Open spreadsheet");
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

    int ProcessPlaylistsWithProgress(
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

                    overallTask.Description = $"[cyan]{Markup.Escape(playlist.Title)}[/]";

                    ProcessPlaylistWithContext(
                        playlist: playlist,
                        spreadsheetId: spreadsheetId,
                        isFirstPlaylist: isFirstPlaylist && processedCount == 0,
                        onVideoProgress: (count) =>
                        {
                            overallTask.Value = videosProcessed + count;
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

    void ProcessPlaylistWithContext(
        YouTubePlaylist playlist,
        string spreadsheetId,
        bool isFirstPlaylist,
        Action<int> onVideoProgress
    )
    {
        var alreadyFetched = 0;
        List<YouTubeVideo> videos = [];
        var existingCache = StateManager.LoadPlaylistCache(playlist.Title);

        if (state.CurrentPlaylistId == playlist.Id && existingCache.Count > 0)
        {
            alreadyFetched = state.CurrentPlaylistVideosFetched;
            videos = existingCache;
            onVideoProgress(alreadyFetched);
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

                onVideoProgress(videosFetchedSoFar);
            },
            ct: ct
        );

        if (ct.IsCancellationRequested)
            return;

        WritePlaylist(playlist, videos, spreadsheetId, isFirstPlaylist);

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

    string GetOrCreateSpreadsheet() =>
        sheetsService.GetOrCreateSpreadsheet(
            currentSpreadsheetId: state.SpreadsheetId,
            defaultSpreadsheetId: SpreadsheetConfig.YouTubeSpreadsheetId,
            spreadsheetTitle: SpreadsheetConfig.YouTubeSpreadsheetTitle,
            onSpreadsheetResolved: id =>
            {
                state.SpreadsheetId = id;
                SaveState();
            }
        );

    internal void SaveState() => StateManager.Save(StateManager.YouTubeStateFile, state);

    static void ArchiveDeletedPlaylist(PlaylistSnapshot snapshot)
    {
        var archivedPath = StateManager.ArchivePlaylistCache(snapshot.Title);

        Logger.Warning("Playlist deleted: {0}", snapshot.Title);
        Logger.Info("Archived to: {0}", archivedPath);
    }

    void WritePlaylist(
        YouTubePlaylist playlist,
        List<YouTubeVideo> videos,
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
            currentVideoIds: playlist.VideoIds,
            storedVideoIds: existingSnapshot.VideoIds
        );

        if (videoChanges.RequiresFullRewrite)
        {
            Logger.Debug("Order changed in '{0}', full rewrite required", playlist.Title);
            WriteFullPlaylist(sheetName, videos, spreadsheetId);
            return;
        }

        if (!videoChanges.HasChanges)
        {
            Logger.Debug("No video changes in '{0}'", playlist.Title);
            return;
        }

        Logger.Info(
            "Incremental update for '{0}': +{1} -{2}",
            playlist.Title,
            videoChanges.AddedVideoIds.Count,
            videoChanges.RemovedVideoIds.Count
        );

        var existingVideos = StateManager.LoadPlaylistCache(playlist.Title);
        var removedSet = videoChanges.RemovedVideoIds.ToHashSet();

        foreach (var removedId in videoChanges.RemovedVideoIds)
        {
            var removedVideo = existingVideos.FirstOrDefault(v => v.VideoId == removedId);
            if (removedVideo != null)
                Logger.Debug("  Removed: {0}", removedVideo.Title);
        }

        if (videoChanges.RemovedRowIndices.Count > 0)
        {
            sheetsService.DeleteRowsFromSubsheet(
                spreadsheetId,
                sheetName,
                videoChanges.RemovedRowIndices
            );
        }

        if (videoChanges.AddedVideoIds.Count > 0)
        {
            var addedVideos = videos
                .Where(v => videoChanges.AddedVideoIds.Contains(v.VideoId))
                .ToList();

            foreach (var addedVideo in addedVideos)
                Logger.Debug("  Added: {0}", addedVideo.Title);

            var rows = addedVideos
                .Select(v =>
                    (IList<object>)
                        [
                            v.Title,
                            v.Description,
                            v.FormattedDuration,
                            $"=HYPERLINK(\"{v.ChannelUrl}\", \"{EscapeFormulaString(v.ChannelName)}\")",
                            v.VideoUrl,
                        ]
                )
                .ToList();

            sheetsService.AppendRows(spreadsheetId, sheetName, rows);
        }

        var updatedVideos = existingVideos
            .Where(v => !removedSet.Contains(v.VideoId))
            .Concat(videos.Where(v => videoChanges.AddedVideoIds.Contains(v.VideoId)))
            .ToList();
        StateManager.SavePlaylistCache(playlist.Title, updatedVideos);
    }

    void WriteFullPlaylist(string sheetName, List<YouTubeVideo> videos, string spreadsheetId)
    {
        Logger.Debug("Full write: {0} videos to '{1}'", videos.Count, sheetName);

        sheetsService.ClearSubsheet(spreadsheetId, sheetName);
        sheetsService.WriteRows(
            spreadsheetId,
            sheetName,
            [
                [.. VideoHeaders],
            ]
        );

        var rows = videos
            .Select(v =>
                (IList<object>)
                    [
                        v.Title,
                        v.Description,
                        v.FormattedDuration,
                        $"=HYPERLINK(\"{v.ChannelUrl}\", \"{EscapeFormulaString(v.ChannelName)}\")",
                        v.VideoUrl,
                    ]
            )
            .ToList();

        sheetsService.WriteRows(spreadsheetId, sheetName, rows);
    }

    static string SanitizeSheetName(string name) =>
        name.Replace(":", " -")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("?", "")
            .Replace("*", "")
            .Replace("[", "(")
            .Replace("]", ")");

    static string EscapeFormulaString(string value) => value.Replace("\"", "\"\"");

    internal static void ExportSheetsAsCSVs(
        CancellationToken ct = default,
        string outputDirectory = "YouTube Playlists"
    )
    {
        var state = StateManager.Load<YouTubeFetchState>(StateManager.YouTubeStateFile);

        if (IsNullOrEmpty(state.SpreadsheetId))
            throw new InvalidOperationException(
                "No YouTube spreadsheet found. Run sync first to create it."
            );

        var desktopPath = GetFolderPath(SpecialFolder.Desktop);
        var fullOutputPath = Combine(desktopPath, outputDirectory);

        var sheetsService = new GoogleSheetsService(
            clientId: AuthenticationConfig.GoogleClientId,
            clientSecret: AuthenticationConfig.GoogleClientSecret
        );

        var exported = sheetsService.ExportEachSheetAsCSV(
            spreadsheetId: state.SpreadsheetId,
            outputDirectory: fullOutputPath,
            ct: ct
        );

        if (exported > 0)
            Logger.Success("Exported {0} playlists to: {1}", exported, GetFullPath(fullOutputPath));
        else
            Logger.Info("All playlists already exported to: {0}", GetFullPath(fullOutputPath));
    }

    internal static void CountPlaylists(CancellationToken ct = default)
    {
        var youtubeService = new YouTubeService(
            clientId: AuthenticationConfig.GoogleClientId,
            clientSecret: AuthenticationConfig.GoogleClientSecret
        );

        var playlists = youtubeService.GetPlaylistSummaries(ct: ct);
        Logger.Info("Playlists: {0}", playlists.Count);
    }
}
