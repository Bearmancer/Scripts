using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.DbContext;

internal sealed class DbContextNoTrackingTests
{
	[Test]
	public void DbContext_DefaultsTo_NoTracking()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("NoTrackingTest_" + Guid.NewGuid())
			.Options;

		using var context = new ScriptsDbContext(options);
		context.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);
	}
}

[RequiresPgConnStr]
internal sealed class DbContextNoTrackingAttachTests : DatabaseTestBase
{
	[Test]
	public void DbContext_CanExplicitly_TrackEntity()
	{
		using var context = Fixture.GetContext();
		var entry = context.Attach(new ExecutionLog
		{
			Id = 1,
			SessionId = "test-session",
			Timestamp = DateTimeOffset.UtcNow
		});

		entry.State.Should().Be(EntityState.Unchanged);
	}
}
