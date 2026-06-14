using Scripts.Services.Language;

namespace Scripts.Tests.Services.Language;

internal sealed class AzureTranslationServiceTests
{
	[Test]
	public async Task IsConfigured_ReturnsTrue_WhenEndpointIsConfigured() =>
		await Assert.That(AzureTranslationService.IsConfigured).IsTrue();

	[Test]
	public async Task TranslateAsync_TranslatesBachBMinor_FromGerman()
	{
		var result = await AzureTranslationService.TranslateAsync(
			text: RealTestData.BachBMinor,
			sourceLanguage: "de");
		await Assert.That(result).IsNotNull();
		await Assert.That(result!.Translation).IsNotEmpty();
	}

	[Test]
	public async Task TranslateAsync_TranslatesKarajan_FromFrench()
	{
		var result = await AzureTranslationService.TranslateAsync(
			text: RealTestData.KarajanFrench,
			sourceLanguage: "fr");
		await Assert.That(result).IsNotNull();
		await Assert.That(result!.Translation).IsNotEmpty();
	}

	[Test]
	public async Task TranslateAsync_TranslatesKarajan_FromItalian()
	{
		var result = await AzureTranslationService.TranslateAsync(
			text: RealTestData.KarajanItalian,
			sourceLanguage: "it");
		await Assert.That(result).IsNotNull();
		await Assert.That(result!.DetectedLanguage).IsEqualTo("it");
	}

	[Test]
	public async Task TranslateAsync_AutoDetectsKarajanGerman()
	{
		var result = await AzureTranslationService.TranslateAsync(text: RealTestData.KarajanGerman);
		await Assert.That(result).IsNotNull();
		await Assert.That(result!.DetectedLanguage).IsEqualTo("de");
	}

	[Test]
	public async Task TranslateBatchAsync_TranslatesMultipleClassicalPieces()
	{
		var texts = new[]
		{
			RealTestData.Beethoven9,
			RealTestData.BachBMinor,
			RealTestData.MozartRequiem,
		};
		var results = await AzureTranslationService.TranslateBatchAsync(texts: texts);
		await Assert.That(results).Count().IsEqualTo(3);
		foreach (var r in results)
		{
			await Assert.That(r.Translation).IsNotEmpty();
		}
	}

	[Test]
	public async Task TranslateAsync_TranslatesKarajanOcrFromBooklet()
	{
		var jpg = await RealTestData.ReadBooklet01JpgAsync();
		var ocrResult = await AzureDocumentIntelligenceService.OcrImageAsync(
			imageBytes: jpg,
			mimeType: "image/jpeg");
		await Assert.That(ocrResult).IsNotNull();
		var ocrText = string.Join(" ", ocrResult!.BodyBlocks);
		if (ocrText.Length > 0)
		{
			var translated = await AzureTranslationService.TranslateAsync(
				text: ocrText,
				sourceLanguage: "de");
			await Assert.That(translated).IsNotNull();
			await Assert.That(translated!.Translation).IsNotEmpty();
		}
	}
}
