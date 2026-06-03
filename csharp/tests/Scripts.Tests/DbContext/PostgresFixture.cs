using System;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Tests.Repositories;
using Testcontainers.PostgreSql;

namespace Scripts.Tests.DbContext;

/// <summary>
/// Spins up a PostgreSQL 18 container per fixture instance (per test class) and provides
/// a <see cref="ScriptsDbContext"/> wired to it. Falls back to the env-supplied
/// <c>PGCONNSTR</c> when no Docker daemon is reachable, so the same fixture works
/// in local dev and CI.
/// </summary>
internal sealed class PostgresFixture : IAsyncDisposable
{
	private PostgreSqlContainer? _container;
	private string? _connectionString;
	private bool _initialized;

	public DbContextOptions<ScriptsDbContext> Options { get; private set; } = null!;

	public async Task InitializeAsync()
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

		Options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseNpgsql(_connectionString)
			.Options;

		await using var ctx = new ScriptsDbContext(Options);
		await ctx.Database.MigrateAsync();

		_initialized = true;
	}

	public ScriptsDbContext GetContext() => new(Options);

	public IDbContextFactory<ScriptsDbContext> GetContextFactory() =>
		new TestDbContextFactory(Options);

	public async ValueTask DisposeAsync()
	{
		if (_container is not null)
			await _container.DisposeAsync();
		GC.SuppressFinalize(this);
	}
}
