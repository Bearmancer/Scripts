namespace CSharpScripts.CLI.Music;

internal static class WorkGrouper
{
	private static readonly HashSet<string> LoggedWorkHierarchyWarnings = [];

	internal static List<WorkSummary> Group(List<TrackInfo> tracks)
	{
		Log.Debug(messageTemplate: "GroupTracks entry {TrackCount}", tracks.Count);

		List<WorkSummary> works = [];
		if (tracks.Count == 0)
		{
			Log.Debug(messageTemplate: "GroupTracks exit 0");
			return works;
		}

		var currentDisc = -1;
		string? currentWorkName = null;
		List<TrackInfo> currentGroup = [];

		void FlushGroup()
		{
			if (currentGroup.Count == 0)
				return;

			TrackInfo first = currentGroup[index: 0];
			TrackInfo last = currentGroup[^1];
			HashSet<int> years = [];
			TimeSpan totalDuration = TimeSpan.Zero;
			HashSet<string> soloistsSet = [];
			HashSet<string> venuesSet = [];

			foreach (TrackInfo t in currentGroup)
			{
				if (t.RecordingYear.HasValue)
					years.Add(item: t.RecordingYear.Value);

				if (t.Duration.HasValue)
					totalDuration += t.Duration.Value;

				foreach (var s in t.Soloists)
					soloistsSet.Add(item: s);

				if (!IsNullOrWhiteSpace(value: t.RecordingVenue))
					venuesSet.Add(item: t.RecordingVenue);
			}

			List<int> sortedYears = [.. years.OrderBy(y => y)];
			List<string> soloists = [.. soloistsSet];
			List<string> recordingVenues = [.. venuesSet];

			var displayWork = first.WorkName ?? first.Title;

			works.Add(
				new WorkSummary(
					Disc: first.DiscNumber,
					FirstTrack: first.TrackNumber,
					LastTrack: last.TrackNumber,
					Work: displayWork,
					Composer: first.Composer,
					Years: sortedYears,
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

			currentGroup.Add(item: track);
		}

		FlushGroup();

		DetectMissingWorkHierarchy(works: works);

		Log.Debug(messageTemplate: "GroupTracks exit {WorkCount}", works.Count);
		return works;
	}

	private static void DetectMissingWorkHierarchy(List<WorkSummary> works)
	{
		HashSet<string> suspectedMissing = [];
		List<string> suspectedMissingInOrder = [];
		var workCount = works.Count;

		for (var i = 0; i < workCount - 1; i++)
		{
			WorkSummary current = works[index: i];
			WorkSummary next = works[i + 1];

			if (current.FirstTrack != current.LastTrack || next.FirstTrack != next.LastTrack)
				continue;

			if (current.Disc != next.Disc)
				continue;

			var currentColon = current.Work.IndexOf(value: ':');
			var nextColon = next.Work.IndexOf(value: ':');

			if (currentColon > 5 && nextColon > 5)
			{
				var currentPrefix = current.Work[..currentColon];
				var nextPrefix = next.Work[..nextColon];

				if (currentPrefix == nextPrefix && suspectedMissing.Add(item: currentPrefix))
					suspectedMissingInOrder.Add(item: currentPrefix);
			}
		}

		foreach (var missing in suspectedMissingInOrder)
		{
			if (!LoggedWorkHierarchyWarnings.Add(item: missing))
				continue;

			Ui.Warn(message: "Work hierarchy missing for '{0}' - tracks not grouped", missing);
		}
	}
}
