namespace CSharpScripts.Core;

internal record PlaylistProgressItem(string Title, int VideoCount);

internal record SyncProgressSnapshot(
	int TotalPlaylists,
	int CompletedPlaylists,
	int CurrentPlaylistIndex,
	string CurrentPlaylistName,
	int CurrentPlaylistTotalVideos,
	int CurrentPlaylistVideosProcessed,
	int TotalVideosAcrossAllPlaylists,
	int TotalVideosProcessedAcrossAllPlaylists,
	double PlaylistProgressPercent,
	double CurrentPlaylistVideoPercent,
	double OverallVideoPercent,
	TimeSpan ElapsedTime,
	TimeSpan? EstimatedTimeRemaining
);

internal sealed class SyncProgressTracker
{
	private DateTime? StartTime;
	public int TotalPlaylists { get; private set; }
	public int CompletedPlaylists { get; private set; }
	public int CurrentPlaylistIndex { get; private set; } = 1;
	public string CurrentPlaylistName { get; private set; } = "";
	public int CurrentPlaylistTotalVideos { get; private set; }
	public int CurrentPlaylistVideosProcessed { get; private set; }
	public int TotalVideosAcrossAllPlaylists { get; private set; }
	public int TotalVideosProcessedAcrossAllPlaylists { get; private set; }

	public TimeSpan ElapsedTime =>
		StartTime.HasValue ? DateTime.UtcNow - StartTime.Value : TimeSpan.Zero;

	public TimeSpan? EstimatedTimeRemaining
	{
		get
		{
			if (TotalVideosProcessedAcrossAllPlaylists <= 0 || TotalVideosAcrossAllPlaylists <= 0)
				return null;

			var rate = ElapsedTime.TotalSeconds / TotalVideosProcessedAcrossAllPlaylists;
			var remaining = TotalVideosAcrossAllPlaylists - TotalVideosProcessedAcrossAllPlaylists;
			return remaining > 0 ? TimeSpan.FromSeconds(rate * remaining) : null;
		}
	}

	public double PlaylistProgressPercent =>
		TotalPlaylists > 0 ? CompletedPlaylists * 100.0 / TotalPlaylists : 0;

	public double CurrentPlaylistVideoPercent =>
		CurrentPlaylistTotalVideos > 0
			? CurrentPlaylistVideosProcessed * 100.0 / CurrentPlaylistTotalVideos
			: 0;

	public double OverallVideoPercent =>
		TotalVideosAcrossAllPlaylists > 0
			? TotalVideosProcessedAcrossAllPlaylists * 100.0 / TotalVideosAcrossAllPlaylists
			: 0;

	public void Initialize(List<PlaylistProgressItem> playlists)
	{
		TotalPlaylists = playlists.Count;
		CompletedPlaylists = 0;
		CurrentPlaylistIndex = 1;
		CurrentPlaylistName = "";
		CurrentPlaylistTotalVideos = 0;
		CurrentPlaylistVideosProcessed = 0;
		TotalVideosAcrossAllPlaylists = Enumerable.Sum(playlists, p => p.VideoCount);
		TotalVideosProcessedAcrossAllPlaylists = 0;
		StartTime = null;
	}

	public void StartPlaylist(string name, int videoCount)
	{
		CurrentPlaylistName = name;
		CurrentPlaylistTotalVideos = videoCount;
		CurrentPlaylistVideosProcessed = 0;
		StartTime ??= DateTime.UtcNow;
	}

	public void UpdateVideoProgress(int videosProcessed)
	{
		var delta = videosProcessed - CurrentPlaylistVideosProcessed;
		CurrentPlaylistVideosProcessed = videosProcessed;
		TotalVideosProcessedAcrossAllPlaylists += delta;
	}

	public void CompleteCurrentPlaylist()
	{
		var remaining = CurrentPlaylistTotalVideos - CurrentPlaylistVideosProcessed;
		TotalVideosProcessedAcrossAllPlaylists += remaining;
		CurrentPlaylistVideosProcessed = CurrentPlaylistTotalVideos;
		CompletedPlaylists++;
		CurrentPlaylistIndex++;
	}

	public SyncProgressSnapshot GetSnapshot() =>
		new(
			TotalPlaylists: TotalPlaylists,
			CompletedPlaylists: CompletedPlaylists,
			CurrentPlaylistIndex: CurrentPlaylistIndex,
			CurrentPlaylistName: CurrentPlaylistName,
			CurrentPlaylistTotalVideos: CurrentPlaylistTotalVideos,
			CurrentPlaylistVideosProcessed: CurrentPlaylistVideosProcessed,
			TotalVideosAcrossAllPlaylists: TotalVideosAcrossAllPlaylists,
			TotalVideosProcessedAcrossAllPlaylists: TotalVideosProcessedAcrossAllPlaylists,
			PlaylistProgressPercent: PlaylistProgressPercent,
			CurrentPlaylistVideoPercent: CurrentPlaylistVideoPercent,
			OverallVideoPercent: OverallVideoPercent,
			ElapsedTime: ElapsedTime,
			EstimatedTimeRemaining: EstimatedTimeRemaining
		);
}

internal sealed class SyncProgressRenderer(SyncProgressTracker tracker)
{
	private const int MaxNameLength = 30;

	public IRenderable BuildDisplay() => BuildDisplayFromSnapshot(tracker.GetSnapshot());

	public static IRenderable BuildDisplayFromSnapshot(SyncProgressSnapshot snapshot)
	{
		var playlistName = TruncateName(name: snapshot.CurrentPlaylistName);
		var progressBar = BuildProgressBar(percent: snapshot.OverallVideoPercent);
		var percentText = $"{snapshot.OverallVideoPercent:F1}%";
		var countsText = $"({snapshot.CompletedPlaylists}/{snapshot.TotalPlaylists} playlists)";
		var videosText =
			$"{snapshot.TotalVideosProcessedAcrossAllPlaylists}/{snapshot.TotalVideosAcrossAllPlaylists} videos";
		var timeText = FormatTimeText(snapshot: snapshot);

		var colorName = GetBarColor(percent: snapshot.OverallVideoPercent);

		Markup line = new(
			$"{UI.Colored(color: colorName, text: playlistName)} {countsText} "
				+ $"[{colorName}]{progressBar}[/] {percentText} {videosText} {timeText}"
		);

		return line;
	}

	private static string TruncateName(string name) =>
		name.Length <= MaxNameLength ? name : name[..(MaxNameLength - 3)] + "...";

	private static string BuildProgressBar(double percent) => UI.WideProgressBar(percent: percent);

	private static string GetBarColor(double percent) => UI.ProgressColor(percent: percent);

	private static string FormatTimeText(SyncProgressSnapshot snapshot) =>
		snapshot.EstimatedTimeRemaining is { } eta
			? $"ETA: {FormatDuration(ts: eta)}"
			: $"Elapsed: {FormatDuration(ts: snapshot.ElapsedTime)}";

	private static string FormatDuration(TimeSpan ts) =>
		ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
		: ts.TotalMinutes >= 1 ? $"{(int)ts.TotalMinutes}m {ts.Seconds}s"
		: $"{ts.Seconds}s";
}
