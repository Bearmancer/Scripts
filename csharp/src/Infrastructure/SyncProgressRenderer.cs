namespace CSharpScripts.Infrastructure;

public sealed class SyncProgressRenderer(SyncProgressTracker tracker)
{
    const int BAR_WIDTH = 40;
    const int MAX_NAME_LENGTH = 30;

    public IRenderable BuildDisplay() => BuildDisplayFromSnapshot(tracker.GetSnapshot());

    public static IRenderable BuildDisplayFromSnapshot(SyncProgressSnapshot snapshot)
    {
        string playlistName = TruncateName(snapshot.CurrentPlaylistName);
        string progressBar = BuildProgressBar(snapshot.OverallVideoPercent);
        string percentText = $"{snapshot.OverallVideoPercent:F1}%";
        string countsText = $"({snapshot.CompletedPlaylists}/{snapshot.TotalPlaylists} playlists)";
        string videosText =
            $"{snapshot.TotalVideosProcessedAcrossAllPlaylists}/{snapshot.TotalVideosAcrossAllPlaylists} videos";
        string timeText = FormatTimeText(snapshot);

        string colorName = GetBarColor(snapshot.OverallVideoPercent);

        Markup line = new(
            $"{Console.Colored(colorName, playlistName)} {countsText} "
                + $"[{colorName}]{progressBar}[/] {percentText} {videosText} {timeText}"
        );

        return line;
    }

    static string TruncateName(string name)
    {
        if (name.Length <= MAX_NAME_LENGTH)
            return name;
        return name[..(MAX_NAME_LENGTH - 3)] + "...";
    }

    static string BuildProgressBar(double percent) => Console.WideProgressBar(percent, BAR_WIDTH);

    static string GetBarColor(double percent) => Console.ProgressColor(percent);

    static string FormatTimeText(SyncProgressSnapshot snapshot)
    {
        if (snapshot.EstimatedTimeRemaining is { } eta)
            return $"ETA: {FormatDuration(eta)}";
        return $"Elapsed: {FormatDuration(snapshot.ElapsedTime)}";
    }

    static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }
}
