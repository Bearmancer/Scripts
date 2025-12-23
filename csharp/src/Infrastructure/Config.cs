namespace CSharpScripts.Infrastructure;

public static class Config
{
    public static string LastFmApiKey =>
        GetEnvironmentVariable(variable: "LAST_FM_API_KEY")
        ?? throw new InvalidOperationException(message: "LASTFM_API_KEY not set");

    public static string LastFmUsername =>
        GetEnvironmentVariable(variable: "LAST_FM_USERNAME") ?? "kanishknishar";

    public static string LastFmSpreadsheetId =>
        GetEnvironmentVariable(variable: "LAST_FM_SPREADSHEET_ID") ?? "";

    public static string LastFmSpreadsheetTitle =>
        GetEnvironmentVariable(variable: "LAST_FM_SPREADSHEET_TITLE") ?? "Last.fm Scrobbles";

    public static string? DiscogsToken => GetEnvironmentVariable(variable: "DISCOGS_USER_TOKEN");

    public static string YouTubeSpreadsheetId =>
        GetEnvironmentVariable(variable: "YOUTUBE_SPREADSHEET_ID") ?? "";

    public static string YouTubeSpreadsheetTitle =>
        GetEnvironmentVariable(variable: "YOUTUBE_SPREADSHEET_TITLE") ?? "YouTube Playlists";

    private static BaseClientService.Initializer? googleInitializer;

    public static BaseClientService.Initializer GoogleInitializer =>
        googleInitializer ??= new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.GetApplicationDefault(),
            ApplicationName = "CSharpScripts",
        };
}
