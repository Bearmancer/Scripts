namespace Scripts.Services.Sync.YouTube;

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
		HashSet<string> currentSet = currentVideoIds.ToHashSet();
		HashSet<string> storedSet = storedVideoIds.ToHashSet();

		List<string> addedIds = new(capacity: currentVideoIds.Count);
		foreach (var id in currentVideoIds)
		{
			if (!storedSet.Contains(item: id))
				addedIds.Add(item: id);
		}

		List<string> removedIds = new(capacity: storedVideoIds.Count);
		List<int> removedIndices = new(capacity: storedVideoIds.Count);
		for (var i = 0; i < storedVideoIds.Count; i++)
		{
			var storedId = storedVideoIds[index: i];
			if (!currentSet.Contains(item: storedId))
			{
				removedIds.Add(item: storedId);
				removedIndices.Add(i + 2);
			}
		}

		Log.Debug(
			messageTemplate: "VideoChanges: current={0}, stored={1}, added={2}, removed={3}, removedIndices={4}",
			currentVideoIds.Count,
			storedVideoIds.Count,
			addedIds.Count,
			removedIds.Count,
			removedIndices.Count
		);

		var requiresFullRewrite =
			addedIds.Count == 0
			&& removedIndices.Count == 0
			&& !currentVideoIds.SequenceEqual(second: storedVideoIds);

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
		Log.Debug(messageTemplate: "=== DETECTING CHANGES ===");
		Log.Debug(messageTemplate: "Current playlists from API: {0}", currentPlaylists.Count);
		Log.Debug(messageTemplate: "Saved snapshots in state: {0}", snapshots.Count);

		HashSet<string> currentIds = currentPlaylists.Select(p => p.Id).ToHashSet();
		HashSet<string> previousIds = snapshots.Keys.ToHashSet();

		List<string> newIds = [.. currentIds.Except(second: previousIds)];
		List<string> deletedIds = [.. previousIds.Except(second: currentIds)];
		List<string> modifiedIds = [];

		Log.Debug(messageTemplate: "New playlist IDs (not in snapshots): {0}", newIds.Count);
		Log.Debug(
			messageTemplate: "Deleted playlist IDs (in snapshots but not API): {0}",
			deletedIds.Count
		);

		HashSet<string> newIdsSet = newIds.ToHashSet();

		foreach (YouTubePlaylist playlist in currentPlaylists)
		{
			if (newIdsSet.Contains(item: playlist.Id))
				continue;

			PlaylistSnapshot snapshot = snapshots[key: playlist.Id];

			if (!playlist.VideoIds.SequenceEqual(second: snapshot.VideoIds))
			{
				modifiedIds.Add(item: playlist.Id);
				Log.Debug(messageTemplate: "  MODIFIED: {0}", playlist.Title);
			}
		}

		Log.Debug(messageTemplate: "Modified playlists: {0}", modifiedIds.Count);
		return new PlaylistChanges(
			NewPlaylistIds: newIds,
			DeletedPlaylistIds: deletedIds,
			ModifiedPlaylistIds: modifiedIds
		);
	}

	public static void LogDetectedChanges(PlaylistChanges changes)
	{
		if (changes.NewPlaylistIds.Count > 0)
			Log.Information(messageTemplate: "New playlists: {0}", changes.NewPlaylistIds.Count);

		if (changes.DeletedPlaylistIds.Count > 0)
			Log.Information(
				messageTemplate: "Deleted playlists: {0}",
				changes.DeletedPlaylistIds.Count
			);

		if (changes.ModifiedPlaylistIds.Count > 0)
			Log.Information(
				messageTemplate: "Modified playlists: {0}",
				changes.ModifiedPlaylistIds.Count
			);
	}

	internal static OptimizedChanges DetectOptimizedChanges(
		List<PlaylistSummary> currentSummaries,
		Dictionary<string, PlaylistSnapshot> snapshots
	)
	{
		Log.Debug(messageTemplate: "=== OPTIMIZED CHANGE DETECTION ===");
		Log.Debug(
			messageTemplate: "Current summaries: {0}, Stored snapshots: {1}",
			currentSummaries.Count,
			snapshots.Count
		);

		HashSet<string> currentIds = currentSummaries.Select(s => s.Id).ToHashSet();
		HashSet<string> previousIds = snapshots.Keys.ToHashSet();

		List<string> newIds = [.. currentIds.Except(second: previousIds)];
		List<string> deletedIds = [.. previousIds.Except(second: currentIds)];
		List<string> modifiedIds = new(capacity: currentSummaries.Count);
		List<PlaylistRename> renamed = new(capacity: currentSummaries.Count);

		Log.Debug(messageTemplate: "New playlist IDs: {0}", newIds.Count);
		Log.Debug(messageTemplate: "Deleted playlist IDs: {0}", deletedIds.Count);

		HashSet<string> newIdsSet = newIds.ToHashSet();

		foreach (PlaylistSummary summary in currentSummaries)
		{
			if (newIdsSet.Contains(item: summary.Id))
				continue;

			PlaylistSnapshot snapshot = snapshots[key: summary.Id];

			if (snapshot.Title != summary.Title)
			{
				renamed.Add(
					new PlaylistRename(
						PlaylistId: summary.Id,
						OldTitle: snapshot.Title,
						NewTitle: summary.Title
					)
				);
			}

			var etagChanged =
				!IsNullOrEmpty(value: snapshot.ETag)
				&& !IsNullOrEmpty(value: summary.ETag)
				&& snapshot.ETag != summary.ETag;

			var countChanged = snapshot.ReportedVideoCount != summary.VideoCount;

			if (etagChanged || countChanged)
			{
				modifiedIds.Add(item: summary.Id);
				var reason = (etagChanged, countChanged) switch
				{
					(true, true) => "etag+count",
					(true, false) => "etag only (likely reorder)",
					(false, true) => "count only",
					_ => "unknown",
				};
				Log.Debug(messageTemplate: "  MODIFIED ({0}): {1}", reason, summary.Title);
			}
		}

		Log.Debug(messageTemplate: "Total modified: {0}", modifiedIds.Count);
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
			Log.Information(messageTemplate: "No changes detected.");
			return;
		}

		Log.Information(messageTemplate: "Changes detected: {0}", totalChanges);

		if (changes.NewIds.Count > 0)
			Log.Information(messageTemplate: "  New: {0}", changes.NewIds.Count);

		if (changes.ModifiedIds.Count > 0)
			Log.Information(messageTemplate: "  Modified: {0}", changes.ModifiedIds.Count);

		if (changes.Renamed.Count > 0)
			Log.Information(messageTemplate: "  Renamed: {0}", changes.Renamed.Count);

		if (changes.DeletedIds.Count > 0)
			Log.Information(messageTemplate: "  Deleted: {0}", changes.DeletedIds.Count);
	}

	public static void LogDetailedChanges(
		OptimizedChanges changes,
		List<PlaylistSummary> summaries,
		Dictionary<string, PlaylistSnapshot> snapshots
	)
	{
		Dictionary<string, PlaylistSummary> summaryLookup = summaries.ToDictionary(
			s => s.Id,
			s => s
		);

		if (changes.ModifiedIds.Count > 0)
		{
			Log.Information(messageTemplate: "Modified playlists: {0}", changes.ModifiedIds.Count);
			foreach (var id in changes.ModifiedIds)
			{
				var name = summaryLookup.TryGetValue(key: id, out PlaylistSummary s) ? s.Title : id;
				var currentCount = s.VideoCount;
				var previousCount = snapshots.TryGetValue(key: id, out PlaylistSnapshot? snap)
					? snap.VideoIds.Count
					: 0;
				var delta = currentCount - previousCount;
				var deltaStr = delta >= 0 ? $"+{delta}" : delta.ToString();
				Log.Information(messageTemplate: "{0}: {1} videos", name, deltaStr);
			}
		}

		if (changes.NewIds.Count > 0)
		{
			Log.Information(messageTemplate: "New playlists: {0}", changes.NewIds.Count);
			foreach (var id in changes.NewIds)
			{
				var name = summaryLookup.TryGetValue(key: id, out PlaylistSummary s) ? s.Title : id;
				var count = s.VideoCount;
				Log.Information(messageTemplate: "  {0}: +{1} videos", name, count);
			}
		}

		if (changes.Renamed.Count > 0)
		{
			Log.Information(messageTemplate: "Renamed playlists: {0}", changes.Renamed.Count);
			foreach (PlaylistRename rename in changes.Renamed)
				Log.Information(messageTemplate: "  {0} → {1}", rename.OldTitle, rename.NewTitle);
		}

		if (changes.DeletedIds.Count > 0)
		{
			Log.Information(messageTemplate: "Deleted playlists: {0}", changes.DeletedIds.Count);
			foreach (var id in changes.DeletedIds)
			{
				var name = snapshots.TryGetValue(key: id, out PlaylistSnapshot? snap)
					? snap.Title
					: id;
				Log.Information(messageTemplate: "  {0}", name);
			}
		}
	}
}
