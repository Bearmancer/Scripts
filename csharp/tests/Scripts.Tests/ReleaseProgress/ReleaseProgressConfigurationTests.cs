using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.ReleaseProgressTests;

internal sealed class ReleaseProgressConfigurationTests
{
    [Test]
    public async Task ReleaseProgress_HasCorrectTableName()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(CSharpScripts.Data.Entities.ReleaseProgress));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("release_progress");
    }

    [Test]
    public async Task ReleaseProgress_HasCompositeUniqueIndex()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(CSharpScripts.Data.Entities.ReleaseProgress));

        var indexes = entityType!.GetIndexes().ToList();
        indexes.Should().Contain(i =>
            i.Properties.Any(p => p.Name == "ReleaseId") &&
            i.Properties.Any(p => p.Name == "DiscNumber") &&
            i.Properties.Any(p => p.Name == "TrackNumber") &&
            i.IsUnique);
    }

    [Test]
    public async Task ReleaseProgress_Soloists_IsJsonb()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql("Host=localhost;Database=dummy;Username=dummy;Password=dummy")
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(CSharpScripts.Data.Entities.ReleaseProgress));
        var prop = entityType!.FindProperty("Soloists");

        prop.Should().NotBeNull();
        prop!.GetColumnType().Should().Be("jsonb");
    }
}
