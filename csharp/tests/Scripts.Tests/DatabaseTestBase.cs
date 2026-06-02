using Scripts.Tests.DbContext;

namespace Scripts.Tests;

/// <summary>
/// Base class for integration tests that need a real PostgreSQL database.
/// One fixture instance is created per test class (not per test).
/// 
/// Migration path: convert consumers to [ClassDataSource&lt;DatabaseTestFixture&gt;]
/// constructor injection instead, then delete this base class.
/// </summary>
internal abstract class DatabaseTestBase
{
#pragma warning disable TUnit0023
	protected DatabaseTestFixture Fixture { get; private set; } = null!;
#pragma warning restore TUnit0023

	[Before(Test)]
	public async Task SetupFixture()
	{
		Fixture = new DatabaseTestFixture();
		await Fixture.InitializeAsync();
	}

	[After(Test)]
	public async Task TeardownFixture()
	{
		if (Fixture is not null)
			await ((IAsyncDisposable)Fixture).DisposeAsync();
	}
}

