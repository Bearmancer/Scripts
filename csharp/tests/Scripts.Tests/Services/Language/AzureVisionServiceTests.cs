using Scripts.Services.Language;

namespace Scripts.Tests.Services.Language;

internal sealed class AzureVisionServiceTests
{
	[Test]
	public async Task IsConfigured_ReturnsTrue_WhenEndpointIsConfigured() =>
		await Assert.That(AzureVisionService.IsConfigured).IsTrue();

	[Test]
	public async Task ExtractTextAsync_OcrsKarajanBooklet01()
	{
		var jpg = await RealTestData.ReadBooklet01JpgAsync();
		var result = await AzureVisionService.ExtractTextAsync(imageBytes: jpg);
		await Assert.That(result).IsNotNull();
	}

	[Test]
	public async Task CaptionAsync_CaptionsKarajanBooklet02()
	{
		var jpg = await RealTestData.ReadBooklet02JpgAsync();
		var result = await AzureVisionService.CaptionAsync(imageBytes: jpg);
		await Assert.That(result).IsNotNull();
		await Assert.That(result).IsNotEmpty();
	}

	[Test]
	public async Task TagAsync_TagsKarajanFrontCover()
	{
		var jpg = await RealTestData.ReadFrontJpgAsync();
		var result = await AzureVisionService.TagAsync(imageBytes: jpg);
		await Assert.That(result).IsNotNull();
	}

	[Test]
	public async Task ExtractTextAsync_TranslatesKarajanOcrResult()
	{
		var jpg = await RealTestData.ReadBooklet01JpgAsync();
		var ocrText = await AzureVisionService.ExtractTextAsync(imageBytes: jpg);
		await Assert.That(ocrText).IsNotNull();
		if (ocrText is { Length: > 0 } text)
		{
			var translated = await AzureTranslationService.TranslateAsync(
				text: text,
				sourceLanguage: "de");
			await Assert.That(translated).IsNotNull();
			await Assert.That(translated!.Translation).IsNotEmpty();
		}
	}

	[Test]
	public async Task ExtractTextAsync_TranslatesKarajanFrontCoverOcr()
	{
		var jpg = await RealTestData.ReadFrontJpgAsync();
		var ocrText = await AzureVisionService.ExtractTextAsync(imageBytes: jpg);
		await Assert.That(ocrText).IsNotNull();
		if (ocrText is { Length: > 0 } text)
		{
			var translated = await AzureOpenAIService.TranslateWithLlmAsync(
				text: text,
				targetLanguage: "en",
				sourceLanguage: "de");
			await Assert.That(translated).IsNotNull();
		}
	}
}
