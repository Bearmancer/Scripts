using System;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Testcontainers.PostgreSql;

namespace Scripts.Tests.DbContext;

internal sealed class PostgresFixture : IAsyncDisposable
{
	private PostgreSqlContainer? _container;
	private string? _connectionString;
	private bool _initialized;
	private bool _disposed;
	private readonly SemaphoreSlim _initLock = new(1, 1);
	private readonly ConcurrentBag<string> _createdSchemas = [];

	public async Task InitializeAsync()
	{
		if (_initialized) return;

		await _initLock.WaitAsync();
		try
		{
			if (_initialized) return;

			var envConn = System.Environment.GetEnvironmentVariable("PGCONNSTR");
			if (!string.IsNullOrWhiteSpace(envConn))
			{
				_connectionString = envConn;
			}
			else
			{
				_container = new PostgreSqlBuilder("postgres:18").Build();
				await _container.StartAsync();
				_connectionString = _container.GetConnectionString();
			}

			// Create a golden template schema
			await using var templateCtx = CreateContextWithSchema("template_schema");
			await templateCtx.Database.MigrateAsync();

			_initialized = true;
		}
		finally
		{
			_initLock.Release();
		}
	}

	public ScriptsDbContext GetContext()
	{
		var schemaName = $"test_{Guid.NewGuid():N}";
		_createdSchemas.Add(schemaName);

		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseNpgsql(_connectionString, npgsqlOptions =>
			{
				npgsqlOptions.SearchPath(schemaName);
			})
			.Options;

		var ctx = new ScriptsDbContext(options);

		// Create schema and migrate it
		ctx.Database.ExecuteSqlRaw($"CREATE SCHEMA {schemaName}");
		ctx.Database.Migrate();

		return ctx;
	}

	public IDbContextFactory<ScriptsDbContext> GetContextFactory()
	{
		return new TestDbContextFactory(this);
	}

	private ScriptsDbContext CreateContextWithSchema(string schemaName)
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseNpgsql(_connectionString, npgsqlOptions =>
			{
				npgsqlOptions.SearchPath(schemaName);
			})
			.Options;

		var ctx = new ScriptsDbContext(options);
		ctx.Database.ExecuteSqlRaw($"CREATE SCHEMA IF NOT EXISTS {schemaName}");
		return ctx;
	}

	public async ValueTask DisposeAsync()
	{
		if (_disposed) return;
		_disposed = true;

		await using var db = new ScriptsDbContext(new DbContextOptionsBuilder<ScriptsDbContext>().UseNpgsql(_connectionString).Options);
		foreach (var schema in _createdSchemas)
		{
			await db.Database.ExecuteSqlRawAsync($"DROP SCHEMA {schema} CASCADE");
		}

		if (_container is not null)
			await _container.DisposeAsync();
		
		_initLock.Dispose();
		GC.SuppressFinalize(this);
	}
}
