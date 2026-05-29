namespace CSharpScripts.Infrastructure;

public sealed class SyncProgressRenderer(SyncProgressTracker tracker)
{
	private const int MAX_NAME_LENGTH = 30;

	public IRenderable BuildDisplay() => BuildDisplayFromSnapshot(tracker.GetSnapshot());

	public static IRenderable BuildDisplayFromSnapshot(SyncProgressSnapshot snapshot)
	{
		var playlistName = TruncateName(snapshot.CurrentPlaylistName);
		var progressBar = BuildProgressBar(snapshot.OverallVideoPercent);
		var percentText = $"{snapshot.OverallVideoPercent:F1}%";
		var countsText = $"({snapshot.CompletedPlaylists}/{snapshot.TotalPlaylists} playlists)";
		var videosText =
			$"{snapshot.TotalVideosProcessedAcrossAllPlaylists}/{snapshot.TotalVideosAcrossAllPlaylists} videos";
		var timeText = FormatTimeText(snapshot);

		var colorName = GetBarColor(snapshot.OverallVideoPercent);

		Markup line = new(
			$"{Console.Colored(color: colorName, text: playlistName)} {countsText} "
				+ $"[{colorName}]{progressBar}[/] {percentText} {videosText} {timeText}"
		);

		return line;
	}

	private static string TruncateName(string name)
	{
		if (name.Length <= MAX_NAME_LENGTH)
			return name;

		return name[..(MAX_NAME_LENGTH - 3)] + "...";
	}

	private static string BuildProgressBar(double percent) => Console.WideProgressBar(percent);

	private static string GetBarColor(double percent) => Console.ProgressColor(percent);

	private static string FormatTimeText(SyncProgressSnapshot snapshot)
	{
		if (snapshot.EstimatedTimeRemaining is { } eta)
			return $"ETA: {FormatDuration(eta)}";

		return $"Elapsed: {FormatDuration(snapshot.ElapsedTime)}";
	}

	private static string FormatDuration(TimeSpan ts)
	{
		if (ts.TotalHours >= 1)
			return $"{(int)ts.TotalHours}h {ts.Minutes}m";
		if (ts.TotalMinutes >= 1)
			return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";

		return $"{ts.Seconds}s";
	}
}
