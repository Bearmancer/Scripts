namespace CSharpScripts.Orchestration.YouTube;

internal record VideoChanges(
    List<string> AddedVideoIds,
    List<int> RemovedRowIndices,
    bool RequiresFullRewrite
)
{
    internal bool HasChanges =>
        AddedVideoIds.Count > 0 || RemovedRowIndices.Count > 0 || RequiresFullRewrite;
}

internal static class YouTubeChangeDetector
{
    internal static VideoChanges DetectVideoChanges(
        List<string> currentVideoIds,
        List<string> storedVideoIds
    )
    {
        var currentSet = currentVideoIds.ToHashSet();
        var storedSet = storedVideoIds.ToHashSet();

        var addedIds = currentVideoIds.Where(id => !storedSet.Contains(id)).ToList();

        List<int> removedIndices = [];
        for (var i = 0; i < storedVideoIds.Count; i++)
        {
            if (!currentSet.Contains(storedVideoIds[i]))
                removedIndices.Add(i + 2);
        }

        var requiresFullRewrite = false;
        if (addedIds.Count == 0 && removedIndices.Count == 0)
        {
            for (var i = 0; i < Math.Min(currentVideoIds.Count, storedVideoIds.Count); i++)
            {
                if (currentVideoIds[i] != storedVideoIds[i])
                {
                    requiresFullRewrite = true;
                    break;
                }
            }
        }

        return new VideoChanges(addedIds, removedIndices, requiresFullRewrite);
    }

    internal static PlaylistChanges DetectPlaylistChanges(
        List<YouTubePlaylist> currentPlaylists,
        Dictionary<string, PlaylistSnapshot> snapshots
    )
    {
        Logger.Debug("=== DETECTING CHANGES ===");
        Logger.Debug("Current playlists from API: {0}", currentPlaylists.Count);
        Logger.Debug("Saved snapshots in state: {0}", snapshots.Count);

        var currentIds = currentPlaylists.Select(p => p.Id).ToHashSet();
        var previousIds = snapshots.Keys.ToHashSet();

        var newIds = currentIds.Except(previousIds).ToList();
        var deletedIds = previousIds.Except(currentIds).ToList();
        List<string> modifiedIds = [];

        Logger.Debug("New playlist IDs (not in snapshots): {0}", newIds.Count);
        Logger.Debug("Deleted playlist IDs (in snapshots but not API): {0}", deletedIds.Count);

        foreach (var playlist in currentPlaylists)
        {
            if (!snapshots.TryGetValue(playlist.Id, out var snapshot))
            {
                Logger.Debug("  No snapshot for: {0} (ID: {1})", playlist.Title, playlist.Id);
                continue;
            }

            var currentVideoIds = playlist.VideoIds.ToHashSet();
            var storedVideoIds = snapshot.VideoIds.ToHashSet();
            var hasContentChanges = !currentVideoIds.SetEquals(storedVideoIds);

            if (hasContentChanges)
            {
                modifiedIds.Add(playlist.Id);
                var added = currentVideoIds.Except(storedVideoIds).Count();
                var removed = storedVideoIds.Except(currentVideoIds).Count();
                Logger.Debug("  MODIFIED: {0} (+{1} -{2} videos)", playlist.Title, added, removed);
            }
            else
            {
                Logger.Debug("  UNCHANGED: {0} ({1} videos)", playlist.Title, playlist.VideoCount);
            }
        }

        Logger.Debug("Modified playlists: {0}", modifiedIds.Count);
        return new PlaylistChanges(newIds, deletedIds, modifiedIds);
    }

    internal static void LogDetectedChanges(PlaylistChanges changes)
    {
        if (changes.NewPlaylistIds.Count > 0)
            Logger.Info("New playlists: {0}", changes.NewPlaylistIds.Count);

        if (changes.DeletedPlaylistIds.Count > 0)
            Logger.Info("Deleted playlists: {0}", changes.DeletedPlaylistIds.Count);

        if (changes.ModifiedPlaylistIds.Count > 0)
            Logger.Info("Modified playlists: {0}", changes.ModifiedPlaylistIds.Count);
    }

    internal static OptimizedChanges DetectOptimizedChanges(
        List<PlaylistSummary> currentSummaries,
        Dictionary<string, PlaylistSnapshot> snapshots
    )
    {
        var currentIds = currentSummaries.Select(s => s.Id).ToHashSet();
        var previousIds = snapshots.Keys.ToHashSet();

        var newIds = currentIds.Except(previousIds).ToList();
        var deletedIds = previousIds.Except(currentIds).ToList();
        List<string> modifiedIds = [];
        List<PlaylistRename> renamed = [];

        foreach (var summary in currentSummaries)
        {
            if (!snapshots.TryGetValue(summary.Id, out var snapshot))
                continue;

            if (snapshot.Title != summary.Title)
                renamed.Add(
                    new PlaylistRename(
                        PlaylistId: summary.Id,
                        OldTitle: snapshot.Title,
                        NewTitle: summary.Title
                    )
                );

            var countChanged = snapshot.ReportedVideoCount != summary.VideoCount;
            var etagChanged =
                !IsNullOrEmpty(snapshot.ETag)
                && !IsNullOrEmpty(summary.ETag)
                && snapshot.ETag != summary.ETag;

            if (countChanged || etagChanged)
            {
                modifiedIds.Add(summary.Id);
                Logger.Debug(
                    "  MODIFIED ({0}): {1} ({2} → {3} videos)",
                    countChanged ? "count" : "etag",
                    summary.Title,
                    snapshot.ReportedVideoCount,
                    summary.VideoCount
                );
            }
        }

        return new OptimizedChanges(
            NewIds: newIds,
            DeletedIds: deletedIds,
            ModifiedIds: modifiedIds,
            Renamed: renamed
        );
    }

    internal static void LogOptimizedChanges(OptimizedChanges changes)
    {
        if (changes.NewIds.Count > 0)
            Logger.Info("New playlists: {0}", changes.NewIds.Count);

        if (changes.DeletedIds.Count > 0)
            Logger.Info("Deleted playlists: {0}", changes.DeletedIds.Count);

        if (changes.ModifiedIds.Count > 0)
            Logger.Info("Modified playlists: {0}", changes.ModifiedIds.Count);

        if (changes.Renamed.Count > 0)
            Logger.Info("Renamed playlists: {0}", changes.Renamed.Count);
    }
}
