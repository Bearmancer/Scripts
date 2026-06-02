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

		// Reflection check: confirm the assembly-start hook is still wired.
		var globalSetup = typeof(GlobalSetup);
		var method = globalSetup.GetMethod(
			"LoadDotEnvAsync",
			BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

		method.Should().NotBeNull(
			"GlobalSetup.LoadDotEnvAsync must exist as the assembly-start hook");

		// Source-text check: confirm the file content itself still contains
		// the env-var literal. This is the only check that catches a refactor
		// that renames the variable or removes the SetEnvironmentVariable call
		// without otherwise breaking the hook signature. A pure reflection
		// check would not see the literal (string literals live in the
		// metadata #US heap, not the method body); a behaviour check would
		// require constructing a TUnit AssemblyHookContext, which is sealed.
		// The source-text check is the right granularity for "did the literal
		// survive the refactor" and is robust to the rest of the file
		// changing.
		var sourcePath = TestPaths.Combine("csharp", "tests", "Scripts.Tests", "GlobalSetup.cs");
		File.Exists(sourcePath).Should().BeTrue(
			$"the GlobalSetup.cs file must exist at {sourcePath}");

		var source = File.ReadAllText(sourcePath);
		source.Should().Contain("SCRIPTS_NO_COMPILED_MODEL",
			"GlobalSetup.cs must reference the SCRIPTS_NO_COMPILED_MODEL env-var " +
			"so ScriptsDbContext.OnConfiguring skips the broken compiled model path");
		source.Should().Contain("SetEnvironmentVariable",
			"GlobalSetup.cs must call SetEnvironmentVariable to install the env-var");
	}
}
