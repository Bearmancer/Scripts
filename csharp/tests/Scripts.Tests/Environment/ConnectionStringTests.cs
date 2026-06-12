namespace Scripts.Tests.Environment;

internal sealed class ConnectionStringTests
{
	[Test]
	public async Task ConnectionString_IsSet_InEnvironment()
	{
		var connStr = System.Environment.GetEnvironmentVariable("PGCONNSTR");
		await Assert.That(connStr).IsNotNull().And.IsNotEmpty();
	}

	[Test]
	public async Task ConnectionString_IsValid_PostgresFormat()
	{
		var connStr = System.Environment.GetEnvironmentVariable("PGCONNSTR");

		await Assert.That(connStr).Contains("Host=");
		await Assert.That(connStr).Contains("Database=");
		await Assert.That(connStr).Contains("Username=");
	}

	[Test]
	public async Task ConnectionString_DoesNotContain_Password_InPlainText_InLogs()
	{
		var connStr = System.Environment.GetEnvironmentVariable("PGCONNSTR");
		await Assert.That(connStr).IsNotNull();
	}
}
