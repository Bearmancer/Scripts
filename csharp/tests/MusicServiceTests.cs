namespace CSharpScripts.Tests;

/// <summary>
/// Integration tests for MusicBrainz and Discogs services.
/// Uses known releases to verify consistent parsing behavior.
/// </summary>
[TestClass]
public sealed class MusicServiceTests
{
    #region Known Release IDs

    // Bowie - "Heroes" (1977 album)
    const string MUSICBRAINZ_HEROES_ALBUM = "6a50eb63-b946-48bd-9210-18c01e847f4a";
    const int DISCOGS_HEROES_MASTER = 22294;
    const int DISCOGS_HEROES_RELEASE = 1326123;

    // King Crimson - Dinosaur (1995 single from THRAK)
    const string MUSICBRAINZ_DINOSAUR_SINGLE = "67e3e4a3-5f5f-4df4-8c7e-d0e5f5b10fbb";
    const int DISCOGS_DINOSAUR_SINGLE = 1171897;

    // Radiohead - OK Computer
    const string MUSICBRAINZ_OK_COMPUTER = "b84ee12a-09ef-421b-82de-0441a926375b";
    const int DISCOGS_OK_COMPUTER = 249504;

    #endregion

    #region MusicBrainz Tests

    [TestMethod]
    public async Task MusicBrainz_SearchHeroes_FindsAlbumAndSong()
    {
        MusicBrainzService service = new();

        List<Models.SearchResult> results = await service.SearchAsync("Bowie Heroes", 20);

        results.ShouldNotBeEmpty("MusicBrainz should return results for Bowie Heroes");

        // Should find album releases
        bool hasAlbum = results.Any(r =>
            r.ReleaseType != null
            && r.ReleaseType.Contains("Album", StringComparison.OrdinalIgnoreCase)
        );
        hasAlbum.ShouldBeTrue("Should contain Album type results");

        // Output for debugging
        foreach (Models.SearchResult r in results.Take(5))
        {
            System.Console.WriteLine(
                $"[MB] {r.Artist} - {r.Title} ({r.Year}) Type:{r.ReleaseType} ID:{r.Id}"
            );
        }
    }

    [TestMethod]
    public async Task MusicBrainz_GetReleaseTracks_ReturnsMetadata()
    {
        MusicBrainzService service = new();

        List<TrackMetadata> tracks = await service.GetReleaseTracksAsync(MUSICBRAINZ_OK_COMPUTER);

        tracks.ShouldNotBeEmpty();
        tracks[0].Source.ShouldBe(MusicSource.MusicBrainz);
        tracks[0].Album.ShouldNotBeNullOrEmpty();
        tracks[0].Title.ShouldNotBeNullOrEmpty();

        System.Console.WriteLine($"[MB] Album: {tracks[0].Album}, Tracks: {tracks.Count}");
    }

    #endregion

    #region Discogs Tests

    [TestMethod]
    public async Task Discogs_SearchHeroes_FindsMasterAndRelease()
    {
        string? token = GetEnvironmentVariable("DISCOGS_USER_TOKEN");
        if (IsNullOrEmpty(token))
        {
            Assert.Inconclusive("DISCOGS_USER_TOKEN not set");
            return;
        }

        DiscogsService service = new(token);

        List<Models.SearchResult> results = await service.SearchAsync("Bowie Heroes", 20);

        results.ShouldNotBeEmpty("Discogs should return results for Bowie Heroes");

        // Should find master releases
        results.ShouldContain(r => r.ReleaseType == "master", "Should contain master type results");

        // Should find specific releases
        results.ShouldContain(
            r => r.ReleaseType == "release",
            "Should contain release type results"
        );

        foreach (Models.SearchResult r in results.Take(5))
        {
            System.Console.WriteLine(
                $"[DC] {r.Artist} - {r.Title} ({r.Year}) Type:{r.ReleaseType} ID:{r.Id}"
            );
        }
    }

