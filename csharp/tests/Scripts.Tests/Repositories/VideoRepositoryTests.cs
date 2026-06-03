using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;
using Scripts.Data.Repositories;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.Repositories;

internal sealed class VideoRepositoryTests : DatabaseTestBase
{
	[RequiresPgConnStr]
	[Test]
	public async Task AddAsync_InsertsNewVideo()
	{
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new VideoRepository(factory, pipeline);

		var video = new Video
		{
			Url = "https://youtube.com/watch?v=test123",
			Title = "Test Video",
			Description = "Test Description",
			ChannelName = "Test Channel",
			UploadDate = DateOnly.FromDateTime(DateTime.UtcNow)
		};

		var result = await repository.AddAsync(video);

		result.Should().NotBeNull();
		result.Url.Should().Be(video.Url);

		await using var verifyContext = Fixture.GetContext();
		var count = await verifyContext.Videos.CountAsync();
		count.Should().Be(1);
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByUrlAsync_ReturnsVideoByUrl()
	{
		await using var context = Fixture.GetContext();

		var video = new Video
		{
			Url = "https://youtube.com/watch?v=test123",
			Title = "Test Video",
			ChannelName = "Test Channel"
		};
		context.Videos.Add(video);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new VideoRepository(factory, pipeline);

		var result = await repository.GetByUrlAsync("https://youtube.com/watch?v=test123");

		result.Should().NotBeNull();
		result!.Title.Should().Be("Test Video");
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByUrlAsync_ReturnsNullWhenNotFound()
	{
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new VideoRepository(factory, pipeline);

		var result = await repository.GetByUrlAsync("https://youtube.com/watch?v=nonexistent");

		result.Should().BeNull();
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByChannelAsync_ReturnsVideosByChannel()
	{
		await using var context = Fixture.GetContext();

		var now = DateOnly.FromDateTime(DateTime.UtcNow);
		context.Videos.AddRange(
			new Video { Url = "url1", Title = "Video 1", ChannelName = "Channel A", UploadDate = now.AddDays(-2) },
			new Video { Url = "url2", Title = "Video 2", ChannelName = "Channel B", UploadDate = now },
			new Video { Url = "url3", Title = "Video 3", ChannelName = "Channel A", UploadDate = now.AddDays(-1) }
		);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new VideoRepository(factory, pipeline);

		var result = await repository.GetByChannelAsync("Channel A");

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(v => v.ChannelName.Should().Be("Channel A"));
		result[0].UploadDate.Should().Be(now.AddDays(-1));
		result[1].UploadDate.Should().Be(now.AddDays(-2));
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByChannelAsync_ReturnsEmptyWhenChannelNotFound()
	{
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new VideoRepository(factory, pipeline);

		var result = await repository.GetByChannelAsync("Nonexistent Channel");

		result.Should().BeEmpty();
	}
}
