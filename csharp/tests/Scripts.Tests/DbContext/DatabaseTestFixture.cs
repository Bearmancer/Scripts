using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Npgsql;

namespace Scripts.Tests.DbContext;






internal sealed class DatabaseTestFixture : IAsyncDisposable
{
	private string? _connectionString;

	public async Task InitializeAsync()
	{
		var baseConnStr = System.Environment.GetEnvironmentVariable("PGCONNSTR")
			?? throw new InvalidOperationException(
				"PGCONNSTR environment variable is not set. " +
				"Load .env before running integration tests.");

		var builder = new NpgsqlConnectionStringBuilder(baseConnStr);
		builder.Database = $"{builder.Database}_{Guid.NewGuid():N}";
		_connectionString = builder.ConnectionString;

		await using var ctx = BuildContext();
		await ctx.Database.EnsureDeletedAsync();
		await ctx.Database.MigrateAsync();
	}

	
	public ScriptsDbContext GetContext() => BuildContext();

	
	public IDbContextFactory<ScriptsDbContext> GetContextFactory()
	{
		if (_connectionString is null)
			throw new InvalidOperationException("Fixture not initialized.");
		return new PostgresContextFactory(_connectionString);
	}

	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		if (_connectionString is null) return;
		await using var ctx = BuildContext();
		await ctx.Database.EnsureDeletedAsync();
	}

	private ScriptsDbContext BuildContext()
	{
		if (_connectionString is null)
			throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");

		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseNpgsql(_connectionString)
			.Options;
		return new ScriptsDbContext(options);
	}

	private sealed class PostgresContextFactory(string connectionString) : IDbContextFactory<ScriptsDbContext>
	{
		public ScriptsDbContext CreateDbContext()
		{
			var options = new DbContextOptionsBuilder<ScriptsDbContext>()
				.UseNpgsql(connectionString)
				.Options;
			return new ScriptsDbContext(options);
		}
	}
}

