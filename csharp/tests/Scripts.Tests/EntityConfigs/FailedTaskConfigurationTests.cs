using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

internal sealed class FailedTaskConfigurationTests
{
	[Test]
	public async Task FailedTask_HasTaskName_Index()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(FailedTask));
		var indexes = entityType!.GetIndexes().ToList();

		await Assert.That(indexes).Contains(i => i.Properties.Any(p => p.Name == "TaskName"));
	}

	[Test]
	public async Task FailedTask_HasTimestamp_Index()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(FailedTask));
		var indexes = entityType!.GetIndexes().ToList();

		await Assert.That(indexes).Contains(i => i.Properties.Any(p => p.Name == "Timestamp"));
	}
}
