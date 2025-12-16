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

        List<string> addedIds = [.. currentVideoIds.Where(id => !storedSet.Contains(id))];

        // Build both removedIds and removedIndices in a single pass
        List<string> removedIds = [];
        List<int> removedIndices = [];
        for (var i = 0; i < storedVideoIds.Count; i++)
        {
            if (!currentSet.Contains(storedVideoIds[i]))
            {
                removedIds.Add(storedVideoIds[i]);
                removedIndices.Add(i + 2); // +2 for 1-indexed sheet with header
            }
        }

        Console.Debug(
            "VideoChanges: current={0}, stored={1}, added={2}, removed={3}, removedIndices={4}",
            currentVideoIds.Count,
            storedVideoIds.Count,
            addedIds.Count,
            removedIds.Count,
            removedIndices.Count
        );

        // Use SequenceEqual for cleaner reorder detection
        var requiresFullRewrite =
            addedIds.Count == 0
            && removedIndices.Count == 0
            && !currentVideoIds.SequenceEqual(storedVideoIds);

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

        List<string> newIds = [.. currentIds.Except(previousIds)];
        List<string> deletedIds = [.. previousIds.Except(currentIds)];
        List<string> modifiedIds = [];

        Console.Debug("New playlist IDs (not in snapshots): {0}", newIds.Count);
        Console.Debug("Deleted playlist IDs (in snapshots but not API): {0}", deletedIds.Count);

        // Build set of new IDs to skip them efficiently
        var newIdsSet = newIds.ToHashSet();

        foreach (var playlist in currentPlaylists)
        {
            // Skip new playlists - no snapshot to compare
            if (newIdsSet.Contains(playlist.Id))
                continue;

            var snapshot = snapshots[playlist.Id];
            var currentVideoIds = playlist.VideoIds.ToHashSet();
            var storedVideoIds = snapshot.VideoIds.ToHashSet();

            if (!currentVideoIds.SetEquals(storedVideoIds))
            {
                modifiedIds.Add(playlist.Id);
                Console.Debug("  MODIFIED: {0}", playlist.Title);
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

        List<string> newIds = [.. currentIds.Except(previousIds)];
        List<string> deletedIds = [.. previousIds.Except(currentIds)];
        List<string> modifiedIds = [];
        List<PlaylistRename> renamed = [];

        Console.Debug("New playlist IDs: {0}", newIds.Count);
        Console.Debug("Deleted playlist IDs: {0}", deletedIds.Count);

        // Build set of new IDs to skip them efficiently
        var newIdsSet = newIds.ToHashSet();

        foreach (var summary in currentSummaries)
        {
            // Skip new playlists - no snapshot to compare
            if (newIdsSet.Contains(summary.Id))
                continue;

            var snapshot = snapshots[summary.Id];

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

            if (etagChanged || countChanged)
            {
                modifiedIds.Add(summary.Id);
                var reason = (etagChanged, countChanged) switch
                {
                    (true, true) => "etag+count",
                    (true, false) => "etag only (likely reorder)",
                    (false, true) => "count only",
                    _ => "unknown",
                };
                Console.Debug("  MODIFIED ({0}): {1}", reason, summary.Title);
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
        Console.Info("Starting sync...");

        var totalChanges =
            changes.NewIds.Count
            + changes.DeletedIds.Count
            + changes.ModifiedIds.Count
            + changes.Renamed.Count;

        if (totalChanges == 0)
        {
            Console.Success("No changes detected.");
            return;
        }

        Console.Info("Changes detected: {0}", totalChanges);

        if (changes.NewIds.Count > 0)
            Console.Info("  New: {0}", changes.NewIds.Count);

        if (changes.ModifiedIds.Count > 0)
            Console.Info("  Modified: {0}", changes.ModifiedIds.Count);

        if (changes.Renamed.Count > 0)
            Console.Info("  Renamed: {0}", changes.Renamed.Count);

        if (changes.DeletedIds.Count > 0)
            Console.Info("  Deleted: {0}", changes.DeletedIds.Count);
    }

    /// <summary>
    /// Logs detailed change information with playlist names and video deltas.
    /// Call this method when snapshots and summaries are available.
    /// </summary>
    public static void LogDetailedChanges(
        OptimizedChanges changes,
        List<PlaylistSummary> summaries,
        Dictionary<string, PlaylistSnapshot> snapshots
    )
    {
        Console.Info("Starting sync...");

        var summaryLookup = summaries.ToDictionary(s => s.Id, s => s);

        // Modified playlists with video delta
        if (changes.ModifiedIds.Count > 0)
        {
            Console.Info("Modified playlists: {0}", changes.ModifiedIds.Count);
            foreach (var id in changes.ModifiedIds)
            {
                var name = summaryLookup.TryGetValue(id, out var s) ? s.Title : id;
                var currentCount = s.VideoCount;
                var previousCount = snapshots.TryGetValue(id, out var snap)
                    ? snap.VideoIds.Count
                    : 0;
                var delta = currentCount - previousCount;
                var deltaStr = delta >= 0 ? $"+{delta}" : delta.ToString();
                Console.Info("  {0}: {1} videos", name, deltaStr);
            }
        }

        // New playlists
        if (changes.NewIds.Count > 0)
        {
            Console.Info("New playlists: {0}", changes.NewIds.Count);
            foreach (var id in changes.NewIds)
            {
                var name = summaryLookup.TryGetValue(id, out var s) ? s.Title : id;
                var count = s.VideoCount;
                Console.Info("  {0}: +{1} videos", name, count);
            }
        }

        // Renamed playlists
        if (changes.Renamed.Count > 0)
        {
            Console.Info("Renamed playlists: {0}", changes.Renamed.Count);
            foreach (var rename in changes.Renamed)
                Console.Info("  {0} → {1}", rename.OldTitle, rename.NewTitle);
        }

        // Deleted playlists
        if (changes.DeletedIds.Count > 0)
        {
            Console.Info("Deleted playlists: {0}", changes.DeletedIds.Count);
            foreach (var id in changes.DeletedIds)
            {
                var name = snapshots.TryGetValue(id, out var snap) ? snap.Title : id;
                Console.Info("  {0}", name);
            }
        }
    }
}
