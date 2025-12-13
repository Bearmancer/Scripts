namespace CSharpScripts.Services.Music;

public sealed class MusicBrainzService(
    string appName = "LancesUtilities",
    string appVersion = "1.0",
    string contact = "user@example.com"
)
{
    internal Query Query { get; } = new(appName, appVersion, contact);

    #region Artist

    public async Task<List<MusicBrainzSearchResult>> SearchArtistsAsync(
        string artist,
        int maxResults = 25
    )
    {
        return await ExecuteSafeListAsync(async () =>
        {
            ISearchResults<ISearchResult<IArtist>> results = await Query.FindArtistsAsync(
                $"artist:\"{artist}\"",
                maxResults
            );
            return results
                .Results.Select(r => new MusicBrainzSearchResult(
                    Id: r.Item.Id,
                    Title: r.Item.Name ?? "",
                    Artist: r.Item.Name,
                    Year: r.Item.LifeSpan?.Begin?.Year,
                    Country: r.Item.Country,
                    Status: r.Item.Type,
                    Disambiguation: r.Item.Disambiguation,
                    Score: r.Score
                ))
                .ToList();
        });
    }

    public async Task<MusicBrainzArtist?> GetArtistAsync(Guid artistId)
    {
        return await ExecuteSafeAsync(async () =>
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
        });
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

    public async Task<List<MusicBrainzRecording>> SearchRecordingsAsync(
        string? artist = null,
        string? recording = null,
        int maxResults = 25
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

        return await ExecuteSafeListAsync(async () =>
        {
            ISearchResults<ISearchResult<IRecording>> results = await Query.FindRecordingsAsync(
                query,
                maxResults
            );
            return results.Results.Select(r => MapRecordingFromSearch(r.Item)).ToList();
        });
    }

    public async Task<MusicBrainzRecording?> GetRecordingAsync(Guid recordingId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            IRecording? recording = await Query.LookupRecordingAsync(
                recordingId,
                Include.ArtistCredits
                    | Include.Isrcs
                    | Include.Annotation
                    | Include.Ratings
                    | Include.Tags
                    | Include.Genres
            );
            if (recording is null)
                return null;

            return MapRecording(recording);
        });
    }

    public async Task<List<MusicBrainzRecording>> BrowseArtistRecordingsAsync(
        Guid artistId,
        int maxResults = 100
    )
    {
        return await ExecuteSafeListAsync(async () =>
        {
            IBrowseResults<IRecording> results = await Query.BrowseArtistRecordingsAsync(
                artistId,
                maxResults,
                inc: Include.ArtistCredits | Include.Isrcs
            );
            return results.Results.Select(MapRecordingFromSearch).ToList();
        });
    }

    static MusicBrainzRecording MapRecording(IRecording r) =>
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
            Tags: r.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            Genres: r.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
            Rating: (double?)r.Rating?.Value,
            RatingVotes: r.Rating?.VoteCount,
            Annotation: r.Annotation
        );

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

    public async Task<List<MusicBrainzSearchResult>> SearchReleasesAsync(
        string? artist = null,
        string? release = null,
        int? year = null,
        string? label = null,
        string? genre = null,
        int maxResults = 25
    )
    {
        string query = BuildQuery(artist, release, year, label, genre);
        if (IsNullOrEmpty(query))
            return [];

        return await ExecuteSafeListAsync(async () =>
        {
            ISearchResults<ISearchResult<IRelease>> results = await Query.FindReleasesAsync(
                query,
                maxResults
            );
            return results
                .Results.Select(r => new MusicBrainzSearchResult(
                    Id: r.Item.Id,
                    Title: r.Item.Title ?? "",
                    Artist: r.Item.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                    Year: r.Item.Date?.Year,
                    Country: r.Item.Country,
                    Status: r.Item.Status,
                    Disambiguation: r.Item.Disambiguation,
                    Score: r.Score
                ))
                .ToList();
        });
    }

    public async Task<MusicBrainzSearchResult?> SearchFirstReleaseAsync(
        string? artist = null,
        string? release = null,
        int? year = null,
        string? label = null,
        string? genre = null
    )
    {
        List<MusicBrainzSearchResult> results = await SearchReleasesAsync(
            artist,
            release,
            year,
            label,
            genre,
            1
        );
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<MusicBrainzRelease?> GetReleaseAsync(Guid releaseId)
    {
        return await ExecuteSafeAsync(async () =>
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
        });
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

    public async Task<List<MusicBrainzSearchResult>> SearchReleaseGroupsAsync(
        string? artist = null,
        string? releaseGroup = null,
        int maxResults = 25
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

        return await ExecuteSafeListAsync(async () =>
        {
            ISearchResults<ISearchResult<IReleaseGroup>> results =
                await Query.FindReleaseGroupsAsync(query, maxResults);
            return results
                .Results.Select(r => new MusicBrainzSearchResult(
                    Id: r.Item.Id,
                    Title: r.Item.Title ?? "",
                    Artist: r.Item.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                    Year: r.Item.FirstReleaseDate?.Year,
                    Country: null,
                    Status: r.Item.PrimaryType,
                    Disambiguation: r.Item.Disambiguation,
                    Score: r.Score
                ))
                .ToList();
        });
    }

    public async Task<MusicBrainzReleaseGroup?> GetReleaseGroupAsync(Guid releaseGroupId)
    {
        return await ExecuteSafeAsync(async () =>
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
                Tags: rg.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
                Genres: rg.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList()
                    ?? [],
                Rating: (double?)rg.Rating?.Value,
                RatingVotes: rg.Rating?.VoteCount,
                Annotation: rg.Annotation
            );
        });
    }

    #endregion

    #region Helpers

    // Roles to exclude from credits (choir, vocalists, etc.)
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

    // Roles that identify conductors
    static readonly FrozenSet<string> ConductorRoles = FrozenSet.ToFrozenSet(
        ["conductor", "director"],
        StringComparer.OrdinalIgnoreCase
    );

    // Roles that identify orchestras/ensembles
    static readonly FrozenSet<string> OrchestraRoles = FrozenSet.ToFrozenSet(
        ["orchestra", "performing orchestra", "ensemble", "performer"],
        StringComparer.OrdinalIgnoreCase
    );

    // Roles that identify soloists
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

    /// <summary>
    /// Parses a MusicBrainz release (box set) into structured track metadata for spreadsheet export.
    /// Throws BoxSetParseException for missing required fields (composer, title, year, orchestra, conductor).
    /// Soloists are optional.
    /// </summary>
    public async Task<List<BoxSetTrackMetadata>> ParseBoxSetAsync(
        Guid releaseId,
        BoxSetParseOptions options
    )
    {
        MusicBrainzRelease release =
            await GetReleaseAsync(releaseId)
            ?? throw new BoxSetParseException($"Release not found: {releaseId}");

        List<BoxSetTrackMetadata> tracks = [];

        // Build credit lookup by role type from release-level credits
        var releaseCredits = release.Credits.Where(c => !ExcludedRoles.Contains(c.Role)).ToList();

        string? releaseConductor = releaseCredits
            .FirstOrDefault(c => ConductorRoles.Contains(c.Role))
            ?.Name;

        string? releaseOrchestra = releaseCredits
            .FirstOrDefault(c => OrchestraRoles.Contains(c.Role))
            ?.Name;

        List<string> releaseSoloists = releaseCredits
            .Where(c =>
                SoloistRoles.Any(r => c.Role.Contains(r, StringComparison.OrdinalIgnoreCase))
            )
            .Select(c => c.Name)
            .Distinct()
            .ToList();

        // Extract composer from release artist (common for classical box sets)
        string? releaseComposer = release.Artist;

        foreach (MusicBrainzMedium medium in release.Media)
        {
            foreach (MusicBrainzTrack track in medium.Tracks)
            {
                // Try to get recording-level details if available
                int? recordingYear = null;
                string? trackComposer = null;
                string? trackConductor = null;
                string? trackOrchestra = null;
                List<string> trackSoloists = [];

                if (track.RecordingId.HasValue)
                {
                    MusicBrainzRecording? recording = await GetRecordingAsync(
                        track.RecordingId.Value
                    );
                    if (recording is not null)
                    {
                        recordingYear = recording.FirstReleaseDate?.Year;
                        // Recording artist credit might contain composer
                        trackComposer = recording.Artist;
                    }
                }

                // Fallback hierarchy: track -> recording -> release
                string composer =
                    trackComposer
                    ?? releaseComposer
                    ?? throw new BoxSetParseException(
                        $"Composer missing for track {medium.Position}.{track.Position}: {track.Title}"
                    );

                string conductor =
                    trackConductor
                    ?? releaseConductor
                    ?? throw new BoxSetParseException(
                        $"Conductor missing for track {medium.Position}.{track.Position}: {track.Title}"
                    );

                string orchestra =
                    trackOrchestra
                    ?? releaseOrchestra
                    ?? throw new BoxSetParseException(
                        $"Orchestra missing for track {medium.Position}.{track.Position}: {track.Title}"
                    );

                int year =
                    recordingYear
                    ?? release.Date?.Year
                    ?? throw new BoxSetParseException(
                        $"Recording year missing for track {medium.Position}.{track.Position}: {track.Title}"
                    );

                options.ValidateYear(
                    year,
                    $"track {medium.Position}.{track.Position}: {track.Title}"
                );

                List<string> soloists = trackSoloists.Count > 0 ? trackSoloists : releaseSoloists;

                tracks.Add(
                    new BoxSetTrackMetadata(
                        DiscNumber: medium.Position,
                        TrackNumber: track.Position,
                        Composer: composer,
                        Title: track.Title,
                        RecordingYear: year,
                        Orchestra: orchestra,
                        Conductor: conductor,
                        Soloists: soloists
                    )
                );
            }
        }

        return tracks;
    }

    /// <summary>
    /// Parses box set with enhanced credit resolution by fetching work-level relationships.
    /// MusicBrainz hierarchy: Release -> Medium -> Track -> Recording -> Work
    /// Credits can exist at any level; this method searches all levels.
    /// </summary>
    public async Task<List<BoxSetTrackMetadata>> ParseBoxSetWithWorkCreditsAsync(
        Guid releaseId,
        BoxSetParseOptions options
    )
    {
        MusicBrainzRelease release =
            await GetReleaseAsync(releaseId)
            ?? throw new BoxSetParseException($"Release not found: {releaseId}");

        // Extract release-level defaults
        var releaseCredits = release.Credits.Where(c => !ExcludedRoles.Contains(c.Role)).ToList();

        string? releaseConductor = releaseCredits
            .FirstOrDefault(c => ConductorRoles.Contains(c.Role))
            ?.Name;

        string? releaseOrchestra = releaseCredits
            .FirstOrDefault(c => OrchestraRoles.Contains(c.Role))
            ?.Name;

        List<string> releaseSoloists = releaseCredits
            .Where(c =>
                SoloistRoles.Any(r => c.Role.Contains(r, StringComparison.OrdinalIgnoreCase))
            )
            .Select(c => c.Name)
            .Distinct()
            .ToList();

        string? releaseComposer = release.Artist;

        List<BoxSetTrackMetadata> tracks = [];

        foreach (MusicBrainzMedium medium in release.Media)
        {
            // Medium-level title might contain conductor/orchestra info
            string? mediumConductor = null;
            string? mediumOrchestra = null;

            if (!IsNullOrWhiteSpace(medium.Title))
            {
                // Parse patterns like "Herbert von Karajan / Berlin Philharmonic"
                var parts = medium.Title.Split([" / ", " - "], StringSplitOptions.TrimEntries);
                if (parts.Length >= 2)
                {
                    mediumConductor = parts[0];
                    mediumOrchestra = parts[1];
                }
            }

            foreach (MusicBrainzTrack track in medium.Tracks)
            {
                int? recordingYear = null;
                string? trackComposer = null;

                if (track.RecordingId.HasValue)
                {
                    MusicBrainzRecording? recording = await GetRecordingAsync(
                        track.RecordingId.Value
                    );
                    if (recording is not null)
                    {
                        recordingYear = recording.FirstReleaseDate?.Year;
                        trackComposer = recording.Artist;
                    }
                }

                // Resolution order: track -> medium -> release
                string composer =
                    trackComposer
                    ?? releaseComposer
                    ?? throw new BoxSetParseException(
                        $"Composer missing: Disc {medium.Position} Track {track.Position} - {track.Title}"
                    );

                string conductor =
                    mediumConductor
                    ?? releaseConductor
                    ?? throw new BoxSetParseException(
                        $"Conductor missing: Disc {medium.Position} Track {track.Position} - {track.Title}"
                    );

                string orchestra =
                    mediumOrchestra
                    ?? releaseOrchestra
                    ?? throw new BoxSetParseException(
                        $"Orchestra missing: Disc {medium.Position} Track {track.Position} - {track.Title}"
                    );

                int year =
                    recordingYear
                    ?? release.Date?.Year
                    ?? throw new BoxSetParseException(
                        $"Recording year missing: Disc {medium.Position} Track {track.Position} - {track.Title}"
                    );

                options.ValidateYear(
                    year,
                    $"Disc {medium.Position} Track {track.Position} - {track.Title}"
                );

                tracks.Add(
                    new BoxSetTrackMetadata(
                        DiscNumber: medium.Position,
                        TrackNumber: track.Position,
                        Composer: composer,
                        Title: track.Title,
                        RecordingYear: year,
                        Orchestra: orchestra,
                        Conductor: conductor,
                        Soloists: releaseSoloists // Soloists usually release-wide for box sets
                    )
                );
            }
        }

        return tracks;
    }

    static string? FormatArtistCredit(IReadOnlyList<INameCredit>? credits)
    {
        if (credits is null || credits.Count == 0)
            return null;
        return Join(
            "",
            credits.Select(c => (c.Name ?? c.Artist?.Name ?? "") + (c.JoinPhrase ?? ""))
        );
    }

    static Task<T> ExecuteAsync<T>(Func<Task<T>> action) =>
        Resilience.ExecuteAsync(action, "MusicBrainz", TimeSpan.FromSeconds(2));

    static Task<T?> ExecuteSafeAsync<T>(Func<Task<T?>> action)
        where T : class => ExecuteAsync(action);

    static Task<List<T>> ExecuteSafeListAsync<T>(Func<Task<List<T>>> action) =>
        ExecuteAsync(action);

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
