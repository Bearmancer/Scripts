using FluentAssertions;
using TUnit;

namespace Scripts.Tests.SignOff;

internal sealed class PlanInventoryTests
{
    private static readonly string PlanDir = TestPaths.Combine("AI", "plans");
    private static readonly string MasterPlan = Path.Combine(PlanDir, "MASTER_PLAN.md");

    [Test]
    public void Master_Plan_Exists()
    {
        File.Exists(MasterPlan).Should().BeTrue(
            $"The consolidated plan file must exist at {TestPaths.Relative(MasterPlan)}"
        );
    }

    [Test]
    public void Master_Plan_Is_Non_Empty()
    {
        new FileInfo(MasterPlan).Length.Should().BeGreaterThan(0,
            $"Plan file must not be empty"
        );
    }

    [Test]
    public void Master_Plan_Is_Only_Root_Level_Plan_File()
    {
        
        
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MASTER_PLAN.md",
            "INDEX.md",
        };

        var topLevelMd = Directory.GetFiles(PlanDir, "*.md")
            .Select(f => Path.GetFileName(f)!)
            .ToArray();

        var unexpected = topLevelMd.Where(f => !allowed.Contains(f!)).ToArray();

        unexpected.Should().BeEmpty(
            because: $"Only MASTER_PLAN.md and INDEX.md (sentinel) should exist directly under AI/plans/. Unexpected: {string.Join(", ", unexpected)}"
        );

        topLevelMd.Should().Contain("MASTER_PLAN.md",
            because: "MASTER_PLAN.md must be present as the consolidated plan file"
        );
    }
}
