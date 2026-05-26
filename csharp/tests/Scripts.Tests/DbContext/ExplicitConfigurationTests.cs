using System.Reflection;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TUnit;

namespace Scripts.Tests.DbContext;

internal sealed class ExplicitConfigurationTests
{
	[Test]
	public async Task OnModelCreating_UsesExplicitApplyConfiguration_ForAllNineEntities()
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

		entityTypes.Should().HaveCount(9, "all 9 entity configurations must be applied explicitly");

		entityTypes.Should().Contain(typeof(Artist), "ArtistConfiguration must be applied");
		entityTypes.Should().Contain(typeof(Album), "AlbumConfiguration must be applied");
		entityTypes.Should().Contain(typeof(Track), "TrackConfiguration must be applied");
		entityTypes.Should().Contain(typeof(Scrobble), "ScrobbleConfiguration must be applied");
		entityTypes.Should().Contain(typeof(Video), "VideoConfiguration must be applied");
		entityTypes
			.Should()
			.Contain(typeof(ExecutionLog), "ExecutionLogConfiguration must be applied");
		entityTypes
			.Should()
			.Contain(typeof(FiberyEntity), "FiberyEntityConfiguration must be applied");
		entityTypes.Should().Contain(typeof(FailedTask), "FailedTaskConfiguration must be applied");
		entityTypes
			.Should()
			.Contain(typeof(SourceRecord), "SourceRecordConfiguration must be applied");
	}

	[Test]
	public void ScriptsDbContext_DoesNotUseApplyConfigurationsFromAssembly()
	{
		var contextType = typeof(ScriptsDbContext);
		var onModelCreatingMethod = contextType.GetMethod(
			"OnModelCreating",
			BindingFlags.NonPublic | BindingFlags.Instance
		);

		onModelCreatingMethod.Should().NotBeNull("OnModelCreating method must exist");

		var methodBody = onModelCreatingMethod!.ToString();

		methodBody
			.Should()
			.NotContain(
				"ApplyConfigurationsFromAssembly",
				"explicit ApplyConfiguration calls must be used instead of ApplyConfigurationsFromAssembly"
			);
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
			nameof(ExecutionLog),
			nameof(FailedTask),
			nameof(FiberyEntity),
			nameof(Scrobble),
			nameof(SourceRecord),
			nameof(Track),
			nameof(Video),
		}
			.OrderBy(n => n)
			.ToList();

		configuredEntities
			.Should()
			.BeEquivalentTo(
				expectedEntities,
				"all 9 entity configurations must be applied together in OnModelCreating"
			);
	}
}
