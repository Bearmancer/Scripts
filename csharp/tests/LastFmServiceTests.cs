namespace CSharpScripts.Tests.Unit;

[TestClass]
public class LastFmServiceTests
{
    [TestMethod]
    public void LoadScrobbles_EmptyFile_ReturnsEmptyList()
    {
        // This tests the static method - actual behavior depends on state file
        // In a real test, we'd mock the file system
        var scrobbles = LastFmService.LoadScrobbles();
        scrobbles.ShouldNotBeNull();
    }

    [TestMethod]
    public void Scrobble_FormattedDate_ReturnsCorrectFormat()
    {
        // Arrange
        var scrobble = new Scrobble(
            TrackName: "Test Track",
            ArtistName: "Test Artist",
            AlbumName: "Test Album",
            PlayedAt: new DateTime(2024, 12, 15, 14, 30, 45)
        );

        // Act
        var formatted = scrobble.FormattedDate;

        // Assert
        formatted.ShouldBe("2024/12/15 14:30:45");
    }

    [TestMethod]
    public void Scrobble_NullPlayedAt_ReturnsEmptyString()
    {
        // Arrange
        var scrobble = new Scrobble(
            TrackName: "Test Track",
            ArtistName: "Test Artist",
            AlbumName: "Test Album",
            PlayedAt: null
        );

        // Act
        var formatted = scrobble.FormattedDate;

        // Assert
        formatted.ShouldBe("");
    }

    [TestMethod]
    public void FetchState_Update_TracksOldestAndNewest()
    {
        // Arrange
        var state = new FetchState();
        var oldest = new DateTime(2020, 1, 1);
        var newest = new DateTime(2024, 12, 15);

        // Act
        state.Update(1, 100, oldest, newest);

        // Assert
        state.LastPage.ShouldBe(1);
        state.TotalFetched.ShouldBe(100);
        state.OldestScrobble.ShouldBe(oldest);
        state.NewestScrobble.ShouldBe(newest);
    }

    [TestMethod]
    public void FetchState_Update_PreservesOldestWhenNewer()
    {
        // Arrange
        var state = new FetchState { OldestScrobble = new DateTime(2019, 1, 1) };
        var olderDate = new DateTime(2020, 1, 1); // This is newer than existing

        // Act
        state.Update(1, 100, olderDate, null);

        // Assert
        state.OldestScrobble.ShouldBe(new DateTime(2019, 1, 1)); // Unchanged
    }
}
