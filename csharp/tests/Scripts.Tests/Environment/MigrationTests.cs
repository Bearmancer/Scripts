using TUnit;
using FluentAssertions;
using Scripts.Data;
using Microsoft.EntityFrameworkCore;

namespace Scripts.Tests.Environment;

internal sealed class MigrationTests
{
	[Test]
	public void DbContext_HasPendingModelChanges_IsFalse()
	{
		using var context = new ScriptsDbContext(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ScriptsDbContext>().UseNpgsql("Host=localhost;Database=MigrationTest;Username=postgres;Password=postgres").Options);

		var hasChanges = context.Database.HasPendingModelChanges();

		hasChanges.Should().BeFalse("because an EF migration should be generated for any changes to the entity models");
	}
}
