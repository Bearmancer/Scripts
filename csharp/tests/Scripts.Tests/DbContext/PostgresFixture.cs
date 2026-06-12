using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scripts.Data;
using TUnit.Core.Interfaces;

namespace Scripts.Tests.DbContext;

internal sealed class PostgresFixture : IAsyncInitializer, IAsyncDisposable
{
	private string _connectionString = null!;
	private readonly List<string> _createdSchemas = new();

	public async Task InitializeAsync()
	{
		var baseConnStr = System.Environment.GetEnvironmentVariable("PGCONNSTR");
		if (string.IsNullOrEmpty(baseConnStr))
		{
			throw new InvalidOperationException("PGCONNSTR environment variable is not set");
		}

		_connectionString = baseConnStr;

		var template_schema = $"template_{Guid.NewGuid():N}";
		_createdSchemas.Add(template_schema);

		var templateBuilder = new NpgsqlConnectionStringBuilder(_connectionString)
		{
			SearchPath = template_schema
		};

		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseNpgsql(templateBuilder.ConnectionString)
			.Options;

		await using var ctx = new ScriptsDbContext(options);
#pragma warning disable EF1002, EF1003
		ctx.Database.ExecuteSqlRaw($"CREATE SCHEMA IF NOT EXISTS {template_schema}");
#pragma warning restore EF1002, EF1003
		ctx.Database.Migrate();
		await ctx.Database.MigrateAsync();
	}

	public ScriptsDbContext GetContext()
	{
		var schemaName = $"test_{Guid.NewGuid():N}";
		_createdSchemas.Add(schemaName);

		var builder = new NpgsqlConnectionStringBuilder(_connectionString)
		{
			SearchPath = schemaName
		};

		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseNpgsql(builder.ConnectionString)
			.Options;

		var ctx = new ScriptsDbContext(options);
#pragma warning disable EF1002, EF1003
		ctx.Database.ExecuteSqlRaw($"CREATE SCHEMA IF NOT EXISTS {schemaName}");
#pragma warning restore EF1002, EF1003

		return ctx;
	}

	public IDbContextFactory<ScriptsDbContext> GetContextFactory() =>
		new TestDbContextFactory(this);

	public async ValueTask DisposeAsync()
	{
		if (_createdSchemas.Count == 0)
			return;

		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseNpgsql(_connectionString)
			.Options;

		await using var ctx = new ScriptsDbContext(options);

		foreach (var schema in _createdSchemas)
		{
			try
			{
#pragma warning disable EF1002, EF1003
				await ctx.Database.ExecuteSqlRawAsync($"DROP SCHEMA IF EXISTS {schema} CASCADE");
#pragma warning restore EF1002, EF1003
			}
			catch (NpgsqlException)
			{
			}
		}
	}
}
