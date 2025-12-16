namespace CSharpScripts.Tests.Unit;

using CSharpScripts.Models;

[TestClass]
public class MusicMetadataSchemaTests
{
    [TestMethod]
    public void GetAllSchemas_ReturnsExpectedEntities()
    {
        // Act
        var schemas = MusicMetadataSchema.GetAllSchemas();

        // Assert
        schemas.ShouldContainKey("Artist");
        schemas.ShouldContainKey("Release");
        schemas.ShouldContainKey("Recording");
        schemas.ShouldContainKey("Track");
        schemas.ShouldContainKey("Label");
        schemas.ShouldContainKey("ReleaseGroup");
        schemas.ShouldContainKey("Master");
    }

    [TestMethod]
    public void GetArtistSchema_ContainsBothSources()
    {
        // Act
        var schema = MusicMetadataSchema.GetArtistSchema();

        // Assert
        schema.EntityName.ShouldBe("Artist");
        schema.Fields.ShouldContain(f => f.Source == "MusicBrainz");
        schema.Fields.ShouldContain(f => f.Source == "Discogs");
    }

    [TestMethod]
    public void GetReleaseSchema_ContainsKeyFields()
    {
        // Act
        var schema = MusicMetadataSchema.GetReleaseSchema();

        // Assert
        schema.Fields.ShouldContain(f => f.Name == "Title");
        schema.Fields.ShouldContain(f => f.Name == "Year");
        schema.Fields.ShouldContain(f => f.Name == "Barcode");
        schema.Fields.ShouldContain(f => f.Name == "Labels");
    }

    [TestMethod]
    public void GetAllFieldsSummary_ReturnsNonEmptyList()
    {
        // Act
        var fields = MusicMetadataSchema.GetAllFieldsSummary();

        // Assert
        fields.ShouldNotBeEmpty();
        fields.Count.ShouldBeGreaterThan(100); // Should have many fields
    }

    [TestMethod]
    public void MetadataField_RecordEquality_Works()
    {
        // Arrange
        var field1 = new MetadataField("Name", "string", "MusicBrainz", "Artist name");
        var field2 = new MetadataField("Name", "string", "MusicBrainz", "Artist name");
        var field3 = new MetadataField("Title", "string", "MusicBrainz", "Release title");

        // Assert
        field1.ShouldBe(field2);
        field1.ShouldNotBe(field3);
    }
}
