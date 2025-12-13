namespace CSharpScripts.Services.Sync.YouTube;

public record VideoChanges(
    List<string> AddedVideoIds,
    List<string> RemovedVideoIds,
    List<int> RemovedRowIndices,
    bool RequiresFullRewrite
)
{
    internal bool HasChanges =>
        AddedVideoIds.Count > 0 || RemovedVideoIds.Count > 0 || RequiresFullRewrite;
}

public static class YouTubeChangeDetector
{
    internal static VideoChanges DetectVideoChanges(
        List<string> currentVideoIds,
        List<string> storedVideoIds
    )
    {
        var currentSet = currentVideoIds.ToHashSet();
        var storedSet = storedVideoIds.ToHashSet();

        var addedIds = currentVideoIds.Where(id => !storedSet.Contains(id)).ToList();
        var removedIds = storedVideoIds.Where(id => !currentSet.Contains(id)).ToList();

        List<int> removedIndices = [];
        for (var i = 0; i < storedVideoIds.Count; i++)
            if (!currentSet.Contains(storedVideoIds[i]))
                removedIndices.Add(i + 2);

        Console.Debug(
            "VideoChanges: current={0}, stored={1}, added={2}, removed={3}, removedIndices={4}",
            currentVideoIds.Count,
            storedVideoIds.Count,
            addedIds.Count,
            removedIds.Count,
            removedIndices.Count
        );

        var requiresFullRewrite = false;
        if (addedIds.Count == 0 && removedIndices.Count == 0)
            for (var i = 0; i < Math.Min(currentVideoIds.Count, storedVideoIds.Count); i++)
                if (currentVideoIds[i] != storedVideoIds[i])
                {
                    requiresFullRewrite = true;
                    break;
                }

        return new VideoChanges(addedIds, removedIds, removedIndices, requiresFullRewrite);
    }

    internal static PlaylistChanges DetectPlaylistChanges(
        List<YouTubePlaylist> currentPlaylists,
        Dictionary<string, PlaylistSnapshot> snapshots
    )
    {
        Console.Debug("=== DETECTING CHANGES ===");
        Console.Debug("Current playlists from API: {0}", currentPlaylists.Count);
        Console.Debug("Saved snapshots in state: {0}", snapshots.Count);

        var currentIds = currentPlaylists.Select(p => p.Id).ToHashSet();
        var previousIds = snapshots.Keys.ToHashSet();

        var newIds = currentIds.Except(previousIds).ToList();
        var deletedIds = previousIds.Except(currentIds).ToList();
        List<string> modifiedIds = [];

        Console.Debug("New playlist IDs (not in snapshots): {0}", newIds.Count);
        Console.Debug("Deleted playlist IDs (in snapshots but not API): {0}", deletedIds.Count);

        foreach (var playlist in currentPlaylists)
        {
            if (!snapshots.TryGetValue(playlist.Id, out var snapshot))
            {
                Console.Debug("  No snapshot for: {0} (ID: {1})", playlist.Title, playlist.Id);
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
                Console.Debug("  MODIFIED: {0} (+{1} -{2} videos)", playlist.Title, added, removed);
            }
            else
            {
                Console.Debug("  UNCHANGED: {0} ({1} videos)", playlist.Title, playlist.VideoCount);
            }
        }

        Console.Debug("Modified playlists: {0}", modifiedIds.Count);
        return new PlaylistChanges(newIds, deletedIds, modifiedIds);
    }

    public static void LogDetectedChanges(PlaylistChanges changes)
    {
        if (changes.NewPlaylistIds.Count > 0)
            Console.Info("New playlists: {0}", changes.NewPlaylistIds.Count);

        if (changes.DeletedPlaylistIds.Count > 0)
            Console.Info("Deleted playlists: {0}", changes.DeletedPlaylistIds.Count);

        if (changes.ModifiedPlaylistIds.Count > 0)
            Console.Info("Modified playlists: {0}", changes.ModifiedPlaylistIds.Count);
    }

    internal static OptimizedChanges DetectOptimizedChanges(
        List<PlaylistSummary> currentSummaries,
        Dictionary<string, PlaylistSnapshot> snapshots
    )
    {
        Console.Debug("=== OPTIMIZED CHANGE DETECTION ===");
        Console.Debug(
            "Current summaries: {0}, Stored snapshots: {1}",
            currentSummaries.Count,
            snapshots.Count
        );

        var currentIds = currentSummaries.Select(s => s.Id).ToHashSet();
        var previousIds = snapshots.Keys.ToHashSet();

        var newIds = currentIds.Except(previousIds).ToList();
        var deletedIds = previousIds.Except(currentIds).ToList();
        List<string> modifiedIds = [];
        List<PlaylistRename> renamed = [];

        Console.Debug("New playlist IDs: {0}", newIds.Count);
        Console.Debug("Deleted playlist IDs: {0}", deletedIds.Count);

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

            var etagChanged =
                !IsNullOrEmpty(snapshot.ETag)
                && !IsNullOrEmpty(summary.ETag)
                && snapshot.ETag != summary.ETag;

            var countChanged = snapshot.ReportedVideoCount != summary.VideoCount;

            // Log warning if ETag is missing (can't detect reorder without it)
            if (IsNullOrEmpty(snapshot.ETag) && !etagChanged && !countChanged)
                Console.Debug(
                    "  WARNING: {0} has no stored ETag - reorder detection disabled (will sync on next change)",
                    summary.Title
                );

            if (etagChanged || countChanged)
            {
                modifiedIds.Add(summary.Id);
                var reason = (etagChanged, countChanged) switch
                {
                    (true, true) => "etag+count",
                    (true, false) => "etag only (likely reorder)",
                    (false, true) => "count only (fallback - stored ETag was null)",
                    _ => "unknown",
                };
                Console.Debug(
                    "  MODIFIED ({0}): {1} (count: {2} → {3}, etag: {4} → {5})",
                    reason,
                    summary.Title,
                    snapshot.ReportedVideoCount,
                    summary.VideoCount,
                    snapshot.ETag?[..8] ?? "null",
                    summary.ETag?[..8] ?? "null"
                );
            }
        }

        Console.Debug("Total modified: {0}", modifiedIds.Count);
        return new OptimizedChanges(
            NewIds: newIds,
            DeletedIds: deletedIds,
            ModifiedIds: modifiedIds,
            Renamed: renamed
        );
    }

    public static void LogOptimizedChanges(OptimizedChanges changes)
    {
        if (changes.NewIds.Count > 0)
            Console.Info("New playlists: {0}", changes.NewIds.Count);

        if (changes.DeletedIds.Count > 0)
            Console.Info("Deleted playlists: {0}", changes.DeletedIds.Count);

        if (changes.ModifiedIds.Count > 0)
            Console.Info("Modified playlists: {0}", changes.ModifiedIds.Count);

        if (changes.Renamed.Count > 0)
            Console.Info("Renamed playlists: {0}", changes.Renamed.Count);
    }
}
