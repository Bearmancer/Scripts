namespace CSharpScripts.Data.Entities;

internal sealed record Artist
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Mbid { get; set; }
    public JsonDocument? Metadata { get; set; }

    public ICollection<Album> Albums { get; set; } = [];
    public ICollection<Track> Tracks { get; set; } = [];
}
