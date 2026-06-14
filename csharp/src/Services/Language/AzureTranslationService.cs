using Azure;
using Azure.AI.Translation.Text;

namespace Scripts.Services.Language;

internal static class AzureTranslationService
{
	private static readonly TextTranslationClient? Client = string.IsNullOrWhiteSpace(
		Secrets.AzureTranslatorEndpoint
	)
		? null
		: new TextTranslationClient(
			Core.Auth.AzureAuth.Credential,
			new Uri(Secrets.AzureTranslatorEndpoint)
		);

	internal static bool IsConfigured => !IsNullOrWhiteSpace(Secrets.AzureTranslatorEndpoint);

#if DEBUG
	internal static Func<string, string?, CancellationToken, Task<TranslationResult?>>? TranslateDelegate;
#endif

	internal static async Task<TranslationResult?> TranslateAsync(
		string text,
		string? sourceLanguage = null,
		CancellationToken ct = default
	)
	{
#if DEBUG
		if (TranslateDelegate is { } fake)
			return await fake(text, sourceLanguage, ct);
#endif

		if (Client is null)
			return null;

		string? cached = await TranslationCache.GetCachedAsync(
			text: text,
			targetLang: "en",
			ct: ct
		);
		if (cached is { })
			return new TranslationResult(Translation: cached, DetectedLanguage: sourceLanguage ?? "unknown");

		try
		{
			Response<IReadOnlyList<TranslatedTextItem>> response = await Client
				.TranslateAsync(
					targetLanguage: "en",
					[text],
					sourceLanguage: sourceLanguage,
					cancellationToken: ct
				)
				.ConfigureAwait(continueOnCapturedContext: false);

			if (response.Value is not { Count: > 0 } items)
				return null;

			TranslatedTextItem item = items[0];
			var detectedLanguage = item.DetectedLanguage?.Language ?? sourceLanguage ?? "unknown";
			var translatedText = item.Translations?[0].Text;

			if (translatedText is null)
				return null;

			await TranslationCache.SetCachedAsync(
				text: text,
				targetLang: "en",
				translation: translatedText,
				ct: ct
			);

			return new TranslationResult(
				Translation: translatedText,
				DetectedLanguage: detectedLanguage
			);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning("Azure translation failed: {Error}", ex.Message);
			return null;
		}
	}

	internal static async Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
		IReadOnlyList<string> texts,
		string? sourceLanguage = null,
		CancellationToken ct = default
	)
	{
#if DEBUG
		if (TranslateDelegate is { } fake)
		{
			List<TranslationResult> batchResults = new(capacity: texts.Count);
			foreach (var t in texts)
			{
				var r = await fake(t, sourceLanguage, ct);
				if (r is { })
					batchResults.Add(r);
			}
			return batchResults;
		}
#endif

		if (Client is null)
			return [];

		if (texts.Count == 0)
			return [];

		try
		{
			Response<IReadOnlyList<TranslatedTextItem>> response = await Client
				.TranslateAsync(
					targetLanguage: "en",
					content: texts,
					sourceLanguage: sourceLanguage,
					cancellationToken: ct
				)
				.ConfigureAwait(continueOnCapturedContext: false);

			List<TranslationResult> results = new(capacity: response.Value.Count);
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
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning("Azure batch translation failed: {Error}", ex.Message);
			return [];
		}
	}
}

internal record TranslationResult(string Translation, string DetectedLanguage);
