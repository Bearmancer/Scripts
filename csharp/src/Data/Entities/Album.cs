namespace CSharpScripts.Data.Entities;

/// <summary>
/// Represents a music album belonging to an artist.
/// ReleaseDate uses DateOnly — maps to PostgreSQL DATE via Npgsql.
/// </summary>
public sealed class Album
{
	public int Id { get; set; }
	public int ArtistId { get; set; }
	public string Title { get; set; } = string.Empty;
	public DateOnly? ReleaseDate { get; set; }

	public Artist Artist { get; set; } = null!;
	public ICollection<Track> Tracks { get; init; } = new List<Track>();
}
