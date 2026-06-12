using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.DbContext;

internal sealed class NullSafeStringComparerTests
{
	[Test]
	public async Task Model_Builds_For_ReleaseProgress_With_Nullable_Strings()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("NullSafeComparer_ModelBuild_" + Guid.NewGuid())
			.Options;

		using var context = new ScriptsDbContext(options);
		var model = context.Model;

		var rp = model.FindEntityType(typeof(Data.Entities.ReleaseProgress));
		await Assert.That(rp).IsNotNull();
	}

	[Test]
	public async Task ReleaseProgress_NullableString_Has_NullSafe_ValueComparer()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("NullSafeComparer_HashCheck_" + Guid.NewGuid())
			.Options;

		using var context = new ScriptsDbContext(options);
		var property = context
			.Model.FindEntityType(typeof(Data.Entities.ReleaseProgress))!
			.FindProperty(nameof(Scripts.Data.Entities.ReleaseProgress.Composer))!;

		var comparer = property.GetValueComparer();
		await Assert.That(comparer).IsNotNull();

		var hashAction = () => comparer!.GetHashCode((string?)null);
		await Assert.That(() => hashAction()).ThrowsNothing();
	}

	[Test]
	public async Task ReleaseProgress_Equality_With_Nulls_Is_Deterministic()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("NullSafeComparer_Equality_" + Guid.NewGuid())
			.Options;

		using var context = new ScriptsDbContext(options);
		var property = context
			.Model.FindEntityType(typeof(Data.Entities.ReleaseProgress))!
			.FindProperty(nameof(Scripts.Data.Entities.ReleaseProgress.WorkName))!;

		var comparer = property.GetValueComparer();
		await Assert.That(comparer).IsNotNull();

		await Assert.That(comparer!.Equals(null, null)).IsTrue();

		await Assert.That(comparer.Equals(null, "x")).IsFalse();
		await Assert.That(comparer.Equals("x", null)).IsFalse();

		await Assert.That(comparer.Equals("hello", "hello")).IsTrue();

		await Assert.That(comparer.Equals("Hello", "hello")).IsFalse();
	}

	[Test]
	public async Task AllStringProperties_Have_NullSafe_Hash_Function()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("NullSafeComparer_AllStrings_" + Guid.NewGuid())
			.Options;

		using var context = new ScriptsDbContext(options);
		var model = context.Model;

		var stringProperties = model
			.GetEntityTypes()
			.SelectMany(et => et.GetProperties())
			.Where(p => p.ClrType == typeof(string))
			.ToList();

		await Assert.That(stringProperties).IsNotEmpty();

		foreach (var property in stringProperties)
		{
			var comparer = property.GetValueComparer();
			await Assert.That(comparer).IsNotNull();

			var hashAction = () => comparer!.GetHashCode((string?)null);
			await Assert.That(() => hashAction()).ThrowsNothing();
		}
	}
}

[RequiresPgConnStr]
internal sealed class NullSafeComparerCompiledModelTests : DatabaseTestBase
{
	[Test]
	public async Task CompiledModel_Initializes_With_NullSafe_Comparer_Path_Enabled()
	{
		await using var context = Fixture.GetContext();
		var model = context.Model;
		await Assert.That(model).IsNotNull();
		await Assert.That(model.GetEntityTypes()).IsNotEmpty();
	}
}
