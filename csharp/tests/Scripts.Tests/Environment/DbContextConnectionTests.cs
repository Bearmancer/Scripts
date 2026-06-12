using Microsoft.EntityFrameworkCore;
using Scripts.Data;

namespace Scripts.Tests.Environment;

internal sealed class DbContextConnectionTests
{
	private static ScriptsDbContext CreateContext() =>
		new(
			new DbContextOptionsBuilder<ScriptsDbContext>()
				.UseNpgsql(
					System.Environment.GetEnvironmentVariable("PGCONNSTR")
						?? throw new InvalidOperationException("PGCONNSTR not set")
				)
				.Options
		);

	[Test]
	public async Task DatabaseConnection_Succeeds_WithValidConnectionString()
	{
		await using var context = CreateContext();
		var canConnect = await context.Database.CanConnectAsync();
		await Assert.That(canConnect).IsTrue();
	}
}
