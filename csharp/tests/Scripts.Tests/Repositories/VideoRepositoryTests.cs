using Microsoft.EntityFrameworkCore;
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
			UploadDate = DateOnly.FromDateTime(DateTime.UtcNow),
		};

		var result = await repository.AddAsync(video);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.Url).IsEqualTo(video.Url);

		await using var verifyContext = Fixture.GetContext();
		var count = await verifyContext.Videos.CountAsync();
		await Assert.That(count).IsEqualTo(1);
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
			ChannelName = "Test Channel",
		};
		context.Videos.Add(video);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new VideoRepository(factory, pipeline);

		var result = await repository.GetByUrlAsync("https://youtube.com/watch?v=test123");

		await Assert.That(result).IsNotNull();
		await Assert.That(result!.Title).IsEqualTo("Test Video");
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByUrlAsync_ReturnsNullWhenNotFound()
	{
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new VideoRepository(factory, pipeline);

		var result = await repository.GetByUrlAsync("https://youtube.com/watch?v=nonexistent");

		await Assert.That(result).IsNull();
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByChannelAsync_ReturnsVideosByChannel()
	{
		await using var context = Fixture.GetContext();

		var now = DateOnly.FromDateTime(DateTime.UtcNow);
		context.Videos.AddRange(
			new Video
			{
				Url = "url1",
				Title = "Video 1",
				ChannelName = "Channel A",
				UploadDate = now.AddDays(-2),
			},
			new Video
			{
				Url = "url2",
				Title = "Video 2",
				ChannelName = "Channel B",
				UploadDate = now,
			},
			new Video
			{
				Url = "url3",
				Title = "Video 3",
				ChannelName = "Channel A",
				UploadDate = now.AddDays(-1),
			}
		);
		await context.SaveChangesAsync();

		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new VideoRepository(factory, pipeline);

		var result = await repository.GetByChannelAsync("Channel A");

		await Assert.That(result).Count().IsEqualTo(2);
		await Assert.That(result).All(v => v.ChannelName == "Channel A");
		await Assert.That(result[0].UploadDate).IsEqualTo(now.AddDays(-1));
		await Assert.That(result[1].UploadDate).IsEqualTo(now.AddDays(-2));
	}

	[RequiresPgConnStr]
	[Test]
	public async Task GetByChannelAsync_ReturnsEmptyWhenChannelNotFound()
	{
		var pipeline = RepositoryResilienceFactory.CreateDatabasePipeline();
		var factory = Fixture.GetContextFactory();
		var repository = new VideoRepository(factory, pipeline);

		var result = await repository.GetByChannelAsync("Nonexistent Channel");

		await Assert.That(result).IsEmpty();
	}
}
