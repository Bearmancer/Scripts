using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Tests.DbContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TUnit;

namespace Scripts.Tests.DbContext;

internal sealed class DbContextSourceRecordDbSetTests
{
	[Test]
	public async Task DbContext_HasSourceRecords_DbSet()
	{
		var fixture = new DatabaseTestFixture();
		await fixture.InitializeAsync();
		await using (fixture)
		{
			var context = fixture.GetContext();
			await using (context)
			{
				context.SourceRecords.Should().NotBeNull();
			}
		}
	}

	[Test]
	public async Task SourceRecord_IsInModel_AfterDbSetAdded()
	{
		var fixture = new DatabaseTestFixture();
		await fixture.InitializeAsync();
		await using (fixture)
		{
			var context = fixture.GetContext();
			await using (context)
			{
				var model = context.Model;
				var sourceRecordType = model.FindEntityType(typeof(SourceRecord));
				sourceRecordType
					.Should()
					.NotBeNull(because: "SourceRecord entity must be discoverable by the model");
			}
		}
	}
}
