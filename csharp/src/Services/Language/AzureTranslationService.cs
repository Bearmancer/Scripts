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

	internal static bool IsConfigured => !string.IsNullOrWhiteSpace(Secrets.AzureTranslatorEndpoint);

	internal static async Task<TranslationResult?> TranslateAsync(
		string text,
		string? sourceLanguage = null,
		CancellationToken ct = default
	)
	{
		_ = text ?? throw new ArgumentNullException(nameof(text));
		using var track = Log.Track(new { textLength = text.Length, sourceLanguage });

		if (string.IsNullOrWhiteSpace(text))
			throw new ArgumentException("Text cannot be empty.", nameof(text));

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
			TranslateInputItem input = new(
				text: text,
				target: new TranslationTarget(language: "en"),
				language: sourceLanguage
			);
			Response<TranslatedTextItem> response = await Client
				.TranslateAsync(input: input, cancellationToken: ct)
				.ConfigureAwait(continueOnCapturedContext: false);

			if (response.Value is null)
				return null;

			TranslatedTextItem item = response.Value;
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
		_ = texts ?? throw new ArgumentNullException(nameof(texts));
		using var track = Log.Track(new { batchSize = texts.Count, sourceLanguage });

		if (texts.Count == 0)
			throw new ArgumentException("Text batch cannot be empty.", nameof(texts));

		if (Client is null)
			return [];

		try
		{
			List<TranslateInputItem> inputs = new(capacity: texts.Count);
			foreach (string t in texts)
			{
				inputs.Add(
					new TranslateInputItem(
						text: t,
						target: new TranslationTarget(language: "en"),
						language: sourceLanguage
					)
				);
			}
			Response<IReadOnlyList<TranslatedTextItem>> response = await Client
				.TranslateAsync(inputs: inputs, cancellationToken: ct)
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
