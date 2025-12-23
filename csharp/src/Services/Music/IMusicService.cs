namespace CSharpScripts.Services.Music;

public interface IMusicService
{
    MusicSource Source { get; }

    Task<List<SearchResult>> SearchAsync(
        string query,
        int maxResults = 10,
        CancellationToken ct = default
    );

    Task<ReleaseData> GetReleaseAsync(
        string releaseId,
        bool deepSearch = true,
        int? maxDiscs = null,
        CancellationToken ct = default
    );
}
