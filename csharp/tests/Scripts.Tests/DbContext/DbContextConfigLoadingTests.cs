using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Tests.DbContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TUnit;

namespace Scripts.Tests.DbContext;

internal sealed class DbContextConfigLoadingTests
{
	[Test]
	public async Task OnModelCreating_Discovers_AllConfigEntities()
	{
		var fixture = new DatabaseTestFixture();
		await fixture.InitializeAsync();
		await using (fixture)
		{
			var context = fixture.GetContext();
			await using (context)
			{
				var model = context.Model;
				var entityTypes = model.GetEntityTypes().Select(e => e.ClrType).ToList();

				entityTypes.Should().Contain(typeof(Artist));
				entityTypes.Should().Contain(typeof(Album));
				entityTypes.Should().Contain(typeof(Track));
				entityTypes.Should().Contain(typeof(Scrobble));
				entityTypes.Should().Contain(typeof(Video));
				entityTypes.Should().Contain(typeof(ExecutionLog));
				entityTypes.Should().Contain(typeof(FailedTask));
				entityTypes.Should().Contain(typeof(FiberyEntity));
			}
		}
	}

	[Test]
	public async Task ArtistsTable_HasCorrectName()
	{
		var fixture = new DatabaseTestFixture();
		await fixture.InitializeAsync();
		await using (fixture)
		{
			var context = fixture.GetContext();
			await using (context)
			{
				var entityType = context.Model.FindEntityType(typeof(Artist));

				entityType.Should().NotBeNull();
				entityType!.GetTableName().Should().Be("artists");
			}
		}
	}

	[Test]
	public async Task ScrobblesTable_HasCorrectTimestampColumnType()
	{
		var fixture = new DatabaseTestFixture();
		await fixture.InitializeAsync();
		await using (fixture)
		{
			var context = fixture.GetContext();
			await using (context)
			{
				var entityType = context.Model.FindEntityType(typeof(Scrobble));
				var scrobbledAt = entityType!.FindProperty("ScrobbledAt");

				scrobbledAt.Should().NotBeNull();
				scrobbledAt!
					.GetAnnotations()
					.FirstOrDefault(a => a.Name == "Relational:ColumnType")
					?.Value.Should()
					.Be("timestamptz");
			}
		}
	}
}
