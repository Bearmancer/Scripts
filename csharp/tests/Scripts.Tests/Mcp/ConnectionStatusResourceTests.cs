using System.Text.Json;
using FluentAssertions;
using Scripts.Mcp.Resources;
using Scripts.Tests.Attributes;
using TUnit.Core;

namespace Scripts.Tests.Mcp;

[RequiresPgConnStr]
internal sealed class ConnectionStatusResourceTests : DatabaseTestBase
{
    private ConnectionStatusResource CreateResource()
    {
        var context = Fixture.GetContext();
        return new ConnectionStatusResource(context);
    }

    [Test]
    public async Task GetStatus_ReportsConnected()
    {
        var resource = CreateResource();
        var result = await resource.GetConnectionStatus();
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("connected").GetBoolean().Should().BeTrue();
    }

    [Test]
    public async Task GetStatus_ReportsDatabaseName()
    {
        var resource = CreateResource();
        var result = await resource.GetConnectionStatus();
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("database", out _).Should().BeTrue();
        json.RootElement.GetProperty("database").GetString().Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task GetStatus_ReportsServerVersion()
    {
        var resource = CreateResource();
        var result = await resource.GetConnectionStatus();
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("serverVersion", out _).Should().BeTrue();
        json.RootElement.GetProperty("serverVersion").GetString().Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task GetStatus_ReportsLatency()
    {
        var resource = CreateResource();
        var result = await resource.GetConnectionStatus();
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("latencyMs", out _).Should().BeTrue();
        json.RootElement.GetProperty("latencyMs").GetInt64().Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GetStatus_MasksPasswordInConnectionString()
    {
        var resource = CreateResource();
        var result = await resource.GetConnectionStatus();
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("connectionString", out _).Should().BeTrue();
        var connStr = json.RootElement.GetProperty("connectionString").GetString();
        connStr.Should().NotContain("lance");
        connStr.Should().Contain("Password=***");
    }

    [Test]
    public async Task GetStatus_HasAllRequiredFields()
    {
        var resource = CreateResource();
        var result = await resource.GetConnectionStatus();
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("connected", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("database", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("server", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("serverVersion", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("latencyMs", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("connectionString", out _).Should().BeTrue();
    }
}
