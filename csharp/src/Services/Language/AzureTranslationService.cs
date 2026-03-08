namespace CSharpScripts.Services.Language;

using Azure;
using Azure.AI.Translation.Text;

internal static class AzureTranslationService
{
	private static readonly string? ApiKey = Secrets.AzureTranslatorKey;
	private static readonly string Region = Secrets.AzureTranslatorRegion;

	private static readonly TextTranslationClient? Client = ApiKey is null
		? null
		: new TextTranslationClient(new AzureKeyCredential(ApiKey), Region);

	internal static bool IsConfigured => ApiKey is not null;

	/// <summary>
	/// Translates a single text to English, detecting the source language automatically.
	/// </summary>
	/// <summary>
	/// Translates a single text to English, detecting the source language automatically.
	/// Returns null when not configured or when translation fails.
	/// </summary>
	internal static async Task<TranslationResult?> TranslateAsync(
		string text,
		string? sourceLanguage = null,
		CancellationToken ct = default
	)
	{
		if (Client is null)
			return null;

		Response<IReadOnlyList<TranslatedTextItem>> response = await Client.TranslateAsync(
			"en",
			[text],
			sourceLanguage,
			ct
		);

		TranslatedTextItem item = response.Value[0];
		var detectedLang = item.DetectedLanguage?.Language ?? sourceLanguage ?? "unknown";
		var translated = item.Translations?[0].Text;

		return translated is null ? null : new TranslationResult(translated, detectedLang);
	}

	/// <summary>
	/// Translates a batch of texts to English (Azure supports up to 100 per request).
	/// The response's <see cref="TranslationResult.DetectedLanguage"/> indicates the source language.
	/// </summary>
	internal static async Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
		IReadOnlyList<string> texts,
		string? sourceLanguage = null,
		CancellationToken ct = default
	)
	{
		if (Client is null)
			return [];

		if (texts.Count == 0)
			return [];

		Response<IReadOnlyList<TranslatedTextItem>> response = await Client.TranslateAsync(
			"en",
			texts,
			sourceLanguage,
			ct
		);

		List<TranslationResult> results = [];
		foreach (TranslatedTextItem item in response.Value)
		{
			var detectedLang = item.DetectedLanguage?.Language ?? sourceLanguage ?? "unknown";
			var translatedText = item.Translations?[0].Text ?? "";
			results.Add(new TranslationResult(translatedText, detectedLang));
		}

		return results;
	}
}
