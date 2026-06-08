using FluentAssertions;
using TUnit;
using TUnit.Core.Interfaces;

namespace Scripts.Tests.DbContext;
















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
		
		
		
		var type = typeof(SingleThreadedParallelLimit);
		type.Name.Should().Be(nameof(SingleThreadedParallelLimit),
			"AssemblyInfo references SingleThreadedParallelLimit by name; a rename would break the ParallelLimiter wiring");
	}

	[Test]
	public void AssemblyInfo_Still_Applies_ParallelLimiter()
	{
		
		
		
		
		var assembly = typeof(SingleThreadedParallelLimit).Assembly;
		var attributes = assembly.GetCustomAttributes(inherit: true)
			.Select(a => a.GetType().Name)
			.ToList();

		attributes.Should().Contain(name => name.Contains("ParallelLimiter"),
			"the test assembly must apply a ParallelLimiter to serialise tests against the EF Core race");
	}

	[Test]
	public void GlobalSetup_Does_Not_Unconditionally_Set_SCRIPTS_NO_COMPILED_MODEL()
	{
		
		
		

		var testRoot = TestPaths.TestsRoot;
		Directory.Exists(testRoot).Should().BeTrue(
			$"the test root must exist at {testRoot}");

		var csFiles = Directory.EnumerateFiles(testRoot, "*.cs", SearchOption.AllDirectories)
			.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
				&& !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			.ToList();

		csFiles.Should().NotBeEmpty(
			$"the test project at {testRoot} must contain at least one .cs source file");

		var foundUnconditionalSet = csFiles
			.Select(f => (path: f, content: File.ReadAllText(f)))
			.FirstOrDefault(t =>
				t.content.Contains("SCRIPTS_NO_COMPILED_MODEL", StringComparison.Ordinal)
				&& t.content.Contains("SetEnvironmentVariable", StringComparison.Ordinal));

		foundUnconditionalSet.path.Should().BeNullOrEmpty(
			"GlobalSetup.cs must not unconditionally set SCRIPTS_NO_COMPILED_MODEL. " +
			"The compiled model is active by default; CI can opt in by setting " +
			"the env var externally before the test process starts.");
	}

	[Test]
	public void CompiledModel_Bypass_Requires_Explicit_Acknowledgment()
	{
		
		
		
		var bypass = System.Environment.GetEnvironmentVariable("SCRIPTS_NO_COMPILED_MODEL");
		if (bypass is not null)
		{
			var acknowledged = System.Environment.GetEnvironmentVariable(
				"SCRIPTS_NO_COMPILED_MODEL_ACKNOWLEDGED");
			acknowledged.Should().NotBeNull(
				"SCRIPTS_NO_COMPILED_MODEL is set, bypassing the compiled model. " +
				"This is only acceptable as an explicit CI opt-in to work around " +
				"the EF Core 10.0.8 TOCTOU race. To acknowledge this bypass, " +
				"set SCRIPTS_NO_COMPILED_MODEL_ACKNOWLEDGED=1 alongside it.");
		}
	}
}
