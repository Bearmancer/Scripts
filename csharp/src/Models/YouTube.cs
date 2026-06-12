namespace Scripts.Models;

internal sealed record YouTubeVideoRaw(
	string Title,
	string Description,
	TimeSpan Duration,
	string ChannelName,
	string VideoId,
	string ChannelId
)
{
	public string? DetectedLanguage { get; init; }
}

internal sealed record YouTubeVideo(
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
	internal string FormattedDuration => Duration.ToString(format: @"hh\:mm\:ss");

	public string? DetectedLanguage { get; init; }
	public string? TranslatedTitle { get; init; }
	public string? TranslatedDescription { get; init; }
	public DateTimeOffset? TranslatedAt { get; init; }

	public string DisplayTitle => TranslatedTitle ?? Title;
	public string DisplayDescription => TranslatedDescription ?? Description;

	public bool NeedsTranslation =>
		DetectedLanguage is { }
		&& !DetectedLanguage.EqualsIgnoreCase(other: "eng")
		&& TranslatedTitle is null;

	public YouTubeVideoRaw ToRaw() =>
		new(
			Title: Title,
			Description: Description,
			Duration: Duration,
			ChannelName: ChannelName,
			VideoId: VideoId,
			ChannelId: ChannelId
		)
		{
			DetectedLanguage = DetectedLanguage,
		};

	public static YouTubeVideo FromRaw(YouTubeVideoRaw raw) =>
		new(raw.Title, raw.Description, raw.Duration, raw.ChannelName, raw.VideoId, raw.ChannelId)
		{
			DetectedLanguage = raw.DetectedLanguage,
		};

	public YouTubeVideo WithTranslation(
		string translatedTitle,
		string translatedDescription,
		string detectedLanguage
	) =>
		this with
		{
			DetectedLanguage = detectedLanguage,
			TranslatedTitle = translatedTitle,
			TranslatedDescription = translatedDescription,
			TranslatedAt = DateTimeOffset.UtcNow,
		};

	public YouTubeVideo WithoutTranslation() =>
		this with
		{
			TranslatedTitle = null,
			TranslatedDescription = null,
			TranslatedAt = null,
		};
}

internal sealed record YouTubePlaylist(
	string Id,
	string Title,
	int VideoCount,
	List<string> VideoIds,
	string? ETag = null
);

internal sealed record PlaylistSnapshot(
	string PlaylistId,
	string Title,
	List<string> VideoIds,
	DateTimeOffset LastUpdated,
	int ReportedVideoCount = 0,
	string? ETag = null
);

internal sealed record YouTubeFetchState
{
	public string? SpreadsheetId { get; init; }
	public Dictionary<string, PlaylistSnapshot> PlaylistSnapshots { get; init; } = [];
	public List<YouTubePlaylist>? CachedPlaylists { get; init; }
	public int VideoIdFetchIndex { get; init; }
	public string? CurrentPlaylistId { get; init; }
	public int CurrentPlaylistVideosFetched { get; init; }
	public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
	public DateTimeOffset LastChecked { get; init; } = DateTimeOffset.UtcNow;
	public bool FetchComplete { get; init; }
}

internal readonly record struct PlaylistSummary(
	string Id,
	string Title,
	int VideoCount,
	string? ETag
);

internal sealed record PlaylistRename(string PlaylistId, string OldTitle, string NewTitle);

internal sealed record PlaylistChanges(
	List<string> NewPlaylistIds,
	List<string> DeletedPlaylistIds,
	List<string> ModifiedPlaylistIds
)
{
	internal bool HasChanges =>
		NewPlaylistIds.Count > 0 || DeletedPlaylistIds.Count > 0 || ModifiedPlaylistIds.Count > 0;
}

internal sealed record OptimizedChanges(
	List<string> NewIds,
	List<string> DeletedIds,
	List<string> ModifiedIds,
	List<PlaylistRename> Renamed
)
{
	internal bool HasAnyChanges =>
		NewIds.Count > 0 || DeletedIds.Count > 0 || ModifiedIds.Count > 0 || Renamed.Count > 0;
}
