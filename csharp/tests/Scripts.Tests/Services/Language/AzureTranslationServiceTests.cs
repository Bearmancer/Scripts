using Scripts.Core;
using Scripts.Services.Language;

namespace Scripts.Tests.Services.Language;

internal sealed class AzureTranslationServiceTests
{
	[After(Test)]
	public void CleanupTranslateDelegate() =>
		AzureTranslationService.TranslateDelegate = null;

	[After(Test)]
	public void CleanupCacheFile() => DeleteCacheFileIfExists();

	private static void DeleteCacheFileIfExists()
	{
		var cachePath = Path.Combine(Paths.StateDirectory, "translation-cache.json");
		if (File.Exists(cachePath))
			File.Delete(cachePath);
	}

	[Test]
	public async Task IsConfigured_ReturnsTrue_WhenEndpointIsConfigured() =>
		await Assert.That(AzureTranslationService.IsConfigured).IsTrue();

	[Test]
	public async Task TranslateAsync_ReturnsTranslationResult_WhenDelegateIsSet()
	{
		AzureTranslationService.TranslateDelegate = (text, sourceLang, ct) =>
			Task.FromResult<TranslationResult?>(
				new TranslationResult(Translation: $"translated_{text}", DetectedLanguage: sourceLang ?? "fr")
			);

		var result = await AzureTranslationService.TranslateAsync(
			text: "Bonjour le monde",
			sourceLanguage: "fr",
			ct: CancellationToken.None
		);

		await Assert.That(result).IsNotNull();
		await Assert.That(result!.Translation).IsEqualTo("translated_Bonjour le monde");
		await Assert.That(result.DetectedLanguage).IsEqualTo("fr");
	}

	[Test]
	public async Task TranslateAsync_ReturnsNull_WhenDelegateReturnsNull()
	{
		AzureTranslationService.TranslateDelegate = (_, _, _) =>
			Task.FromResult<TranslationResult?>(null);

		var result = await AzureTranslationService.TranslateAsync(
			text: "some text",
			sourceLanguage: "fr"
		);

		await Assert.That(result).IsNull();
	}

	[Test]
	public async Task TranslateAsync_ThrowsOperationCanceledException_WhenDelegateIsCancelled()
	{
		AzureTranslationService.TranslateDelegate = async (text, sourceLang, ct) =>
		{
			await Task.Delay(millisecondsDelay: 5000, ct);
			return null;
		};

		using var cts = new CancellationTokenSource(millisecondsDelay: 50);
		await Assert.That(
			async () => await AzureTranslationService.TranslateAsync(text: "test", ct: cts.Token)
		).Throws<OperationCanceledException>();
	}

	[Test]
	public async Task TranslateAsync_ThrowsOperationCanceledException_WhenTokenIsCancelled()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.That(
			async () => await AzureTranslationService.TranslateAsync(text: "Hello", ct: cts.Token)
		).Throws<OperationCanceledException>();
	}

	[Test]
	public async Task TranslateBatchAsync_ReturnsAllResults_WhenDelegateIsSet()
	{
		AzureTranslationService.TranslateDelegate = (text, sourceLang, ct) =>
			Task.FromResult<TranslationResult?>(
				new TranslationResult(Translation: $"batch_{text}", DetectedLanguage: sourceLang ?? "de")
			);

		var results = await AzureTranslationService.TranslateBatchAsync(
			texts: ["eins", "zwei", "drei"],
			sourceLanguage: "de"
		);

		await Assert.That(results.Count).IsEqualTo(3);
		await Assert.That(results[0].Translation).IsEqualTo("batch_eins");
		await Assert.That(results[1].Translation).IsEqualTo("batch_zwei");
		await Assert.That(results[2].Translation).IsEqualTo("batch_drei");
	}

	[Test]
	public async Task TranslateBatchAsync_ReturnsEmpty_WhenInputIsEmpty()
	{
		var results = await AzureTranslationService.TranslateBatchAsync(
			texts: [],
			sourceLanguage: "de"
		);

		await Assert.That(results).IsEmpty();
	}

	[Test]
	public async Task TranslateBatchAsync_ThrowsOperationCanceledException_WhenTokenIsCancelled()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.That(
			async () => await AzureTranslationService.TranslateBatchAsync(texts: ["Hello"], ct: cts.Token)
		).Throws<OperationCanceledException>();
	}

	[Test]
	public async Task TranslationCache_GetCachedAsync_ReturnsNullForUncachedText()
	{
		var uniqueText = $"never_cached_{Guid.NewGuid()}_{DateTime.UtcNow.Ticks}";

		var cached = await TranslationCache.GetCachedAsync(
			text: uniqueText,
			targetLang: "en",
			ct: CancellationToken.None
		);

		await Assert.That(cached).IsNull();
	}

	[Test]
	public async Task TranslationCache_SetAndGet_RoundTrips()
	{
		var text = $"roundtrip_{Guid.NewGuid()}";
		var translation = "bonjour le monde";

		await TranslationCache.SetCachedAsync(
			text: text,
			targetLang: "en",
			translation: translation,
			ct: CancellationToken.None
		);
		var retrieved = await TranslationCache.GetCachedAsync(
			text: text,
			targetLang: "en",
			ct: CancellationToken.None
		);

		await Assert.That(retrieved).IsEqualTo(translation);
	}

	[Test]
	public async Task TranslationCache_KeyIsCaseInsensitive_OnTargetLang()
	{
		var text = $"case_test_{Guid.NewGuid()}";
		var translation = "case insensitive test";

		await TranslationCache.SetCachedAsync(
			text: text,
			targetLang: "EN",
			translation: translation,
			ct: CancellationToken.None
		);
		var retrieved = await TranslationCache.GetCachedAsync(
			text: text,
			targetLang: "en",
			ct: CancellationToken.None
		);

		await Assert.That(retrieved).IsEqualTo(translation);
	}

	[Test]
	public async Task TranslationCache_KeyIsWhitespaceTrimmed()
	{
		var text = $"  whitespace_test_{Guid.NewGuid()}  ";
		var canonicalText = text.Trim();
		var translation = "trimmed test";

		await TranslationCache.SetCachedAsync(
			text: text,
			targetLang: "en",
			translation: translation,
			ct: CancellationToken.None
		);
		var retrieved = await TranslationCache.GetCachedAsync(
			text: canonicalText,
			targetLang: "en",
			ct: CancellationToken.None
		);

		await Assert.That(retrieved).IsEqualTo(translation);
	}

	[Test]
	public async Task TranslationCache_BatchSet_StoresAllEntries()
	{
		var entries = new[]
		{
			($"batch1_{Guid.NewGuid()}", "en", "translation1"),
			($"batch2_{Guid.NewGuid()}", "en", "translation2"),
			($"batch3_{Guid.NewGuid()}", "en", "translation3"),
		};

		await TranslationCache.SetBatchCachedAsync(entries, CancellationToken.None);

		foreach (var (text, lang, expected) in entries)
		{
			var retrieved = await TranslationCache.GetCachedAsync(
				text: text,
				targetLang: lang,
				ct: CancellationToken.None
			);
			await Assert.That(retrieved).IsEqualTo(expected);
		}
	}
}
