namespace CSharpScripts.Infrastructure;

public static class Config
{
	public static string GoogleClientId => GetEnvVar("GOOGLE_CLIENT_ID");
	public static string GoogleClientSecret => GetEnvVar("GOOGLE_CLIENT_SECRET");

	public static string YouTubeSpreadsheetId => GetEnvVar("YOUTUBE_SPREADSHEET_ID");

	public static string LastFmApiKey => GetEnvVar("LAST_FM_API_KEY");
	public static string LastFmApiSecret => GetEnvVar("LAST_FM_API_SECRET");
	public static string LastFmSpreadsheetId => GetEnvVar("LAST_FM_SPREADSHEET_ID");

	public static string DiscogsToken => GetEnvVar("DISCOGS_USER_TOKEN");

	private static string GetEnvVar(string name) =>
		GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is not set");
}
