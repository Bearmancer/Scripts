namespace Scripts.Data.Entities;

public sealed class Video
{
	public int Id { get; set; }
	public string VideoId { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string TitleLower { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string ChannelName { get; set; } = string.Empty;
	public string ChannelNameLower { get; set; } = string.Empty;
	public DateOnly? UploadDate { get; set; }
	public DateTimeOffset? SyncedAt { get; set; }
	public JsonDocument? Metadata { get; set; }
	public string? TranslatedTitle { get; set; }
	public string? TranslatedDescription { get; set; }

	public ICollection<PlaylistVideo> PlaylistVideos { get; set; } = new List<PlaylistVideo>();
}
