namespace CSharpScripts.Data.Entities;

/// <summary>
/// Records a single music play event (scrobble) from a streaming platform.
/// Id is long (BIGINT) to handle high-volume scrobble history.
/// Platform is stored as a string (e.g., "lastfm", "spotify").
/// </summary>
public sealed class Scrobble
{
	public long Id { get; set; }
	public int TrackId { get; set; }
	public DateTimeOffset ScrobbledAt { get; set; }
	public string Platform { get; set; } = string.Empty;

	public Track Track { get; set; } = null!;
}

