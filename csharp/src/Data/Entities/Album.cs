namespace Scripts.Data.Entities;

public sealed class Album
{
	public int Id { get; set; }
	public int? ArtistId { get; set; }
	public string Title { get; set; } = string.Empty;
	public DateOnly? ReleaseDate { get; set; }

	public Artist? Artist { get; set; }
	public ICollection<Track> Tracks { get; init; } = new List<Track>();
}
