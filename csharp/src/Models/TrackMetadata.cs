namespace CSharpScripts.Models;

/// <summary>
/// Unified track metadata supporting both classical and pop use cases.
/// </summary>
public record TrackMetadata(
    MusicSource Source,
    string ReleaseId,
    int DiscNumber,
    int TrackNumber,
    string Title,
    int? FirstIssuedYear,
    string? Composer,
    string? Conductor,
    string? Orchestra,
    List<string> Soloists,
    string? Artist,
    string Album,
    string? Label,
    string? CatalogNumber,
    string? RecordingVenue,
    string? Notes,
    TimeSpan? Duration
);

/// <summary>
/// Search result from either API.
/// </summary>
public record SearchResult(
    MusicSource Source,
    string Id,
    string Title,
    string? Artist,
    int? Year,
    string? Format,
    string? Label,
    string? ReleaseType // Album, EP, Single, Compilation, etc.
);
