using Microsoft.EntityFrameworkCore.Design;

namespace CSharpScripts.Data;

internal sealed class ScriptsDbContextFactory : IDesignTimeDbContextFactory<ScriptsDbContext>
{
    public ScriptsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ScriptsDbContext>();
        // During local EF migrations, we will just use a dummy connection string 
        // to satisfy the builder. The actual migrations don't connect to a DB.
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR") 
                      ?? "Host=localhost;Database=dummy;Username=dummy;Password=dummy";
                      
        optionsBuilder.UseNpgsql(connStr);
        return new ScriptsDbContext(optionsBuilder.Options);
    }
}