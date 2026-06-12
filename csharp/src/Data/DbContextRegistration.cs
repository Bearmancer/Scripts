using Microsoft.Extensions.DependencyInjection;
using Scripts.Data.Repositories;

namespace Scripts.Data;

internal static class DbContextRegistration
{
	public static IServiceCollection AddScriptsDbContext(this IServiceCollection services)
	{
		var connStr =
			GetEnvironmentVariable(variable: "PGCONNSTR") ?? Variables.DefaultConnectionString;

		services.AddDbContext<ScriptsDbContext>(opts =>
			opts.UseNpgsql(
				connectionString: connStr,
				npgsqlOpts =>
					npgsqlOpts.EnableRetryOnFailure(
						maxRetryCount: 5,
						maxRetryDelay: TimeSpan.FromSeconds(2),
						errorCodesToAdd: null
					)
			)
		);
		services.AddRepositories();

		return services;
	}

	public static IDbContextFactory<ScriptsDbContext> CreateContextFactory()
	{
		var connStr =
			GetEnvironmentVariable(variable: "PGCONNSTR") ?? Variables.DefaultConnectionString;

		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseNpgsql(
				connectionString: connStr,
				npgsqlOpts =>
					npgsqlOpts.EnableRetryOnFailure(
						maxRetryCount: 5,
						maxRetryDelay: TimeSpan.FromSeconds(2),
						errorCodesToAdd: null
					)
			)
			.Options;

		return new ContextFactory(options);
	}

	private static string? GetEnvironmentVariable(string variable) =>
		Environment.GetEnvironmentVariable(variable: variable);

	private sealed class ContextFactory(DbContextOptions<ScriptsDbContext> options)
		: IDbContextFactory<ScriptsDbContext>
	{
		public ScriptsDbContext CreateDbContext() => new(options);
	}
}
