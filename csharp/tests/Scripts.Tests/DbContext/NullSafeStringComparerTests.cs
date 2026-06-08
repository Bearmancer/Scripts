using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Scripts.Data;
using Scripts.Data.Entities;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.DbContext;












internal sealed class NullSafeStringComparerTests
{
	[Test]
	public void Model_Builds_For_ReleaseProgress_With_Nullable_Strings()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("NullSafeComparer_ModelBuild_" + Guid.NewGuid())
			.Options;

		using var context = new ScriptsDbContext(options);
		var model = context.Model;

		var rp = model.FindEntityType(typeof(ReleaseProgress));
		rp.Should().NotBeNull();
	}

	[Test]
	public void ReleaseProgress_NullableString_Has_NullSafe_ValueComparer()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("NullSafeComparer_HashCheck_" + Guid.NewGuid())
			.Options;

		using var context = new ScriptsDbContext(options);
		var property = context.Model
			.FindEntityType(typeof(ReleaseProgress))!
			.FindProperty(nameof(ReleaseProgress.Composer))!;

		var comparer = property.GetValueComparer();
		comparer.Should().NotBeNull();

		
		
		var hashAction = () => comparer!.GetHashCode((string?)null);
		hashAction.Should().NotThrow("the comparer hash function must be null-safe");
	}

	[Test]
	public void ReleaseProgress_Equality_With_Nulls_Is_Deterministic()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("NullSafeComparer_Equality_" + Guid.NewGuid())
			.Options;

		using var context = new ScriptsDbContext(options);
		var property = context.Model
			.FindEntityType(typeof(ReleaseProgress))!
			.FindProperty(nameof(ReleaseProgress.WorkName))!;

		var comparer = property.GetValueComparer();
		comparer.Should().NotBeNull();

		
		comparer!.Equals(null, null).Should().BeTrue();
		
		comparer.Equals(null, "x").Should().BeFalse();
		comparer.Equals("x", null).Should().BeFalse();
		
		comparer.Equals("hello", "hello").Should().BeTrue();
		
		comparer.Equals("Hello", "hello").Should().BeFalse();
	}

	[Test]
	public void AllStringProperties_Have_NullSafe_Hash_Function()
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

		stringProperties.Should().NotBeEmpty(
			"ScriptsDbContext should expose at least one string property");

		foreach (var property in stringProperties)
		{
			var comparer = property.GetValueComparer();
			comparer.Should().NotBeNull(
				$"string property {property.DeclaringType.ClrType.Name}.{property.Name} must have a value comparer");

			
			
			
			var hashAction = () => comparer!.GetHashCode((string?)null);
			hashAction.Should().NotThrow(
				$"comparer for {property.DeclaringType.ClrType.Name}.{property.Name} must hash null safely");
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
		model.Should().NotBeNull();
		model.GetEntityTypes().Should().NotBeEmpty();
	}
}
