using TUnit;
using FluentAssertions;
using CSharpScripts.Data;
using Microsoft.EntityFrameworkCore;

namespace Scripts.Tests.Environment;

internal sealed class MigrationTests
{
    [Test]
    public void DbContext_HasPendingModelChanges_IsFalse()
    {
        // This test ensures that the current code model matches the latest migration snapshot
        using var context = new ScriptsDbContext(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ScriptsDbContext>().UseNpgsql("Host=localhost;Database=MigrationTest;Username=postgres;Password=postgres").Options);
        
        var hasChanges = context.Database.HasPendingModelChanges();
        
        hasChanges.Should().BeFalse("because an EF migration should be generated for any changes to the entity models");
    }
}
