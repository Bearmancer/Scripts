namespace Scripts.Data.Entities;

public sealed class Movement
{
	public int Id { get; set; }
	public int WorkId { get; set; }
	public int Position { get; set; }
	public string Title { get; set; } = string.Empty;

	public MusicWork MusicWork { get; set; } = null!;
	public ICollection<Track> Tracks { get; init; } = new List<Track>();
}
