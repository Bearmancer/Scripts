namespace Scripts.Data.Entities;






public sealed class Scrobble
{
	public long Id { get; set; }
	public int TrackId { get; set; }
	public DateTimeOffset ScrobbledAt { get; set; }
	public string Platform { get; set; } = string.Empty;

	public Track Track { get; set; } = null!;
}
