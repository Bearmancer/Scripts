using CSharpScripts.Data;
using CSharpScripts.Tests.DbContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TUnit;

namespace Scripts.Tests.DbContext;

internal sealed class DbContextNoTrackingTests
{
	[Test]
	public async Task DbContext_DefaultsTo_NoTracking()
	{
		var fixture = new DatabaseTestFixture();
		await fixture.InitializeAsync();
		await using (fixture)
		{
			var context = fixture.GetContext();
			await using (context)
			{
				context
					.ChangeTracker.QueryTrackingBehavior.Should()
					.Be(QueryTrackingBehavior.NoTracking);
			}
		}
	}

	[Test]
	public async Task DbContext_CanExplicitly_TrackEntity()
	{
		var fixture = new DatabaseTestFixture();
		await fixture.InitializeAsync();
		await using (fixture)
		{
			var context = fixture.GetContext();
			await using (context)
			{
				var entry = context.Attach(
					new CSharpScripts.Data.Entities.ExecutionLog
					{
						Id = 1,
						SessionId = "test-session",
						Timestamp = DateTimeOffset.UtcNow,
					}
				);

				entry.State.Should().Be(EntityState.Unchanged);
			}
		}
	}
}
