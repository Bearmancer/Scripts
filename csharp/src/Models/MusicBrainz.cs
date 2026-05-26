namespace CSharpScripts.Models;

internal sealed record WorkDetails(string? Composer, string? ParentWorkName);

internal sealed record MusicBrainzArtist(
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

internal sealed record MusicBrainzRecording(
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

internal sealed record MusicBrainzTrack(
	Guid Id,
	string Title,
	int Position,
	string? Number,
	TimeSpan? Length,
	Guid? RecordingId,
	string? ArtistCredit
);

internal sealed record MusicBrainzMedium(
	int Position,
	string? Format,
	string? Title,
	int TrackCount,
	List<MusicBrainzTrack> Tracks
);

internal sealed record MusicBrainzRelease(
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
	public List<MusicBrainzTrack> Tracks => field ??= [.. Media.SelectMany(m => m.Tracks)];
}

internal sealed record MusicBrainzReleaseGroup(
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

internal sealed record MusicBrainzLabel(Guid? Id, string? Name, string? CatalogNumber);

internal sealed record MusicBrainzCredit(
	string Name,
	string Role,
	Guid? ArtistId,
	string? Attributes
);

internal sealed record MusicBrainzEnrichmentState(
	string ReleaseId,
	int TotalTracks,
	List<TrackInfo> EnrichedTracks,
	DateTime LastUpdated
);

internal sealed record ReleaseCredits(
	string? Conductor,
	string? Orchestra,
	List<string> Soloists,
	string? Composer
);
