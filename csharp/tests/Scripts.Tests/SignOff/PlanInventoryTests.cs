using FluentAssertions;
using TUnit;

namespace Scripts.Tests.SignOff;

internal sealed class PlanInventoryTests
{
    private static readonly string PlanDir =
        TestPaths.Combine("AI", "plans", "tier-1-ef-migration");

    private static readonly string[] RequiredPlans =
    {
        "00-environment.md",
        "01-entities.md",
        "02-entity-refactoring.md",
        "03-dbcontext-config.md",
        "04-entity-configurations.md",
        "05-migrations.md",
        "06-repositories.md",
        "07-state-manager.md",
        "08-release-cache.md",
        "09-sync-service-updates.md",
        "10-ef10-queries.md",
        "11-compiled-model.md",
        "12-logging.md",
        "13-lingua.md",
        "14-resilience.md",
        "15-testcontainers.md",
        "16-sign-off.md",
    };

    [Test]
    public void All_17_Plan_Files_Exist()
    {
        var missing = new List<string>();
        foreach (var plan in RequiredPlans)
        {
            var path = Path.Combine(PlanDir, plan);
            if (!File.Exists(path))
                missing.Add(plan);
        }

        missing.Should().BeEmpty(
            $"All 17 plan files must exist. Missing: {string.Join(", ", missing)}"
        );
    }

    [Test]
    public void Plan_Files_Are_Non_Empty()
    {
        var empty = new List<string>();
        foreach (var plan in RequiredPlans)
        {
            var path = Path.Combine(PlanDir, plan);
            if (File.Exists(path) && new FileInfo(path).Length == 0)
                empty.Add(plan);
        }

        empty.Should().BeEmpty(
            $"Plan files must not be empty. Empty: {string.Join(", ", empty)}"
        );
    }
}
