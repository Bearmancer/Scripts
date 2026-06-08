using System.Text.Json;
using FluentAssertions;
using Scripts.Mcp.Tools;
using Scripts.Tests.Attributes;
using TUnit.Core;

namespace Scripts.Tests.Mcp;

[RequiresPgConnStr]
internal sealed class QueryExecuteToolTests : DatabaseTestBase
{
    [Test]
    public async Task Select_CurrentDatabase_ReturnsPgDb()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("SELECT current_database()");
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("rowCount").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("columns").EnumerateArray()
            .Should().Contain(c => c.GetString() == "current_database");
        json.RootElement.GetProperty("rows")[0].GetProperty("current_database").GetString()
            .Should().Be("pg_db");
    }

    [Test]
    public async Task Select_DatabaseVersion_ReturnsString()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("SELECT version()");
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("rowCount").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("rows")[0].GetProperty("version").GetString()
            .Should().Contain("PostgreSQL");
    }

    [Test]
    public async Task Select_InformationSchemaTables_ReturnsMultipleRows()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name");
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("rowCount").GetInt32().Should().BeGreaterThan(0);
        json.RootElement.GetProperty("columns")[0].GetString().Should().Be("table_name");
        var tables = json.RootElement.GetProperty("rows").EnumerateArray()
            .Select(r => r.GetProperty("table_name").GetString()).ToList();
        tables.Should().Contain("artists");
        tables.Should().Contain("albums");
    }

    [Test]
    public async Task Select_WithParameters_ReturnsFilteredResults()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute(
            "SELECT @p0 AS name, @p1 AS value",
            "[\"test_name\", 42]");
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("rowCount").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("rows")[0].GetProperty("name").GetString().Should().Be("test_name");
        json.RootElement.GetProperty("rows")[0].GetProperty("value").GetInt32().Should().Be(42);
    }

    [Test]
    public async Task Select_WithNullParameter_ReturnsNull()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute(
            "SELECT @p0 AS nullable_value",
            "[null]");
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("rowCount").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("rows")[0].GetProperty("nullable_value").ValueKind
            .Should().Be(JsonValueKind.Null);
    }

    [Test]
    public async Task Select_WithBooleanParameter_ReturnsCorrectly()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute(
            "SELECT @p0 AS is_true, @p1 AS is_false",
            "[true, false]");
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("rows")[0].GetProperty("is_true").GetBoolean().Should().BeTrue();
        json.RootElement.GetProperty("rows")[0].GetProperty("is_false").GetBoolean().Should().BeFalse();
    }

    [Test]
    public async Task With_CteQuery_ReturnsResults()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("WITH numbers AS (SELECT generate_series(1, 5) AS n) SELECT n, n * n AS squared FROM numbers ORDER BY n");
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("rowCount").GetInt32().Should().Be(5);
        json.RootElement.GetProperty("columns").EnumerateArray()
            .Should().Contain(c => c.GetString() == "n")
            .And.Contain(c => c.GetString() == "squared");
    }

    [Test]
    public async Task Explain_Select_ReturnsPlan()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("EXPLAIN SELECT 1");
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("rowCount").GetInt32().Should().BeGreaterThan(0);
        json.RootElement.GetProperty("columns")[0].GetString().Should().Be("QUERY PLAN");
    }

    [Test]
    public async Task Show_ServerVersion_ReturnsValue()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("SHOW server_version");
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("rowCount").GetInt32().Should().Be(1);
    }

    [Test]
    public async Task Table_InformationSchemaTables_ReturnsRows()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("TABLE information_schema.tables LIMIT 5");
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("rowCount").GetInt32().Should().BeGreaterThan(0);
    }

    [Test]
    public async Task EmptySql_ReturnsError()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("");
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("error", out _).Should().BeTrue();
    }

    [Test]
    public async Task WhitespaceSql_ReturnsError()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("   \t\n  ");
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("error", out _).Should().BeTrue();
    }

    [Test]
    public async Task Insert_Blocked_ReturnsError()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("INSERT INTO artists (name) VALUES ('test')");
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("error", out _).Should().BeTrue();
        json.RootElement.GetProperty("error").GetString().Should().Contain("read-only");
    }

    [Test]
    public async Task Update_Blocked_ReturnsError()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("UPDATE artists SET name = 'x' WHERE id = 1");
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("error", out _).Should().BeTrue();
    }

    [Test]
    public async Task Delete_Blocked_ReturnsError()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("DELETE FROM artists WHERE id = 1");
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("error", out _).Should().BeTrue();
    }

    [Test]
    public async Task Drop_Blocked_ReturnsError()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("DROP TABLE IF EXISTS test_table");
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("error", out _).Should().BeTrue();
    }

    [Test]
    public async Task InvalidSql_ReturnsPostgresError()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("SELECT * FROM nonexistent_table_xyz");
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("error", out _).Should().BeTrue();
        json.RootElement.GetProperty("error").GetString().Should().Contain("PostgreSQL");
    }

    [Test]
    public async Task Select_ReturnsColumnsAndRows_StructureIsValid()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("SELECT 1 AS a, 2 AS b, 3 AS c");
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("columns", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("rows", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("rowCount", out _).Should().BeTrue();
        json.RootElement.GetProperty("columns").GetArrayLength().Should().Be(3);
        json.RootElement.GetProperty("rowCount").GetInt32().Should().Be(1);
    }

    [Test]
    public async Task Select_EmptyResult_ReturnsZeroRows()
    {
        await using var context = Fixture.GetContext();
        var tool = new QueryExecuteTool(context);
        var result = await tool.query_execute("SELECT * FROM artists WHERE name = 'NO_MATCH_XYZ_123'");
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("rowCount").GetInt32().Should().Be(0);
        json.RootElement.GetProperty("rows").GetArrayLength().Should().Be(0);
    }
}
