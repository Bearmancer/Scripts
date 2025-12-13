namespace CSharpScripts.Services.Music;

sealed class DiscogsClientConfig(string token) : IClientConfig
{
    public string AuthToken => token;
    public string BaseUrl => "https://api.discogs.com";
}

public sealed class DiscogsService
{
    internal DiscogsClient Client { get; }

    public DiscogsService(string? token)
    {
        string validToken =
            token ?? throw new ArgumentException("Discogs token is required", nameof(token));
        HttpClient http = new(new HttpClientHandler());
        Client = new DiscogsClient(http, new ApiQueryBuilder(new DiscogsClientConfig(validToken)));
    }

    #region Search

    public async Task<List<DiscogsSearchResult>> SearchAsync(
        string? artist = null,
        string? release = null,
        string? track = null,
        int? year = null,
        string? label = null,
        string? genre = null,
        int maxResults = 50
    )
    {
        SearchCriteria criteria = new()
        {
            Artist = artist,
            ReleaseTitle = release,
            Track = track,
            Year = year,
            Label = label,
            Genre = genre,
        };

        return await ExecuteSafeListAsync(async () =>
        {
            SearchResults results = await Client.SearchAsync(
                criteria,
                new PageOptions { PageNumber = 1, PageSize = Math.Min(maxResults, 100) }
            );

            return results.Results.Take(maxResults).Select(MapSearchResult).ToList();
        });
    }

    public async Task<DiscogsSearchResult?> SearchFirstAsync(
        string? artist = null,
        string? release = null,
        string? track = null,
        int? year = null,
        string? label = null,
        string? genre = null
    )
    {
        List<DiscogsSearchResult> results = await SearchAsync(
            artist,
            release,
            track,
            year,
            label,
            genre,
            1
        );
        return results.Count > 0 ? results[0] : null;
    }

    static DiscogsSearchResult MapSearchResult(ParkSquare.Discogs.Dto.SearchResult r) =>
        new(
            ReleaseId: r.ReleaseId,
            MasterId: r.MasterId,
            Title: r.Title,
            Artist: ExtractArtist(r.Title),
            Year: ParseYear(r.Year),
            Country: r.Country,
            Format: r.Format is { } fmt ? Join(", ", fmt) : null,
            Label: r.Label is { } lbl ? Join(", ", lbl) : null,
            CatalogNumber: r.CatalogNumber,
            Type: r.Type,
            Thumb: r.Thumb,
            CoverImage: r.CoverImage,
            Genres: r.Genre?.ToList(),
            Styles: r.Style?.ToList(),
            Barcodes: r.Barcode?.ToList()
        );

    #endregion

    #region Release

