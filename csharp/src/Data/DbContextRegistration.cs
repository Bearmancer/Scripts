using Scripts.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Scripts.Data;

internal static class DbContextRegistration
{
	public static IServiceCollection AddScriptsDbContext(this IServiceCollection services)
	{
		var connStr =
			GetEnvironmentVariable(variable: "PGCONNSTR") ?? Variables.DefaultConnectionString;

		services.AddDbContext<ScriptsDbContext>(opts => opts.UseNpgsql(connectionString: connStr));
		services.AddRepositories();

		return services;
	}
}
