using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

internal sealed class ExecutionLogConfigurationTests
{
	[Test]
	public async Task ExecutionLog_HasSessionId_Index()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(ExecutionLog));
		var indexes = entityType!.GetIndexes().ToList();

		indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "SessionId"));
	}

	[Test]
	public async Task ExecutionLog_HasTimestamp_Index()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(ExecutionLog));
		var indexes = entityType!.GetIndexes().ToList();

		indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "Timestamp"));
	}
}
