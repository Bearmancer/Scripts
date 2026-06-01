using Microsoft.EntityFrameworkCore.Design;

namespace Scripts.Data;

internal sealed class ScriptsDbContextFactory : IDesignTimeDbContextFactory<ScriptsDbContext>
{
	public ScriptsDbContext CreateDbContext(string[] args)
	{
		DbContextOptionsBuilder<ScriptsDbContext> optionsBuilder = new();
		var connStr =
			GetEnvironmentVariable(variable: "PGCONNSTR") ?? Variables.DefaultConnectionString;

		optionsBuilder.UseNpgsql(connectionString: connStr);
		return new ScriptsDbContext(options: optionsBuilder.Options);
	}
}
