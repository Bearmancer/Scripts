using Azure;
using Azure.AI.Translation.Text;
using Azure.Identity;

namespace CSharpScripts.Services.Language;

internal static class AzureTranslationService
{
	private static readonly TextTranslationClient? Client =
		string.IsNullOrWhiteSpace(Secrets.AzureTranslatorEndpoint)
			? null
			: new TextTranslationClient(new DefaultAzureCredential(), new Uri(Secrets.AzureTranslatorEndpoint));

	internal static bool IsConfigured => Client is not null;

	internal static async Task<TranslationResult?> TranslateAsync(
		string text,
		string? sourceLanguage = null,
		CancellationToken ct = default
	)
	{
		if (Client is null)
			return null;

		Response<IReadOnlyList<TranslatedTextItem>> response = await Client
			.TranslateAsync(targetLanguage: "en", [text], sourceLanguage: sourceLanguage, cancellationToken: ct)
			.ConfigureAwait(continueOnCapturedContext: false);

		TranslatedTextItem item = response.Value[0];
		var detectedLanguage = item.DetectedLanguage?.Language ?? sourceLanguage ?? "unknown";
		var translatedText = item.Translations?[0].Text;

		return translatedText is null
			? null
			: new TranslationResult(Translation: translatedText, DetectedLanguage: detectedLanguage);
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
				cancellationToken: ct
			)
			.ConfigureAwait(continueOnCapturedContext: false);

		List<TranslationResult> results = [with(capacity: response.Value.Count)];
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
