using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Tests.DbContext;

namespace CSharpScripts.Tests.EntityConfigs;

internal class VideoConfigurationAdditionalTests : DatabaseTestBase
{
	[Test]
	public async Task Video_CanInsertAndRetrieve()
	{
		var context = Fixture.GetContext();

		var video = new Video
		{
			Url = "https://example.com/video",
			Title = "Test Video",
			Description = "Test Description",
			ChannelName = "Test Channel",
			UploadDate = DateOnly.FromDateTime(DateTime.UtcNow),
			SyncedAt = DateTimeOffset.UtcNow
		};

		context.Videos.Add(video);
		await context.SaveChangesAsync();

		var retrieved = await context.Videos.FirstOrDefaultAsync(v => v.Url == "https://example.com/video");

		retrieved.Should().NotBeNull();
		retrieved!.Title.Should().Be("Test Video");
		retrieved.ChannelName.Should().Be("Test Channel");

	}

	[Test]
	public async Task Video_UrlIsUnique()
	{
		var context = Fixture.GetContext();

		var video1 = new Video
		{
			Url = "https://example.com/video1",
			Title = "Video 1",
			Description = "Description 1",
			ChannelName = "Channel 1",
			UploadDate = DateOnly.FromDateTime(DateTime.UtcNow),
			SyncedAt = DateTimeOffset.UtcNow
		};

		var video2 = new Video
		{
			Url = "https://example.com/video1",
			Title = "Video 2",
			Description = "Description 2",
			ChannelName = "Channel 2",
			UploadDate = DateOnly.FromDateTime(DateTime.UtcNow),
			SyncedAt = DateTimeOffset.UtcNow
		};

		context.Videos.Add(video1);
		await context.SaveChangesAsync();

		context.Videos.Add(video2);
		var act = async () => await context.SaveChangesAsync();

		await act.Should().ThrowAsync<DbUpdateException>();

	}

	[Test]
	public async Task Video_CanQueryByChannelName()
	{
		var context = Fixture.GetContext();

		var video1 = new Video
		{
			Url = "https://example.com/video1",
			Title = "Video 1",
			Description = "Description 1",
			ChannelName = "Channel A",
			UploadDate = DateOnly.FromDateTime(DateTime.UtcNow),
			SyncedAt = DateTimeOffset.UtcNow
		};

		var video2 = new Video
		{
			Url = "https://example.com/video2",
			Title = "Video 2",
			Description = "Description 2",
			ChannelName = "Channel B",
			UploadDate = DateOnly.FromDateTime(DateTime.UtcNow),
			SyncedAt = DateTimeOffset.UtcNow
		};

		context.Videos.AddRange(video1, video2);
		await context.SaveChangesAsync();

		var channelAVideos = await context.Videos
			.Where(v => v.ChannelName == "Channel A")
			.ToListAsync();

		channelAVideos.Should().HaveCount(1);
		channelAVideos[0].Title.Should().Be("Video 1");

	}

	[Test]
	public async Task Video_CanQueryByUploadDate()
	{
		var context = Fixture.GetContext();

		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var yesterday = today.AddDays(-1);

		var video1 = new Video
		{
			Url = "https://example.com/video1",
			Title = "Video 1",
			Description = "Description 1",
			ChannelName = "Channel A",
			UploadDate = today,
			SyncedAt = DateTimeOffset.UtcNow
		};

		var video2 = new Video
		{
			Url = "https://example.com/video2",
			Title = "Video 2",
			Description = "Description 2",
			ChannelName = "Channel B",
			UploadDate = yesterday,
			SyncedAt = DateTimeOffset.UtcNow
		};

		context.Videos.AddRange(video1, video2);
		await context.SaveChangesAsync();

		var todayVideos = await context.Videos
			.Where(v => v.UploadDate == today)
			.ToListAsync();

		todayVideos.Should().HaveCount(1);
		todayVideos[0].Title.Should().Be("Video 1");

	}

	[Test]
	public async Task Video_CanUpdateMetadata()
	{
		var context = Fixture.GetContext();

		var video = new Video
		{
			Url = "https://example.com/video",
			Title = "Original Title",
			Description = "Original Description",
			ChannelName = "Channel",
			UploadDate = DateOnly.FromDateTime(DateTime.UtcNow),
			SyncedAt = DateTimeOffset.UtcNow
		};

		context.Videos.Add(video);
		await context.SaveChangesAsync();

		video.Title = "Updated Title";
		video.Description = "Updated Description";
		context.Videos.Update(video);
		await context.SaveChangesAsync();

		var retrieved = await context.Videos.FirstOrDefaultAsync(v => v.Url == "https://example.com/video");

		retrieved.Should().NotBeNull();
		retrieved!.Title.Should().Be("Updated Title");
		retrieved.Description.Should().Be("Updated Description");

	}
}
