namespace CSharpScripts.Core.Auth;

internal static class Secrets
{
	// Temporary default supplied for the Karajan OCR workflow when no endpoint env var is set.
	private const string DefaultAzureDocumentIntelligenceEndpoint =
		"https://document-intelligence-lance.cognitiveservices.azure.com/";

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

	public static string AzureDocumentIntelligenceEndpoint =>
		GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT")
		?? DefaultAzureDocumentIntelligenceEndpoint;

	public static string? AzureDocumentIntelligenceKey =>
		GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_KEY");

	public static string AzureDocumentIntelligenceModelId =>
		GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_MODEL_ID") ?? "prebuilt-layout";

	private static string GetRequired(string name) =>
		GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is not set");
}
