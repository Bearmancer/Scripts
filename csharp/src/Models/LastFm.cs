namespace CSharpScripts.Models;

internal sealed record Scrobble(
	string TrackName,
	string ArtistName,
	string AlbumName,
	DateTime? PlayedAt
)
{
	public string FormattedDate =>
		PlayedAt?.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
}

internal sealed record FetchState
{
	public int LastPage { get; init; }
	public int TotalFetched { get; init; }
	public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
	public string? SpreadsheetId { get; init; }
	public bool FetchComplete { get; init; }
	public DateTime? OldestScrobble { get; init; }
	public DateTime? NewestScrobble { get; init; }

	internal FetchState WithUpdate(
		int page,
		int total,
		DateTime? oldest = null,
		DateTime? newest = null
	) =>
		this with
		{
			LastPage = page,
			TotalFetched = total,
			LastUpdated = DateTime.UtcNow,
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


