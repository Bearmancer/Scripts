using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;

namespace Scripts.Tests.DbContext;

internal sealed class DbContextSourceRecordDbSetTests
{
	[Test]
	public void DbContext_HasSourceRecords_DbSet()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("SourceRecordDbSetTest_" + Guid.NewGuid())
			.Options;

		using var context = new ScriptsDbContext(options);
		context.SourceRecords.Should().NotBeNull();
	}

	[Test]
	public async Task SourceRecord_IsInModel_AfterDbSetAdded()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("SourceRecordModelTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);
		var model = context.Model;

		var sourceRecordType = model.FindEntityType(typeof(SourceRecord));
		sourceRecordType.Should().NotBeNull(because: "SourceRecord entity must be discoverable by the model");
	}
}
