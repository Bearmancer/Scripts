namespace CSharpScripts.Infrastructure;

public record PlaylistProgressItem(string Title, int VideoCount);

public record SyncProgressSnapshot(
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

public sealed class SyncProgressTracker
{
    public int TotalPlaylists { get; private set; }
    public int CompletedPlaylists { get; private set; }
    public int CurrentPlaylistIndex { get; private set; } = 1;
    public string CurrentPlaylistName { get; private set; } = "";
    public int CurrentPlaylistTotalVideos { get; private set; }
    public int CurrentPlaylistVideosProcessed { get; private set; }
    public int TotalVideosAcrossAllPlaylists { get; private set; }
    public int TotalVideosProcessedAcrossAllPlaylists { get; private set; }

    DateTime? StartTime;

    public TimeSpan ElapsedTime =>
        StartTime.HasValue ? DateTime.UtcNow - StartTime.Value : TimeSpan.Zero;

    public TimeSpan? EstimatedTimeRemaining
    {
        get
        {
            if (TotalVideosProcessedAcrossAllPlaylists <= 0 || TotalVideosAcrossAllPlaylists <= 0)
                return null;

            double rate = ElapsedTime.TotalSeconds / TotalVideosProcessedAcrossAllPlaylists;
            int remaining = TotalVideosAcrossAllPlaylists - TotalVideosProcessedAcrossAllPlaylists;
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
        if (playlists.Count == 0)
            throw new ArgumentException("Playlists cannot be empty", nameof(playlists));

        TotalPlaylists = playlists.Count;
        CompletedPlaylists = 0;
        CurrentPlaylistIndex = 1;
        CurrentPlaylistName = "";
        CurrentPlaylistTotalVideos = 0;
        CurrentPlaylistVideosProcessed = 0;
        TotalVideosAcrossAllPlaylists = playlists.Sum(p => p.VideoCount);
        TotalVideosProcessedAcrossAllPlaylists = 0;
        StartTime = null;
    }

    public void StartPlaylist(string name, int videoCount)
    {
        if (IsNullOrWhiteSpace(name))
            throw new ArgumentException("Playlist name cannot be empty", nameof(name));
        if (videoCount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(videoCount),
                "Video count cannot be negative"
            );

        CurrentPlaylistName = name;
        CurrentPlaylistTotalVideos = videoCount;
        CurrentPlaylistVideosProcessed = 0;
        StartTime ??= DateTime.UtcNow;
    }

    public void UpdateVideoProgress(int videosProcessed)
    {
        if (videosProcessed < 0)
            throw new ArgumentOutOfRangeException(nameof(videosProcessed), "Cannot be negative");
        if (videosProcessed > CurrentPlaylistTotalVideos)
            throw new ArgumentOutOfRangeException(nameof(videosProcessed), "Cannot exceed total");

        int delta = videosProcessed - CurrentPlaylistVideosProcessed;
        CurrentPlaylistVideosProcessed = videosProcessed;
        TotalVideosProcessedAcrossAllPlaylists += delta;
    }

    public void CompleteCurrentPlaylist()
    {
        int remaining = CurrentPlaylistTotalVideos - CurrentPlaylistVideosProcessed;
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
