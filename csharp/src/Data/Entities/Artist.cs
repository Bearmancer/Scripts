using System.Text.Json;

namespace Scripts.Data.Entities;

/// <summary>
/// Represents a music artist. Metadata is stored as JSONB (configured in ArtistConfiguration).
/// </summary>
public sealed class Artist
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public JsonDocument? Metadata { get; set; }
	public ICollection<Album> Albums { get; init; } = new List<Album>();
	public ICollection<Track> Tracks { get; init; } = new List<Track>();
}
