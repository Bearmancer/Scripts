using System;
using System.Threading.Tasks;
using CSharpScripts.Tests.DbContext;

namespace CSharpScripts.Tests;

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
		{
			await ((IAsyncDisposable)Fixture).DisposeAsync();
		}
	}
}
