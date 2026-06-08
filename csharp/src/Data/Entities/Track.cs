namespace Scripts.Data.Entities;





public sealed class Track
{
	public int Id { get; set; }
	public int? AlbumId { get; set; }
	public int? ArtistId { get; set; }
	public int? WorkId { get; set; }
	public int? MovementId { get; set; }
	public string Title { get; set; } = string.Empty;
	public int? DurationSeconds { get; set; }

	public Album? Album { get; set; }
	public Artist? Artist { get; set; }
	public MusicWork? MusicWork { get; set; }
	public Movement? Movement { get; set; }
	public ICollection<Scrobble> Scrobbles { get; init; } = new List<Scrobble>();

	public string DisplayArtist => Artist?.Name ?? "Unknown Artist";
}
