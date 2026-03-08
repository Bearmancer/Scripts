namespace CSharpScripts.Models;

internal sealed record DiscogsArtist(
	int Id,
	string Name,
	string? RealName,
	string? Profile,
	string? ResourceUrl,
	List<string> NameVariations,
	List<string> Aliases,
	List<DiscogsImage> Images,
	List<string> Urls
);

internal sealed record DiscogsRelease(
	int Id,
	string Title,
	int Year,
	string? Country,
	string? Released,
	string? ReleasedFormatted,
	int? MasterId,
	string? MasterUrl,
	string? Status,
	string? DataQuality,
	string? Notes,
	string? Uri,
	string? ResourceUrl,
	List<DiscogsArtistRef> Artists,
	List<DiscogsArtistRef> ExtraArtists,
	List<DiscogsLabel> Labels,
	List<DiscogsCompany> Companies,
	List<string> Genres,
	List<string> Styles,
	List<DiscogsTrack> Tracks,
	List<DiscogsFormat> Formats,
	List<DiscogsIdentifier> Identifiers,
	List<DiscogsImage> Images,
	List<DiscogsVideo> Videos,
	DiscogsCommunity? Community,
	int? EstimatedWeight
)
{
	public List<DiscogsCredit> Credits =>
		[
			.. ExtraArtists
				.Where(a => !IsNullOrEmpty(value: a.Role))
				.Select(a => new DiscogsCredit(Name: a.Name, a.Role ?? "", Tracks: a.Tracks)),
		];
}

internal sealed record DiscogsCredit(string Name, string Role, string? Tracks);

internal sealed record DiscogsMaster(
	int Id,
	string Title,
	int Year,
	int MainReleaseId,
	int MostRecentReleaseId,
	string? MainReleaseUrl,
	string? MostRecentReleaseUrl,
	string? VersionsUrl,
	string? ResourceUrl,
	string? Uri,
	string? DataQuality,
	List<DiscogsArtistRef> Artists,
	List<string> Genres,
	List<string> Styles,
	List<DiscogsTrack> Tracks,
	List<DiscogsImage> Images,
	List<DiscogsVideo> Videos,
	int? QuantityForSale,
	decimal? LowestPrice
);

internal sealed record DiscogsTrack(
	string Position,
	string Title,
	string? Duration,
	string? Type,
	List<DiscogsArtistRef>? Artists,
	List<DiscogsArtistRef>? ExtraArtists
);

internal sealed record DiscogsFormat(
	string Name,
	string? Quantity,
	string? Text,
	List<string> Descriptions
);

internal sealed record DiscogsLabel(
	int Id,
	string Name,
	string? CatalogNumber,
	string? EntityType,
	string? EntityTypeName,
	string? ResourceUrl
);

internal sealed record DiscogsCompany(
	int Id,
	string Name,
	string? CatalogNumber,
	string? EntityType,
	string? EntityTypeName,
	string? ResourceUrl
);

internal sealed record DiscogsArtistRef(
	int Id,
	string Name,
	string? Anv,
	string? Join,
	string? Role,
	string? Tracks,
	string? ResourceUrl
);

internal sealed record DiscogsImage(
	string Type,
	string? Uri,
	string? Uri150,
	int? Width,
	int? Height,
	string? ResourceUrl
);

internal sealed record DiscogsVideo(
	string Uri,
	string? Title,
	string? Description,
	int? Duration,
	bool Embed
);

internal sealed record DiscogsIdentifier(string Type, string Value, string? Description);

internal sealed record DiscogsCommunity(
	int Have,
	int Want,
	double? Rating,
	int? RatingCount,
	string? Status,
	string? DataQuality,
	DiscogsSubmitter? Submitter
);

internal sealed record DiscogsSubmitter(string Username, string? ResourceUrl);

internal sealed record DiscogsVersion(
	int Id,
	string Title,
	string? Format,
	string? Label,
	string? Country,
	int? Year,
	string? CatalogNumber,
	string? Status,
	string? ResourceUrl,
	string? Thumb
);

internal sealed record DiscogsSearchResult(
	int ReleaseId,
	int? MasterId,
	string? Title,
	string? Artist,
	int? Year,
	string? Country,
	string? Format,
	string? Label,
	string? CatalogNumber,
	string? Type,
	string? Thumb,
	string? CoverImage,
	List<string>? Genres,
	List<string>? Styles,
	List<string>? Barcodes
);
