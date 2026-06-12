namespace Scripts.Data.Entities;

public sealed class Playlist
{
	public int Id { get; set; }
	public string PlaylistId { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string TitleLower { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string ChannelName { get; set; } = string.Empty;
	public string ChannelNameLower { get; set; } = string.Empty;

	public ICollection<PlaylistVideo> PlaylistVideos { get; set; } = new List<PlaylistVideo>();
}
