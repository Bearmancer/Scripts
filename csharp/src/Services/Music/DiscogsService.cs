using SearchResult = CSharpScripts.Models.SearchResult;

namespace CSharpScripts.Services.Music;

sealed class DiscogsClientConfig(string token) : IClientConfig
{
    public string AuthToken => token;
    public string BaseUrl => "https://api.discogs.com";
}

public sealed class DiscogsService : IMusicService
{
    public MusicSource Source => MusicSource.Discogs;

    internal DiscogsClient Client { get; }

    public DiscogsService(string? token)
    {
        string validToken =
            token ?? throw new ArgumentException("Discogs token is required", nameof(token));
        HttpClient http = new(new HttpClientHandler());
        Client = new DiscogsClient(http, new ApiQueryBuilder(new DiscogsClientConfig(validToken)));
    }

    #region IMusicService

    public async Task<List<SearchResult>> SearchAsync(
        string query,
        int maxResults = 10,
        CancellationToken ct = default
    )
    {
        return await ExecuteSafeListAsync(
            async () =>
            {
                SearchCriteria criteria = new() { Query = query };
                SearchResults results = await Client.SearchAsync(
                    criteria,
                    new PageOptions { PageNumber = 1, PageSize = Math.Min(maxResults, 100) }
                );

                return results
                    .Results.Take(maxResults)
                    .Select(r => new SearchResult(
                        Source: MusicSource.Discogs,
                        Id: (r.ReleaseId > 0 ? r.ReleaseId : r.MasterId).ToString()!,
                        Title: r.Title ?? "",
                        Artist: ExtractArtist(r.Title),
                        Year: ParseYear(r.Year),
                        Format: r.Format is { } fmt ? Join(", ", fmt) : null,
                        Label: r.Label is { } lbl ? Join(", ", lbl) : null,
                        ReleaseType: r.Type, // release, master, artist, label
                        Score: null, // Discogs doesn't provide relevance score
                        Country: r.Country,
                        CatalogNumber: r.CatalogNumber,
                        Status: null, // Discogs doesn't have status like MB
                        Disambiguation: null, // Discogs doesn't have disambiguation
                        Genres: r.Genre?.ToList(),
                        Styles: r.Style?.ToList()
                    ))
                    .ToList();
            },
            ct
        );
    }

