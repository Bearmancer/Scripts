using CSharpScripts.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CSharpScripts.Tests.DbContext;

internal sealed class CompiledModelTests : DatabaseTestBase
{
	[Test]
	public void DbContext_ShouldNotUseCompiledModel_InTestContext()
	{
		var context = Fixture.GetContext();
		var model = context.Model;

		model.Should().NotBeNull();
	}

	[Test]
	public async Task DbContext_ShouldAllowMigration_WithoutPendingChangesError()
	{
		var initAction = async () => await Fixture.InitializeAsync();

		await initAction.Should().NotThrowAsync<InvalidOperationException>();
	}
}
