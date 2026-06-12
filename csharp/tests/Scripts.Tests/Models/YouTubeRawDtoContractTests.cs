using System.Text.Json;
using FluentAssertions;
using Scripts.Models;
using TUnit;

namespace Scripts.Tests.Models;

internal sealed class YouTubeRawDtoContractTests
{
	[Test]
	public void RawVideoSerialization_OmitsTranslatedAndDisplayFields()
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
			DetectedLanguage = "es"
		};

		var json = JsonSerializer.Serialize(raw);
		using var document = JsonDocument.Parse(json);
		var root = document.RootElement;

		root.GetProperty("Title").GetString().Should().Be("Original Title");
		root.GetProperty("Description").GetString().Should().Be("Original Description");
		root.GetProperty("ChannelName").GetString().Should().Be("Channel");
		root.GetProperty("VideoId").GetString().Should().Be("video-123");
		root.GetProperty("ChannelId").GetString().Should().Be("channel-456");
		root.GetProperty("DetectedLanguage").GetString().Should().Be("es");

		root.TryGetProperty("TranslatedTitle", out _).Should().BeFalse();
		root.TryGetProperty("TranslatedDescription", out _).Should().BeFalse();
		root.TryGetProperty("TranslatedAt", out _).Should().BeFalse();
		root.TryGetProperty("DisplayTitle", out _).Should().BeFalse();
		root.TryGetProperty("DisplayDescription", out _).Should().BeFalse();
		root.TryGetProperty("NeedsTranslation", out _).Should().BeFalse();
	}
}
