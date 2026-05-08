namespace CSharpScripts.Data.Entities;

internal sealed record Scrobble
{
    public long Id { get; set; }
    public Guid TrackId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Platform { get; set; } = string.Empty;

    public Track Track { get; set; } = null!;
}
