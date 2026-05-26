namespace CSharpScripts.Data.Entities;

/// <summary>
/// Represents a YouTube video tracked in a playlist.
/// IsDeleted uses soft-delete to preserve history of removed videos.
/// </summary>
public sealed class Video
{
	public int Id { get; set; }
	public string Url { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string ChannelName { get; set; } = string.Empty;
	public DateOnly? UploadDate { get; set; }
	public DateTimeOffset? SyncedAt { get; set; }
	public System.Text.Json.JsonDocument? Metadata { get; set; }
}

