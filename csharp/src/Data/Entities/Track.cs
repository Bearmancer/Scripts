namespace CSharpScripts.Data.Entities;

internal sealed record Track
{
    public Guid Id { get; set; }
    public Guid AlbumId { get; set; }
    public Guid ArtistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? Duration { get; set; }
    public string? Mbid { get; set; }

    public Album Album { get; set; } = null!;
    public Artist Artist { get; set; } = null!;
    public ICollection<Scrobble> Scrobbles { get; set; } = [];
}
