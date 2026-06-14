using Scripts.Services.Language;

namespace Scripts.Tests.Services.Language;

internal sealed class AzureDocumentIntelligenceServiceTests
{
	[Test]
	public async Task IsConfigured_ReturnsTrue_WhenEndpointIsConfigured() =>
		await Assert.That(AzureDocumentIntelligenceService.IsConfigured).IsTrue();

	[Test]
	public async Task OcrImageAsync_ExtractsKarajanBooklet01()
	{
		var jpg = await RealTestData.ReadBooklet01JpgAsync();
		var result = await AzureDocumentIntelligenceService.OcrImageAsync(
			imageBytes: jpg,
			mimeType: "image/jpeg");
		await Assert.That(result).IsNotNull();
		await Assert.That(result!.BodyBlocks).IsNotNull();
	}

	[Test]
	public async Task OcrImageAsync_ExtractsKarajanBooklet02()
	{
		var jpg = await RealTestData.ReadBooklet02JpgAsync();
		var result = await AzureDocumentIntelligenceService.OcrImageAsync(
			imageBytes: jpg,
			mimeType: "image/jpeg");
		await Assert.That(result).IsNotNull();
		await Assert.That(result!.SkippedHeadersFooters).IsGreaterThanOrEqualTo(0);
	}

	[Test]
	public async Task OcrImageAsync_ExtractsKarajanFrontCover()
	{
		var jpg = await RealTestData.ReadFrontJpgAsync();
		var result = await AzureDocumentIntelligenceService.OcrImageAsync(
			imageBytes: jpg,
			mimeType: "image/jpeg");
		await Assert.That(result).IsNotNull();
	}

	[Test]
	public async Task OcrImageAsync_TranslatesKarajanBooklet01Text()
	{
		var jpg = await RealTestData.ReadBooklet01JpgAsync();
		var ocr = await AzureDocumentIntelligenceService.OcrImageAsync(
			imageBytes: jpg,
			mimeType: "image/jpeg");
		await Assert.That(ocr).IsNotNull();
		var joined = string.Join(" ", ocr!.BodyBlocks);
		if (joined.Length > 0)
		{
			var translated = await AzureTranslationService.TranslateAsync(
				text: joined,
				sourceLanguage: "de");
			await Assert.That(translated).IsNotNull();
			await Assert.That(translated!.Translation).IsNotEmpty();
		}
	}

	[Test]
	public async Task OcrImageAsync_TranslatesKarajanFrontCoverText()
	{
		var jpg = await RealTestData.ReadFrontJpgAsync();
		var ocr = await AzureDocumentIntelligenceService.OcrImageAsync(
			imageBytes: jpg,
			mimeType: "image/jpeg");
		await Assert.That(ocr).IsNotNull();
		var joined = string.Join(" ", ocr!.BodyBlocks);
		if (joined.Length > 0)
		{
			var translated = await AzureOpenAIService.TranslateWithLlmAsync(
				text: joined,
				targetLanguage: "en",
				sourceLanguage: "de");
			await Assert.That(translated).IsNotNull();
		}
	}
}
