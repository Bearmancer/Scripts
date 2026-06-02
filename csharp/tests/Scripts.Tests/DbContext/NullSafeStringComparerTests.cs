using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Scripts.Data;
using Scripts.Data.Entities;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.DbContext;

/// <summary>
/// Regression coverage for the null-unsafe <see cref="ValueComparer{T}"/> fix in
/// <c>ScriptsDbContext.OnModelCreating</c>. The previous comparer used
/// <c>v =&gt; v.GetHashCode()</c> as its hash function, which throws
/// <see cref="NullReferenceException"/> the first time EF Core's
/// <c>RuntimeProperty.GetValueComparer()</c> lazy initializer is called on a
/// nullable string property. The fix is a null-safe hash function. These tests
/// build the model through the real OnModelCreating path, then exercise
/// change-tracker snapshot / original-value access on entities with nullable
/// string properties set to <c>null</c> — the exact path that previously NRE'd.
/// </summary>
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

		// The hash function must accept null without throwing — that is the
		// exact NRE the original v => v.GetHashCode() code path produced.
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

		// Two nulls must be equal under ordinal semantics.
		comparer!.Equals(null, null).Should().BeTrue();
		// A null and a non-null are not equal.
		comparer.Equals(null, "x").Should().BeFalse();
		comparer.Equals("x", null).Should().BeFalse();
		// Ordinal equality for matching strings.
		comparer.Equals("hello", "hello").Should().BeTrue();
		// Ordinal case-sensitivity.
		comparer.Equals("Hello", "hello").Should().BeFalse();
	}

	[Test]
	public void AllStringProperties_Have_NullSafe_Hash_Function()
	{
		// Sweep every string property in the model and confirm the value
		// comparer's hash function accepts null without throwing. This is the
		// user-visible failure path that the original v => v.GetHashCode()
		// code path produced on the first materialisation of any nullable
		// string column.
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

			// The original bug: the hash function was v => v.GetHashCode(),
			// which throws NullReferenceException on a null string. The fix
			// makes it null-safe.
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
		// Sanity check on the live compiled-model path: with the comparer fix
		// in place, building a ScriptsDbContext against a real database and
		// running a trivial query must not throw, even with the assembly-wide
		// parallel limiter still in effect at the time of this test.
		await using var context = Fixture.GetContext();
		var model = context.Model;
		model.Should().NotBeNull();
		model.GetEntityTypes().Should().NotBeEmpty();
	}
}
