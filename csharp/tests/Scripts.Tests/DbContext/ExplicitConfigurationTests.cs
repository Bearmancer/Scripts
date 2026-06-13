using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;

namespace Scripts.Tests.DbContext;

internal sealed class ExplicitConfigurationTests
{
	[Test]
	public async Task OnModelCreating_UsesExplicitApplyConfiguration_ForAllTenEntities()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("ExplicitConfigTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);
		var model = context.Model;

		var entityTypes = model
			.GetEntityTypes()
			.Where(e => !e.IsOwned())
			.Select(e => e.ClrType)
			.ToList();

		await Assert.That(entityTypes).Count().IsEqualTo(8);

		await Assert.That(entityTypes).Contains(typeof(Artist));
		await Assert.That(entityTypes).Contains(typeof(Album));
		await Assert.That(entityTypes).Contains(typeof(Track));
		await Assert.That(entityTypes).Contains(typeof(Scrobble));
		await Assert.That(entityTypes).Contains(typeof(Video));
		await Assert.That(entityTypes).Contains(typeof(FiberyEntity));
		await Assert.That(entityTypes).Contains(typeof(SourceRecord));
		await Assert.That(entityTypes).Contains(typeof(Data.Entities.ReleaseProgress));
	}

	[Test]
	public async Task ScriptsDbContext_DoesNotUseApplyConfigurationsFromAssembly()
	{
		var contextType = typeof(ScriptsDbContext);
		var onModelCreatingMethod = contextType.GetMethod(
			"OnModelCreating",
			BindingFlags.NonPublic | BindingFlags.Instance
		);

		await Assert.That(onModelCreatingMethod).IsNotNull();

		var methodBody = onModelCreatingMethod!.ToString();

		await Assert.That(methodBody).DoesNotContain("ApplyConfigurationsFromAssembly");
	}

	[Test]
	public async Task AllEntityConfigurations_AreAppliedTogether()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("AllConfigsTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);
		var model = context.Model;

		var configuredEntities = model
			.GetEntityTypes()
			.Where(e => !e.IsOwned())
			.Select(e => e.ClrType.Name)
			.OrderBy(n => n)
			.ToList();

		var expectedEntities = new[]
		{
			nameof(Album),
			nameof(Artist),
			nameof(FiberyEntity),
			nameof(Scripts.Data.Entities.ReleaseProgress),
			nameof(Scrobble),
			nameof(SourceRecord),
			nameof(Track),
			nameof(Video),
		}
			.OrderBy(n => n)
			.ToList();

		await Assert.That(configuredEntities).IsEquivalentTo(expectedEntities);
	}
}
