using TUnit;
using FluentAssertions;
using Scripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class VideoEntityTests
{
	[Test]
	public void Video_HasRequired_Properties()
	{
		var props = typeof(Video).GetProperties().Select(p => p.Name).ToList();

		props.Should().Contain("Id");
		props.Should().Contain("Url");
		props.Should().Contain("Title");
		props.Should().Contain("Description");
		props.Should().Contain("ChannelName");
		props.Should().Contain("UploadDate");
		props.Should().Contain("SyncedAt");
		props.Should().Contain("Metadata");
	}

	[Test]
	public void Video_Url_IsString()
	{
		var prop = typeof(Video).GetProperty("Url");
		prop.Should().NotBeNull();
		prop!.PropertyType.Should().Be<string>();
	}

	[Test]
	public void Video_UploadDate_IsDateOnly()
	{
		var prop = typeof(Video).GetProperty("UploadDate");
		prop.Should().NotBeNull();
		prop!.PropertyType.Should().Be<DateOnly?>();
	}

	[Test]
	public void Video_CanBeInstantiated_WithDefaults()
	{
		var video = new Video { Url = "https://youtube.com/watch?v=dQw4w9WgXcQ", Title = "Never Gonna Give You Up" };
		video.Description.Should().BeEmpty();
	}
}
