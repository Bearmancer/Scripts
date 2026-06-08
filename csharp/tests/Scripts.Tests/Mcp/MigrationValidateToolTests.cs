using System.Text.Json;
using FluentAssertions;
using Scripts.Mcp.Tools;
using Scripts.Tests.Attributes;
using TUnit.Core;

namespace Scripts.Tests.Mcp;

[RequiresPgConnStr]
internal sealed class MigrationValidateToolTests : DatabaseTestBase
{
    private MigrationValidateTool CreateTool()
    {
        var context = Fixture.GetContext();
        return new MigrationValidateTool(context);
    }

    [Test]
    public async Task Validate_AfterMigration_ReportsUpToDate()
    {
        var tool = CreateTool();
        var result = await tool.migration_validate();
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("status").GetString().Should().Be("up_to_date");
        json.RootElement.GetProperty("canConnect").GetBoolean().Should().BeTrue();
        json.RootElement.GetProperty("pendingCount").GetInt32().Should().Be(0);
    }

    [Test]
    public async Task Validate_AppliedMigrations_ReturnsNonEmptyList()
    {
        var tool = CreateTool();
        var result = await tool.migration_validate();
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("appliedCount").GetInt32().Should().BeGreaterThan(0);
        json.RootElement.GetProperty("appliedMigrations").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Test]
    public async Task Validate_Response_HasAllRequiredFields()
    {
        var tool = CreateTool();
        var result = await tool.migration_validate();
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("status", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("canConnect", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("appliedMigrations", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("appliedCount", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("pendingMigrations", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("pendingCount", out _).Should().BeTrue();
    }

    [Test]
    public async Task Validate_CanConnect_ReturnsTrue()
    {
        var tool = CreateTool();
        var result = await tool.migration_validate();
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("canConnect").GetBoolean().Should().BeTrue();
    }

    [Test]
    public async Task Validate_InitialCreateMigration_IsApplied()
    {
        var tool = CreateTool();
        var result = await tool.migration_validate();
        var json = JsonDocument.Parse(result);

        var applied = json.RootElement.GetProperty("appliedMigrations").EnumerateArray()
            .Select(m => m.GetString()).ToList();
        applied.Should().Contain(m => m!.Contains("InitialCreate"));
    }

    [Test]
    public async Task Validate_AppliedAndPendingCounts_AreConsistent()
    {
        var tool = CreateTool();
        var result = await tool.migration_validate();
        var json = JsonDocument.Parse(result);

        var appliedArrayLen = json.RootElement.GetProperty("appliedMigrations").GetArrayLength();
        var appliedCount = json.RootElement.GetProperty("appliedCount").GetInt32();
        appliedCount.Should().Be(appliedArrayLen);

        var pendingArrayLen = json.RootElement.GetProperty("pendingMigrations").GetArrayLength();
        var pendingCount = json.RootElement.GetProperty("pendingCount").GetInt32();
        pendingCount.Should().Be(pendingArrayLen);
    }

    [Test]
    public async Task Validate_StatusIsValidEnum()
    {
        var tool = CreateTool();
        var result = await tool.migration_validate();
        var json = JsonDocument.Parse(result);

        var status = json.RootElement.GetProperty("status").GetString();
        var validStatuses = new[] { "up_to_date", "pending_migrations", "disconnected", "error" };
        validStatuses.Should().Contain(status);
    }
}
