using Microsoft.EntityFrameworkCore;
using Scripts.Data.Entities;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.EntityConfigs;

[RequiresPgConnStr]
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
			Timestamp = DateTimeOffset.UtcNow,
		};

		context.FailedTasks.Add(task);
		await context.SaveChangesAsync();

		var retrieved = await context.FailedTasks.FirstOrDefaultAsync(t =>
			t.TaskName == "SyncLastFm"
		);

		await Assert.That(retrieved).IsNotNull();
		await Assert.That(retrieved!.ErrorMessage).IsEqualTo("Connection timeout");
	}

	[Test]
	public async Task FailedTask_CanQueryByTaskName()
	{
		await using var context = Fixture.GetContext();

		var task1 = new FailedTask
		{
			TaskName = "SyncLastFm",
			ErrorMessage = "Error 1",
			Timestamp = DateTimeOffset.UtcNow,
		};

		var task2 = new FailedTask
		{
			TaskName = "SyncYouTube",
			ErrorMessage = "Error 2",
			Timestamp = DateTimeOffset.UtcNow,
		};

		context.FailedTasks.AddRange(task1, task2);
		await context.SaveChangesAsync();

		var lastFmTasks = await context
			.FailedTasks.Where(t => t.TaskName == "SyncLastFm")
			.ToListAsync();

		await Assert.That(lastFmTasks).Count().IsEqualTo(1);
		await Assert.That(lastFmTasks[0].ErrorMessage).IsEqualTo("Error 1");
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
			Timestamp = now,
		};

		var task2 = new FailedTask
		{
			TaskName = "Task2",
			ErrorMessage = "Error 2",
			Timestamp = oneHourAgo,
		};

		context.FailedTasks.AddRange(task1, task2);
		await context.SaveChangesAsync();

		var recentTasks = await context
			.FailedTasks.Where(t => t.Timestamp > oneHourAgo.AddMinutes(1))
			.ToListAsync();

		await Assert.That(recentTasks).Count().IsEqualTo(1);
		await Assert.That(recentTasks[0].TaskName).IsEqualTo("Task1");
	}

	[Test]
	public async Task FailedTask_CanUpdateErrorMessage()
	{
		await using var context = Fixture.GetContext();

		var task = new FailedTask
		{
			TaskName = "SyncLastFm",
			ErrorMessage = "Original error",
			Timestamp = DateTimeOffset.UtcNow,
		};

		context.FailedTasks.Add(task);
		await context.SaveChangesAsync();

		task.ErrorMessage = "Updated error";
		context.FailedTasks.Update(task);
		await context.SaveChangesAsync();

		var retrieved = await context.FailedTasks.FirstOrDefaultAsync(t =>
			t.TaskName == "SyncLastFm"
		);

		await Assert.That(retrieved).IsNotNull();
		await Assert.That(retrieved!.ErrorMessage).IsEqualTo("Updated error");
	}

	[Test]
	public async Task FailedTask_CanDeleteByTaskName()
	{
		await using var context = Fixture.GetContext();

		var task1 = new FailedTask
		{
			TaskName = "SyncLastFm",
			ErrorMessage = "Error 1",
			Timestamp = DateTimeOffset.UtcNow,
		};

		var task2 = new FailedTask
		{
			TaskName = "SyncYouTube",
			ErrorMessage = "Error 2",
			Timestamp = DateTimeOffset.UtcNow,
		};

		context.FailedTasks.AddRange(task1, task2);
		await context.SaveChangesAsync();

		await context.FailedTasks.Where(t => t.TaskName == "SyncLastFm").ExecuteDeleteAsync();

		var remaining = await context.FailedTasks.ToListAsync();

		await Assert.That(remaining).Count().IsEqualTo(1);
		await Assert.That(remaining[0].TaskName).IsEqualTo("SyncYouTube");
	}
}
