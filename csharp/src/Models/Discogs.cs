namespace CSharpScripts.Models;

public record DiscogsArtist(
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

public record DiscogsRelease(
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
                .Where(a => !IsNullOrEmpty(a.Role))
                .Select(a => new DiscogsCredit(a.Name, a.Role ?? "", a.Tracks)),
        ];
}

public record DiscogsCredit(string Name, string Role, string? Tracks);

public record DiscogsMaster(
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

public record DiscogsTrack(
    string Position,
    string Title,
    string? Duration,
    string? Type,
    List<DiscogsArtistRef>? Artists,
    List<DiscogsArtistRef>? ExtraArtists
);

public record DiscogsFormat(string Name, string? Quantity, string? Text, List<string> Descriptions);

public record DiscogsLabel(
    int Id,
    string Name,
    string? CatalogNumber,
    string? EntityType,
    string? EntityTypeName,
    string? ResourceUrl
);

public record DiscogsCompany(
    int Id,
    string Name,
    string? CatalogNumber,
    string? EntityType,
    string? EntityTypeName,
    string? ResourceUrl
);

public record DiscogsArtistRef(
    int Id,
    string Name,
    string? Anv,
    string? Join,
    string? Role,
    string? Tracks,
    string? ResourceUrl
);

public record DiscogsImage(
    string Type,
    string? Uri,
    string? Uri150,
    int? Width,
    int? Height,
    string? ResourceUrl
);

public record DiscogsVideo(
    string Uri,
    string? Title,
    string? Description,
    int? Duration,
    bool Embed
);

public record DiscogsIdentifier(string Type, string Value, string? Description);

public record DiscogsCommunity(
    int Have,
    int Want,
    double? Rating,
    int? RatingCount,
    string? Status,
    string? DataQuality,
    DiscogsSubmitter? Submitter
);

public record DiscogsSubmitter(string Username, string? ResourceUrl);

public record DiscogsVersion(
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

public record DiscogsSearchResult(
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
