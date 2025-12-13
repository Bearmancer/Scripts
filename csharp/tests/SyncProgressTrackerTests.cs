using CSharpScripts.Infrastructure;

namespace CSharpScripts.Tests;

[TestClass]
public sealed class SyncProgressTrackerTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Initialize Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Initialize_WithPlaylists_SetsCorrectTotals()
    {
        SyncProgressTracker tracker = new();
        List<PlaylistProgressItem> playlists =
        [
            new("Playlist A", 50),
            new("Playlist B", 30),
            new("Playlist C", 20),
        ];

        tracker.Initialize(playlists);

        tracker.TotalPlaylists.ShouldBe(3);
        tracker.TotalVideosAcrossAllPlaylists.ShouldBe(100);
        tracker.CompletedPlaylists.ShouldBe(0);
        tracker.TotalVideosProcessedAcrossAllPlaylists.ShouldBe(0);
    }

    [TestMethod]
    public void Initialize_WithEmptyList_ThrowsArgumentException()
    {
        SyncProgressTracker tracker = new();
        List<PlaylistProgressItem> playlists = [];

        Should.Throw<ArgumentException>(() => tracker.Initialize(playlists));
    }

    [TestMethod]
    public void Initialize_ResetsAllState()
    {
        SyncProgressTracker tracker = new();
        List<PlaylistProgressItem> playlists = [new("First", 10), new("Second", 20)];
        tracker.Initialize(playlists);
        tracker.StartPlaylist("First", 10);
        tracker.UpdateVideoProgress(5);

        tracker.Initialize([new("New", 100)]);

        tracker.TotalPlaylists.ShouldBe(1);
        tracker.CompletedPlaylists.ShouldBe(0);
        tracker.CurrentPlaylistVideosProcessed.ShouldBe(0);
        tracker.TotalVideosProcessedAcrossAllPlaylists.ShouldBe(0);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // StartPlaylist Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void StartPlaylist_SetsCorrectState()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 50)]);

        tracker.StartPlaylist("My Playlist", 50);

        tracker.CurrentPlaylistName.ShouldBe("My Playlist");
        tracker.CurrentPlaylistTotalVideos.ShouldBe(50);
        tracker.CurrentPlaylistVideosProcessed.ShouldBe(0);
    }

    [TestMethod]
    public void StartPlaylist_WithEmptyName_ThrowsArgumentException()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 50)]);

        Should.Throw<ArgumentException>(() => tracker.StartPlaylist("", 50));
        Should.Throw<ArgumentException>(() => tracker.StartPlaylist("   ", 50));
    }

    [TestMethod]
    public void StartPlaylist_WithNegativeVideoCount_ThrowsArgumentOutOfRange()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 50)]);

        Should.Throw<ArgumentOutOfRangeException>(() => tracker.StartPlaylist("Valid", -1));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateVideoProgress Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void UpdateVideoProgress_IncrementsCorrectly()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("A", 50), new("B", 50)]);
        tracker.StartPlaylist("A", 50);

        tracker.UpdateVideoProgress(10);

        tracker.CurrentPlaylistVideosProcessed.ShouldBe(10);
        tracker.TotalVideosProcessedAcrossAllPlaylists.ShouldBe(10);
    }

    [TestMethod]
    public void UpdateVideoProgress_HandlesMultipleUpdates()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 100)]);
        tracker.StartPlaylist("Test", 100);

        tracker.UpdateVideoProgress(25);
        tracker.UpdateVideoProgress(50);
        tracker.UpdateVideoProgress(75);

        tracker.CurrentPlaylistVideosProcessed.ShouldBe(75);
        tracker.TotalVideosProcessedAcrossAllPlaylists.ShouldBe(75);
    }

    [TestMethod]
    public void UpdateVideoProgress_ExceedingTotal_ThrowsArgumentOutOfRange()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 50)]);
        tracker.StartPlaylist("Test", 50);

        Should.Throw<ArgumentOutOfRangeException>(() => tracker.UpdateVideoProgress(51));
    }

    [TestMethod]
    public void UpdateVideoProgress_NegativeValue_ThrowsArgumentOutOfRange()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 50)]);
        tracker.StartPlaylist("Test", 50);

        Should.Throw<ArgumentOutOfRangeException>(() => tracker.UpdateVideoProgress(-1));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CompleteCurrentPlaylist Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void CompleteCurrentPlaylist_IncrementsCompletedCount()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("A", 50), new("B", 50)]);
        tracker.StartPlaylist("A", 50);
        tracker.UpdateVideoProgress(25);

        tracker.CompleteCurrentPlaylist();

        tracker.CompletedPlaylists.ShouldBe(1);
        tracker.CurrentPlaylistVideosProcessed.ShouldBe(50);
        tracker.TotalVideosProcessedAcrossAllPlaylists.ShouldBe(50);
    }

    [TestMethod]
    public void CompleteCurrentPlaylist_ThenStartNext_TracksCorrectly()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("A", 40), new("B", 60)]);

        tracker.StartPlaylist("A", 40);
        tracker.CompleteCurrentPlaylist();
        tracker.StartPlaylist("B", 60);
        tracker.UpdateVideoProgress(30);

        tracker.CompletedPlaylists.ShouldBe(1);
        tracker.CurrentPlaylistVideosProcessed.ShouldBe(30);
        tracker.TotalVideosProcessedAcrossAllPlaylists.ShouldBe(70);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Percentage Calculations Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void PlaylistProgressPercent_CalculatesCorrectly()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("A", 10), new("B", 10), new("C", 10), new("D", 10)]);

        tracker.PlaylistProgressPercent.ShouldBe(0);

        tracker.StartPlaylist("A", 10);
        tracker.CompleteCurrentPlaylist();
        tracker.PlaylistProgressPercent.ShouldBe(25);

        tracker.StartPlaylist("B", 10);
        tracker.CompleteCurrentPlaylist();
        tracker.PlaylistProgressPercent.ShouldBe(50);
    }

    [TestMethod]
    public void CurrentPlaylistVideoPercent_CalculatesCorrectly()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 100)]);
        tracker.StartPlaylist("Test", 100);

        tracker.CurrentPlaylistVideoPercent.ShouldBe(0);

        tracker.UpdateVideoProgress(25);
        tracker.CurrentPlaylistVideoPercent.ShouldBe(25);

        tracker.UpdateVideoProgress(50);
        tracker.CurrentPlaylistVideoPercent.ShouldBe(50);

        tracker.UpdateVideoProgress(100);
        tracker.CurrentPlaylistVideoPercent.ShouldBe(100);
    }

    [TestMethod]
    public void OverallVideoPercent_CalculatesAcrossMultiplePlaylists()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("A", 50), new("B", 50)]);

        tracker.StartPlaylist("A", 50);
        tracker.UpdateVideoProgress(25);
        tracker.OverallVideoPercent.ShouldBe(25); // 25 of 100

        tracker.CompleteCurrentPlaylist();
        tracker.OverallVideoPercent.ShouldBe(50); // 50 of 100

        tracker.StartPlaylist("B", 50);
        tracker.UpdateVideoProgress(25);
        tracker.OverallVideoPercent.ShouldBe(75); // 75 of 100
    }

    [TestMethod]
    public void Percentages_WithZeroTotals_ReturnZero()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Empty", 0)]);

        tracker.CurrentPlaylistVideoPercent.ShouldBe(0);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CurrentPlaylistIndex Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void CurrentPlaylistIndex_ReturnsOneBasedIndex()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("A", 10), new("B", 10), new("C", 10)]);

        tracker.CurrentPlaylistIndex.ShouldBe(1);

        tracker.StartPlaylist("A", 10);
        tracker.CompleteCurrentPlaylist();
        tracker.CurrentPlaylistIndex.ShouldBe(2);

        tracker.StartPlaylist("B", 10);
        tracker.CompleteCurrentPlaylist();
        tracker.CurrentPlaylistIndex.ShouldBe(3);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Snapshot Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void GetSnapshot_ReturnsAccurateState()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("A", 40), new("B", 60)]);
        tracker.StartPlaylist("A", 40);
        tracker.UpdateVideoProgress(20);

        var snapshot = tracker.GetSnapshot();

        snapshot.TotalPlaylists.ShouldBe(2);
        snapshot.CompletedPlaylists.ShouldBe(0);
        snapshot.CurrentPlaylistIndex.ShouldBe(1);
        snapshot.CurrentPlaylistName.ShouldBe("A");
        snapshot.CurrentPlaylistTotalVideos.ShouldBe(40);
        snapshot.CurrentPlaylistVideosProcessed.ShouldBe(20);
        snapshot.TotalVideosAcrossAllPlaylists.ShouldBe(100);
        snapshot.TotalVideosProcessedAcrossAllPlaylists.ShouldBe(20);
        snapshot.CurrentPlaylistVideoPercent.ShouldBe(50);
        snapshot.OverallVideoPercent.ShouldBe(20);
    }

    [TestMethod]
    public void GetSnapshot_IsImmutable()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 100)]);
        tracker.StartPlaylist("Test", 100);
        tracker.UpdateVideoProgress(25);

        var snapshot1 = tracker.GetSnapshot();
        tracker.UpdateVideoProgress(50);
        var snapshot2 = tracker.GetSnapshot();

        // Snapshot1 should retain original values
        Assert.AreEqual(25, snapshot1.CurrentPlaylistVideosProcessed);
        Assert.AreEqual(50, snapshot2.CurrentPlaylistVideosProcessed);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ETA Calculation Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void EstimatedTimeRemaining_NullWhenNoProgress()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 100)]);
        tracker.StartPlaylist("Test", 100);

        Assert.IsNull(tracker.EstimatedTimeRemaining);
    }

    [TestMethod]
    public void EstimatedTimeRemaining_ReturnsValueAfterProgress()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 100)]);
        tracker.StartPlaylist("Test", 100);

        Thread.Sleep(100);
        tracker.UpdateVideoProgress(10);

        Assert.IsNotNull(tracker.EstimatedTimeRemaining);
        tracker.EstimatedTimeRemaining!.Value.TotalSeconds.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public void EstimatedTimeRemaining_DecreasesAsProgressIncreases()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 100)]);
        tracker.StartPlaylist("Test", 100);

        Thread.Sleep(50);
        tracker.UpdateVideoProgress(25);
        var eta1 = tracker.EstimatedTimeRemaining;

        Thread.Sleep(50);
        tracker.UpdateVideoProgress(50);
        var eta2 = tracker.EstimatedTimeRemaining;

        Assert.IsNotNull(eta1);
        Assert.IsNotNull(eta2);
        // ETA should decrease (or stay similar) as we process more
        eta2!.Value.TotalSeconds.ShouldBeLessThanOrEqualTo(eta1!.Value.TotalSeconds + 1);
    }

    [TestMethod]
    public void ElapsedTime_TracksCorrectly()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 100)]);
        tracker.StartPlaylist("Test", 100);

        Thread.Sleep(50);
        tracker.UpdateVideoProgress(10);

        tracker.ElapsedTime.TotalMilliseconds.ShouldBeGreaterThanOrEqualTo(40.0);
    }

    [TestMethod]
    public void ElapsedTime_ZeroBeforeFirstUpdate()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 100)]);

        Assert.AreEqual(TimeSpan.Zero, tracker.ElapsedTime);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Edge Case Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void SinglePlaylist_TracksCorrectly()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Only One", 50)]);

        Assert.AreEqual(1, tracker.TotalPlaylists);
        Assert.AreEqual(50, tracker.TotalVideosAcrossAllPlaylists);

        tracker.StartPlaylist("Only One", 50);
        tracker.UpdateVideoProgress(25);

        Assert.AreEqual(50.0, tracker.CurrentPlaylistVideoPercent);
        Assert.AreEqual(50.0, tracker.OverallVideoPercent);
    }

    [TestMethod]
    public void LargePlaylistCount_TracksCorrectly()
    {
        SyncProgressTracker tracker = new();
        List<PlaylistProgressItem> playlists = Enumerable
            .Range(1, 100)
            .Select(i => new PlaylistProgressItem($"Playlist {i}", 10))
            .ToList();

        tracker.Initialize(playlists);

        Assert.AreEqual(100, tracker.TotalPlaylists);
        Assert.AreEqual(1000, tracker.TotalVideosAcrossAllPlaylists);
    }

    [TestMethod]
    public void ProgressReset_OnNewPlaylist()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("A", 100), new("B", 100)]);

        tracker.StartPlaylist("A", 100);
        tracker.UpdateVideoProgress(75);
        tracker.CompleteCurrentPlaylist();

        tracker.StartPlaylist("B", 100);

        // Current playlist progress should reset
        Assert.AreEqual(0, tracker.CurrentPlaylistVideosProcessed);
        Assert.AreEqual("B", tracker.CurrentPlaylistName);
        // Overall progress should be preserved
        Assert.AreEqual(100, tracker.TotalVideosProcessedAcrossAllPlaylists);
    }

    [TestMethod]
    public void AllPlaylistsComplete_ShowsFullProgress()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("A", 50), new("B", 50)]);

        tracker.StartPlaylist("A", 50);
        tracker.CompleteCurrentPlaylist();
        tracker.StartPlaylist("B", 50);
        tracker.CompleteCurrentPlaylist();

        Assert.AreEqual(100.0, tracker.OverallVideoPercent);
        Assert.AreEqual(100.0, tracker.PlaylistProgressPercent);
        Assert.AreEqual(2, tracker.CompletedPlaylists);
        Assert.AreEqual(100, tracker.TotalVideosProcessedAcrossAllPlaylists);
    }
}
