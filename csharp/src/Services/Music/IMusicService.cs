namespace CSharpScripts.Services.Music;

public interface IMusicService
{
    MusicSource Source { get; }

    /// <summary>
    /// Free-text search. User can type anything:
    /// "Bowie Heroes 1977", "Deutsche Grammophon Beethoven", etc.
    /// </summary>
    Task<List<Models.SearchResult>> SearchAsync(string query, int maxResults = 10);

    /// <summary>
    /// Get full track metadata for any release type (Album, EP, Box Set, etc.).
    /// </summary>
    Task<List<TrackMetadata>> GetReleaseTracksAsync(string releaseId);
}
