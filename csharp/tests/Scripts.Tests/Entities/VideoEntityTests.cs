using Scripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class VideoEntityTests
{
	[Test]
	public async Task Video_HasRequired_Properties()
	{
		var props = typeof(Video).GetProperties().Select(p => p.Name).ToList();

		await Assert.That(props).Contains("Id");
		await Assert.That(props).Contains("Url");
		await Assert.That(props).Contains("Title");
		await Assert.That(props).Contains("Description");
		await Assert.That(props).Contains("ChannelName");
		await Assert.That(props).Contains("UploadDate");
		await Assert.That(props).Contains("SyncedAt");
		await Assert.That(props).Contains("Metadata");
	}

	[Test]
	public async Task Video_Url_IsString()
	{
		var prop = typeof(Video).GetProperty("Url");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(string));
	}

	[Test]
	public async Task Video_UploadDate_IsDateOnly()
	{
		var prop = typeof(Video).GetProperty("UploadDate");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(DateOnly?));
	}

	[Test]
	public async Task Video_CanBeInstantiated_WithDefaults()
	{
		var video = new Video
		{
			Url = "https://youtube.com/watch?v=dQw4w9WgXcQ",
			Title = "Never Gonna Give You Up",
		};
		await Assert.That(video.Description).IsEmpty();
	}
}
