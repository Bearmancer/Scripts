namespace CSharpScripts.Infrastructure;

public static class Config
{
    public static string GoogleClientId =>
        GetEnvironmentVariable("GOOGLE_CLIENT_ID")
        ?? throw new InvalidOperationException("GOOGLE_CLIENT_ID not set");

    public static string GoogleClientSecret =>
        GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
        ?? throw new InvalidOperationException("GOOGLE_CLIENT_SECRET not set");

    public static string LastFmApiKey =>
        GetEnvironmentVariable("LASTFM_API_KEY")
        ?? throw new InvalidOperationException("LASTFM_API_KEY not set");

    public static string LastFmUsername =>
        GetEnvironmentVariable("LASTFM_USERNAME") ?? "kanishknishar";

    public static string LastFmSpreadsheetId =>
        GetEnvironmentVariable("LASTFM_SPREADSHEET_ID") ?? "";

    public static string LastFmSpreadsheetTitle =>
        GetEnvironmentVariable("LASTFM_SPREADSHEET_TITLE") ?? "Last.fm Scrobbles";

    public static string YouTubeSpreadsheetId =>
        GetEnvironmentVariable("YOUTUBE_SPREADSHEET_ID") ?? "";

    public static string YouTubeSpreadsheetTitle =>
        GetEnvironmentVariable("YOUTUBE_SPREADSHEET_TITLE") ?? "YouTube Playlists";
}
