using System.Text.Json;
using Scripts.Models;

namespace Scripts.Tests.Models;

internal sealed class YouTubeRawDtoContractTests
{
	[Test]
	public async Task DerivedVideoDoesNotInheritFromRawDto() =>
		await Assert.That(typeof(YouTubeVideoRaw).IsAssignableFrom(typeof(YouTubeVideo))).IsFalse();

	[Test]
	public async Task RawVideoSerialization_OmitsTranslatedAndDisplayFields()
	{
		var raw = new YouTubeVideoRaw(
			Title: "Original Title",
			Description: "Original Description",
			Duration: TimeSpan.FromMinutes(5),
			ChannelName: "Channel",
			VideoId: "video-123",
			ChannelId: "channel-456"
		)
		{
			DetectedLanguage = "es",
		};

		var json = JsonSerializer.Serialize(raw);
		using var document = JsonDocument.Parse(json);
		var root = document.RootElement;

		await Assert.That(root.GetProperty("Title").GetString()).IsEqualTo("Original Title");
		await Assert
			.That(root.GetProperty("Description").GetString())
			.IsEqualTo("Original Description");
		await Assert.That(root.GetProperty("ChannelName").GetString()).IsEqualTo("Channel");
		await Assert.That(root.GetProperty("VideoId").GetString()).IsEqualTo("video-123");
		await Assert.That(root.GetProperty("ChannelId").GetString()).IsEqualTo("channel-456");
		await Assert.That(root.GetProperty("DetectedLanguage").GetString()).IsEqualTo("es");

		await Assert.That(root.TryGetProperty("TranslatedTitle", out _)).IsFalse();
		await Assert.That(root.TryGetProperty("TranslatedDescription", out _)).IsFalse();
		await Assert.That(root.TryGetProperty("TranslatedAt", out _)).IsFalse();
		await Assert.That(root.TryGetProperty("DisplayTitle", out _)).IsFalse();
		await Assert.That(root.TryGetProperty("DisplayDescription", out _)).IsFalse();
		await Assert.That(root.TryGetProperty("NeedsTranslation", out _)).IsFalse();
	}

	[Test]
	public async Task DerivedVideoConversion_RoundTripsRawFieldsExplicitly()
	{
		var video = new YouTubeVideo(
			Title: "Translated Title",
			Description: "Translated Description",
			Duration: TimeSpan.FromMinutes(5),
			ChannelName: "Channel",
			VideoId: "video-123",
			ChannelId: "channel-456"
		)
		{
			DetectedLanguage = "es",
			TranslatedTitle = "Título",
			TranslatedDescription = "Descripción",
			TranslatedAt = DateTimeOffset.Parse("2026-06-12T00:00:00Z"),
		};

		var raw = video.ToRaw();

		await Assert.That(raw.Title).IsEqualTo("Translated Title");
		await Assert.That(raw.Description).IsEqualTo("Translated Description");
		await Assert.That(raw.ChannelName).IsEqualTo("Channel");
		await Assert.That(raw.VideoId).IsEqualTo("video-123");
		await Assert.That(raw.ChannelId).IsEqualTo("channel-456");
		await Assert.That(raw.DetectedLanguage).IsEqualTo("es");

		var rebuilt = YouTubeVideo.FromRaw(raw);

		await Assert.That(rebuilt.Title).IsEqualTo(video.Title);
		await Assert.That(rebuilt.Description).IsEqualTo(video.Description);
		await Assert.That(rebuilt.ChannelName).IsEqualTo(video.ChannelName);
		await Assert.That(rebuilt.VideoId).IsEqualTo(video.VideoId);
		await Assert.That(rebuilt.ChannelId).IsEqualTo(video.ChannelId);
		await Assert.That(rebuilt.DetectedLanguage).IsEqualTo("es");
		await Assert.That(rebuilt.TranslatedTitle).IsNull();
		await Assert.That(rebuilt.TranslatedDescription).IsNull();
		await Assert.That(rebuilt.TranslatedAt).IsNull();
	}
}
