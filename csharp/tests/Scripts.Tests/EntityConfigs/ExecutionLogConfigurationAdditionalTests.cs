using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Tests.DbContext;

namespace CSharpScripts.Tests.EntityConfigs;

internal class ExecutionLogConfigurationAdditionalTests : DatabaseTestBase
{
	[Test]
	public async Task ExecutionLog_CanInsertAndRetrieve()
	{
		await using var context = Fixture.GetContext();

		var log = new ExecutionLog
		{
			Timestamp = DateTimeOffset.UtcNow,
			SessionId = "session-123",
			Payload = System.Text.Json.JsonDocument.Parse("{}"),
			ExitCode = 0
		};

		context.ExecutionLogs.Add(log);
		await context.SaveChangesAsync();

		var retrieved = await context.ExecutionLogs.FirstOrDefaultAsync(l => l.SessionId == "session-123");

		retrieved.Should().NotBeNull();
		retrieved!.ExitCode.Should().Be(0);

	}

	[Test]
	public async Task ExecutionLog_CanQueryBySessionId()
	{
		await using var context = Fixture.GetContext();

		var log1 = new ExecutionLog
		{
			Timestamp = DateTimeOffset.UtcNow,
			SessionId = "session-1",
			Payload = System.Text.Json.JsonDocument.Parse("{}"),
			ExitCode = 0
		};

		var log2 = new ExecutionLog
		{
			Timestamp = DateTimeOffset.UtcNow,
			SessionId = "session-2",
			Payload = System.Text.Json.JsonDocument.Parse("{}"),
			ExitCode = 1
		};

		context.ExecutionLogs.AddRange(log1, log2);
		await context.SaveChangesAsync();

		var session1Logs = await context.ExecutionLogs
			.Where(l => l.SessionId == "session-1")
			.ToListAsync();

		session1Logs.Should().HaveCount(1);
		session1Logs[0].ExitCode.Should().Be(0);

	}

	[Test]
	public async Task ExecutionLog_CanQueryByTimestamp()
	{
		await using var context = Fixture.GetContext();

		var now = DateTimeOffset.UtcNow;
		var oneHourAgo = now.AddHours(-1);

		var log1 = new ExecutionLog
		{
			Timestamp = now,
			SessionId = "session-1",
			Payload = System.Text.Json.JsonDocument.Parse("{}"),
			ExitCode = 0
		};

		var log2 = new ExecutionLog
		{
			Timestamp = oneHourAgo,
			SessionId = "session-2",
			Payload = System.Text.Json.JsonDocument.Parse("{}"),
			ExitCode = 0
		};

		context.ExecutionLogs.AddRange(log1, log2);
		await context.SaveChangesAsync();

		var recentLogs = await context.ExecutionLogs
			.Where(l => l.Timestamp > oneHourAgo.AddMinutes(1))
			.ToListAsync();

		recentLogs.Should().HaveCount(1);
		recentLogs[0].SessionId.Should().Be("session-1");

	}

	[Test]
	public async Task ExecutionLog_CanQueryByExitCode()
	{
		await using var context = Fixture.GetContext();

		var log1 = new ExecutionLog
		{
			Timestamp = DateTimeOffset.UtcNow,
			SessionId = "session-1",
			Payload = System.Text.Json.JsonDocument.Parse("{}"),
			ExitCode = 0
		};

		var log2 = new ExecutionLog
		{
			Timestamp = DateTimeOffset.UtcNow,
			SessionId = "session-2",
			Payload = System.Text.Json.JsonDocument.Parse("{}"),
			ExitCode = 1
		};

		var log3 = new ExecutionLog
		{
			Timestamp = DateTimeOffset.UtcNow,
			SessionId = "session-3",
			Payload = System.Text.Json.JsonDocument.Parse("{}"),
			ExitCode = 1
		};

		context.ExecutionLogs.AddRange(log1, log2, log3);
		await context.SaveChangesAsync();

		var failedLogs = await context.ExecutionLogs
			.Where(l => l.ExitCode != 0)
			.ToListAsync();

		failedLogs.Should().HaveCount(2);

	}

	[Test]
	public async Task ExecutionLog_CanUpdatePayload()
	{
		await using var context = Fixture.GetContext();

		var log = new ExecutionLog
		{
			Timestamp = DateTimeOffset.UtcNow,
			SessionId = "session-1",
			Payload = System.Text.Json.JsonDocument.Parse("{}"),
			ExitCode = 0
		};

		context.ExecutionLogs.Add(log);
		await context.SaveChangesAsync();

		log.Payload = System.Text.Json.JsonDocument.Parse("{\"key\":\"value\"}");
		context.ExecutionLogs.Update(log);
		await context.SaveChangesAsync();

		var retrieved = await context.ExecutionLogs.FirstOrDefaultAsync(l => l.SessionId == "session-1");

		retrieved.Should().NotBeNull();
		retrieved!.Payload.Should().NotBeNull();

	}
}
