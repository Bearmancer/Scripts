namespace CSharpScripts.Core.Auth;

internal static class Secrets
{
	public static string GoogleClientId { get; } = GetRequired("GOOGLE_CLIENT_ID");
	public static string GoogleClientSecret { get; } = GetRequired("GOOGLE_CLIENT_SECRET");

	public static string YouTubeSpreadsheetId { get; } = GetRequired("YOUTUBE_SPREADSHEET_ID");

	public static string LastFmApiKey { get; } = GetRequired("LAST_FM_API_KEY");
	public static string LastFmApiSecret { get; } = GetRequired("LAST_FM_API_SECRET");
	public static string LastFmSpreadsheetId { get; } = GetRequired("LAST_FM_SPREADSHEET_ID");

	public static string DiscogsToken { get; } = GetRequired("DISCOGS_USER_TOKEN");

	public static string GoogleDocumentAiProcessorName { get; } =
		GetRequired("GOOGLE_DOCUMENTAI_PROCESSOR_NAME");

	public static string? LibreTranslateUrl => GetEnvironmentVariable("LIBRE_TRANSLATE_URL");

	public static string? AzureTranslatorKey => GetEnvironmentVariable("AZURE_TRANSLATOR_KEY");

	public static string AzureTranslatorRegion { get; } =
		GetEnvironmentVariable("AZURE_TRANSLATOR_REGION") ?? "global";

	public static string AzureDocumentIntelligenceKey { get; } =
		GetRequired("AZURE_DOCUMENT_INTELLIGENCE_KEY");

	public static string AzureDocumentIntelligenceEndpoint =>
		GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT")
		?? "https://document-intelligence-lance.cognitiveservices.azure.com/";

	public static string AzureDocumentIntelligenceModelId =>
		GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_MODEL_ID") ?? "prebuilt-layout";

	private static string GetRequired(string name) =>
		GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is not set");
}
