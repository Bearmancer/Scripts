<<<<<<< HEAD
using CSharpScripts.Data;
using CSharpScripts.Tests.DbContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TUnit;
=======
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
>>>>>>> d057b9bb8ac223cfc175063f75aa77cad063fcb1

namespace Scripts.Tests.DbContext;

internal sealed class DbContextNoTrackingTests
{
	[Test]
<<<<<<< HEAD
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
=======
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
			Id = 1,
			SessionId = "test-session",
			Timestamp = DateTimeOffset.UtcNow
		});

		entry.State.Should().Be(EntityState.Unchanged);
>>>>>>> d057b9bb8ac223cfc175063f75aa77cad063fcb1
	}
}
