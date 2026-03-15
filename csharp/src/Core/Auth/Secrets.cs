namespace CSharpScripts.Core.Auth;

internal static class Secrets
{
	public static string GoogleClientId => GetRequired("GOOGLE_CLIENT_ID");
	public static string GoogleClientSecret => GetRequired("GOOGLE_CLIENT_SECRET");

	public static string YouTubeSpreadsheetId => GetRequired("YOUTUBE_SPREADSHEET_ID");

	public static string LastFmApiKey => GetRequired("LAST_FM_API_KEY");
	public static string LastFmApiSecret => GetRequired("LAST_FM_API_SECRET");
	public static string LastFmSpreadsheetId => GetRequired("LAST_FM_SPREADSHEET_ID");

	public static string DiscogsToken => GetRequired("DISCOGS_USER_TOKEN");

	public static string GoogleDocumentAiProcessorName =>
		GetRequired("GOOGLE_DOCUMENTAI_PROCESSOR_NAME");

	public static string? LibreTranslateUrl => GetEnvironmentVariable("LIBRE_TRANSLATE_URL");

	public static string? AzureTranslatorKey => GetEnvironmentVariable("AZURE_TRANSLATOR_KEY");

	public static string AzureTranslatorRegion =>
		GetEnvironmentVariable("AZURE_TRANSLATOR_REGION") ?? "global";

	public static string AzureDocumentIntelligenceKey =>
		GetRequired("AZURE_DOCUMENT_INTELLIGENCE_KEY");

	public static string AzureDocumentIntelligenceEndpoint =>
		"https://document-intelligence-lance.cognitiveservices.azure.com/";

	private static string GetRequired(string name) =>
		GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is not set");
}
