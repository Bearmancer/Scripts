using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;

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

    [Test]
    public void DbContext_CanExplicitly_TrackEntity()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase("TrackExplicitlyTest_" + Guid.NewGuid())
            .Options;

        using var context = new ScriptsDbContext(options);
        var entry = context.Attach(new CSharpScripts.Data.Entities.ExecutionLog
        {
            Id = 0,
            SessionId = "test-session",
            Timestamp = DateTimeOffset.UtcNow
        });

        entry.State.Should().Be(EntityState.Unchanged);
    }
}