    public async Task<ReleaseData> GetReleaseAsync(
        string releaseId,
        bool deepSearch = true,
        int? maxDiscs = null,
        CancellationToken ct = default
    )
    {
        int id = int.Parse(releaseId);
        DiscogsRelease release =
            await GetReleaseAsync(id, ct)
            ?? throw new InvalidOperationException($"Release not found: {releaseId}");

        // Get master for original year if available
        int? originalYear = release.MasterId.HasValue
            ? (await GetMasterAsync(release.MasterId.Value, ct))?.Year
            : null;

        // Extract credits from ExtraArtists
        string? composer = release
            .ExtraArtists.FirstOrDefault(a =>
                a.Role?.Contains("Composed By", StringComparison.OrdinalIgnoreCase) == true
            )
            ?.Name;
        string? conductor = release
            .ExtraArtists.FirstOrDefault(a =>
                a.Role?.Contains("Conductor", StringComparison.OrdinalIgnoreCase) == true
            )
            ?.Name;
        string? orchestra = release
            .ExtraArtists.FirstOrDefault(a =>
                a.Role?.Contains("Orchestra", StringComparison.OrdinalIgnoreCase) == true
            )
            ?.Name;
        List<string> soloists =
        [
            .. release
                .ExtraArtists.Where(a =>
                    a.Role?.Contains("Soloist", StringComparison.OrdinalIgnoreCase) == true
                    || a.Role?.Contains("Performer", StringComparison.OrdinalIgnoreCase) == true
                )
                .Select(a => a.Name)
                .Distinct(),
        ];

        string? primaryArtist = release.Artists.FirstOrDefault()?.Name;
        string? label = release.Labels.FirstOrDefault()?.Name;
        string? catalogNumber = release.Labels.FirstOrDefault()?.CatalogNumber;

        List<TrackInfo> tracks = [];
        int discNum = 1;
        int trackNum = 0;

        foreach (var track in release.Tracks)
        {
            // Detect disc changes from position (e.g., "A1", "B1", "1-1", "2-1")
            if (
                track.Position.StartsWith($"{discNum + 1}-", StringComparison.Ordinal)
                || (
                    discNum == 1
                    && track.Position.StartsWith("1-", StringComparison.Ordinal)
                    && trackNum > 0
                )
            )
            {
                discNum++;
                trackNum = 0;
            }
            trackNum++;

            // Parse recording info from Notes for this disc
            var (recordingYear, recordingVenue) = ParseNotesForRecordingInfo(
                release.Notes,
                discNum
            );

            tracks.Add(
                new TrackInfo(
                    DiscNumber: discNum,
                    TrackNumber: trackNum,
                    Title: track.Title,
                    Duration: ParseDuration(track.Duration),
                    RecordingYear: recordingYear,
                    Composer: composer,
                    WorkName: null,
                    Conductor: conductor,
                    Orchestra: orchestra,
                    Soloists: soloists,
                    Artist: primaryArtist,
                    RecordingVenue: recordingVenue
                )
            );
        }

        TimeSpan totalDuration = tracks
            .Where(t => t.Duration.HasValue)
            .Aggregate(TimeSpan.Zero, (sum, t) => sum + t.Duration!.Value);

        ReleaseInfo info = new(
            Source: MusicSource.Discogs,
            Id: releaseId,
            Title: release.Title,
            Artist: primaryArtist,
            Label: label,
            CatalogNumber: catalogNumber,
            Year: originalYear ?? release.Year,
            Notes: release.Notes,
            DiscCount: discNum,
            TrackCount: tracks.Count,
            TotalDuration: totalDuration
        );

        return new ReleaseData(info, tracks);
    }

    static TimeSpan? ParseDuration(string? duration) =>
        TimeSpan.TryParse(duration, out TimeSpan result) ? result : null;

    #endregion

    #region Notes Parsing

    internal static (int? Year, string? Venue) ParseNotesForRecordingInfo(
        string? notes,
        int discNumber
    )
    {
        if (IsNullOrWhiteSpace(notes))
            return (null, null);

        int? year = null;
        string? venue = null;

        string[] lines = notes.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            bool appliesToDisc = true;
            var discRangeMatch = Regex.Match(
                line,
                @"^(?:CD|Disc)\s*(\d+)(?:\s*[-–]\s*(\d+))?:",
                RegexOptions.IgnoreCase
            );
            if (discRangeMatch.Success)
            {
                int startDisc = int.Parse(discRangeMatch.Groups[1].Value);
                int endDisc = discRangeMatch.Groups[2].Success
                    ? int.Parse(discRangeMatch.Groups[2].Value)
                    : startDisc;
                appliesToDisc = discNumber >= startDisc && discNumber <= endDisc;
            }

            if (!appliesToDisc)
                continue;

            // Extract year: "Recorded [Month] [Year]" or "1954 recording" or "(1954)"
            year ??= ExtractYearFromLine(line);

            // Extract venue: "@ Venue", "at Venue", "in Venue", ", Venue"
            venue ??= ExtractVenueFromLine(line);
        }

