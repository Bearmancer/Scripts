using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Tests.DbContext;

namespace CSharpScripts.Tests.EntityConfigs;

internal class FailedTaskAdditionalTests : DatabaseTestBase
{
	[Test]
	public async Task FailedTask_CanInsertAndRetrieve()
	{
		await using var context = Fixture.GetContext();

		var task = new FailedTask
		{
			TaskName = "SyncLastFm",
			ErrorMessage = "Connection timeout",
			Timestamp = DateTimeOffset.UtcNow
		};

		context.FailedTasks.Add(task);
		await context.SaveChangesAsync();

		var retrieved = await context.FailedTasks.FirstOrDefaultAsync(t => t.TaskName == "SyncLastFm");

		retrieved.Should().NotBeNull();
		retrieved!.ErrorMessage.Should().Be("Connection timeout");

	}

	[Test]
	public async Task FailedTask_CanQueryByTaskName()
	{
		await using var context = Fixture.GetContext();

		var task1 = new FailedTask
		{
			TaskName = "SyncLastFm",
			ErrorMessage = "Error 1",
			Timestamp = DateTimeOffset.UtcNow
		};

		var task2 = new FailedTask
		{
			TaskName = "SyncYouTube",
			ErrorMessage = "Error 2",
			Timestamp = DateTimeOffset.UtcNow
		};

		context.FailedTasks.AddRange(task1, task2);
		await context.SaveChangesAsync();

		var lastFmTasks = await context.FailedTasks
			.Where(t => t.TaskName == "SyncLastFm")
			.ToListAsync();

		lastFmTasks.Should().HaveCount(1);
		lastFmTasks[0].ErrorMessage.Should().Be("Error 1");

	}

	[Test]
	public async Task FailedTask_CanQueryByTimestamp()
	{
		await using var context = Fixture.GetContext();

		var now = DateTimeOffset.UtcNow;
		var oneHourAgo = now.AddHours(-1);

		var task1 = new FailedTask
		{
			TaskName = "Task1",
			ErrorMessage = "Error 1",
			Timestamp = now
		};

		var task2 = new FailedTask
		{
			TaskName = "Task2",
			ErrorMessage = "Error 2",
			Timestamp = oneHourAgo
		};

		context.FailedTasks.AddRange(task1, task2);
		await context.SaveChangesAsync();

		var recentTasks = await context.FailedTasks
			.Where(t => t.Timestamp > oneHourAgo.AddMinutes(1))
			.ToListAsync();

		recentTasks.Should().HaveCount(1);
		recentTasks[0].TaskName.Should().Be("Task1");

	}

	[Test]
	public async Task FailedTask_CanUpdateErrorMessage()
	{
		await using var context = Fixture.GetContext();

		var task = new FailedTask
		{
			TaskName = "SyncLastFm",
			ErrorMessage = "Original error",
			Timestamp = DateTimeOffset.UtcNow
		};

		context.FailedTasks.Add(task);
		await context.SaveChangesAsync();

		task.ErrorMessage = "Updated error";
		context.FailedTasks.Update(task);
		await context.SaveChangesAsync();

		var retrieved = await context.FailedTasks.FirstOrDefaultAsync(t => t.TaskName == "SyncLastFm");

		retrieved.Should().NotBeNull();
		retrieved!.ErrorMessage.Should().Be("Updated error");

	}

	[Test]
	public async Task FailedTask_CanDeleteByTaskName()
	{
		await using var context = Fixture.GetContext();

		var task1 = new FailedTask
		{
			TaskName = "SyncLastFm",
			ErrorMessage = "Error 1",
			Timestamp = DateTimeOffset.UtcNow
		};

		var task2 = new FailedTask
		{
			TaskName = "SyncYouTube",
			ErrorMessage = "Error 2",
			Timestamp = DateTimeOffset.UtcNow
		};

		context.FailedTasks.AddRange(task1, task2);
		await context.SaveChangesAsync();

		await context.FailedTasks
			.Where(t => t.TaskName == "SyncLastFm")
			.ExecuteDeleteAsync();

		var remaining = await context.FailedTasks.ToListAsync();

		remaining.Should().HaveCount(1);
		remaining[0].TaskName.Should().Be("SyncYouTube");

	}
}
