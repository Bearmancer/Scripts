using Microsoft.EntityFrameworkCore;
using Scripts.Data;

namespace Scripts.Tests.DbContext;

internal class TestDbContextFactory(PostgresFixture fixture) : IDbContextFactory<ScriptsDbContext>
{
	public ScriptsDbContext CreateDbContext() => fixture.GetContext();
}
