using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;

namespace Scripts.Tests.DbContext;

internal sealed class DbContextSourceRecordDbSetTests
{
	[Test]
	public async Task DbContext_HasSourceRecords_DbSet()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("SourceRecordDbSetTest_" + Guid.NewGuid())
			.Options;

		using var context = new ScriptsDbContext(options);
		await Assert.That(context.SourceRecords).IsNotNull();
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
		await Assert.That(sourceRecordType).IsNotNull();
	}
}
