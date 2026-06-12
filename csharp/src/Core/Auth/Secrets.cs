namespace Scripts.Core.Auth;

internal static class Secrets
{
	public static string GoogleClientId => GetRequired(name: "GOOGLE_CLIENT_ID");
	public static string GoogleClientSecret => GetRequired(name: "GOOGLE_CLIENT_SECRET");

	public static string YouTubeSpreadsheetId =>
		GetRequired(name: "YOUTUBE_SPREADSHEET_ID");

	public static string LastFmApiKey => GetRequired(name: "LAST_FM_API_KEY");
	public static string LastFmApiSecret => GetRequired(name: "LAST_FM_API_SECRET");
	public static string LastFmSpreadsheetId => GetRequired(name: "LAST_FM_SPREADSHEET_ID");

	public static string DiscogsToken => GetRequired(name: "DISCOGS_USER_TOKEN");

	public static string GoogleDocumentAiProcessorName =>
		GetRequired(name: "GOOGLE_DOCUMENTAI_PROCESSOR_NAME");

	public static string? LibreTranslateUrl =>
		GetEnvironmentVariable("LIBRE_TRANSLATE_URL");

	public static string AzureTranslatorEndpoint =>
		GetEnvironmentVariable("AZURE_TRANSLATOR_ENDPOINT")
		?? "https://translator-lance.cognitiveservices.azure.com/";

	public static string AzureDocumentIntelligenceEndpoint =>
		GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT")
		?? "https://document-intelligence-lance.cognitiveservices.azure.com/";

	public static string AzureDocumentIntelligenceModelId =>
		GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_MODEL_ID")
		?? "prebuilt-layout";

	private static string? GetEnvironmentVariable(string name) =>
		Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
		?? Environment.GetEnvironmentVariable(name);

	private static string GetRequired(string name) =>
		GetEnvironmentVariable(name)
		?? throw new InvalidOperationException($"{name} is not set");
}
