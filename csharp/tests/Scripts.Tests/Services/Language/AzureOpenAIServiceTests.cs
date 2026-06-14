using Scripts.Services.Language;

namespace Scripts.Tests.Services.Language;

internal sealed class AzureOpenAIServiceTests
{
	[Test]
	public async Task IsConfigured_ReturnsTrue_WhenEndpointIsConfigured() =>
		await Assert.That(AzureOpenAIService.IsConfigured).IsTrue();

	[Test]
	public async Task TranscribeAudioAsync_TranscribesSvetlanovConductor()
	{
		var wav = await RealTestData.ReadSvetlanovAudio3MinAsync();
		var result = await AzureOpenAIService.TranscribeAudioAsync(
			audioBytes: wav,
			audioFilename: "svetlanov.wav");
		await Assert.That(result).IsNotNull();
		await Assert.That(result).IsNotEmpty();
	}

	[Test]
	public async Task TranscribeAudioSrtAsync_TranscribesSvetlanovConductor()
	{
		var wav = await RealTestData.ReadSvetlanovAudio3MinAsync();
		var result = await AzureOpenAIService.TranscribeAudioSrtAsync(
			audioBytes: wav,
			audioFilename: "svetlanov.wav");
		await Assert.That(result).IsNotNull();
		await Assert.That(result).IsNotEmpty();
	}

	[Test]
	public async Task TranscribeAudioSrtAsync_ContainsSrtTimestamps()
	{
		var wav = await RealTestData.ReadSvetlanovAudio3MinAsync();
		var result = await AzureOpenAIService.TranscribeAudioSrtAsync(
			audioBytes: wav,
			audioFilename: "svetlanov.wav");
		await Assert.That(result).IsNotNull();
		await Assert.That(result!).Contains("-->");
	}

	[Test]
	public async Task TranslateWithLlmAsync_TranslatesBachBMinor_FromGerman()
	{
		var result = await AzureOpenAIService.TranslateWithLlmAsync(
			text: RealTestData.BachBMinor,
			targetLanguage: "en",
			sourceLanguage: "de");
		await Assert.That(result).IsNotNull();
		await Assert.That(result).IsNotEmpty();
	}

	[Test]
	public async Task TranslateWithLlmAsync_TranslatesKarajan_FromFrench()
	{
		var result = await AzureOpenAIService.TranslateWithLlmAsync(
			text: RealTestData.KarajanFrench,
			targetLanguage: "en",
			sourceLanguage: "fr");
		await Assert.That(result).IsNotNull();
		await Assert.That(result).IsNotEmpty();
	}

	[Test]
	public async Task TranslateWithLlmAsync_TranslatesKarajanGermanAutoDetect()
	{
		var result = await AzureOpenAIService.TranslateWithLlmAsync(
			text: RealTestData.KarajanGerman,
			targetLanguage: "en");
		await Assert.That(result).IsNotNull();
		await Assert.That(result).IsNotEmpty();
	}

	[Test]
	public async Task TranscribeSvetlanov_ThenTranslateToEnglish()
	{
		var wav = await RealTestData.ReadSvetlanovAudio3MinAsync();
		var transcription = await AzureOpenAIService.TranscribeAudioAsync(
			audioBytes: wav,
			audioFilename: "svetlanov.wav");
		await Assert.That(transcription).IsNotNull();
		if (transcription is { Length: > 0 } text)
		{
			var translated = await AzureOpenAIService.TranslateWithLlmAsync(
				text: text,
				targetLanguage: "en",
				sourceLanguage: "ru");
			await Assert.That(translated).IsNotNull();
			await Assert.That(translated).IsNotEmpty();
		}
	}
}
