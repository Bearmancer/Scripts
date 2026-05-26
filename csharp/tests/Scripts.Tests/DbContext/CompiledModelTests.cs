using CSharpScripts.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CSharpScripts.Tests.DbContext;

internal sealed class CompiledModelTests : DatabaseTestBase
{
	[Test]
	public async Task DbContext_RuntimeModel_BuildsWithoutError()
	{
		await using var context = Fixture.GetContext();
		var model = context.Model;
		model.Should().NotBeNull();
		model.GetEntityTypes().Should().HaveCount(10, "all 10 entity configurations must be applied");
	}

	[Test]
	public async Task DbContext_Migrations_AppliedWithoutPendingChangesWarning()
	{
		await using var context = Fixture.GetContext();
		var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
		pendingMigrations.Should().BeEmpty("all migrations must be applied during fixture setup");
	}
}

