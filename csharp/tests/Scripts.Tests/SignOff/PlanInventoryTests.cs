namespace Scripts.Tests.SignOff;

internal sealed class PlanInventoryTests
{
	private static readonly string PlanDir = TestPaths.Combine("AI", "plans");
	private static readonly string MasterPlan = Path.Combine(PlanDir, "MASTER_PLAN.md");

	[Test]
	public async Task Master_Plan_Exists() => await Assert.That(File.Exists(MasterPlan)).IsTrue();

	[Test]
	public async Task Master_Plan_Is_Non_Empty() =>
		await Assert.That(new FileInfo(MasterPlan).Length).IsGreaterThan(0);

	[Test]
	public async Task Master_Plan_Is_Only_Root_Level_Plan_File()
	{
		var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"MASTER_PLAN.md",
			"INDEX.md",
		};

		var topLevelMd = Directory
			.GetFiles(PlanDir, "*.md")
			.Select(f => Path.GetFileName(f)!)
			.ToArray();

		var unexpected = topLevelMd.Where(f => !allowed.Contains(f!)).ToArray();

		await Assert.That(unexpected).IsEmpty();

		await Assert.That(topLevelMd).Contains("MASTER_PLAN.md");
	}
}
