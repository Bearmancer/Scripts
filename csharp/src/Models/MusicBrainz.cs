namespace CSharpScripts.Models;

#region Core Models

internal record WorkDetails(string? Composer, string? ParentWorkName);

internal record MusicBrainzArtist(
    Guid Id,
    string Name,
    string? SortName,
    string? Type,
    string? Gender,
    string? Country,
    string? Area,
    string? Disambiguation,
    DateOnly? BeginDate,
    DateOnly? EndDate,
    bool? Ended,
    List<string> Aliases,
    List<string> Tags,
    List<string> Genres,
    string? Annotation,
    double? Rating,
    int? RatingVotes
);

internal record MusicBrainzRecording(
    Guid Id,
    string Title,
    string? Artist,
    string? ArtistCredit,
    TimeSpan? Length,
    DateOnly? FirstReleaseDate,
    bool IsVideo,
    string? Disambiguation,
    List<string> Isrcs,
    List<string> Tags,
    List<string> Genres,
    double? Rating,
    int? RatingVotes,
    string? Annotation,
    string? WorkName = null,
    Guid? WorkId = null,
    string? WorkComposer = null,
    string? Conductor = null,
    string? Orchestra = null,
    string? RecordingVenue = null,
    DateOnly? RecordingDate = null
);

internal record MusicBrainzTrack(
    Guid Id,
    string Title,
    int Position,
    string? Number,
    TimeSpan? Length,
    Guid? RecordingId,
    string? ArtistCredit
);

internal record MusicBrainzMedium(
    int Position,
    string? Format,
    string? Title,
    int TrackCount,
    List<MusicBrainzTrack> Tracks
);

internal record MusicBrainzRelease(
    Guid Id,
    string Title,
    string? Artist,
    string? ArtistCredit,
    DateOnly? Date,
    string? Country,
    string? Status,
    string? Barcode,
    string? Asin,
    string? Quality,
    string? Packaging,
    string? Disambiguation,
    Guid? ReleaseGroupId,
    string? ReleaseGroupTitle,
    string? ReleaseGroupType,
    List<MusicBrainzMedium> Media,
    List<MusicBrainzCredit> Credits,
    List<MusicBrainzLabel> Labels,
    List<string> Tags,
    List<string> Genres,
    string? Annotation
)
{
    public List<MusicBrainzTrack> Tracks => [.. Media.SelectMany(m => m.Tracks)];
}

internal record MusicBrainzReleaseGroup(
    Guid Id,
    string Title,
    string? Artist,
    string? ArtistCredit,
    string? PrimaryType,
    List<string> SecondaryTypes,
    DateOnly? FirstReleaseDate,
    int ReleaseCount,
    string? Disambiguation,
    List<string> Tags,
    List<string> Genres,
    double? Rating,
    int? RatingVotes,
    string? Annotation
);

internal record MusicBrainzLabel(Guid? Id, string? Name, string? CatalogNumber);

internal record MusicBrainzCredit(string Name, string Role, Guid? ArtistId, string? Attributes);

#endregion

#region Supporting Records

internal record MusicBrainzEnrichmentState(
    string ReleaseId,
    int TotalTracks,
    List<TrackInfo> EnrichedTracks,
    DateTime LastUpdated
);

#endregion
