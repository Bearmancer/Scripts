namespace Scripts.Models;

internal sealed record Scrobble(
	string TrackName,
	string ArtistName,
	string AlbumName,
	DateTimeOffset? PlayedAt
)
{
	public string FormattedDate =>
		PlayedAt?.ToString(format: "yyyy-MM-dd HH:mm", formatProvider: CultureInfo.InvariantCulture)
		?? "Unknown";
}

internal sealed record FetchState
{
	public int LastPage { get; init; }
	public int TotalFetched { get; init; }
	public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
	public string? SpreadsheetId { get; init; }
	public bool FetchComplete { get; init; }
	public DateTimeOffset? OldestScrobble { get; init; }
	public DateTimeOffset? NewestScrobble { get; init; }

	internal FetchState WithUpdate(
		int page,
		int total,
		DateTimeOffset? oldest = null,
		DateTimeOffset? newest = null
	) =>
		this with
		{
			LastPage = page,
			TotalFetched = total,
			LastUpdated = DateTimeOffset.UtcNow,
			OldestScrobble =
				oldest.HasValue && (!OldestScrobble.HasValue || oldest < OldestScrobble)
					? oldest
					: OldestScrobble,
			NewestScrobble =
				newest.HasValue && (!NewestScrobble.HasValue || newest > NewestScrobble)
					? newest
					: NewestScrobble,
		};
}
