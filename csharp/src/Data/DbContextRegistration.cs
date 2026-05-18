using Microsoft.Extensions.DependencyInjection;

namespace CSharpScripts.Data;

internal static class DbContextRegistration
{
	public static IServiceCollection AddScriptsDbContext(this IServiceCollection services)
	{
		var connStr = GetEnvironmentVariable("PGCONNSTR")
			?? throw new InvalidOperationException("PGCONNSTR environment variable is not set.");
		return services.AddDbContext<ScriptsDbContext>(opts => opts.UseNpgsql(connStr));
	}
}
