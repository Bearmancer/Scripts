namespace CSharpScripts.Services.Sync.YouTube;

internal record VideoChanges(
	List<string> AddedVideoIds,
	List<string> RemovedVideoIds,
	List<int> RemovedRowIndices,
	bool RequiresFullRewrite
)
{
	internal bool HasChanges =>
		AddedVideoIds.Count > 0 || RemovedVideoIds.Count > 0 || RequiresFullRewrite;
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

		List<string> addedIds = [.. currentVideoIds.Where(id => !storedSet.Contains(id))];

		List<string> removedIds = [];
		List<int> removedIndices = [];
		for (var i = 0; i < storedVideoIds.Count; i++)
			if (!currentSet.Contains(storedVideoIds[index: i]))
			{
				removedIds.Add(storedVideoIds[index: i]);
				removedIndices.Add(i + 2);
			}

		Log.Debug(
			"VideoChanges: current={0}, stored={1}, added={2}, removed={3}, removedIndices={4}",
			currentVideoIds.Count,
			storedVideoIds.Count,
			addedIds.Count,
			removedIds.Count,
			removedIndices.Count
		);

		var requiresFullRewrite =
			addedIds.Count == 0
			&& removedIndices.Count == 0
			&& !currentVideoIds.SequenceEqual(storedVideoIds);

		return new VideoChanges(
			AddedVideoIds: addedIds,
			RemovedVideoIds: removedIds,
			RemovedRowIndices: removedIndices,
			RequiresFullRewrite: requiresFullRewrite
		);
	}

	internal static PlaylistChanges DetectPlaylistChanges(
		List<YouTubePlaylist> currentPlaylists,
		Dictionary<string, PlaylistSnapshot> snapshots
	)
	{
		Log.Debug("=== DETECTING CHANGES ===");
		Log.Debug("Current playlists from API: {0}", currentPlaylists.Count);
		Log.Debug("Saved snapshots in state: {0}", snapshots.Count);

		var currentIds = currentPlaylists.Select(p => p.Id).ToHashSet();
		var previousIds = snapshots.Keys.ToHashSet();

		List<string> newIds = [.. currentIds.Except(previousIds)];
		List<string> deletedIds = [.. previousIds.Except(currentIds)];
		List<string> modifiedIds = [];

		Log.Debug("New playlist IDs (not in snapshots): {0}", newIds.Count);
		Log.Debug("Deleted playlist IDs (in snapshots but not API): {0}", deletedIds.Count);

		var newIdsSet = newIds.ToHashSet();

		foreach (YouTubePlaylist playlist in currentPlaylists)
		{
			if (newIdsSet.Contains(playlist.Id))
				continue;

			PlaylistSnapshot snapshot = snapshots[playlist.Id];

			if (!playlist.VideoIds.SequenceEqual(snapshot.VideoIds))
			{
				modifiedIds.Add(playlist.Id);
				Log.Debug("  MODIFIED: {0}", playlist.Title);
			}
		}

		Log.Debug("Modified playlists: {0}", modifiedIds.Count);
		return new PlaylistChanges(
			NewPlaylistIds: newIds,
			DeletedPlaylistIds: deletedIds,
			ModifiedPlaylistIds: modifiedIds
		);
	}

	public static void LogDetectedChanges(PlaylistChanges changes)
	{
		if (changes.NewPlaylistIds.Count > 0)
			Log.Information("New playlists: {0}", changes.NewPlaylistIds.Count);

		if (changes.DeletedPlaylistIds.Count > 0)
			Log.Information("Deleted playlists: {0}", changes.DeletedPlaylistIds.Count);

		if (changes.ModifiedPlaylistIds.Count > 0)
			Log.Information("Modified playlists: {0}", changes.ModifiedPlaylistIds.Count);
	}

	internal static OptimizedChanges DetectOptimizedChanges(
		List<PlaylistSummary> currentSummaries,
		Dictionary<string, PlaylistSnapshot> snapshots
	)
	{
		Log.Debug("=== OPTIMIZED CHANGE DETECTION ===");
		Log.Debug(
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

		Log.Debug("New playlist IDs: {0}", newIds.Count);
		Log.Debug("Deleted playlist IDs: {0}", deletedIds.Count);

		var newIdsSet = newIds.ToHashSet();

		foreach (PlaylistSummary summary in currentSummaries)
		{
			if (newIdsSet.Contains(summary.Id))
				continue;

			PlaylistSnapshot snapshot = snapshots[summary.Id];

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
				Log.Debug("  MODIFIED ({0}): {1}", reason, summary.Title);
			}
		}

		Log.Debug("Total modified: {0}", modifiedIds.Count);
		return new OptimizedChanges(
			NewIds: newIds,
			DeletedIds: deletedIds,
			ModifiedIds: modifiedIds,
			Renamed: renamed
		);
	}

	public static void LogOptimizedChanges(OptimizedChanges changes)
	{
		var totalChanges =
			changes.NewIds.Count
			+ changes.DeletedIds.Count
			+ changes.ModifiedIds.Count
			+ changes.Renamed.Count;

		if (totalChanges == 0)
		{
			Log.Information("No changes detected.");
			return;
		}

		Log.Information("Changes detected: {0}", totalChanges);

		if (changes.NewIds.Count > 0)
			Log.Information("  New: {0}", changes.NewIds.Count);

		if (changes.ModifiedIds.Count > 0)
			Log.Information("  Modified: {0}", changes.ModifiedIds.Count);

		if (changes.Renamed.Count > 0)
			Log.Information("  Renamed: {0}", changes.Renamed.Count);

		if (changes.DeletedIds.Count > 0)
			Log.Information("  Deleted: {0}", changes.DeletedIds.Count);
	}

	public static void LogDetailedChanges(
		OptimizedChanges changes,
		List<PlaylistSummary> summaries,
		Dictionary<string, PlaylistSnapshot> snapshots
	)
	{
		var summaryLookup = summaries.ToDictionary(s => s.Id, s => s);

		if (changes.ModifiedIds.Count > 0)
		{
			Log.Information("Modified playlists: {0}", changes.ModifiedIds.Count);
			foreach (var id in changes.ModifiedIds)
			{
				var name = summaryLookup.TryGetValue(id, out PlaylistSummary s) ? s.Title : id;
				var currentCount = s.VideoCount;
				var previousCount = snapshots.TryGetValue(id, out PlaylistSnapshot? snap)
					? snap.VideoIds.Count
					: 0;
				var delta = currentCount - previousCount;
				var deltaStr = delta >= 0 ? $"+{delta}" : delta.ToString();
				Log.Information("{0}: {1} videos", name, deltaStr);
			}
		}

		if (changes.NewIds.Count > 0)
		{
			Log.Information("New playlists: {0}", changes.NewIds.Count);
			foreach (var id in changes.NewIds)
			{
				var name = summaryLookup.TryGetValue(id, out PlaylistSummary s) ? s.Title : id;
				var count = s.VideoCount;
				Log.Information("  {0}: +{1} videos", name, count);
			}
		}

		if (changes.Renamed.Count > 0)
		{
			Log.Information("Renamed playlists: {0}", changes.Renamed.Count);
			foreach (PlaylistRename rename in changes.Renamed)
				Log.Information("  {0} → {1}", rename.OldTitle, rename.NewTitle);
		}

		if (changes.DeletedIds.Count > 0)
		{
			Log.Information("Deleted playlists: {0}", changes.DeletedIds.Count);
			foreach (var id in changes.DeletedIds)
			{
				var name = snapshots.TryGetValue(id, out PlaylistSnapshot? snap) ? snap.Title : id;
				Log.Information("  {0}", name);
			}
		}
	}
}