    public async Task<Models.DiscogsRelease?> GetReleaseAsync(int releaseId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            Release? release = await Client.GetReleaseAsync(releaseId);
            if (release is null)
                return null;

            return MapRelease(release);
        });
    }

    static Models.DiscogsRelease MapRelease(Release r) =>
        new(
            Id: r.ReleaseId,
            Title: r.Title ?? "",
            Year: r.Year,
            Country: r.Country,
            Released: r.Released,
            ReleasedFormatted: r.ReleasedFormatted,
            MasterId: r.MasterId,
            MasterUrl: r.MasterUrl,
            Status: r.Status,
            DataQuality: r.DataQuality,
            Notes: r.Notes,
            Uri: r.Uri,
            ResourceUrl: r.ResourceUrl,
            Artists: r.Artists?.Select(MapArtistRef).ToList() ?? [],
            ExtraArtists: r.ExtraArtists?.Select(MapArtistRef).ToList() ?? [],
            Labels: r.Labels?.Select(MapLabel).ToList() ?? [],
            Companies: r.Companies?.Select(MapCompany).ToList() ?? [],
            Genres: r.Genres?.ToList() ?? [],
            Styles: r.Styles?.ToList() ?? [],
            Tracks: r.Tracklist?.Select(MapTrack).ToList() ?? [],
            Formats: r.Formats?.Select(MapFormat).ToList() ?? [],
            Identifiers: r.Identifiers?.Select(MapIdentifier).ToList() ?? [],
            Images: r.Images?.Select(MapImage).ToList() ?? [],
            Videos: r.Videos?.Select(MapVideo).ToList() ?? [],
            Community: r.Community is { } c ? MapCommunity(c) : null,
            EstimatedWeight: r.EstimatedWeight
        );

    public async Task<Dictionary<string, List<Models.DiscogsTrack>>> GetTracksByMediaAsync(
        int releaseId
    )
    {
        return await ExecuteSafeDictAsync(async () =>
        {
            Release? release = await Client.GetReleaseAsync(releaseId);
            if (release?.Tracklist is null)
                return [];

            Dictionary<string, List<Tracklist>> mediaDict = release.Tracklist.SplitMedia();

            return mediaDict.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(MapTrack).ToList()
            );
        });
    }

    #endregion

    #region Master

    public async Task<Models.DiscogsMaster?> GetMasterAsync(int masterId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            MasterRelease? master = await Client.GetMasterReleaseAsync(masterId);
            if (master is null)
                return null;

            return MapMaster(master);
        });
    }

    static Models.DiscogsMaster MapMaster(MasterRelease m) =>
        new(
            Id: m.MasterId,
            Title: m.Title ?? "",
            Year: m.Year,
            MainReleaseId: m.MainReleaseId,
            MostRecentReleaseId: m.MostRecentReleaseId,
            MainReleaseUrl: m.MainReleaseUrl,
            MostRecentReleaseUrl: m.MostRecentReleaseUrl,
            VersionsUrl: m.VersionsUrl,
            ResourceUrl: m.ResourceUrl,
            Uri: m.Uri,
            DataQuality: m.DataQuality,
            Artists: m.Artists?.Select(MapArtistRef).ToList() ?? [],
            Genres: m.Genres?.ToList() ?? [],
            Styles: m.Styles?.ToList() ?? [],
            Tracks: m.Tracklist?.Select(MapTrack).ToList() ?? [],
            Images: m.Images?.Select(MapImage).ToList() ?? [],
            Videos: m.Videos?.Select(MapVideo).ToList() ?? [],
            QuantityForSale: m.QuantityForSale,
            LowestPrice: (decimal?)m.LowestPrice
        );

    public async Task<List<Models.DiscogsVersion>> GetVersionsAsync(
        int masterId,
        int maxResults = 50
    )
    {
        return await ExecuteSafeListAsync(async () =>
        {
            VersionResults results = await Client.GetVersionsAsync(
                new VersionsCriteria(masterId),
                new PageOptions { PageNumber = 1, PageSize = Math.Min(maxResults, 100) }
            );

            return results.Versions.Take(maxResults).Select(MapVersion).ToList();
        });
    }

    static Models.DiscogsVersion MapVersion(ParkSquare.Discogs.Dto.Version v) =>
        new(
            Id: v.ReleaseId,
            Title: v.Title ?? "",
            Format: v.Format,
            Label: v.Label,
            Country: v.Country,
            Year: ParseYear(v.ReleaseYear),
            CatalogNumber: v.CatalogNumber,
            Status: v.Status,
            ResourceUrl: v.ResourceUrl,
            Thumb: v.Thumb
        );

    #endregion

    #region Mappers

    static Models.DiscogsTrack MapTrack(Tracklist t) =>
        new(
            Position: t.Position ?? "",
            Title: t.Title ?? "",
            Duration: t.Duration,
            Type: t.Type,
            Artists: t.Artists?.Select(MapArtistRef).ToList(),
            ExtraArtists: t.ExtraArtists?.Select(MapArtistRef).ToList()
        );

    static Models.DiscogsFormat MapFormat(Format f) =>
        new(
            Name: f.Name ?? "",
            Quantity: f.Quantity,
            Text: f.Text,
            Descriptions: f.Descriptions?.ToList() ?? []
        );

    static Models.DiscogsLabel MapLabel(Label l) =>
        new(
            Id: l.Id,
            Name: l.Name ?? "",
            CatalogNumber: l.CatalogNumber,
            EntityType: l.EntityType,
            EntityTypeName: l.EntityTypeName,
            ResourceUrl: l.ResourceUrl
        );

    static Models.DiscogsCompany MapCompany(Company c) =>
        new(
            Id: c.Id,
            Name: c.Name ?? "",
            CatalogNumber: c.CatalogNumber,
            EntityType: c.EntityType,
            EntityTypeName: c.EntityTypeName,
            ResourceUrl: c.ResourceUrl
        );

    static Models.DiscogsArtistRef MapArtistRef(ParkSquare.Discogs.Dto.Artist a) =>
        new(
            Id: a.Id,
            Name: a.Name ?? "",
            Anv: a.Alias,
            Join: a.Join,
            Role: a.Role,
            Tracks: a.Tracks,
            ResourceUrl: a.ResourceUrl
        );

    static Models.DiscogsImage MapImage(ParkSquare.Discogs.Dto.Image i) =>
        new(
            Type: i.Type ?? "",
            Uri: i.Uri,
            Uri150: i.UriSmall,
            Width: i.Width,
            Height: i.Height,
            ResourceUrl: i.ResourceUrl
        );

    static Models.DiscogsVideo MapVideo(ParkSquare.Discogs.Dto.Video v) =>
        new(
            Uri: v.Uri ?? "",
            Title: v.Title,
            Description: v.Description,
            Duration: v.Duration,
            Embed: v.Embed
        );

    static Models.DiscogsIdentifier MapIdentifier(Identifier i) =>
        new(Type: i.Type ?? "", Value: i.Value ?? "", Description: i.Description);

    static Models.DiscogsCommunity MapCommunity(Community c) =>
        new(
            Have: c.Have,
            Want: c.Want,
            Rating: c.Rating?.Average,
            RatingCount: c.Rating?.Count,
            Status: c.Status,
            DataQuality: c.DataQuality,
            Submitter: c.Submitter is { } s
                ? new Models.DiscogsSubmitter(s.Username ?? "", s.ResourceUrl)
                : null
        );

    #endregion

    #region Helpers

    static async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await Resilience.ExecuteAsync(
                action,
                "Discogs",
                TimeSpan.FromMilliseconds(1500)
            );
        }
        catch (Exception ex)
        {
            Console.CriticalFailure("Discogs", ex.Message);
            throw;
        }
    }

    static Task<T?> ExecuteSafeAsync<T>(Func<Task<T?>> action)
        where T : class => ExecuteAsync(action);

    static Task<List<T>> ExecuteSafeListAsync<T>(Func<Task<List<T>>> action) =>
        ExecuteAsync(action);

    static Task<Dictionary<TKey, TValue>> ExecuteSafeDictAsync<TKey, TValue>(
        Func<Task<Dictionary<TKey, TValue>>> action
    )
        where TKey : notnull => ExecuteAsync(action);

    static string? ExtractArtist(string? title) =>
        title?.Contains(" - ") == true ? title.Split(" - ")[0].Trim() : null;

    static int? ParseYear(string? year) => int.TryParse(year, out int y) ? y : null;

    #endregion
}
