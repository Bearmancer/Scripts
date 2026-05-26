using CSharpScripts.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpScripts.Data;

internal static class DbContextRegistration
{
	public static IServiceCollection AddScriptsDbContext(this IServiceCollection services)
	{
		var connStr =
			GetEnvironmentVariable(variable: "PGCONNSTR")
			?? throw new InvalidOperationException(
				message: "PGCONNSTR environment variable is not set."
			);

		services.AddDbContext<ScriptsDbContext>(opts => opts.UseNpgsql(connectionString: connStr));
		services.AddRepositories();

		return services;
	}
}
