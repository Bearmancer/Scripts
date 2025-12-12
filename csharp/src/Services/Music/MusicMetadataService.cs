namespace CSharpScripts.Services.Music;

public record MusicSearchQuery(
    string? Artist = null,
    string? Album = null,
    string? Track = null,
    int? Year = null,
    string? Label = null,
    string? Genre = null,
    string? SortBy = null,
    int MaxResults = 25
);

public record MusicSearchResult(
    string Title,
    string Artist,
    int? Year,
    string Source,
    string ExternalId
);

public record UnifiedRelease(
    string Title,
    string? Artist,
    int? Year,
    string? Country,
    List<string> Genres,
    List<string> Styles,
    List<UnifiedTrack> Tracks,
    List<UnifiedCredit> Credits,
    string Source,
    string ExternalId
);

public record UnifiedTrack(string Position, string Title, TimeSpan? Duration, string? RecordingId);

public record UnifiedCredit(string Name, string Role);

public sealed class MusicMetadataService
{
    public DiscogsService? Discogs { get; }
    public MusicBrainzService MusicBrainz { get; }

    public MusicMetadataService(string? discogsToken = null)
    {
        Discogs = !IsNullOrEmpty(discogsToken) ? new DiscogsService(discogsToken) : null;
        MusicBrainz = new MusicBrainzService();
    }

    public async Task<MusicSearchResult?> SearchAsync(MusicSearchQuery query)
    {
        MusicBrainzSearchResult? mbResult = await MusicBrainz.SearchFirstReleaseAsync(
            artist: query.Artist,
            release: query.Album,
            year: query.Year,
            label: query.Label,
            genre: query.Genre
        );

        if (mbResult is not null)
        {
            return new MusicSearchResult(
                Title: mbResult.Title,
                Artist: mbResult.Artist ?? query.Artist ?? "Unknown",
                Year: mbResult.Year,
                Source: "MusicBrainz",
                ExternalId: mbResult.Id.ToString()
            );
        }

        if (Discogs is null)
            return null;

        DiscogsSearchResult? discogsResult = await Discogs.SearchFirstAsync(
            artist: query.Artist,
            release: query.Album,
            track: query.Track,
            year: query.Year,
            label: query.Label,
            genre: query.Genre
        );

        return discogsResult is null
            ? null
            : new MusicSearchResult(
                Title: discogsResult.Title ?? query.Album ?? "",
                Artist: discogsResult.Artist ?? query.Artist ?? "Unknown",
                Year: discogsResult.Year,
                Source: "Discogs",
                ExternalId: discogsResult.ReleaseId.ToString()
            );
    }

    public async Task<UnifiedRelease?> GetReleaseAsync(
        MusicSearchQuery query,
        bool includeCredits = false
    )
    {
        MusicBrainzSearchResult? mbSearch = await MusicBrainz.SearchFirstReleaseAsync(
            artist: query.Artist,
            release: query.Album,
            year: query.Year,
            label: query.Label,
            genre: query.Genre
        );

        if (mbSearch is not null)
        {
            MusicBrainzRelease? mbRelease = await MusicBrainz.GetReleaseAsync(mbSearch.Id);

            if (mbRelease is not null)
                return ApplyTrackSorting(ToUnified(mbRelease), query.SortBy);
        }

        if (Discogs is null)
            return null;

        DiscogsSearchResult? discogsSearch = await Discogs.SearchFirstAsync(
            artist: query.Artist,
            release: query.Album,
            track: query.Track,
            year: query.Year,
            label: query.Label,
            genre: query.Genre
        );

        if (discogsSearch is null)
            return null;

        DiscogsRelease? discogsRelease = await Discogs.GetReleaseAsync(discogsSearch.ReleaseId);
        return discogsRelease is null
            ? null
            : ApplyTrackSorting(ToUnified(discogsRelease), query.SortBy);
    }

    public async Task<(
        List<DiscogsSearchResult>? Discogs,
        List<MusicBrainzSearchResult> MusicBrainz
    )> SearchBothAsync(MusicSearchQuery query, string source = "both")
    {
        List<DiscogsSearchResult>? discogsResults = null;
        List<MusicBrainzSearchResult> mbResults = [];

        if (source is "both" or "discogs")
        {
            discogsResults = Discogs is null
                ? null
                : await Discogs.SearchAsync(
                    artist: query.Artist,
                    release: query.Album,
                    track: query.Track,
                    year: query.Year,
                    label: query.Label,
                    genre: query.Genre,
                    maxResults: query.MaxResults
                );
        }

        if (source is "both" or "musicbrainz")
        {
            mbResults = await MusicBrainz.SearchReleasesAsync(
                artist: query.Artist,
                release: query.Album,
                year: query.Year,
                label: query.Label,
                genre: query.Genre,
                maxResults: query.MaxResults
            );
        }

        List<DiscogsSearchResult>? sortedDiscogs = SortDiscogs(discogsResults, query.SortBy);
        List<MusicBrainzSearchResult> sortedMb = SortMusicBrainz(mbResults, query.SortBy);

        return (sortedDiscogs, sortedMb);
    }

