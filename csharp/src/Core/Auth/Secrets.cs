namespace CSharpScripts.Core.Auth;

internal static class Secrets
{
	public static string GoogleClientId { get; } = GetRequired(name: "GOOGLE_CLIENT_ID");
	public static string GoogleClientSecret { get; } = GetRequired(name: "GOOGLE_CLIENT_SECRET");

	public static string YouTubeSpreadsheetId { get; } = GetRequired(name: "YOUTUBE_SPREADSHEET_ID");

	public static string LastFmApiKey { get; } = GetRequired(name: "LAST_FM_API_KEY");
	public static string LastFmApiSecret { get; } = GetRequired(name: "LAST_FM_API_SECRET");
	public static string LastFmSpreadsheetId { get; } = GetRequired(name: "LAST_FM_SPREADSHEET_ID");

	public static string DiscogsToken { get; } = GetRequired(name: "DISCOGS_USER_TOKEN");

	public static string GoogleDocumentAiProcessorName { get; } =
		GetRequired(name: "GOOGLE_DOCUMENTAI_PROCESSOR_NAME");

	public static string? LibreTranslateUrl => GetEnvironmentVariable(variable: "LIBRE_TRANSLATE_URL");

	public static string AzureTranslatorEndpoint =>
		GetEnvironmentVariable(variable: "AZURE_TRANSLATOR_ENDPOINT")
		?? "https://translator-lance.cognitiveservices.azure.com/";

	public static string AzureDocumentIntelligenceEndpoint =>
		GetEnvironmentVariable(variable: "AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT")
		?? "https://document-intelligence-lance.cognitiveservices.azure.com/";

	public static string AzureDocumentIntelligenceModelId =>
		GetEnvironmentVariable(variable: "AZURE_DOCUMENT_INTELLIGENCE_MODEL_ID") ?? "prebuilt-layout";

	private static string GetRequired(string name) =>
		GetEnvironmentVariable(variable: name) ?? throw new InvalidOperationException($"{name} is not set");
}
