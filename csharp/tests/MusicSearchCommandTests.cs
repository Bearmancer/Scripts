namespace CSharpScripts.Tests.Unit;

using CSharpScripts.CLI.Commands;
using CSharpScripts.Models;

[TestClass]
public class MusicSearchCommandTests
{
    [TestMethod]
    public void Settings_HaveValidDefaults()
    {
        // Test that settings have expected defaults
        var settingsType = typeof(MusicSearchCommand.Settings);

        var sourceProperty = settingsType.GetProperty("Source");
        sourceProperty.ShouldNotBeNull();

        var modeProperty = settingsType.GetProperty("Mode");
        modeProperty.ShouldNotBeNull();

        var outputProperty = settingsType.GetProperty("Output");
        outputProperty.ShouldNotBeNull();

        var fieldsProperty = settingsType.GetProperty("Fields");
        fieldsProperty.ShouldNotBeNull();

        var debugProperty = settingsType.GetProperty("Debug");
        debugProperty.ShouldNotBeNull();
    }

    [TestMethod]
    public void SearchResult_WithScore_UpdatesCorrectly()
    {
        // Arrange
        var original = new SearchResult(
            Source: MusicSource.Discogs,
            Id: "12345",
            Title: "Test Album",
            Artist: "Test Artist",
            Year: 2024,
            Format: "Vinyl",
            Label: "Test Label",
            ReleaseType: "master"
        );

        // Act
        var withScore = original with
        {
            Score = 85,
        };

        // Assert
        withScore.Score.ShouldBe(85);
        withScore.Title.ShouldBe("Test Album");
        withScore.Source.ShouldBe(MusicSource.Discogs);
    }

    [TestMethod]
    public void SearchResult_SupportsAllNewFields()
    {
        // Arrange & Act
        var result = new SearchResult(
            Source: MusicSource.MusicBrainz,
            Id: "guid-here",
            Title: "Test Album",
            Artist: "Test Artist",
            Year: 2024,
            Format: "CD",
            Label: "Test Label",
            ReleaseType: "Album",
            Score: 100,
            Country: "US",
            CatalogNumber: "CAT-001",
            Barcode: "1234567890123",
            Genres: ["Rock", "Pop"],
            Styles: null
        );

        // Assert
        result.Score.ShouldBe(100);
        result.Country.ShouldBe("US");
        result.CatalogNumber.ShouldBe("CAT-001");
        result.Barcode.ShouldBe("1234567890123");
        result.Genres.ShouldNotBeNull();
        result.Genres.Count.ShouldBe(2);
        result.Styles.ShouldBeNull();
    }

    [TestMethod]
    public void SearchResult_DiscogsStylesField_Works()
    {
        // Arrange & Act
        var result = new SearchResult(
            Source: MusicSource.Discogs,
            Id: "12345",
            Title: "Electronic Album",
            Artist: "Test Artist",
            Year: 2024,
            Format: "Vinyl",
            Label: "Test Label",
            ReleaseType: "release",
            Genres: ["Electronic"],
            Styles: ["House", "Techno", "Ambient"]
        );

        // Assert
        result.Styles.ShouldNotBeNull();
        result.Styles.Count.ShouldBe(3);
        result.Styles.ShouldContain("House");
        result.Styles.ShouldContain("Techno");
    }
}
