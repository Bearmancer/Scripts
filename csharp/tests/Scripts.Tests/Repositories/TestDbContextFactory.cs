using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Tests.DbContext;

namespace Scripts.Tests.Repositories;

internal sealed class TestDbContextFactory(PostgresFixture fixture)
	: IDbContextFactory<ScriptsDbContext>
{
	public ScriptsDbContext CreateDbContext() => fixture.GetContext();

	public ValueTask<ScriptsDbContext> CreateDbContextAsync() =>
		ValueTask.FromResult(CreateDbContext());
}
