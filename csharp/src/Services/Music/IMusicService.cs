namespace CSharpScripts.Services.Music;

internal interface IMusicService
{
	MusicSource Source { get; }

	Task<List<SearchResult>> SearchAsync(
		string query,
		int maxResults = 10,
		CancellationToken ct = default
	);

	Task<ReleaseData> GetReleaseAsync(
		string releaseId,
		int? maxDiscs = null,
		CancellationToken ct = default
	);
}
