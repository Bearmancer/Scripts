namespace CSharpScripts.Data.Entities;

internal sealed record Album
{
    public Guid Id { get; set; }
    public Guid ArtistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly? ReleaseDate { get; set; }
    public string? Mbid { get; set; }

    public Artist Artist { get; set; } = null!;
    public ICollection<Track> Tracks { get; set; } = [];
}
