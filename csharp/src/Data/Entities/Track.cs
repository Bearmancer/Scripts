namespace CSharpScripts.Data.Entities;

/// <summary>
/// Represents a music track belonging to an album and artist.
/// Duration is stored in seconds (integer) for simplicity.
/// </summary>
public sealed class Track
{
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public int ArtistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? DurationSeconds { get; set; }

    // Navigation properties
    public Album Album { get; set; } = null!;
    public Artist Artist { get; set; } = null!;
    public ICollection<Scrobble> Scrobbles { get; init; } = new List<Scrobble>();
}