    static List<DiscogsSearchResult>? SortDiscogs(List<DiscogsSearchResult>? input, string? sortBy)
    {
        if (input is null || IsNullOrWhiteSpace(sortBy))
            return input;

        string key = sortBy.ToLowerInvariant();

        return key switch
        {
            "releasedate" or "releaseyear" => [.. input.OrderByDescending(r => r.Year ?? 0)],
            "artist" => [.. input.OrderBy(r => r.Artist ?? Empty)],
            "album" => [.. input.OrderBy(r => r.Title ?? Empty)],
            "label" => [.. input.OrderBy(r => r.Label ?? Empty)],
            _ => input,
        };
    }

    static List<MusicBrainzSearchResult> SortMusicBrainz(
        List<MusicBrainzSearchResult> input,
        string? sortBy
    )
    {
        if (IsNullOrWhiteSpace(sortBy))
            return input;

        string key = sortBy.ToLowerInvariant();

        return key switch
        {
            "releasedate" or "releaseyear" => [.. input.OrderByDescending(r => r.Year ?? 0)],
            "artist" => [.. input.OrderBy(r => r.Artist ?? Empty)],
            "album" => [.. input.OrderBy(r => r.Title ?? Empty)],
            _ => input,
        };
    }

    public async Task<List<UnifiedCredit>> GetCreditsAsync(MusicSearchQuery query)
    {
        MusicBrainzSearchResult? mbSearch = await MusicBrainz.SearchFirstReleaseAsync(
            artist: query.Artist,
            release: query.Album,
            year: query.Year,
            label: query.Label,
            genre: query.Genre
        );

        if (mbSearch is not null)
        {
            MusicBrainzRelease? mbRelease = await MusicBrainz.GetReleaseAsync(mbSearch.Id);

            if (mbRelease is not null && mbRelease.Credits.Count > 0)
                return [.. mbRelease.Credits.Select(c => new UnifiedCredit(c.Name, c.Role))];
        }

        if (Discogs is null)
            return [];

        DiscogsSearchResult? discogsSearch = await Discogs.SearchFirstAsync(
            artist: query.Artist,
            release: query.Album,
            track: query.Track,
            year: query.Year,
            label: query.Label,
            genre: query.Genre
        );

        if (discogsSearch is null)
            return [];

        DiscogsRelease? discogsRelease = await Discogs.GetReleaseAsync(discogsSearch.ReleaseId);
        return discogsRelease?.Credits.Select(c => new UnifiedCredit(c.Name, c.Role)).ToList()
            ?? [];
    }

    static UnifiedRelease ToUnified(MusicBrainzRelease mb) =>
        new(
            Title: mb.Title,
            Artist: mb.ArtistCredit ?? mb.Artist,
            Year: mb.Date?.Year,
            Country: mb.Country,
            Genres: [],
            Styles: [],
            Tracks:
            [
                .. mb.Tracks.Select(t => new UnifiedTrack(
                    t.Position.ToString(),
                    t.Title,
                    t.Length,
                    t.RecordingId?.ToString() ?? t.Id.ToString()
                )),
            ],
            Credits: [.. mb.Credits.Select(c => new UnifiedCredit(c.Name, c.Role))],
            Source: "MusicBrainz",
            ExternalId: mb.Id.ToString()
        );

    static UnifiedRelease ToUnified(DiscogsRelease discogs) =>
        new(
            Title: discogs.Title,
            Artist: discogs.Artists.Count > 0 ? Join(", ", discogs.Artists) : null,
            Year: discogs.Year,
            Country: discogs.Country,
            Genres: discogs.Genres,
            Styles: discogs.Styles,
            Tracks:
            [
                .. discogs.Tracks.Select(t => new UnifiedTrack(
                    t.Position,
                    t.Title,
                    ParseDuration(t.Duration),
                    null
                )),
            ],
            Credits: [.. discogs.Credits.Select(c => new UnifiedCredit(c.Name, c.Role))],
            Source: "Discogs",
            ExternalId: discogs.Id.ToString()
        );

    static UnifiedRelease ApplyTrackSorting(UnifiedRelease release, string? sortBy)
    {
        if (IsNullOrWhiteSpace(sortBy))
            return release;

        string key = sortBy.ToLowerInvariant();

        if (key == "duration")
        {
            List<UnifiedTrack> sortedTracks =
            [
                .. release.Tracks.OrderByDescending(t => t.Duration ?? TimeSpan.Zero),
            ];
            return release with { Tracks = sortedTracks };
        }

        if (key == "track")
        {
            List<UnifiedTrack> sortedTracks = [.. release.Tracks.OrderBy(t => t.Title)];
            return release with { Tracks = sortedTracks };
        }

        return release;
    }

    static TimeSpan? ParseDuration(string? duration)
    {
        if (IsNullOrEmpty(duration))
            return null;

        string[] parts = duration.Split(':');
        return
            parts.Length == 2
            && int.TryParse(parts[0], out int minutes)
            && int.TryParse(parts[1], out int seconds)
            ? TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds)
            : null;
    }
}
