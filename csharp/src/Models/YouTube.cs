namespace CSharpScripts.Models;

public record YouTubeVideo(
    string Title,
    string Description,
    TimeSpan Duration,
    string ChannelName,
    string VideoId,
    string ChannelId
)
{
    internal string VideoUrl => $"https://www.youtube.com/watch?v={VideoId}";
    internal string ChannelUrl => $"https://www.youtube.com/channel/{ChannelId}";
    internal string FormattedDuration => Duration.ToString(@"hh\:mm\:ss");
}

public record YouTubePlaylist(
    string Id,
    string Title,
    int VideoCount,
    List<string> VideoIds,
    string? ETag = null
);

public record PlaylistSnapshot(
    string PlaylistId,
    string Title,
    List<string> VideoIds,
    DateTime LastUpdated,
    int ReportedVideoCount = 0,
    string? ETag = null
);

public record YouTubeFetchState
{
    public string? SpreadsheetId { get; set; }
    public Dictionary<string, PlaylistSnapshot> PlaylistSnapshots { get; set; } = [];
    public List<YouTubePlaylist>? CachedPlaylists { get; set; }
    public int VideoIdFetchIndex { get; set; }
    public string? CurrentPlaylistId { get; set; }
    public int CurrentPlaylistVideosFetched { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    public DateTime LastChecked { get; set; } = DateTime.Now;
    public bool FetchComplete { get; set; }

    internal void UpdatePlaylistProgress(string playlistId, int videosFetched)
    {
        CurrentPlaylistId = playlistId;
        CurrentPlaylistVideosFetched = videosFetched;
        LastUpdated = DateTime.Now;
    }

    internal void ClearCurrentProgress()
    {
        CurrentPlaylistId = null;
        CurrentPlaylistVideosFetched = 0;
        LastUpdated = DateTime.Now;
    }
}

public readonly record struct PlaylistSummary(
    string Id,
    string Title,
    int VideoCount,
    string? ETag
);

public record PlaylistRename(string PlaylistId, string OldTitle, string NewTitle);

public record PlaylistChanges(
    List<string> NewPlaylistIds,
    List<string> DeletedPlaylistIds,
    List<string> ModifiedPlaylistIds
)
{
    internal bool HasChanges =>
        NewPlaylistIds.Count > 0 || DeletedPlaylistIds.Count > 0 || ModifiedPlaylistIds.Count > 0;
}

public record OptimizedChanges(
    List<string> NewIds,
    List<string> DeletedIds,
    List<string> ModifiedIds,
    List<PlaylistRename> Renamed
)
{
    internal bool HasAnyChanges =>
        NewIds.Count > 0 || DeletedIds.Count > 0 || ModifiedIds.Count > 0 || Renamed.Count > 0;
}
