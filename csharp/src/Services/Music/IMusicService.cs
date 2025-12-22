using SearchResult = CSharpScripts.Models.SearchResult;

namespace CSharpScripts.Services.Music;

public interface IMusicService
{
    MusicSource Source { get; }

    /// <summary>
    /// Free-text search. User can type anything:
    /// "Bowie Heroes 1977", "Deutsche Grammophon Beethoven", etc.
    /// </summary>
    Task<List<SearchResult>> SearchAsync(
        string query,
        int maxResults = 10,
        CancellationToken ct = default
    );

    /// <summary>
    /// Get release with full track data for any release type (Album, EP, Box Set, etc.).
    /// Returns ReleaseData containing release-level info + all tracks.
    /// </summary>
    Task<ReleaseData> GetReleaseAsync(
        string releaseId,
        bool deepSearch = true,
        int? maxDiscs = null,
        CancellationToken ct = default
    );
}
