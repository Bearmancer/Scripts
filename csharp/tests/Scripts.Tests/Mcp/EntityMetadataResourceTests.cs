using System.Text.Json;
using FluentAssertions;
using Scripts.Mcp.Resources;
using TUnit.Core;

namespace Scripts.Tests.Mcp;

internal sealed class EntityMetadataResourceTests : DatabaseTestBase
{
    
    
    

    private EntityMetadataResource CreateResource()
    {
        var context = Fixture.GetContext();
        return new EntityMetadataResource(context);
    }

    [Test]
    public void GetMetadata_EntityCount_IsAtLeast5()
    {
        var resource = CreateResource();
        var result = resource.GetEntityMetadata();
        var json = JsonDocument.Parse(result);

        json.RootElement.GetProperty("entityCount").GetInt32().Should().BeGreaterThanOrEqualTo(5);
    }

    [Test]
    public void GetMetadata_Entities_ContainsCoreTables()
    {
        var resource = CreateResource();
        var result = resource.GetEntityMetadata();
        var json = JsonDocument.Parse(result);

        var entities = json.RootElement.GetProperty("entities").EnumerateArray()
            .Select(e => e.GetProperty("entity").GetString()).ToList();
        entities.Should().Contain("Artist");
        entities.Should().Contain("Album");
        entities.Should().Contain("Track");
        entities.Should().Contain("Video");
        entities.Should().Contain("Scrobble");
    }

    [Test]
    public void GetMetadata_EachEntity_HasProperties()
    {
        var resource = CreateResource();
        var result = resource.GetEntityMetadata();
        var json = JsonDocument.Parse(result);

        var entities = json.RootElement.GetProperty("entities").EnumerateArray();
        foreach (var entity in entities)
        {
            entity.TryGetProperty("properties", out _).Should().BeTrue();
            entity.GetProperty("properties").GetArrayLength().Should().BeGreaterThan(0);
        }
    }

    [Test]
    public void GetMetadata_ArtistEntity_HasCorrectFields()
    {
        var resource = CreateResource();
        var result = resource.GetEntityMetadata();
        var json = JsonDocument.Parse(result);

        var artist = json.RootElement.GetProperty("entities").EnumerateArray()
            .First(e => e.GetProperty("entity").GetString() == "Artist");
        
        artist.GetProperty("table").GetString().Should().Be("artists");
        artist.GetProperty("schema").GetString().Should().NotBeNullOrEmpty();

        var props = artist.GetProperty("properties").EnumerateArray().ToList();
        props.Should().Contain(p => p.GetProperty("name").GetString() == "Id");
        props.Should().Contain(p => p.GetProperty("name").GetString() == "Name");
    }

    [Test]
    public void GetMetadata_ArtistEntity_HasNavigationProperties()
    {
        var resource = CreateResource();
        var result = resource.GetEntityMetadata();
        var json = JsonDocument.Parse(result);

        var artist = json.RootElement.GetProperty("entities").EnumerateArray()
            .First(e => e.GetProperty("entity").GetString() == "Artist");
        
        artist.TryGetProperty("navigationProperties", out _).Should().BeTrue();
        var navs = artist.GetProperty("navigationProperties").EnumerateArray()
            .Select(n => n.GetProperty("name").GetString()).ToList();
        navs.Should().Contain("Albums");
    }

    [Test]
    public void GetMetadata_AlbumEntity_HasRelationships()
    {
        var resource = CreateResource();
        var result = resource.GetEntityMetadata();
        var json = JsonDocument.Parse(result);

        var album = json.RootElement.GetProperty("entities").EnumerateArray()
            .First(e => e.GetProperty("entity").GetString() == "Album");
        
        album.TryGetProperty("relationships", out _).Should().BeTrue();
        var rels = album.GetProperty("relationships").EnumerateArray().ToList();
        rels.Should().NotBeEmpty();
    }

    [Test]
    public void GetMetadata_Relationships_HaveRequiredFields()
    {
        var resource = CreateResource();
        var result = resource.GetEntityMetadata();
        var json = JsonDocument.Parse(result);

        var album = json.RootElement.GetProperty("entities").EnumerateArray()
            .First(e => e.GetProperty("entity").GetString() == "Album");
        var rels = album.GetProperty("relationships").EnumerateArray();

        foreach (var rel in rels)
        {
            rel.TryGetProperty("principalEntity", out _).Should().BeTrue();
            rel.TryGetProperty("foreignKeyProperties", out _).Should().BeTrue();
            rel.TryGetProperty("deleteBehavior", out _).Should().BeTrue();
        }
    }

    [Test]
    public void GetMetadata_EntityAndTableCounts_Match()
    {
        var resource = CreateResource();
        var result = resource.GetEntityMetadata();
        var json = JsonDocument.Parse(result);

        var entityCount = json.RootElement.GetProperty("entityCount").GetInt32();
        var entitiesArray = json.RootElement.GetProperty("entities").EnumerateArray().Count();
        entitiesArray.Should().Be(entityCount);
    }

    [Test]
    public void GetMetadata_EachEntity_HasSchemaField()
    {
        var resource = CreateResource();
        var result = resource.GetEntityMetadata();
        var json = JsonDocument.Parse(result);

        var entities = json.RootElement.GetProperty("entities").EnumerateArray();
        foreach (var entity in entities)
        {
            entity.TryGetProperty("schema", out _).Should().BeTrue();
            entity.TryGetProperty("table", out _).Should().BeTrue();
            entity.TryGetProperty("entity", out _).Should().BeTrue();
        }
    }
}
