namespace CSharpScripts.Services.Music;

public sealed class MusicBrainzService(
    string appName = "LancesUtilities",
    string appVersion = "1.0",
    string contact = "user@example.com"
) : IMusicService
{
    internal Query Query { get; } =
        new(application: appName, version: appVersion, contact: contact);
    public MusicSource Source => MusicSource.MusicBrainz;

    private static readonly JsonSerializerOptions DumpOptions = new() { WriteIndented = true };

    private static readonly Lock TraceLock = new();

    private static string GetEntityDumpDirectory(string entity, string id) =>
        Combine(path1: Paths.DumpsDirectory, path2: entity, path3: id);

    private static async Task<T?> ExecuteAndLogAsync<T>(
        Func<Task<T?>> action,
        string entity,
        string id,
        CancellationToken ct
    )
        where T : class
    {
        string dir = GetEntityDumpDirectory(entity: entity, id: id);
        CreateDirectory(path: dir);

        string tracePath = Combine(path1: dir, path2: "http.log");
        using TextWriterTraceListener listener = new(fileName: tracePath);

        lock (TraceLock)
        {
            HttpUtils.TraceSource.Listeners.Add(listener: listener);
            HttpUtils.TraceSource.Switch.Level = SourceLevels.All;
        }

        try
        {
            T? result = await Resilience.ExecuteAsync(
                operation: "MusicBrainz",
                action: action,
                ct: ct
            );

            if (result is { })
            {
                string json = JsonSerializer.Serialize(value: result, options: DumpOptions);
                await WriteAllTextAsync(
                    path: Combine(path1: dir, path2: "data.json"),
                    contents: json,
                    cancellationToken: ct
                );
            }

            return result;
        }
        finally
        {
            lock (TraceLock)
            {
                listener.Flush();
                HttpUtils.TraceSource.Listeners.Remove(listener: listener);
            }
        }
    }

    private readonly Dictionary<Guid, WorkDetails> workDetailsCache = [];
    private Guid? currentWorkId;
    private MusicBrainzRecording? currentWorkRecording;
    private WorkDetails? currentWorkDetails;

    public void ClearCache()
    {
        workDetailsCache.Clear();
        currentWorkId = null;
        currentWorkRecording = null;
        currentWorkDetails = null;
    }

    private void UpdateWorkContext(
        Guid? workId,
        MusicBrainzRecording recording,
        WorkDetails? details
    )
    {
        currentWorkId = workId;
        currentWorkRecording = recording;
        currentWorkDetails = details;
    }

    public async Task<List<SearchResult>> SearchAsync(
        string query,
        int maxResults = 10,
        CancellationToken ct = default
    )
    {
        if (query.Contains(value: "artist:") || query.Contains(value: "release:"))
            return await SearchReleasesAsync(release: query, maxResults: maxResults, ct: ct);

        return await SearchReleasesAsync(release: query, maxResults: maxResults, ct: ct);
    }

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
        string query = BuildQuery(
            artist: artist,
            release: release,
            year: year,
            label: label,
            genre: genre
        );
        if (IsNullOrEmpty(value: query))
            return [];

        return await ExecuteSafeListAsync(
            async () =>
            {
                var results = await Query.FindReleasesAsync(query: query, limit: maxResults);
                return results
                    .Results.Select(r => new SearchResult(
                        Source: MusicSource.MusicBrainz,
                        r.Item.Id.ToString(),
                        r.Item.Title ?? "",
                        Artist: r.Item.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                        Year: r.Item.Date?.NearestDate.Year,
                        Format: r.Item.Media?.FirstOrDefault()?.Format,
                        Label: r.Item.LabelInfo?.FirstOrDefault()?.Label?.Name,
                        ReleaseType: r.Item.ReleaseGroup?.PrimaryType,
                        Score: r.Score,
                        Country: r.Item.Country,
                        CatalogNumber: r.Item.LabelInfo?.FirstOrDefault()?.CatalogNumber,
                        Status: r.Item.Status,
                        Disambiguation: r.Item.Disambiguation,
                        r.Item.Genres?.Select(g => g.Name)
                            .Where(n => n is { })
                            .Cast<string>()
                            .ToList()
                    ))
                    .ToList();
            },
            ct: ct
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
        var results = await SearchReleasesAsync(
            artist: artist,
            release: release,
            year: year,
            label: label,
            genre: genre,
            maxResults: 1,
            ct: ct
        );
        return results.Count > 0 ? results[index: 0] : null;
    }

    public async Task<List<SearchResult>> SearchArtistsAsync(
        string artist,
        int maxResults = 25,
        CancellationToken ct = default
    )
    {
        return await ExecuteSafeListAsync(
            async () =>
            {
                var results = await Query.FindArtistsAsync(
                    $"artist:\"{artist}\"",
                    limit: maxResults
                );
                return results
                    .Results.Select(r => new SearchResult(
                        Source: MusicSource.MusicBrainz,
                        r.Item.Id.ToString(),
                        r.Item.Name ?? "",
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
            ct: ct
        );
    }

    public async Task<List<SearchResult>> SearchReleaseGroupsAsync(
        string? artist = null,
        string? releaseGroup = null,
        int maxResults = 25,
        CancellationToken ct = default
    )
    {
        List<string> parts = [];
        if (!IsNullOrWhiteSpace(value: artist))
            parts.Add($"artist:\"{artist}\"");
        if (!IsNullOrWhiteSpace(value: releaseGroup))
            parts.Add($"releasegroup:\"{releaseGroup}\"");

        if (parts.Count == 0)
            return [];

        string query = Join(separator: " AND ", values: parts);

        return await ExecuteSafeListAsync(
            async () =>
            {
                var results = await Query.FindReleaseGroupsAsync(query: query, limit: maxResults);
                return results
                    .Results.Select(r => new SearchResult(
                        Source: MusicSource.MusicBrainz,
                        r.Item.Id.ToString(),
                        r.Item.Title ?? "",
                        Artist: r.Item.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                        Year: r.Item.FirstReleaseDate?.Year,
                        Format: null,
                        Label: null,
                        ReleaseType: r.Item.PrimaryType,
                        Score: r.Score,
                        Status: r.Item.PrimaryType,
                        Disambiguation: r.Item.Disambiguation
                    ))
                    .ToList();
            },
            ct: ct
        );
    }

    internal async Task<List<MusicBrainzRecording>> SearchRecordingsAsync(
        string? artist = null,
        string? recording = null,
        int maxResults = 25,
        CancellationToken ct = default
    )
    {
        List<string> parts = [];
        if (!IsNullOrWhiteSpace(value: artist))
            parts.Add($"artist:\"{artist}\"");
        if (!IsNullOrWhiteSpace(value: recording))
            parts.Add($"recording:\"{recording}\"");

        if (parts.Count == 0)
            return [];

        string query = Join(separator: " AND ", values: parts);

        return await ExecuteSafeListAsync(
            async () =>
            {
                var results = await Query.FindRecordingsAsync(query: query, limit: maxResults);
                return results.Results.Select(r => MapRecordingFromSearch(r: r.Item)).ToList();
            },
            ct: ct
        );
    }

    public async Task<ReleaseData> GetReleaseAsync(
        string releaseId,
        bool deepSearch = true,
        int? maxDiscs = null,
        CancellationToken ct = default
    )
    {
        var guid = Guid.Parse(input: releaseId);
        var release =
            await GetReleaseAsync(releaseId: guid, ct: ct)
            ?? throw new InvalidOperationException($"Release not found: {releaseId}");

        var credits = ExtractReleaseCredits(release: release);
        var tracks = await BuildTracksAsync(
            release: release,
            credits: credits,
            deepSearch: deepSearch,
            maxDiscs: maxDiscs,
            ct: ct
        );

        var totalDuration = tracks
            .Where(t => t.Duration.HasValue)
            .Aggregate(seed: TimeSpan.Zero, (sum, t) => sum + t.Duration!.Value);

        ReleaseInfo info = new(
            Source: MusicSource.MusicBrainz,
            Id: releaseId,
            Title: release.Title,
            Artist: release.Artist,
            Label: release.Labels.FirstOrDefault()?.Name,
            CatalogNumber: release.Labels.FirstOrDefault()?.CatalogNumber,
            Year: release.Date?.Year,
            Notes: release.Annotation,
            DiscCount: release.Media.Count,
            TrackCount: tracks.Count,
            TotalDuration: totalDuration
        );

        ReleaseData data = new(Info: info, Tracks: tracks);

        Logger.AppendJsonLine(
            path: Logger.GetLogPath(service: ServiceType.Music),
            entry: new LogEntry(
                Timestamp: DateTime.Now.ToString(format: "yyyy/MM/dd HH:mm:ss"),
                Level: LogLevel.Info.ToString(),
                Event: "ReleaseFetched",
                Data: new Dictionary<string, object> { ["Data"] = data }
            )
        );

        return data;
    }

    internal async Task<MusicBrainzRelease?> GetReleaseAsync(
        Guid releaseId,
        CancellationToken ct = default
    )
    {
        return await ExecuteAndLogAsync(
            async () =>
            {
                var release = await Query.LookupReleaseAsync(
                    mbid: releaseId,
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

                return MapRelease(r: release);
            },
            entity: "releases",
            id: releaseId.ToString(),
            ct: ct
        );
    }

    internal async Task<MusicBrainzReleaseGroup?> GetReleaseGroupAsync(
        Guid releaseGroupId,
        CancellationToken ct = default
    )
    {
        return await ExecuteAndLogAsync(
            async () =>
            {
                var rg = await Query.LookupReleaseGroupAsync(
                    mbid: releaseGroupId,
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
                    ArtistCredit: FormatArtistCredit(credits: rg.ArtistCredit),
                    PrimaryType: rg.PrimaryType,
                    SecondaryTypes: rg.SecondaryTypes?.ToList() ?? [],
                    FirstReleaseDate: rg.FirstReleaseDate?.NearestDate is DateTime dt
                        ? DateOnly.FromDateTime(dateTime: dt)
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
            entity: "release-groups",
            id: releaseGroupId.ToString(),
            ct: ct
        );
    }

    internal async Task<MusicBrainzArtist?> GetArtistAsync(
        Guid artistId,
        CancellationToken ct = default
    )
    {
        return await ExecuteAndLogAsync(
            async () =>
            {
                var artist = await Query.LookupArtistAsync(
                    mbid: artistId,
                    Include.Aliases
                        | Include.Annotation
                        | Include.Ratings
                        | Include.Tags
                        | Include.Genres
                );
                if (artist is null)
                    return null;

                return MapArtist(a: artist);
            },
            entity: "artists",
            id: artistId.ToString(),
            ct: ct
        );
    }

    internal async Task<MusicBrainzRecording?> GetRecordingAsync(
        Guid recordingId,
        CancellationToken ct = default
    )
    {
        return await ExecuteAndLogAsync(
            async () =>
            {
                var rec = await Query.LookupRecordingAsync(
                    mbid: recordingId,
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
                if (rec is null)
                    return null;

                return MapRecording(r: rec);
            },
            entity: "recordings",
            id: recordingId.ToString(),
            ct: ct
        );
    }

    internal async Task<WorkDetails?> GetWorkDetailsAsync(
        Guid workId,
        CancellationToken ct = default
    )
    {
        return await ExecuteAndLogAsync(
            async () =>
            {
                var work = await Query.LookupWorkAsync(
                    mbid: workId,
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
                        composerName ??= artist.Name;
                    else if (
                        relType is "parts"
                        && rel.Direction == "backward"
                        && rel.Work is { } parentWork
                    )
                        parentWorkName = parentWork.Title;
                }

                if (composerName is { } || parentWorkName is { })
                    Console.Debug(
                        message: "Work '{0}' → Composer: {1}, Parent: {2}",
                        work.Title,
                        composerName ?? "(none)",
                        parentWorkName ?? "(none)"
                    );

                return new WorkDetails(Composer: composerName, ParentWorkName: parentWorkName);
            },
            entity: "works",
            id: workId.ToString(),
            ct: ct
        );
    }

    public async Task<string?> GetWorkComposerAsync(Guid workId, CancellationToken ct = default)
    {
        var details = await GetWorkDetailsAsync(workId: workId, ct: ct);
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
                var results = await Query.BrowseArtistRecordingsAsync(
                    mbid: artistId,
                    limit: maxResults,
                    inc: Include.ArtistCredits | Include.Isrcs
                );
                return results.Results.Select(selector: MapRecordingFromSearch).ToList();
            },
            ct: ct
        );
    }

    private static ReleaseCredits ExtractReleaseCredits(MusicBrainzRelease release)
    {
        var credits = release.Credits.Where(c => !ExcludedRoles.Contains(item: c.Role)).ToList();

        return new ReleaseCredits(
            Conductor: credits.FirstOrDefault(c => ConductorRoles.Contains(item: c.Role))?.Name,
            Orchestra: credits.FirstOrDefault(c => OrchestraRoles.Contains(item: c.Role))?.Name,
            Soloists:
            [
                .. credits
                    .Where(c =>
                        SoloistRoles.Any(r =>
                            c.Role.Contains(
                                value: r,
                                comparisonType: StringComparison.OrdinalIgnoreCase
                            )
                        )
                    )
                    .Select(c => c.Name)
                    .Distinct(),
            ],
            Composer: release.Artist
        );
    }

    private async Task<List<TrackInfo>> BuildTracksAsync(
        MusicBrainzRelease release,
        ReleaseCredits credits,
        bool deepSearch,
        int? maxDiscs,
        CancellationToken ct
    )
    {
        List<TrackInfo> tracks = [];

        foreach (var medium in release.Media)
        {
            if (maxDiscs.HasValue && medium.Position > maxDiscs.Value)
                break;

            foreach (var track in medium.Tracks)
            {
                int? recordingYear = null;
                string? trackComposer = null;

                if (deepSearch && track.RecordingId.HasValue)
                {
                    var recording = await GetRecordingAsync(
                        recordingId: track.RecordingId.Value,
                        ct: ct
                    );
                    if (recording is { })
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
                        trackComposer ?? credits.Composer,
                        WorkName: null,
                        Conductor: credits.Conductor,
                        Orchestra: credits.Orchestra,
                        Soloists: credits.Soloists,
                        Artist: release.Artist,
                        RecordingVenue: null,
                        track.RecordingId?.ToString()
                    )
                );
            }
        }

        return tracks;
    }

    public async Task<TrackInfo> EnrichTrackAsync(TrackInfo track, CancellationToken ct = default)
    {
        if (IsNullOrEmpty(value: track.RecordingId))
            return track;

        if (!Guid.TryParse(input: track.RecordingId, out var recordingId))
            return track;

        var recording = await GetRecordingAsync(recordingId: recordingId, ct: ct);
        if (recording is null)
            return track;

        WorkDetails? workDetails = null;
        var workId = recording.WorkId;

        if (workId.HasValue && workId == currentWorkId && currentWorkRecording is { })
        {
            workDetails = currentWorkDetails;
            Console.Debug(
                message: "[{0}] {1} → Work: {2} (reusing context)",
                track.TrackNumber,
                track.Title,
                recording.WorkName ?? "(none)"
            );
        }
        else
        {
            if (workId.HasValue)
            {
                if (workDetailsCache.TryGetValue(key: workId.Value, out var cached))
                {
                    workDetails = cached;
                }
                else
                {
                    workDetails = await GetWorkDetailsAsync(workId: workId.Value, ct: ct);
                    if (workDetails is { })
                        workDetailsCache[key: workId.Value] = workDetails;
                }
            }

            UpdateWorkContext(workId: workId, recording: recording, details: workDetails);

            Console.Debug(
                message: "[{0}] {1} → Work: {2}, Composer: {3}, Parent: {4}",
                track.TrackNumber,
                track.Title,
                recording.WorkName ?? "(none)",
                workDetails?.Composer ?? "(none)",
                workDetails?.ParentWorkName ?? "(none)"
            );
        }

        string? parentWorkName = workDetails?.ParentWorkName;

        var enriched = track with
        {
            WorkName = parentWorkName ?? track.WorkName,
            Composer = workDetails?.Composer ?? track.Composer,
            Conductor = recording.Conductor ?? track.Conductor,
            Orchestra = recording.Orchestra ?? track.Orchestra,
            RecordingVenue = recording.RecordingVenue ?? track.RecordingVenue,
            RecordingYear = recording.RecordingDate?.Year ?? track.RecordingYear,
        };

        var missingFields = enriched.GetMissingFields();

        if (missingFields.Count > 0)
            Console.Warning(
                message: "[{0}.{1:D2}] {2} → Missing: {3}",
                track.DiscNumber,
                track.TrackNumber,
                track.Title,
                Join(separator: ", ", values: missingFields)
            );

        LanguageDetector.LogNonLatinScript(
            disc: track.DiscNumber,
            track: track.TrackNumber,
            work: enriched.WorkName,
            composer: enriched.Composer,
            conductor: enriched.Conductor,
            orchestra: enriched.Orchestra
        );

        return enriched;
    }

    private static MusicBrainzRelease MapRelease(IRelease r)
    {
        List<MusicBrainzMedium> media = [];
        if (r.Media is { } mediaList)
            foreach (var medium in mediaList)
            {
                List<MusicBrainzTrack> tracks = [];
                if (medium.Tracks is { } trackList)
                    foreach (var track in trackList)
                        tracks.Add(
                            new MusicBrainzTrack(
                                Id: track.Id,
                                track.Title ?? track.Recording?.Title ?? "",
                                track.Position ?? 0,
                                Number: track.Number,
                                Length: track.Length,
                                RecordingId: track.Recording?.Id,
                                FormatArtistCredit(credits: track.ArtistCredit)
                            )
                        );

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

        List<MusicBrainzCredit> credits = [];
        if (r.Relationships is { } relationships)
            foreach (var rel in relationships)
                if (rel.Artist is { } artist && !IsNullOrEmpty(value: rel.Type))
                    credits.Add(
                        new MusicBrainzCredit(
                            artist.Name ?? "",
                            Role: rel.Type,
                            ArtistId: artist.Id,
                            rel.Attributes is { } attrs
                                ? Join(separator: ", ", values: attrs)
                                : null
                        )
                    );

        List<MusicBrainzLabel> labels = [];
        if (r.LabelInfo is { } labelInfo)
            foreach (var li in labelInfo)
                labels.Add(
                    new MusicBrainzLabel(
                        Id: li.Label?.Id,
                        Name: li.Label?.Name,
                        CatalogNumber: li.CatalogNumber
                    )
                );

        return new MusicBrainzRelease(
            Id: r.Id,
            r.Title ?? "",
            Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
            FormatArtistCredit(credits: r.ArtistCredit),
            r.Date?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dateTime: dt) : null,
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
            r.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            r.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            Annotation: r.Annotation
        );
    }

    private static MusicBrainzArtist MapArtist(IArtist a) =>
        new(
            Id: a.Id,
            a.Name ?? "",
            SortName: a.SortName,
            Type: a.Type,
            Gender: a.Gender,
            Country: a.Country,
            Area: a.Area?.Name,
            Disambiguation: a.Disambiguation,
            a.LifeSpan?.Begin?.NearestDate is DateTime b
                ? DateOnly.FromDateTime(dateTime: b)
                : null,
            a.LifeSpan?.End?.NearestDate is DateTime e ? DateOnly.FromDateTime(dateTime: e) : null,
            Ended: a.LifeSpan?.Ended,
            a.Aliases?.Select(al => al.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            a.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            a.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            Annotation: a.Annotation,
            (double?)a.Rating?.Value,
            RatingVotes: a.Rating?.VoteCount
        );

    private static MusicBrainzRecording MapRecording(IRecording r)
    {
        var workRelationship = r.Relationships?.FirstOrDefault(rel => rel.Work is { });
        string? workName = workRelationship?.Work?.Title;
        var workId = workRelationship?.Work?.Id;

        string? conductor = null;
        string? orchestra = null;
        string? recordingVenue = null;
        DateOnly? recordingDate = null;

        if (r.Relationships is { } relationships)
            foreach (var rel in relationships)
            {
                string? relType = rel.Type?.ToLowerInvariant();
                if (relType is null)
                    continue;

                if (relType == "conductor" && rel.Artist is { } conductorArtist)
                {
                    conductor = conductorArtist.Name;
                    if (recordingDate is null && rel.Begin?.NearestDate is DateTime beginDate)
                        recordingDate = DateOnly.FromDateTime(dateTime: beginDate);
                }
                else if (
                    (
                        relType
                            is "orchestra"
                                or "performing orchestra"
                                or "ensemble"
                                or "choir"
                                or "philharmonic"
                        || relType == "instrument"
                            && rel.Artist?.Name is { } name
                            && (
                                name.Contains(
                                    value: "Orchestra",
                                    comparisonType: StringComparison.OrdinalIgnoreCase
                                )
                                || name.Contains(
                                    value: "Philharmonic",
                                    comparisonType: StringComparison.OrdinalIgnoreCase
                                )
                                || name.Contains(
                                    value: "Symphony",
                                    comparisonType: StringComparison.OrdinalIgnoreCase
                                )
                                || name.Contains(
                                    value: "Choir",
                                    comparisonType: StringComparison.OrdinalIgnoreCase
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
                        recordingDate = DateOnly.FromDateTime(dateTime: beginDate);
                }
            }

        return new MusicBrainzRecording(
            Id: r.Id,
            r.Title ?? "",
            Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
            FormatArtistCredit(credits: r.ArtistCredit),
            Length: r.Length,
            r.FirstReleaseDate?.NearestDate is DateTime dt
                ? DateOnly.FromDateTime(dateTime: dt)
                : null,
            IsVideo: r.Video,
            Disambiguation: r.Disambiguation,
            r.Isrcs?.ToList() ?? [],
            r.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            r.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            (double?)r.Rating?.Value,
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

    private static MusicBrainzRecording MapRecordingFromSearch(IRecording r) =>
        new(
            Id: r.Id,
            r.Title ?? "",
            Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
            FormatArtistCredit(credits: r.ArtistCredit),
            Length: r.Length,
            r.FirstReleaseDate?.NearestDate is DateTime dt
                ? DateOnly.FromDateTime(dateTime: dt)
                : null,
            IsVideo: r.Video,
            Disambiguation: r.Disambiguation,
            r.Isrcs?.ToList() ?? [],
            [],
            [],
            Rating: null,
            RatingVotes: null,
            Annotation: null
        );

    private static readonly FrozenSet<string> ExcludedRoles = FrozenSet.ToFrozenSet(
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
        comparer: StringComparer.OrdinalIgnoreCase
    );

    private static readonly FrozenSet<string> ConductorRoles = FrozenSet.ToFrozenSet(
        ["conductor", "director"],
        comparer: StringComparer.OrdinalIgnoreCase
    );

    private static readonly FrozenSet<string> OrchestraRoles = FrozenSet.ToFrozenSet(
        ["orchestra", "performing orchestra", "ensemble", "performer", "choir", "philharmonic"],
        comparer: StringComparer.OrdinalIgnoreCase
    );

    private static readonly FrozenSet<string> SoloistRoles = FrozenSet.ToFrozenSet(
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
        comparer: StringComparer.OrdinalIgnoreCase
    );

    private static string? FormatArtistCredit(IReadOnlyList<INameCredit>? credits)
    {
        if (credits is null || credits.Count == 0)
            return null;

        return Join(
            separator: "",
            credits.Select(c => (c.Name ?? c.Artist?.Name ?? "") + (c.JoinPhrase ?? ""))
        );
    }

    private static Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct) =>
        Resilience.ExecuteAsync(operation: "MusicBrainz", action: action, ct: ct);

    private static Task<List<T>> ExecuteSafeListAsync<T>(
        Func<Task<List<T>>> action,
        CancellationToken ct = default
    ) => ExecuteAsync(action: action, ct: ct);

    private static string BuildQuery(
        string? artist,
        string? release,
        int? year,
        string? label,
        string? genre
    )
    {
        List<string> parts = [];
        if (!IsNullOrWhiteSpace(value: artist))
            parts.Add($"artist:\"{artist}\"");
        if (!IsNullOrWhiteSpace(value: release))
            parts.Add($"release:\"{release}\"");
        if (!IsNullOrWhiteSpace(value: label))
            parts.Add($"label:\"{label}\"");
        if (!IsNullOrWhiteSpace(value: genre))
            parts.Add($"tag:\"{genre}\"");
        if (year.HasValue)
            parts.Add($"date:{year}");
        return Join(separator: " AND ", values: parts);
    }
}

internal record ReleaseCredits(
    string? Conductor,
    string? Orchestra,
    List<string> Soloists,
    string? Composer
);
