using Microsoft.EntityFrameworkCore.Design;

namespace CSharpScripts.Data;

internal sealed class ScriptsDbContextFactory : IDesignTimeDbContextFactory<ScriptsDbContext>
{
	public ScriptsDbContext CreateDbContext(string[] args)
	{
		DbContextOptionsBuilder<ScriptsDbContext> optionsBuilder = new();
		var connStr =
			GetEnvironmentVariable(variable: "PGCONNSTR")
			?? "Host=localhost;Database=dummy;Username=dummy;Password=dummy";

		optionsBuilder.UseNpgsql(connectionString: connStr);
		return new ScriptsDbContext(options: optionsBuilder.Options);
	}
}
