using Microsoft.EntityFrameworkCore;

namespace CSharpScripts.Data;

/// <summary>
/// Primary EF Core DbContext for the Scripts application.
/// NoTracking is the default; enable tracking explicitly per-operation when needed.
/// </summary>
public sealed class ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Entity configurations will be loaded here in subsequent plans (03-dbcontext-config)
    }
}
