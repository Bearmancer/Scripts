namespace CSharpScripts.Data.Entities;

/// <summary>
/// Represents a YouTube video tracked in a playlist.
/// IsDeleted uses soft-delete to preserve history of removed videos.
/// </summary>
public sealed class Video
{
    public int Id { get; set; }
    public string YoutubeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PlaylistId { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
