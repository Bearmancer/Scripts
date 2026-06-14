using Scripts.CLI.Subtitle;
using Scripts.Services.Language;

namespace Scripts.Tests.CLI.Subtitle;

internal sealed class SubtitleCommandTests
{
	[Test]
	public async Task IsConfigured_ReturnsTrue_WhenEndpointIsConfigured() =>
		await Assert.That(AzureOpenAIService.IsConfigured).IsTrue();

	[Test]
	public async Task ExecuteAsync_TranscribesSvetlanovAudioDirectly()
	{
		var srt = Path.Combine(
			Path.GetTempPath(),
			$"svetlanov_{Guid.NewGuid():N}.srt");

		var wav = await RealTestData.ReadSvetlanovAudio3MinAsync();
		var content = await AzureOpenAIService.TranscribeAudioSrtAsync(
			audioBytes: wav,
			audioFilename: "svetlanov.wav");
		await File.WriteAllTextAsync(srt, content);
		try
		{
			await Assert.That(File.Exists(srt)).IsTrue();
			await Assert.That(content).IsNotEmpty();
			await Assert.That(content).Contains("-->");
		}
		finally
		{
			if (File.Exists(srt)) File.Delete(srt);
		}
	}

	[Test]
	public async Task Settings_Input_StoresProvidedValue()
	{
		var settings = new SubtitleCommand.Settings { Input = "video.mp4" };
		await Assert.That(settings.Input).IsEqualTo("video.mp4");
	}

	[Test]
	public async Task Settings_Output_DefaultsToNull()
	{
		var settings = new SubtitleCommand.Settings { Input = "video.mp4" };
		await Assert.That(settings.Output).IsNull();
	}

	[Test]
	public async Task Settings_Language_DefaultsToEnglish()
	{
		var settings = new SubtitleCommand.Settings { Input = "video.mp4" };
		await Assert.That(settings.Language).IsEqualTo("en");
	}

	[Test]
	public async Task Settings_AcceptsWavExtension()
	{
		var settings = new SubtitleCommand.Settings { Input = "recording.wav" };
		await Assert.That(settings.Input).IsEqualTo("recording.wav");
	}

	[Test]
	public async Task Settings_AcceptsOpusExtension()
	{
		var settings = new SubtitleCommand.Settings { Input = "music.opus" };
		await Assert.That(settings.Input).IsEqualTo("music.opus");
	}
}
