namespace CSharpScripts.Configuration;

internal static class AuthenticationConfig
{
    static string GetRequired(string name)
    {
        var value = GetEnvironmentVariable(name);
        if (IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Missing environment variable: {name}. Set this before running the application."
            );
        return value;
    }

    internal static readonly string GoogleClientId = GetRequired("GOOGLE_CLIENT_ID");
    internal static readonly string GoogleClientSecret = GetRequired("GOOGLE_CLIENT_SECRET");
    internal static readonly string LastFmApiKey = GetRequired("LASTFM_API_KEY");
    internal const string LastFmUsername = "kanishknishar";
}

internal static class SpreadsheetConfig
{
    internal const string LastFmSpreadsheetId = "1rJzYVDnVRr2pbRp3vd4ZAb1_N42g8gTogoYPCwaJ_Yg";
    internal const string LastFmSpreadsheetTitle = "last.fm scrobbles";

    internal const string? YouTubeSpreadsheetId = null;
    internal const string YouTubeSpreadsheetTitle = "YouTube Playlists";
}
