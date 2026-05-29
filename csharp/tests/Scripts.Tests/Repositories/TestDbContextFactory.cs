using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;

namespace Scripts.Tests.Repositories;

#pragma warning disable CA2000
internal sealed class TestDbContextFactory(DbContextOptions<ScriptsDbContext> options) : IDbContextFactory<ScriptsDbContext>
{
	public ScriptsDbContext CreateDbContext() => new(options);

	public ValueTask<ScriptsDbContext> CreateDbContextAsync() => ValueTask.FromResult(CreateDbContext());
}
#pragma warning restore CA2000
