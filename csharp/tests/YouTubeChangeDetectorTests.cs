namespace CSharpScripts.Tests.Unit;

[TestClass]
public class YouTubeChangeDetectorTests
{
    [TestMethod]
    public void DetectVideoChanges_NoChanges_ReturnsEmptyLists()
    {
        // Arrange
        List<string> current = ["video1", "video2", "video3"];
        List<string> stored = ["video1", "video2", "video3"];

        // Act
        var changes = YouTubeChangeDetector.DetectVideoChanges(current, stored);

        // Assert
        changes.AddedVideoIds.ShouldBeEmpty();
        changes.RemovedVideoIds.ShouldBeEmpty();
        changes.RequiresFullRewrite.ShouldBeFalse();
        changes.HasChanges.ShouldBeFalse();
    }

    [TestMethod]
    public void DetectVideoChanges_AddedVideos_ReturnsAddedList()
    {
        // Arrange
        List<string> current = ["video1", "video2", "video3", "video4"];
        List<string> stored = ["video1", "video2", "video3"];

        // Act
        var changes = YouTubeChangeDetector.DetectVideoChanges(current, stored);

        // Assert
        changes.AddedVideoIds.ShouldBe(["video4"]);
        changes.RemovedVideoIds.ShouldBeEmpty();
        changes.HasChanges.ShouldBeTrue();
    }

    [TestMethod]
    public void DetectVideoChanges_RemovedVideos_ReturnsRemovedList()
    {
        // Arrange
        List<string> current = ["video1", "video3"];
        List<string> stored = ["video1", "video2", "video3"];

        // Act
        var changes = YouTubeChangeDetector.DetectVideoChanges(current, stored);

        // Assert
        changes.AddedVideoIds.ShouldBeEmpty();
        changes.RemovedVideoIds.ShouldBe(["video2"]);
        changes.RemovedRowIndices.ShouldContain(3); // Row index = position (1-indexed) + header row
        changes.HasChanges.ShouldBeTrue();
    }

    [TestMethod]
    public void DetectVideoChanges_Reordered_RequiresFullRewrite()
    {
        // Arrange
        List<string> current = ["video3", "video1", "video2"];
        List<string> stored = ["video1", "video2", "video3"];

        // Act
        var changes = YouTubeChangeDetector.DetectVideoChanges(current, stored);

        // Assert
        changes.AddedVideoIds.ShouldBeEmpty();
        changes.RemovedVideoIds.ShouldBeEmpty();
        changes.RequiresFullRewrite.ShouldBeTrue();
        changes.HasChanges.ShouldBeTrue();
    }

    [TestMethod]
    public void DetectVideoChanges_MixedChanges_ReturnsAll()
    {
        // Arrange
        List<string> current = ["video1", "video4", "video5"];
        List<string> stored = ["video1", "video2", "video3"];

        // Act
        var changes = YouTubeChangeDetector.DetectVideoChanges(current, stored);

        // Assert
        changes.AddedVideoIds.ShouldBe(["video4", "video5"]);
        changes.RemovedVideoIds.ShouldBe(["video2", "video3"]);
        changes.HasChanges.ShouldBeTrue();
    }
}
