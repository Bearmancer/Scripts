<<<<<<< HEAD
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Tests.DbContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TUnit;
=======
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
>>>>>>> d057b9bb8ac223cfc175063f75aa77cad063fcb1

namespace Scripts.Tests.DbContext;

internal sealed class DbContextSourceRecordDbSetTests
{
	[Test]
<<<<<<< HEAD
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
=======
	public void DbContext_HasSourceRecords_DbSet()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("SourceRecordDbSetTest_" + Guid.NewGuid())
			.Options;

		using var context = new ScriptsDbContext(options);
		context.SourceRecords.Should().NotBeNull();
>>>>>>> d057b9bb8ac223cfc175063f75aa77cad063fcb1
	}

	[Test]
	public async Task SourceRecord_IsInModel_AfterDbSetAdded()
	{
<<<<<<< HEAD
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
=======
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("SourceRecordModelTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);
		var model = context.Model;

		var sourceRecordType = model.FindEntityType(typeof(SourceRecord));
		sourceRecordType.Should().NotBeNull(because: "SourceRecord entity must be discoverable by the model");
>>>>>>> d057b9bb8ac223cfc175063f75aa77cad063fcb1
	}
}
