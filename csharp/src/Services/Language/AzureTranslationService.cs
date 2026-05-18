using Azure;
using Azure.AI.Translation.Text;

namespace CSharpScripts.Services.Language;

internal static class AzureTranslationService
{
	private static readonly string? ApiKey = Secrets.AzureTranslatorKey;
	private static readonly string Region = Secrets.AzureTranslatorRegion;

	private static readonly TextTranslationClient? Client = ApiKey is null
		? null
		: new TextTranslationClient(new AzureKeyCredential(ApiKey), Region);

	internal static bool IsConfigured => ApiKey is { };

	internal static async Task<TranslationResult?> TranslateAsync(
		string text,
		string? sourceLanguage = null,
		CancellationToken ct = default
	)
	{
		if (Client is null)
			return null;

		Response<IReadOnlyList<TranslatedTextItem>> response = await Client
			.TranslateAsync("en", [text], sourceLanguage, ct)
			.ConfigureAwait(false);

		TranslatedTextItem item = response.Value[0];
		var detectedLanguage = item.DetectedLanguage?.Language ?? sourceLanguage ?? "unknown";
		var translatedText = item.Translations?[0].Text;

		return translatedText is null
			? null
			: new TranslationResult(translatedText, detectedLanguage);
	}

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

		Response<IReadOnlyList<TranslatedTextItem>> response = await Client
			.TranslateAsync(
				targetLanguage: "en",
				content: texts,
				sourceLanguage: sourceLanguage,
				ct
			)
			.ConfigureAwait(false);

		var results = new List<TranslationResult>(response.Value.Count);
		foreach (TranslatedTextItem item in response.Value)
		{
			var detectedLanguage = item.DetectedLanguage?.Language ?? sourceLanguage ?? "unknown";
			var translatedText = item.Translations?[0].Text ?? "";
			results.Add(
				new TranslationResult(
					Translation: translatedText,
					DetectedLanguage: detectedLanguage
				)
			);
		}

		return results;
	}
}


