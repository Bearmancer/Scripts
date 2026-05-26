using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using CSharpScripts.Data;

namespace CSharpScripts.Tests.DbContext;

internal sealed class DatabaseTestFixture : IAsyncDisposable
{
	private PostgreSqlContainer? _container;
	private ScriptsDbContext? _context;

	public async Task InitializeAsync()
	{
		_container = new PostgreSqlBuilder()
			.WithImage("postgres:18")
			.WithDatabase("scripts_test")
			.WithUsername("postgres")
			.WithPassword("postgres")
			.Build();

		await _container.StartAsync();

		var connectionString = _container.GetConnectionString();
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseNpgsql(connectionString)
			.Options;

		_context = new ScriptsDbContext(options);
		await _context.Database.MigrateAsync();
	}

	public ScriptsDbContext GetContext()
	{
		if (_context is null)
			throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");
		return _context;
	}

	public IDbContextFactory<ScriptsDbContext> GetContextFactory()
	{
		if (_context is null)
			throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");

		var connectionString = _container!.GetConnectionString();
		return new TestDbContextFactory(connectionString);
	}

	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		if (_context is not null)
		{
			await _context.Database.EnsureDeletedAsync();
			await _context.DisposeAsync();
		}

		if (_container is not null)
		{
			await _container.StopAsync();
			await _container.DisposeAsync();
		}
	}

	private sealed class TestDbContextFactory(string connectionString) : IDbContextFactory<ScriptsDbContext>
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
