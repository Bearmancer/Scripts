using System.Text.Json;
using FluentAssertions;
using Scripts.Mcp.Tools;
using Scripts.Tests.Attributes;
using TUnit.Core;

namespace Scripts.Tests.Mcp;

[RequiresPgConnStr]
internal sealed class SchemaDescribeToolTests : DatabaseTestBase
{
    private SchemaDescribeTool CreateTool()
    {
        var context = Fixture.GetContext();
        return new SchemaDescribeTool(context);
    }

    [Test]
    public void Describe_AllTables_ReturnsMultipleEntities()
    {
        var tool = CreateTool();
        var result = tool.schema_describe();
        var json = JsonDocument.Parse(result);

        json.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        json.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(5);

        var tables = json.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("table").GetString()).ToList();
        tables.Should().Contain("artists");
        tables.Should().Contain("albums");
        tables.Should().Contain("tracks");
        tables.Should().Contain("videos");
    }

    [Test]
    public void Describe_ArtistsTable_HasCorrectMetadata()
    {
        var tool = CreateTool();
        var result = tool.schema_describe("artists");
        var json = JsonDocument.Parse(result);

        json.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        json.RootElement.GetArrayLength().Should().Be(1);

        var entity = json.RootElement[0];
        entity.GetProperty("table").GetString().Should().Be("artists");
        entity.GetProperty("schema").GetString().Should().NotBeNullOrEmpty();

        var columns = entity.GetProperty("columns").EnumerateArray().ToList();
        columns.Should().Contain(c => c.GetProperty("name").GetString() == "Id");
        columns.Should().Contain(c => c.GetProperty("name").GetString() == "Name");

        var pk = columns.First(c => c.GetProperty("name").GetString() == "Id");
        pk.GetProperty("isPrimaryKey").GetBoolean().Should().BeTrue();
    }

    [Test]
    public void Describe_AlbumsTable_HasColumnsAndForeignKeys()
    {
        var tool = CreateTool();
        var result = tool.schema_describe("albums");
        var json = JsonDocument.Parse(result);

        var entity = json.RootElement[0];
        entity.GetProperty("table").GetString().Should().Be("albums");

        var columns = entity.GetProperty("columns").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString()).ToList();
        columns.Should().Contain("Id");
        columns.Should().Contain("Title");
        columns.Should().Contain("ArtistId");
    }

    [Test]
    public void Describe_AlbumsTable_HasArtistForeignKey()
    {
        var tool = CreateTool();
        var result = tool.schema_describe("albums");
        var json = JsonDocument.Parse(result);

        var fks = json.RootElement[0].GetProperty("foreignKeys").EnumerateArray().ToList();
        fks.Should().NotBeEmpty();
        fks.Should().Contain(fk => fk.GetProperty("principalTable").GetString() == "artists");
    }

    [Test]
    public void Describe_ArtistsTable_HasIndexes()
    {
        var tool = CreateTool();
        var result = tool.schema_describe("artists");
        var json = JsonDocument.Parse(result);

        var entity = json.RootElement[0];
        entity.TryGetProperty("indexes", out _).Should().BeTrue();
    }

    [Test]
    public void Describe_ArtistsTable_PrimaryKeyIsNotNullable()
    {
        var tool = CreateTool();
        var result = tool.schema_describe("artists");
        var json = JsonDocument.Parse(result);

        var columns = json.RootElement[0].GetProperty("columns").EnumerateArray();
        var pk = columns.First(c => c.GetProperty("isPrimaryKey").GetBoolean());
        pk.GetProperty("isNullable").GetBoolean().Should().BeFalse();
    }

    [Test]
    public void Describe_NonExistentTable_ReturnsErrorWithAvailableTables()
    {
        var tool = CreateTool();
        var result = tool.schema_describe("nonexistent_table_xyz");
        var json = JsonDocument.Parse(result);

        json.RootElement.TryGetProperty("error", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("availableTables", out _).Should().BeTrue();
        json.RootElement.GetProperty("availableTables").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Test]
    public void Describe_CaseInsensitiveTableMatch_Works()
    {
        var tool = CreateTool();
        var result = tool.schema_describe("ARTISTS");
        var json = JsonDocument.Parse(result);

        json.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        json.RootElement[0].GetProperty("table").GetString().Should().Be("artists");
    }

    [Test]
    public void Describe_EachColumn_HasRequiredFields()
    {
        var tool = CreateTool();
        var result = tool.schema_describe("artists");
        var json = JsonDocument.Parse(result);

        var columns = json.RootElement[0].GetProperty("columns").EnumerateArray();
        foreach (var col in columns)
        {
            col.TryGetProperty("name", out _).Should().BeTrue();
            col.TryGetProperty("type", out _).Should().BeTrue();
            col.TryGetProperty("isNullable", out _).Should().BeTrue();
            col.TryGetProperty("isPrimaryKey", out _).Should().BeTrue();
        }
    }

    [Test]
    public void Describe_TracksTable_HasRelationships()
    {
        var tool = CreateTool();
        var result = tool.schema_describe("tracks");
        var json = JsonDocument.Parse(result);

        var entity = json.RootElement[0];
        entity.GetProperty("table").GetString().Should().Be("tracks");
        entity.GetProperty("foreignKeys").EnumerateArray().Should().NotBeEmpty();
    }

    [Test]
    public void Describe_VideosTable_Exists()
    {
        var tool = CreateTool();
        var result = tool.schema_describe("videos");
        var json = JsonDocument.Parse(result);

        json.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        json.RootElement[0].GetProperty("table").GetString().Should().Be("videos");
    }
}