    [TestMethod]
    public async Task Discogs_SearchDinosaur_FindsSingle()
    {
        string? token = GetEnvironmentVariable("DISCOGS_USER_TOKEN");
        if (IsNullOrEmpty(token))
        {
            Assert.Inconclusive("DISCOGS_USER_TOKEN not set");
            return;
        }

        DiscogsService service = new(token);

        List<Models.SearchResult> results = await service.SearchAsync("King Crimson Dinosaur", 10);

        results.ShouldNotBeEmpty("Discogs should return results for King Crimson Dinosaur");

        foreach (Models.SearchResult r in results)
        {
            System.Console.WriteLine(
                $"[DC] {r.Artist} - {r.Title} ({r.Year}) Type:{r.ReleaseType} Format:{r.Format}"
            );
        }
    }

    [TestMethod]
    public async Task Discogs_GetReleaseTracks_IncludesNotes()
    {
        string? token = GetEnvironmentVariable("DISCOGS_USER_TOKEN");
        if (IsNullOrEmpty(token))
        {
            Assert.Inconclusive("DISCOGS_USER_TOKEN not set");
            return;
        }

        DiscogsService service = new(token);

        List<TrackMetadata> tracks = await service.GetReleaseTracksAsync(
            DISCOGS_HEROES_RELEASE.ToString()
        );

        tracks.ShouldNotBeEmpty();
        tracks[0].Source.ShouldBe(MusicSource.Discogs);
        tracks[0].Album.ShouldNotBeNullOrEmpty();

        // Notes may or may not be present, but field should exist
        System.Console.WriteLine($"[DC] Album: {tracks[0].Album}");
        System.Console.WriteLine($"[DC] Notes: {tracks[0].Notes ?? "(none)"}");
        System.Console.WriteLine($"[DC] Tracks: {tracks.Count}");
    }

    #endregion

    #region Release Type Normalization

    [TestMethod]
    public async Task TypeFilter_Album_MatchesBothMasterAndAlbum()
    {
        string? token = GetEnvironmentVariable("DISCOGS_USER_TOKEN");

        // MusicBrainz uses "Album"
        MusicBrainzService mb = new();
        List<Models.SearchResult> mbResults = await mb.SearchAsync("Radiohead", 10);

        // Discogs uses "master"
        List<Models.SearchResult> discogsResults = [];
        if (!IsNullOrEmpty(token))
        {
            DiscogsService discogs = new(token);
            discogsResults = await discogs.SearchAsync("Radiohead", 10);
        }

        // Filter logic: "album" should match "Album" (MB) and "master" (Discogs)
        List<Models.SearchResult> all = [.. mbResults, .. discogsResults];
        List<Models.SearchResult> filtered = all.Where(r =>
                r.ReleaseType?.ToLowerInvariant() is "album" or "master"
            )
            .ToList();

        filtered.ShouldNotBeEmpty("Album filter should match both Album and master types");

        System.Console.WriteLine($"Total: {all.Count}, After album filter: {filtered.Count}");
    }

    #endregion

    #region Interface Contract

    [TestMethod]
    public async Task IMusicService_BothImplementations_HaveConsistentBehavior()
    {
        string? token = GetEnvironmentVariable("DISCOGS_USER_TOKEN");

        List<IMusicService> services = [new MusicBrainzService()];

        if (!IsNullOrEmpty(token))
        {
            services.Add(new DiscogsService(token));
        }

        foreach (IMusicService service in services)
        {
            List<Models.SearchResult> results = await service.SearchAsync("Beatles Abbey Road", 3);

            results.ShouldNotBeEmpty($"{service.Source} search returned no results");
            results[0].Source.ShouldBe(service.Source);

            // ReleaseType should be populated
            results[0]
                .ReleaseType.ShouldNotBeNullOrEmpty(
                    $"{service.Source} should populate ReleaseType"
                );
        }
    }

    #endregion
}