        return (year, venue);
    }

    static int? ExtractYearFromLine(string line)
    {
        // Pattern: "Recorded [optional month] [year]"
        var recordedMatch = Regex.Match(
            line,
            @"[Rr]ecorded\s+(?:\w+\s+)?(\d{4})",
            RegexOptions.IgnoreCase
        );
        if (recordedMatch.Success && int.TryParse(recordedMatch.Groups[1].Value, out int y1))
            return y1;

        // Pattern: "[year] recording" or "(year)"
        var yearMatch = Regex.Match(line, @"\b(19\d{2}|20\d{2})\b");
        if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out int y2))
            return y2;

        return null;
    }

    static string? ExtractVenueFromLine(string line)
    {
        // Pattern: "@ Venue" or "at Venue" followed by venue name
        var venueMatch = Regex.Match(
            line,
            @"(?:@|at|in)\s+([A-Z][^,\.\n]+(?:,\s*[A-Z][^,\.\n]+)?)",
            RegexOptions.IgnoreCase
        );
        if (venueMatch.Success)
            return venueMatch.Groups[1].Value.Trim();

        // Pattern: ", Venue" after year (e.g., "1954, Musikverein, Vienna")
        var commaVenueMatch = Regex.Match(
            line,
            @"\d{4},\s*([A-Z][^,\.\n]+(?:,\s*[A-Z][^,\.\n]+)?)",
            RegexOptions.None
        );
        if (commaVenueMatch.Success)
            return commaVenueMatch.Groups[1].Value.Trim();

        return null;
    }

    #endregion

    #region Search (Advanced)

    public async Task<List<DiscogsSearchResult>> SearchAdvancedAsync(
        string? artist = null,
        string? release = null,
        string? track = null,
        int? year = null,
        string? label = null,
        string? genre = null,
        int maxResults = 50,
        CancellationToken ct = default
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

        return await ExecuteSafeListAsync(
            async () =>
            {
                SearchResults results = await Client.SearchAsync(
                    criteria,
                    new PageOptions { PageNumber = 1, PageSize = Math.Min(maxResults, 100) }
                );

                return results.Results.Take(maxResults).Select(MapSearchResult).ToList();
            },
            ct
        );
    }

    public async Task<DiscogsSearchResult?> SearchFirstAsync(
        string? artist = null,
        string? release = null,
        string? track = null,
        int? year = null,
        string? label = null,
        string? genre = null,
        CancellationToken ct = default
    )
    {
        List<DiscogsSearchResult> results = await SearchAdvancedAsync(
            artist,
            release,
            track,
            year,
            label,
            genre,
            1,
            ct
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

    public async Task<DiscogsRelease?> GetReleaseAsync(
        int releaseId,
        CancellationToken ct = default
    )
    {
        return await ExecuteSafeAsync(
            async () =>
            {
                Release? release = await Client.GetReleaseAsync(releaseId);
                if (release is null)
                    return null;

                return MapRelease(release);
            },
            ct
        );
    }

    static DiscogsRelease MapRelease(Release r) =>
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

    public async Task<Dictionary<string, List<DiscogsTrack>>> GetTracksByMediaAsync(
        int releaseId,
        CancellationToken ct = default
    )
    {
        return await ExecuteSafeDictAsync(
            async () =>
            {
                Release? release = await Client.GetReleaseAsync(releaseId);
                if (release?.Tracklist is null)
                    return [];

                Dictionary<string, List<Tracklist>> mediaDict = release.Tracklist.SplitMedia();

                return mediaDict.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Select(MapTrack).ToList()
                );
            },
            ct
        );
    }

    #endregion

    #region Master

    public async Task<DiscogsMaster?> GetMasterAsync(int masterId, CancellationToken ct = default)
    {
        return await ExecuteSafeAsync(
            async () =>
            {
                MasterRelease? master = await Client.GetMasterReleaseAsync(masterId);
                if (master is null)
                    return null;

                return MapMaster(master);
            },
            ct
        );
    }

    static DiscogsMaster MapMaster(MasterRelease m) =>
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

    public async Task<List<DiscogsVersion>> GetVersionsAsync(
        int masterId,
        int maxResults = 50,
        CancellationToken ct = default
    )
    {
        return await ExecuteSafeListAsync(
            async () =>
            {
                VersionResults results = await Client.GetVersionsAsync(
                    new VersionsCriteria(masterId),
                    new PageOptions { PageNumber = 1, PageSize = Math.Min(maxResults, 100) }
                );

                return results.Versions.Take(maxResults).Select(MapVersion).ToList();
            },
            ct
        );
    }

    static DiscogsVersion MapVersion(ParkSquare.Discogs.Dto.Version v) =>
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

    static DiscogsTrack MapTrack(Tracklist t) =>
        new(
            Position: t.Position ?? "",
            Title: t.Title ?? "",
            Duration: t.Duration,
            Type: t.Type,
            Artists: t.Artists?.Select(MapArtistRef).ToList(),
            ExtraArtists: t.ExtraArtists?.Select(MapArtistRef).ToList()
        );

    static DiscogsFormat MapFormat(Format f) =>
        new(
            Name: f.Name ?? "",
            Quantity: f.Quantity,
            Text: f.Text,
            Descriptions: f.Descriptions?.ToList() ?? []
        );

    static DiscogsLabel MapLabel(Label l) =>
        new(
            Id: l.Id,
            Name: l.Name ?? "",
            CatalogNumber: l.CatalogNumber,
            EntityType: l.EntityType,
            EntityTypeName: l.EntityTypeName,
            ResourceUrl: l.ResourceUrl
        );

    static DiscogsCompany MapCompany(Company c) =>
        new(
            Id: c.Id,
            Name: c.Name ?? "",
            CatalogNumber: c.CatalogNumber,
            EntityType: c.EntityType,
            EntityTypeName: c.EntityTypeName,
            ResourceUrl: c.ResourceUrl
        );

    static DiscogsArtistRef MapArtistRef(Artist a) =>
        new(
            Id: a.Id,
            Name: a.Name ?? "",
            Anv: a.Alias,
            Join: a.Join,
            Role: a.Role,
            Tracks: a.Tracks,
            ResourceUrl: a.ResourceUrl
        );

    static DiscogsImage MapImage(Image i) =>
        new(
            Type: i.Type ?? "",
            Uri: i.Uri,
            Uri150: i.UriSmall,
            Width: i.Width,
            Height: i.Height,
            ResourceUrl: i.ResourceUrl
        );

    static DiscogsVideo MapVideo(Video v) =>
        new(
            Uri: v.Uri ?? "",
            Title: v.Title,
            Description: v.Description,
            Duration: v.Duration,
            Embed: v.Embed
        );

    static DiscogsIdentifier MapIdentifier(Identifier i) =>
        new(Type: i.Type ?? "", Value: i.Value ?? "", Description: i.Description);

    static DiscogsCommunity MapCommunity(Community c) =>
        new(
            Have: c.Have,
            Want: c.Want,
            Rating: c.Rating?.Average,
            RatingCount: c.Rating?.Count,
            Status: c.Status,
            DataQuality: c.DataQuality,
            Submitter: c.Submitter is { } s
                ? new DiscogsSubmitter(s.Username ?? "", s.ResourceUrl)
                : null
        );

    #endregion

    #region Helpers

    static async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        try
        {
            return await Resilience.ExecuteAsync(operation: "Discogs", action: action, ct: ct);
        }
        catch (Exception ex)
        {
            Console.CriticalFailure("Discogs", ex.Message);
            throw;
        }
    }

    static Task<T?> ExecuteSafeAsync<T>(Func<Task<T?>> action, CancellationToken ct)
        where T : class => ExecuteAsync(action, ct);

    static Task<List<T>> ExecuteSafeListAsync<T>(
        Func<Task<List<T>>> action,
        CancellationToken ct
    ) => ExecuteAsync(action, ct);

    static Task<Dictionary<TKey, TValue>> ExecuteSafeDictAsync<TKey, TValue>(
        Func<Task<Dictionary<TKey, TValue>>> action,
        CancellationToken ct
    )
        where TKey : notnull => ExecuteAsync(action, ct);

    static string? ExtractArtist(string? title) =>
        title?.Contains(" - ") == true ? title.Split(" - ")[0].Trim() : null;

    static int? ParseYear(string? year) => int.TryParse(year, out int y) ? y : null;

    #endregion
}
