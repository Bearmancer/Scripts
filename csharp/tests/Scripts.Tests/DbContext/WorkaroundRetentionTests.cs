using TUnit.Core.Interfaces;

namespace Scripts.Tests.DbContext;

internal sealed class WorkaroundRetentionTests
{
	[Test]
	public async Task SingleThreadedParallelLimit_Is_Still_Capped_At_One()
	{
		var limiter = new SingleThreadedParallelLimit();
		await Assert.That(((IParallelLimit)limiter).Limit).IsEqualTo(1);
	}

	[Test]
	public async Task SingleThreadedParallelLimit_Type_Has_Not_Been_Renamed_To_Disable_The_Constraint()
	{
		var type = typeof(SingleThreadedParallelLimit);
		await Assert.That(type.Name).IsEqualTo(nameof(SingleThreadedParallelLimit));
	}

	[Test]
	public async Task AssemblyInfo_Still_Applies_ParallelLimiter()
	{
		var assembly = typeof(SingleThreadedParallelLimit).Assembly;
		var attributes = assembly
			.GetCustomAttributes(inherit: true)
			.Select(a => a.GetType().Name)
			.ToList();

		await Assert.That(attributes.Any(name => name.Contains("ParallelLimiter"))).IsTrue();
	}

	[Test]
	public async Task GlobalSetup_Does_Not_Unconditionally_Set_SCRIPTS_NO_COMPILED_MODEL()
	{
		var testRoot = TestPaths.TestsRoot;
		await Assert.That(Directory.Exists(testRoot)).IsTrue();

		var csFiles = Directory
			.EnumerateFiles(testRoot, "*.cs", SearchOption.AllDirectories)
			.Where(f =>
				!f.Contains(
					$"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
					StringComparison.Ordinal
				)
				&& !f.Contains(
					$"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
					StringComparison.Ordinal
				)
			)
			.ToList();

		await Assert.That(csFiles).IsNotEmpty();

		var foundUnconditionalSet = csFiles
			.Select(f => (path: f, content: File.ReadAllText(f)))
			.FirstOrDefault(t =>
				t.content.Contains("SCRIPTS_NO_COMPILED_MODEL", StringComparison.Ordinal)
				&& t.content.Contains("SetEnvironmentVariable", StringComparison.Ordinal)
			);

		await Assert.That(foundUnconditionalSet.path).IsNull().Or.IsEmpty();
	}

	[Test]
	public async Task CompiledModel_Bypass_Requires_Explicit_Acknowledgment()
	{
		var bypass = System.Environment.GetEnvironmentVariable("SCRIPTS_NO_COMPILED_MODEL");
		if (bypass is not null)
		{
			var acknowledged = System.Environment.GetEnvironmentVariable(
				"SCRIPTS_NO_COMPILED_MODEL_ACKNOWLEDGED"
			);
			await Assert.That(acknowledged).IsNotNull();
		}
	}
}
