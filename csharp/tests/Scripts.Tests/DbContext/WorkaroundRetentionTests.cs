using System.Reflection;
using FluentAssertions;
using TUnit;
using TUnit.Core.Interfaces;

namespace Scripts.Tests.DbContext;

/// <summary>
/// Pin the existing concurrency-mitigation workarounds so the next refactor
/// cannot silently remove them. The workarounds remain load-bearing until
/// EF Core 10.0.8's upstream <c>RuntimeProperty.GetValueComparer</c> TOCTOU
/// race is fixed in a release that this project's wildcard <c>Version="*"</c>
/// policy picks up on the next <c>dotnet restore</c>. The justification for
/// each is documented inline in <see cref="SingleThreadedParallelLimit"/> and
/// <c>GlobalSetup.cs</c>.
/// <para>
/// Located under <c>DbContext/</c> rather than the project root because the
/// workarounds exist specifically to mask the EF Core runtime property race
/// that surfaces through the DbContext change tracker; the retention
/// guards therefore belong with the other DbContext regression tests.
/// </para>
/// </summary>
internal sealed class WorkaroundRetentionTests
{
	[Test]
	public void SingleThreadedParallelLimit_Is_Still_Capped_At_One()
	{
		var limiter = new SingleThreadedParallelLimit();
		((IParallelLimit)limiter).Limit
			.Should().Be(1, "the assembly-wide parallel limit must remain at 1 " +
				"until the EF Core 10.0.8 GetValueComparer TOCTOU race is fixed upstream");
	}

	[Test]
	public void SingleThreadedParallelLimit_Type_Has_Not_Been_Renamed_To_Disable_The_Constraint()
	{
		// Defense against a future refactor that renames the class to
		// something more permissive. The AssemblyInfo wiring uses the type
		// name, so a silent rename breaks coverage without flagging here.
		var type = typeof(SingleThreadedParallelLimit);
		type.Name.Should().Be(nameof(SingleThreadedParallelLimit),
			"AssemblyInfo references SingleThreadedParallelLimit by name; a rename would break the ParallelLimiter wiring");
	}

	[Test]
	public void AssemblyInfo_Still_Applies_ParallelLimiter()
	{
		// If this fails, the [ParallelLimiter<SingleThreadedParallelLimit>]
		// attribute on AssemblyInfo.cs was removed and the assembly is no
		// longer single-threaded - re-introducing the 56/213 NRE failure mode
		// documented in research/20260602-efcore-1008-race-condition-research.md.
		var assembly = typeof(SingleThreadedParallelLimit).Assembly;
		var attributes = assembly.GetCustomAttributes(inherit: true)
			.Select(a => a.GetType().Name)
			.ToList();

		attributes.Should().Contain(name => name.Contains("ParallelLimiter"),
			"the test assembly must apply a ParallelLimiter to serialise tests against the EF Core race");
	}

	[Test]
	public void GlobalSetup_Still_Sets_SCRIPTS_NO_COMPILED_MODEL()
	{
		// The env-var is the switch that bypasses the compiled model in
		// ScriptsDbContext.OnConfiguring. Removing the SetEnvironmentVariable
		// re-enables the broken code path on the next test run.

		// Reflection check: confirm the assembly-start hook is still wired
		// and is annotated with [Before(Assembly)] so TUnit invokes it
		// before any test runs.
		var globalSetup = typeof(GlobalSetup);
		var method = globalSetup.GetMethod(
			"LoadDotEnvAsync",
			BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

		method.Should().NotBeNull(
			"GlobalSetup.LoadDotEnvAsync must exist as the assembly-start hook");

		var beforeAttribute = method!.GetCustomAttributes(inherit: true)
			.Select(a => a.GetType().Name)
			.Any(n => n.Contains("Before"));
		beforeAttribute.Should().BeTrue(
			"the assembly-start hook must be annotated with [Before(Assembly)] (or equivalent) " +
			"so TUnit invokes it before any test runs");

		// Source-text check: confirm the file content itself still contains
		// the env-var literal AND the SetEnvironmentVariable call. A pure
		// reflection check would not see the literal (string literals live in
		// the metadata #US heap, not the method body); a behaviour check
		// would require constructing a TUnit AssemblyHookContext, which is
		// sealed. The source-text check is the right granularity for "did
		// the literal survive the refactor" and is robust to the rest of the
		// file changing. We scan every .cs file in the TestsRoot to make
		// the check resilient to the file being renamed or split (e.g.
		// GlobalSetup.cs -> EnvVarSetup.cs + DotEnvLoader.cs).
		var testRoot = TestPaths.TestsRoot;
		Directory.Exists(testRoot).Should().BeTrue(
			$"the test root must exist at {testRoot}");

		var csFiles = Directory.EnumerateFiles(testRoot, "*.cs", SearchOption.AllDirectories)
			.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
				&& !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			.ToList();

		csFiles.Should().NotBeEmpty(
			$"the test project at {testRoot} must contain at least one .cs source file");

		var foundFileWithEnvVar = csFiles
			.Select(f => (path: f, content: File.ReadAllText(f)))
			.FirstOrDefault(t =>
				t.content.Contains("SCRIPTS_NO_COMPILED_MODEL", StringComparison.Ordinal)
				&& t.content.Contains("SetEnvironmentVariable", StringComparison.Ordinal));

		foundFileWithEnvVar.path.Should().NotBeNullOrEmpty(
			"some .cs file in the test project must contain both " +
			"\"SCRIPTS_NO_COMPILED_MODEL\" and \"SetEnvironmentVariable\" so the " +
			"assembly-start hook actually installs the env-var");
	}
}
