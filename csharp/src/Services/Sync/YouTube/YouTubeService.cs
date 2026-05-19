#pragma warning disable IDE0060
namespace CSharpScripts.Services.Sync.YouTube;

internal class YouTubeService
{
	public static Task<YouTubeService> CreateAsync(CancellationToken ct) =>
		Task.FromResult(new YouTubeService());

	public static void Dispose()
	{
	}

	public static Task<List<PlaylistSummary>> GetPlaylistSummariesAsync(CancellationToken ct) =>
		Task.FromResult(new List<PlaylistSummary>());
}
