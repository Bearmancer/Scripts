using Microsoft.EntityFrameworkCore;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.DbContext;

[RequiresPgConnStr]
internal sealed class CompiledModelTests : DatabaseTestBase
{
	[Test]
	public async Task DbContext_RuntimeModel_BuildsWithoutError()
	{
		await using var context = Fixture.GetContext();
		var model = context.Model;
		await Assert.That(model).IsNotNull();
		await Assert.That(model.GetEntityTypes()).Count().IsEqualTo(10);
	}

	[Test]
	public async Task DbContext_Migrations_AppliedWithoutPendingChangesWarning()
	{
		await using var context = Fixture.GetContext();
		var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
		await Assert.That(pendingMigrations).IsEmpty();
	}
}
