using Microsoft.EntityFrameworkCore;
using Scripts.Data;

namespace Scripts.Tests.Environment;

internal sealed class MigrationTests
{
	[Test]
	public async Task DbContext_HasPendingModelChanges_IsFalse()
	{
		using var context = new ScriptsDbContext(
			new DbContextOptionsBuilder<ScriptsDbContext>()
				.UseNpgsql(
					"Host=localhost;Database=MigrationTest;Username=postgres;Password=postgres"
				)
				.Options
		);

		var hasChanges = context.Database.HasPendingModelChanges();

		await Assert.That(hasChanges).IsFalse();
	}
}
