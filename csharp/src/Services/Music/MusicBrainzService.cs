using SearchResult = CSharpScripts.Models.SearchResult;

namespace CSharpScripts.Services.Music;

public sealed class MusicBrainzService(
    string appName = "LancesUtilities",
    string appVersion = "1.0",
    string contact = "user@example.com"
) : IMusicService
{
    public MusicSource Source => MusicSource.MusicBrainz;

    internal Query Query { get; } = new(appName, appVersion, contact);

    #region Caching

    readonly Dictionary<Guid, WorkDetails> workDetailsCache = [];

    Guid? currentWorkId;
    MusicBrainzRecording? currentWorkRecording;
    WorkDetails? currentWorkDetails;

    void UpdateWorkContext(Guid? workId, MusicBrainzRecording recording, WorkDetails? details)
    {
        currentWorkId = workId;
        currentWorkRecording = recording;
        currentWorkDetails = details;
    }

    public void ClearCache()
    {
        workDetailsCache.Clear();
        currentWorkId = null;
        currentWorkRecording = null;
        currentWorkDetails = null;
    }

    #endregion


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
                ISearchResults<ISearchResult<IRelease>> results = await Query.FindReleasesAsync(
                    query,
                    maxResults
                );
                return results
                    .Results.Select(r => new SearchResult(
                        Source: MusicSource.MusicBrainz,
                        Id: r.Item.Id.ToString(),
                        Title: r.Item.Title ?? "",
                        Artist: r.Item.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                        Year: r.Item.Date?.Year,
                        Format: r.Item.Media?.FirstOrDefault()?.Format,
                        Label: r.Item.LabelInfo?.FirstOrDefault()?.Label?.Name,
                        ReleaseType: r.Item.ReleaseGroup?.PrimaryType,
                        Score: r.Score,
                        Country: r.Item.Country,
                        CatalogNumber: r.Item.LabelInfo?.FirstOrDefault()?.CatalogNumber,
                        Status: r.Item.Status,
                        Disambiguation: r.Item.Disambiguation,
                        Genres: r.Item.Genres?.Select(g => g.Name)
                            .Where(n => n is not null)
                            .Cast<string>()
                            .ToList()
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
        Guid guid = Guid.Parse(releaseId);
        MusicBrainzRelease release =
            await GetReleaseAsync(guid, ct)
            ?? throw new InvalidOperationException($"Release not found: {releaseId}");

        List<TrackInfo> tracks = [];

        var releaseCredits = release.Credits.Where(c => !ExcludedRoles.Contains(c.Role)).ToList();

        string? releaseConductor = releaseCredits
            .FirstOrDefault(c => ConductorRoles.Contains(c.Role))
            ?.Name;

        string? releaseOrchestra = releaseCredits
            .FirstOrDefault(c => OrchestraRoles.Contains(c.Role))
            ?.Name;

        List<string> releaseSoloists =
        [
            .. releaseCredits
                .Where(c =>
                    SoloistRoles.Any(r => c.Role.Contains(r, StringComparison.OrdinalIgnoreCase))
                )
                .Select(c => c.Name)
                .Distinct(),
        ];

        string? releaseComposer = release.Artist;
        string? releaseLabel = release.Labels.FirstOrDefault()?.Name;
        string? releaseCatalogNumber = release.Labels.FirstOrDefault()?.CatalogNumber;

        foreach (MusicBrainzMedium medium in release.Media)
        {
            if (maxDiscs.HasValue && medium.Position > maxDiscs.Value)
                break;

            foreach (MusicBrainzTrack track in medium.Tracks)
            {
                int? recordingYear = null;
                string? trackComposer = null;

                if (deepSearch && track.RecordingId.HasValue)
                {
                    MusicBrainzRecording? recording = await GetRecordingAsync(
                        track.RecordingId.Value,
                        ct
                    );
                    if (recording is not null)
                    {
                        recordingYear = recording.FirstReleaseDate?.Year;
                        trackComposer = recording.Artist;
                    }
                }

                tracks.Add(
                    new TrackInfo(
                        DiscNumber: medium.Position,
                        TrackNumber: track.Position,
                        Title: track.Title,
                        Duration: track.Length,
                        RecordingYear: recordingYear,
                        Composer: trackComposer ?? releaseComposer,
                        WorkName: null,
                        Conductor: releaseConductor,
                        Orchestra: releaseOrchestra,
                        Soloists: releaseSoloists,
                        Artist: release.Artist,
                        RecordingVenue: null,
                        RecordingId: track.RecordingId?.ToString()
                    )
                );
            }
        }

        TimeSpan totalDuration = tracks
            .Where(t => t.Duration.HasValue)
            .Aggregate(TimeSpan.Zero, (sum, t) => sum + t.Duration!.Value);

        ReleaseInfo info = new(
            Source: MusicSource.MusicBrainz,
            Id: releaseId,
            Title: release.Title,
            Artist: release.Artist,
            Label: releaseLabel,
            CatalogNumber: releaseCatalogNumber,
            Year: release.Date?.Year,
            Notes: release.Annotation,
            DiscCount: release.Media.Count,
            TrackCount: tracks.Count,
            TotalDuration: totalDuration
        );

        return new ReleaseData(info, tracks);
    }

    /// <summary>
    /// Enrich a single track with deeper metadata (recordings, works, etc.).
    /// MusicBrainz-specific enrichment using work hierarchy.
    /// </summary>
    public async Task<TrackInfo> EnrichTrackAsync(TrackInfo track, CancellationToken ct = default)
    {
        if (IsNullOrEmpty(track.RecordingId))
            return track;

        if (!Guid.TryParse(track.RecordingId, out Guid recordingId))
            return track;

        MusicBrainzRecording? recording = await GetRecordingAsync(recordingId, ct);
        if (recording is null)
            return track;

        WorkDetails? workDetails = null;
        Guid? workId = recording.WorkId;

        if (workId.HasValue && workId == currentWorkId && currentWorkRecording is not null)
        {
            workDetails = currentWorkDetails;
            Console.Debug(
                "[{0}] {1} → Work: {2} (reusing context)",
                track.TrackNumber,
                track.Title,
                recording.WorkName ?? "(none)"
            );
        }
        else
        {
            if (workId.HasValue)
            {
                if (workDetailsCache.TryGetValue(workId.Value, out WorkDetails? cached))
                {
                    workDetails = cached;
                }
                else
                {
                    workDetails = await GetWorkDetailsAsync(workId.Value, ct);
                    if (workDetails is not null)
                        workDetailsCache[workId.Value] = workDetails;
                }
            }

            UpdateWorkContext(workId, recording, workDetails);

            Console.Debug(
                "[{0}] {1} → Work: {2}, Composer: {3}, Parent: {4}",
                track.TrackNumber,
                track.Title,
                recording.WorkName ?? "(none)",
                workDetails?.Composer ?? "(none)",
                workDetails?.ParentWorkName ?? "(none)"
            );
        }

        string? parentWorkName = workDetails?.ParentWorkName;

        TrackInfo enriched = track with
        {
            WorkName = parentWorkName ?? track.WorkName,
            Composer = workDetails?.Composer ?? track.Composer,
            Conductor = recording.Conductor ?? track.Conductor,
            Orchestra = recording.Orchestra ?? track.Orchestra,
            RecordingVenue = recording.RecordingVenue ?? track.RecordingVenue,
            RecordingYear = recording.RecordingDate?.Year ?? track.RecordingYear,
        };

        List<string> missingFields = ApiResponseDumper.ValidateMandatoryFields(
            enriched.Composer,
            enriched.WorkName,
            enriched.Title,
            enriched.Duration,
            enriched.RecordingYear
        );

        if (missingFields.Count > 0)
        {
            ApiResponseDumper.LogMissingFields(
                "",
                track.DiscNumber,
                track.TrackNumber,
                track.Title,
                missingFields
            );
        }

        // Log non-Latin scripts for review
        LanguageDetector.LogNonLatinScript(
            track.DiscNumber,
            track.TrackNumber,
            enriched.WorkName,
            enriched.Composer,
            enriched.Conductor,
            enriched.Orchestra
        );

        return enriched;
    }

    #endregion

    #region Artist

    public async Task<List<SearchResult>> SearchArtistsAsync(
        string artist,
        int maxResults = 25,
        CancellationToken ct = default
    )
    {
        return await ExecuteSafeListAsync(
            async () =>
            {
                ISearchResults<ISearchResult<IArtist>> results = await Query.FindArtistsAsync(
                    $"artist:\"{artist}\"",
                    maxResults
                );
                return results
                    .Results.Select(r => new SearchResult(
                        Source: MusicSource.MusicBrainz,
                        Id: r.Item.Id.ToString(),
                        Title: r.Item.Name ?? "",
                        Artist: r.Item.Name,
                        Year: r.Item.LifeSpan?.Begin?.Year,
                        Format: null,
                        Label: null,
                        ReleaseType: r.Item.Type,
                        Score: r.Score,
                        Country: r.Item.Country,
                        Status: r.Item.Type,
                        Disambiguation: r.Item.Disambiguation
                    ))
                    .ToList();
            },
            ct
        );
    }

    internal async Task<MusicBrainzArtist?> GetArtistAsync(
        Guid artistId,
        CancellationToken ct = default
    )
    {
        return await ExecuteSafeAsync(
            async () =>
            {
                IArtist? artist = await Query.LookupArtistAsync(
                    artistId,
                    Include.Aliases
                        | Include.Annotation
                        | Include.Ratings
                        | Include.Tags
                        | Include.Genres
                );
                if (artist is null)
                    return null;

                return MapArtist(artist);
            },
            ct
        );
    }

    static MusicBrainzArtist MapArtist(IArtist a) =>
        new(
            Id: a.Id,
            Name: a.Name ?? "",
            SortName: a.SortName,
            Type: a.Type,
            Gender: a.Gender,
            Country: a.Country,
            Area: a.Area?.Name,
            Disambiguation: a.Disambiguation,
            BeginDate: a.LifeSpan?.Begin?.NearestDate is DateTime b
                ? DateOnly.FromDateTime(b)
                : null,
            EndDate: a.LifeSpan?.End?.NearestDate is DateTime e ? DateOnly.FromDateTime(e) : null,
            Ended: a.LifeSpan?.Ended,
            Aliases: a.Aliases?.Select(al => al.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            Tags: a.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            Genres: a.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            Annotation: a.Annotation,
            Rating: (double?)a.Rating?.Value,
            RatingVotes: a.Rating?.VoteCount
        );

    #endregion

    #region Recording

    internal async Task<List<MusicBrainzRecording>> SearchRecordingsAsync(
        string? artist = null,
        string? recording = null,
        int maxResults = 25,
        CancellationToken ct = default
    )
    {
        List<string> parts = [];
        if (!IsNullOrWhiteSpace(artist))
            parts.Add($"artist:\"{artist}\"");
        if (!IsNullOrWhiteSpace(recording))
            parts.Add($"recording:\"{recording}\"");

        if (parts.Count == 0)
            return [];

        string query = Join(" AND ", parts);

        return await ExecuteSafeListAsync(
            async () =>
            {
                ISearchResults<ISearchResult<IRecording>> results = await Query.FindRecordingsAsync(
                    query,
                    maxResults
                );
                return results.Results.Select(r => MapRecordingFromSearch(r.Item)).ToList();
            },
            ct
        );
    }

    internal async Task<MusicBrainzRecording?> GetRecordingAsync(
        Guid recordingId,
        CancellationToken ct = default
    )
    {
        return await ExecuteSafeAsync(
            async () =>
            {
                IRecording? recording = await Query.LookupRecordingAsync(
                    recordingId,
                    Include.ArtistCredits
                        | Include.Isrcs
                        | Include.Annotation
                        | Include.Ratings
                        | Include.Tags
                        | Include.Genres
                        | Include.WorkRelationships
                        | Include.ArtistRelationships
                        | Include.PlaceRelationships
                );
                if (recording is null)
                    return null;

                return MapRecording(recording);
            },
            ct
        );
    }

    /// <summary>
    /// Fetch composer and parent work from Work relationships.
    /// - Composer: Work→Artist("composer" or "writer")
    /// - Parent Work: Work→Work("parts" reverse relationship, i.e., this work is "part of" parent)
    /// </summary>
    internal async Task<WorkDetails?> GetWorkDetailsAsync(
        Guid workId,
        CancellationToken ct = default
    )
    {
        return await ExecuteSafeAsync(
            async () =>
            {
                IWork? work = await Query.LookupWorkAsync(
                    workId,
                    Include.ArtistRelationships | Include.WorkRelationships
                );
                if (work?.Relationships is null)
                    return null;

                string? composerName = null;
                string? parentWorkName = null;

                foreach (var rel in work.Relationships)
                {
                    string? relType = rel.Type?.ToLowerInvariant();
                    if (relType is null)
                        continue;

                    if (relType is "composer" or "writer" && rel.Artist is { } artist)
                    {
                        composerName ??= artist.Name;
                    }
                    else if (
                        relType is "parts"
                        && rel.Direction == "backward"
                        && rel.Work is { } parentWork
                    )
                    {
                        parentWorkName = parentWork.Title;
                    }
                }

                if (composerName is not null || parentWorkName is not null)
                {
                    Console.Debug(
                        "Work '{0}' → Composer: {1}, Parent: {2}",
                        work.Title,
                        composerName ?? "(none)",
                        parentWorkName ?? "(none)"
                    );
                }

                return new WorkDetails(composerName, parentWorkName);
            },
            ct
        );
    }

    public async Task<string?> GetWorkComposerAsync(Guid workId, CancellationToken ct = default)
    {
        WorkDetails? details = await GetWorkDetailsAsync(workId, ct);
        return details?.Composer;
    }

    internal async Task<List<MusicBrainzRecording>> BrowseArtistRecordingsAsync(
        Guid artistId,
        int maxResults = 100,
        CancellationToken ct = default
    )
    {
        return await ExecuteSafeListAsync(
            async () =>
            {
                IBrowseResults<IRecording> results = await Query.BrowseArtistRecordingsAsync(
                    artistId,
                    maxResults,
                    inc: Include.ArtistCredits | Include.Isrcs
                );
                return results.Results.Select(MapRecordingFromSearch).ToList();
            },
            ct
        );
    }

    static MusicBrainzRecording MapRecording(IRecording r)
    {
        var workRelationship = r.Relationships?.FirstOrDefault(rel => rel.Work is not null);
        string? workName = workRelationship?.Work?.Title;
        Guid? workId = workRelationship?.Work?.Id;

        string? conductor = null;
        string? orchestra = null;
        string? recordingVenue = null;
        DateOnly? recordingDate = null;

        if (r.Relationships is { } relationships)
        {
            foreach (var rel in relationships)
            {
                string? relType = rel.Type?.ToLowerInvariant();
                if (relType is null)
                    continue;

                if (relType == "conductor" && rel.Artist is { } conductorArtist)
                {
                    conductor = conductorArtist.Name;
                    if (recordingDate is null && rel.Begin?.NearestDate is DateTime beginDate)
                        recordingDate = DateOnly.FromDateTime(beginDate);
                }
                else if (
                    (
                        relType
                            is "orchestra"
                                or "performing orchestra"
                                or "ensemble"
                                or "choir"
                                or "philharmonic"
                        || (
                            relType == "instrument"
                            && rel.Artist?.Name is { } name
                            && (
                                name.Contains("Orchestra", StringComparison.OrdinalIgnoreCase)
                                || name.Contains("Philharmonic", StringComparison.OrdinalIgnoreCase)
                                || name.Contains("Symphony", StringComparison.OrdinalIgnoreCase)
                                || name.Contains("Choir", StringComparison.OrdinalIgnoreCase)
                            )
                        )
                    ) && rel.Artist is { } orchestraArtist
                )
                {
                    orchestra = orchestraArtist.Name;
                }
                else if (relType is "recorded at" or "recorded in" && rel.Place is { } place)
                {
                    recordingVenue = place.Name;
                    if (recordingDate is null && rel.Begin?.NearestDate is DateTime beginDate)
                        recordingDate = DateOnly.FromDateTime(beginDate);
                }
            }
        }

        return new MusicBrainzRecording(
            Id: r.Id,
            Title: r.Title ?? "",
            Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
            ArtistCredit: FormatArtistCredit(r.ArtistCredit),
            Length: r.Length,
            FirstReleaseDate: r.FirstReleaseDate?.NearestDate is DateTime dt
                ? DateOnly.FromDateTime(dt)
                : null,
            IsVideo: r.Video,
            Disambiguation: r.Disambiguation,
            Isrcs: r.Isrcs?.ToList() ?? [],
            Tags: r.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            Genres: r.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            Rating: (double?)r.Rating?.Value,
            RatingVotes: r.Rating?.VoteCount,
            Annotation: r.Annotation,
            WorkName: workName,
            WorkId: workId,
            Conductor: conductor,
            Orchestra: orchestra,
            RecordingVenue: recordingVenue,
            RecordingDate: recordingDate
        );
    }

    static MusicBrainzRecording MapRecordingFromSearch(IRecording r) =>
        new(
            Id: r.Id,
            Title: r.Title ?? "",
            Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
            ArtistCredit: FormatArtistCredit(r.ArtistCredit),
            Length: r.Length,
            FirstReleaseDate: r.FirstReleaseDate?.NearestDate is DateTime dt
                ? DateOnly.FromDateTime(dt)
                : null,
            IsVideo: r.Video,
            Disambiguation: r.Disambiguation,
            Isrcs: r.Isrcs?.ToList() ?? [],
            Tags: [],
            Genres: [],
            Rating: null,
            RatingVotes: null,
            Annotation: null
        );

    #endregion

    #region Release

    public async Task<List<SearchResult>> SearchReleasesAsync(
        string? artist = null,
        string? release = null,
        int? year = null,
        string? label = null,
        string? genre = null,
        int maxResults = 25,
        CancellationToken ct = default
    )
    {
        string query = BuildQuery(artist, release, year, label, genre);
        if (IsNullOrEmpty(query))
            return [];

        return await ExecuteSafeListAsync(
            async () =>
            {
                ISearchResults<ISearchResult<IRelease>> results = await Query.FindReleasesAsync(
                    query,
                    maxResults
                );
                return results
                    .Results.Select(r => new SearchResult(
                        Source: MusicSource.MusicBrainz,
                        Id: r.Item.Id.ToString(),
                        Title: r.Item.Title ?? "",
                        Artist: r.Item.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                        Year: r.Item.Date?.Year,
                        Format: r.Item.Media?.FirstOrDefault()?.Format,
                        Label: r.Item.LabelInfo?.FirstOrDefault()?.Label?.Name,
                        ReleaseType: r.Item.ReleaseGroup?.PrimaryType,
                        Score: r.Score,
                        Country: r.Item.Country,
                        CatalogNumber: r.Item.LabelInfo?.FirstOrDefault()?.CatalogNumber,
                        Status: r.Item.Status,
                        Disambiguation: r.Item.Disambiguation,
                        Genres: r.Item.Genres?.Select(g => g.Name)
                            .Where(n => n is not null)
                            .Cast<string>()
                            .ToList()
                    ))
                    .ToList();
            },
            ct
        );
    }

    public async Task<SearchResult?> SearchFirstReleaseAsync(
        string? artist = null,
        string? release = null,
        int? year = null,
        string? label = null,
        string? genre = null,
        CancellationToken ct = default
    )
    {
        List<SearchResult> results = await SearchReleasesAsync(
            artist,
            release,
            year,
            label,
            genre,
            1,
            ct
        );
        return results.Count > 0 ? results[0] : null;
    }

    internal async Task<MusicBrainzRelease?> GetReleaseAsync(
        Guid releaseId,
        CancellationToken ct = default
    )
    {
        return await ExecuteSafeAsync(
            async () =>
            {
                IRelease? release = await Query.LookupReleaseAsync(
                    releaseId,
                    Include.ArtistCredits
                        | Include.Recordings
                        | Include.Media
                        | Include.Labels
                        | Include.ArtistRelationships
                        | Include.Annotation
                        | Include.Tags
                        | Include.Genres
                        | Include.ReleaseGroups
                );
                if (release is null)
                    return null;

                return MapRelease(release);
            },
            ct
        );
    }

    static MusicBrainzRelease MapRelease(IRelease r)
    {
        List<MusicBrainzMedium> media = [];
        if (r.Media is { } mediaList)
        {
            foreach (IMedium medium in mediaList)
            {
                List<MusicBrainzTrack> tracks = [];
                if (medium.Tracks is { } trackList)
                {
                    foreach (ITrack track in trackList)
                    {
                        tracks.Add(
                            new MusicBrainzTrack(
                                Id: track.Id,
                                Title: track.Title ?? track.Recording?.Title ?? "",
                                Position: track.Position ?? 0,
                                Number: track.Number,
                                Length: track.Length,
                                RecordingId: track.Recording?.Id,
                                ArtistCredit: FormatArtistCredit(track.ArtistCredit)
                            )
                        );
                    }
                }

                media.Add(
                    new MusicBrainzMedium(
                        Position: medium.Position,
                        Format: medium.Format,
                        Title: medium.Title,
                        TrackCount: medium.TrackCount,
                        Tracks: tracks
                    )
                );
            }
        }

        List<MusicBrainzCredit> credits = [];
        if (r.Relationships is { } relationships)
        {
            foreach (IRelationship rel in relationships)
            {
                if (rel.Artist is { } artist && !IsNullOrEmpty(rel.Type))
                {
                    credits.Add(
                        new MusicBrainzCredit(
                            Name: artist.Name ?? "",
                            Role: rel.Type,
                            ArtistId: artist.Id,
                            Attributes: rel.Attributes is { } attrs ? Join(", ", attrs) : null
                        )
                    );
                }
            }
        }

        List<MusicBrainzLabel> labels = [];
        if (r.LabelInfo is { } labelInfo)
        {
            foreach (ILabelInfo li in labelInfo)
            {
                labels.Add(
                    new MusicBrainzLabel(
                        Id: li.Label?.Id,
                        Name: li.Label?.Name,
                        CatalogNumber: li.CatalogNumber
                    )
                );
            }
        }

        return new MusicBrainzRelease(
            Id: r.Id,
            Title: r.Title ?? "",
            Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
            ArtistCredit: FormatArtistCredit(r.ArtistCredit),
            Date: r.Date?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dt) : null,
            Country: r.Country,
            Status: r.Status,
            Barcode: r.Barcode,
            Asin: r.Asin,
            Quality: r.Quality,
            Packaging: r.Packaging,
            Disambiguation: r.Disambiguation,
            ReleaseGroupId: r.ReleaseGroup?.Id,
            ReleaseGroupTitle: r.ReleaseGroup?.Title,
            ReleaseGroupType: r.ReleaseGroup?.PrimaryType,
            Media: media,
            Credits: credits,
            Labels: labels,
            Tags: r.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            Genres: r.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            Annotation: r.Annotation
        );
    }

    #endregion

    #region Release Group

    public async Task<List<SearchResult>> SearchReleaseGroupsAsync(
        string? artist = null,
        string? releaseGroup = null,
        int maxResults = 25,
        CancellationToken ct = default
    )
    {
        List<string> parts = [];
        if (!IsNullOrWhiteSpace(artist))
            parts.Add($"artist:\"{artist}\"");
        if (!IsNullOrWhiteSpace(releaseGroup))
            parts.Add($"releasegroup:\"{releaseGroup}\"");

        if (parts.Count == 0)
            return [];

        string query = Join(" AND ", parts);

        return await ExecuteSafeListAsync(
            async () =>
            {
                ISearchResults<ISearchResult<IReleaseGroup>> results =
                    await Query.FindReleaseGroupsAsync(query, maxResults);
                return results
                    .Results.Select(r => new SearchResult(
                        Source: MusicSource.MusicBrainz,
                        Id: r.Item.Id.ToString(),
                        Title: r.Item.Title ?? "",
                        Artist: r.Item.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                        Year: r.Item.FirstReleaseDate?.Year,
                        Format: null,
                        Label: null,
                        ReleaseType: r.Item.PrimaryType,
                        Score: r.Score,
                        Country: null,
                        Status: r.Item.PrimaryType,
                        Disambiguation: r.Item.Disambiguation
                    ))
                    .ToList();
            },
            ct
        );
    }

    internal async Task<MusicBrainzReleaseGroup?> GetReleaseGroupAsync(
        Guid releaseGroupId,
        CancellationToken ct = default
    )
    {
        return await ExecuteSafeAsync(
            async () =>
            {
                IReleaseGroup? rg = await Query.LookupReleaseGroupAsync(
                    releaseGroupId,
                    Include.ArtistCredits
                        | Include.Releases
                        | Include.Annotation
                        | Include.Ratings
                        | Include.Tags
                        | Include.Genres
                );
                if (rg is null)
                    return null;

                return new MusicBrainzReleaseGroup(
                    Id: rg.Id,
                    Title: rg.Title ?? "",
                    Artist: rg.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                    ArtistCredit: FormatArtistCredit(rg.ArtistCredit),
                    PrimaryType: rg.PrimaryType,
                    SecondaryTypes: rg.SecondaryTypes?.ToList() ?? [],
                    FirstReleaseDate: rg.FirstReleaseDate?.NearestDate is DateTime dt
                        ? DateOnly.FromDateTime(dt)
                        : null,
                    ReleaseCount: rg.Releases?.Count ?? 0,
                    Disambiguation: rg.Disambiguation,
                    Tags: rg.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList()
                        ?? [],
                    Genres: rg.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList()
                        ?? [],
                    Rating: (double?)rg.Rating?.Value,
                    RatingVotes: rg.Rating?.VoteCount,
                    Annotation: rg.Annotation
                );
            },
            ct
        );
    }

    #endregion

    #region Helpers

    static readonly FrozenSet<string> ExcludedRoles = FrozenSet.ToFrozenSet(
        [
            "choir",
            "chorus",
            "chorus master",
            "choir conductor",
            "choir director",
            "vocal",
            "vocals",
            "singer",
            "soprano",
            "mezzo-soprano",
            "alto",
            "contralto",
            "tenor",
            "baritone",
            "bass",
            "bass-baritone",
            "narrator",
            "speaker",
        ],
        StringComparer.OrdinalIgnoreCase
    );

    static readonly FrozenSet<string> ConductorRoles = FrozenSet.ToFrozenSet(
        ["conductor", "director"],
        StringComparer.OrdinalIgnoreCase
    );

    static readonly FrozenSet<string> OrchestraRoles = FrozenSet.ToFrozenSet(
        ["orchestra", "performing orchestra", "ensemble", "performer", "choir", "philharmonic"],
        StringComparer.OrdinalIgnoreCase
    );

    static readonly FrozenSet<string> SoloistRoles = FrozenSet.ToFrozenSet(
        [
            "instrument",
            "piano",
            "violin",
            "viola",
            "cello",
            "double bass",
            "flute",
            "oboe",
            "clarinet",
            "bassoon",
            "horn",
            "trumpet",
            "trombone",
            "tuba",
            "harp",
            "organ",
            "harpsichord",
            "guitar",
            "percussion",
            "timpani",
            "soloist",
        ],
        StringComparer.OrdinalIgnoreCase
    );

    static string? FormatArtistCredit(IReadOnlyList<INameCredit>? credits)
    {
        if (credits is null || credits.Count == 0)
            return null;
        return Join(
            "",
            credits.Select(c => (c.Name ?? c.Artist?.Name ?? "") + (c.JoinPhrase ?? ""))
        );
    }

    static Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct) =>
        Resilience.ExecuteAsync(operation: "MusicBrainz", action: action, ct: ct);

    static Task<T?> ExecuteSafeAsync<T>(Func<Task<T?>> action, CancellationToken ct = default)
        where T : class => ExecuteAsync(action, ct);

    static Task<List<T>> ExecuteSafeListAsync<T>(
        Func<Task<List<T>>> action,
        CancellationToken ct = default
    ) => ExecuteAsync(action, ct);

    static string BuildQuery(
        string? artist,
        string? release,
        int? year,
        string? label,
        string? genre
    )
    {
        List<string> parts = [];
        if (!IsNullOrWhiteSpace(artist))
            parts.Add($"artist:\"{artist}\"");
        if (!IsNullOrWhiteSpace(release))
            parts.Add($"release:\"{release}\"");
        if (!IsNullOrWhiteSpace(label))
            parts.Add($"label:\"{label}\"");
        if (!IsNullOrWhiteSpace(genre))
            parts.Add($"tag:\"{genre}\"");
        if (year.HasValue)
            parts.Add($"date:{year}");
        return Join(" AND ", parts);
    }

    #endregion
}
