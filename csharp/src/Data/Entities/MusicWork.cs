namespace Scripts.Data.Entities;

using System.Text.Json;

public sealed class MusicWork
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? Composer { get; set; }
	public JsonDocument? Metadata { get; set; }

	public ICollection<Movement> Movements { get; init; } = new List<Movement>();
	public ICollection<Track> Tracks { get; init; } = new List<Track>();
}
