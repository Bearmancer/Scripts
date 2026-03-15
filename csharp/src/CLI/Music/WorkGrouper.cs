namespace CSharpScripts.CLI.Music;

internal static class WorkGrouper
{
	private static readonly HashSet<string> LoggedWorkHierarchyWarnings = [];

	internal static List<WorkSummary> Group(List<TrackInfo> tracks)
	{
		Log.Debug("GroupTracks entry {TrackCount}", tracks.Count);

		List<WorkSummary> works = [];
		if (tracks.Count == 0)
		{
			Log.Debug("GroupTracks exit 0");
			return works;
		}

		var currentDisc = -1;
		string? currentWorkName = null;
		List<TrackInfo> currentGroup = [];

		void FlushGroup()
		{
			if (currentGroup.Count == 0)
				return;

			TrackInfo first = currentGroup[0];
			List<int> years =
			[
				.. currentGroup
					.Select(t => t.RecordingYear)
					.Where(y => y.HasValue)
					.Select(y => y!.Value)
					.Distinct()
					.OrderBy(y => y),
			];

			TimeSpan totalDuration = currentGroup
				.Where(t => t.Duration.HasValue)
				.Aggregate(TimeSpan.Zero, (sum, t) => sum + t.Duration!.Value);

			List<string> soloists = [.. currentGroup.SelectMany(t => t.Soloists).Distinct()];
			List<string> recordingVenues =
			[
				.. currentGroup
					.Select(t => t.RecordingVenue)
					.Where(static venue => !IsNullOrWhiteSpace(venue))
					.Cast<string>()
					.Distinct(),
			];

			var displayWork = first.WorkName ?? first.Title;

			works.Add(
				new WorkSummary(
					Disc: first.DiscNumber,
					FirstTrack: currentGroup[0].TrackNumber,
					LastTrack: currentGroup[^1].TrackNumber,
					Work: displayWork,
					Composer: first.Composer,
					Years: years,
					Conductor: first.Conductor,
					Orchestra: first.Orchestra,
					Soloists: soloists,
					RecordingVenues: recordingVenues,
					TotalDuration: totalDuration
				)
			);

			currentGroup.Clear();
		}

		foreach (TrackInfo track in tracks)
		{
			var workKey = track.WorkName ?? track.Title;

			if (track.DiscNumber != currentDisc || workKey != currentWorkName)
			{
				FlushGroup();
				currentDisc = track.DiscNumber;
				currentWorkName = workKey;
			}

			currentGroup.Add(track);
		}

		FlushGroup();

		DetectMissingWorkHierarchy(works);

		Log.Debug("GroupTracks exit {WorkCount}", works.Count);
		return works;
	}

	private static void DetectMissingWorkHierarchy(List<WorkSummary> works)
	{
		List<string> suspectedMissing = [];

		for (var i = 0; i < works.Count - 1; i++)
		{
			WorkSummary current = works[i];
			WorkSummary next = works[i + 1];

			if (current.FirstTrack != current.LastTrack || next.FirstTrack != next.LastTrack)
				continue;

			if (current.Disc != next.Disc)
				continue;

			var currentColon = current.Work.IndexOf(':');
			var nextColon = next.Work.IndexOf(':');

			if (currentColon > 5 && nextColon > 5)
			{
				var currentPrefix = current.Work[..currentColon];
				var nextPrefix = next.Work[..nextColon];

				if (currentPrefix == nextPrefix && !suspectedMissing.Contains(currentPrefix))
					suspectedMissing.Add(currentPrefix);
			}
		}

		foreach (var missing in suspectedMissing)
		{
			if (!LoggedWorkHierarchyWarnings.Add(missing))
				continue;

			UI.Warn("Work hierarchy missing for '{0}' - tracks not grouped", missing);
		}
	}
}
