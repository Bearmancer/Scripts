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
    /// <summary>First release year (when the release came out)</summary>
    int? FirstIssuedYear,
    /// <summary>When the recording was actually made (for classical - may differ from release)</summary>
    int? RecordingYear,
    string? Composer,
    /// <summary>Work/piece name for classical (e.g., "Symphony No. 5 in C minor, Op. 67")</summary>
    string? WorkName,
    string? Conductor,
    string? Orchestra,
    List<string> Soloists,
    string? Artist,
    string Album,
    string? Label,
    string? CatalogNumber,
    string? RecordingVenue,
    string? Notes,
    TimeSpan? Duration,
    string? RecordingId = null
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
    string? ReleaseType, // Album, EP, Single, Compilation, etc.
    int? Score = null, // MusicBrainz relevance score (0-100), null for Discogs
    string? Country = null,
    string? CatalogNumber = null,
    string? Barcode = null,
    List<string>? Genres = null,
    List<string>? Styles = null // Discogs only
);
