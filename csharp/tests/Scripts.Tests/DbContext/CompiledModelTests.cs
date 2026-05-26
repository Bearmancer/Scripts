using CSharpScripts.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CSharpScripts.Tests.DbContext;

internal sealed class CompiledModelTests : DatabaseTestBase
{
	[Test]
	public async Task DbContext_RuntimeModel_BuildsWithoutError()
	{
		// Compiled model is regenerated in T1-11. Until then, EF builds at runtime.
		// This test guards that OnModelCreating runs without exceptions.
		await using var context = Fixture.GetContext();
		var model = context.Model;
		model.Should().NotBeNull();
		model.GetEntityTypes().Should().HaveCount(9, "all 9 entity configurations must be applied");
	}

	[Test]
	public async Task DbContext_Migrations_AppliedWithoutPendingChangesWarning()
	{
		// Fixture.InitializeAsync() already ran MigrateAsync() — this verifies
		// no pending model changes exist after migration.
		await using var context = Fixture.GetContext();
		var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
		pendingMigrations.Should().BeEmpty("all migrations must be applied during fixture setup");
	}
}

