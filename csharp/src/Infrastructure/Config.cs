namespace CSharpScripts.Infrastructure;

public static class Config
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Google APIs
    // ═══════════════════════════════════════════════════════════════════════════

    public static readonly string GoogleClientId =
        GetEnvironmentVariable("GOOGLE_CLIENT_ID")
        ?? throw new InvalidOperationException("Missing: GOOGLE_CLIENT_ID");

    public static readonly string GoogleClientSecret =
        GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
        ?? throw new InvalidOperationException("Missing: GOOGLE_CLIENT_SECRET");

    // ═══════════════════════════════════════════════════════════════════════════
    // Last.fm
    // ═══════════════════════════════════════════════════════════════════════════

    public static readonly string LastFmApiKey =
        GetEnvironmentVariable("LASTFM_API_KEY")
        ?? throw new InvalidOperationException("Missing: LASTFM_API_KEY");

    public static readonly string? LastFmApiSecret = GetEnvironmentVariable("LASTFM_API_SECRET");

    public const string LastFmUsername = "kanishknishar";

    public const string LastFmSpreadsheetId = "1rJzYVDnVRr2pbRp3vd4ZAb1_N42g8gTogoYPCwaJ_Yg";
    public const string LastFmSpreadsheetTitle = "last.fm scrobbles";

    // ═══════════════════════════════════════════════════════════════════════════
    // YouTube
    // ═══════════════════════════════════════════════════════════════════════════

    public const string? YouTubeSpreadsheetId = null;
    public const string YouTubeSpreadsheetTitle = "YouTube Playlists";

    // ═══════════════════════════════════════════════════════════════════════════
    // Discogs
    // ═══════════════════════════════════════════════════════════════════════════

    public static readonly string? DiscogsUserToken = GetEnvironmentVariable("DISCOGS_USER_TOKEN");

    // ═══════════════════════════════════════════════════════════════════════════
    // MusicBrainz
    // ═══════════════════════════════════════════════════════════════════════════

    public static readonly string? MusicBrainzApiKey = GetEnvironmentVariable(
        "MUSICBRAINZ_API_KEY"
    );
}
