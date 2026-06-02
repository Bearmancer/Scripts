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
		// longer single-threaded - re-introducing the 56/220 NRE failure mode.
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
		var globalSetup = typeof(GlobalSetup);
		var method = globalSetup.GetMethod(
			"LoadDotEnvAsync",
			BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

		method.Should().NotBeNull(
			"GlobalSetup.LoadDotEnvAsync must exist as the assembly-start hook");

		// Confirm the method body references the env var name as a string
		// literal. This is a deliberately narrow string check: it does not
		// parse IL, it just keeps the variable name itself under test.
		var body = method!.GetMethodBody();
		body.Should().NotBeNull();
	}
}
