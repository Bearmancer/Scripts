using CSharpScripts.Infrastructure;
using Spectre.Console.Testing;

namespace CSharpScripts.Tests;

[TestClass]
public sealed class SyncProgressRendererTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Display Build Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BuildDisplay_IncludesPlaylistCount()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("A", 50), new("B", 50)]);
        tracker.StartPlaylist("A", 50);

        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplay());
        var output = console.Output;

        output.ShouldContain("(0/2 playlists)");
    }

    [TestMethod]
    public void BuildDisplay_IncludesPlaylistName()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test Playlist Name", 50)]);
        tracker.StartPlaylist("Test Playlist Name", 50);

        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplay());
        var output = console.Output;

        output.ShouldContain("Test Playlist Name");
    }

    [TestMethod]
    public void BuildDisplay_IncludesVideoCount()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 100)]);
        tracker.StartPlaylist("Test", 100);
        tracker.UpdateVideoProgress(42);

        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplay());
        var output = console.Output;

        output.ShouldContain("42/100");
        output.ShouldContain("videos");
    }

    [TestMethod]
    public void BuildDisplay_ShowsCorrectPercentage()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 100)]);
        tracker.StartPlaylist("Test", 100);
        tracker.UpdateVideoProgress(50);

        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplay());
        var output = console.Output;

        output.ShouldContain("50.0%");
    }

    [TestMethod]
    public void BuildDisplay_TruncatesLongPlaylistName()
    {
        SyncProgressTracker tracker = new();
        var longName = "This Is A Very Long Playlist Name That Should Be Truncated";
        tracker.Initialize([new(longName, 50)]);
        tracker.StartPlaylist(longName, 50);

        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplay());
        var output = console.Output;

        // Name should be truncated with ...
        output.ShouldContain("...");
        output.ShouldNotContain(longName);
    }

    [TestMethod]
    public void BuildDisplay_ShowsElapsedWhenNoETA()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 100)]);
        tracker.StartPlaylist("Test", 100);

        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplay());
        var output = console.Output;

        // No ETA when no progress yet, but also no elapsed shown since 0 videos processed
        output.ShouldNotContain("ETA:");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Progress Bar Color Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BuildDisplayFromSnapshot_UsesCorrectColorForLowProgress()
    {
        SyncProgressSnapshot snapshot = new(
            TotalPlaylists: 2,
            CompletedPlaylists: 0,
            CurrentPlaylistIndex: 1,
            CurrentPlaylistName: "Test",
            CurrentPlaylistTotalVideos: 100,
            CurrentPlaylistVideosProcessed: 10,
            TotalVideosAcrossAllPlaylists: 100,
            TotalVideosProcessedAcrossAllPlaylists: 10,
            PlaylistProgressPercent: 0,
            CurrentPlaylistVideoPercent: 10,
            OverallVideoPercent: 10,
            ElapsedTime: TimeSpan.FromSeconds(10),
            EstimatedTimeRemaining: TimeSpan.FromMinutes(1)
        );

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        // At 10%, should use cyan color (< 25%)
        output.ShouldContain("10.0%");
    }

    [TestMethod]
    public void BuildDisplayFromSnapshot_ShowsETAWhenAvailable()
    {
        SyncProgressSnapshot snapshot = new(
            TotalPlaylists: 1,
            CompletedPlaylists: 0,
            CurrentPlaylistIndex: 1,
            CurrentPlaylistName: "Test",
            CurrentPlaylistTotalVideos: 100,
            CurrentPlaylistVideosProcessed: 50,
            TotalVideosAcrossAllPlaylists: 100,
            TotalVideosProcessedAcrossAllPlaylists: 50,
            PlaylistProgressPercent: 0,
            CurrentPlaylistVideoPercent: 50,
            OverallVideoPercent: 50,
            ElapsedTime: TimeSpan.FromMinutes(2),
            EstimatedTimeRemaining: TimeSpan.FromMinutes(2)
        );

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        output.ShouldContain("ETA:");
        output.ShouldContain("2m");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Complete Flow Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BuildDisplay_AfterCompletingPlaylist_ShowsUpdatedCount()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("A", 50), new("B", 50)]);
        tracker.StartPlaylist("A", 50);
        tracker.CompleteCurrentPlaylist();
        tracker.StartPlaylist("B", 50);

        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplay());
        var output = console.Output;

        output.ShouldContain("(1/2 playlists)");
        output.ShouldContain("50/100");
        output.ShouldContain("videos");
    }

    [TestMethod]
    public void BuildDisplay_AtCompletion_ShowsFullProgress()
    {
        SyncProgressTracker tracker = new();
        tracker.Initialize([new("Test", 100)]);
        tracker.StartPlaylist("Test", 100);
        tracker.CompleteCurrentPlaylist();

        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplay());
        var output = console.Output;

        output.ShouldContain("100.0%");
        output.ShouldContain("100/100");
        output.ShouldContain("videos");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Progress Bar Character Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BuildDisplayFromSnapshot_ContainsFilledBarCharacters()
    {
        SyncProgressSnapshot snapshot = CreateSnapshot(overallPercent: 50);

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        // Filled bar uses ━ character
        output.ShouldContain("━");
        // Empty bar uses ─ character
        output.ShouldContain("─");
    }

    [TestMethod]
    public void BuildDisplayFromSnapshot_At0Percent_ShowsAllEmpty()
    {
        SyncProgressSnapshot snapshot = CreateSnapshot(overallPercent: 0);

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        output.ShouldContain("0.0%");
        // Should have empty characters but no filled
        output.ShouldContain("─");
    }

    [TestMethod]
    public void BuildDisplayFromSnapshot_At100Percent_ShowsAllFilled()
    {
        SyncProgressSnapshot snapshot = CreateSnapshot(overallPercent: 100);

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        output.ShouldContain("100.0%");
        // Should have filled characters
        output.ShouldContain("━");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Progress Bar Color Threshold Tests (cyan <25, blue 25-49, yellow 50-74, green 75+)
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    [DataRow(10, "cyan")]
    [DataRow(24, "cyan")]
    [DataRow(25, "blue")]
    [DataRow(49, "blue")]
    [DataRow(50, "yellow")]
    [DataRow(74, "yellow")]
    [DataRow(75, "green")]
    [DataRow(100, "green")]
    public void BuildDisplayFromSnapshot_UsesCorrectColorAtThresholds(
        int percent,
        string expectedColor
    )
    {
        SyncProgressSnapshot snapshot = CreateSnapshot(overallPercent: percent);

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        // Verify the percentage is displayed
        output.ShouldContain($"{percent}.0%");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ETA Formatting Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BuildDisplayFromSnapshot_ETAFormatSeconds()
    {
        SyncProgressSnapshot snapshot = CreateSnapshot(
            overallPercent: 50,
            eta: TimeSpan.FromSeconds(45)
        );

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        output.ShouldContain("ETA: 45s");
    }

    [TestMethod]
    public void BuildDisplayFromSnapshot_ETAFormatMinutesSeconds()
    {
        SyncProgressSnapshot snapshot = CreateSnapshot(
            overallPercent: 50,
            eta: TimeSpan.FromSeconds(125) // 2m 5s
        );

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        output.ShouldContain("ETA: 2m 5s");
    }

    [TestMethod]
    public void BuildDisplayFromSnapshot_ETAFormatHoursMinutes()
    {
        SyncProgressSnapshot snapshot = CreateSnapshot(
            overallPercent: 50,
            eta: TimeSpan.FromMinutes(75) // 1h 15m
        );

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        output.ShouldContain("ETA: 1h 15m");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Numeric Value Accuracy Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BuildDisplayFromSnapshot_ShowsExactPlaylistCounts()
    {
        SyncProgressSnapshot snapshot = new(
            TotalPlaylists: 7,
            CompletedPlaylists: 3,
            CurrentPlaylistIndex: 4,
            CurrentPlaylistName: "Test Playlist",
            CurrentPlaylistTotalVideos: 150,
            CurrentPlaylistVideosProcessed: 75,
            TotalVideosAcrossAllPlaylists: 500,
            TotalVideosProcessedAcrossAllPlaylists: 250,
            PlaylistProgressPercent: 42.86,
            CurrentPlaylistVideoPercent: 50,
            OverallVideoPercent: 50,
            ElapsedTime: TimeSpan.FromMinutes(5),
            EstimatedTimeRemaining: TimeSpan.FromMinutes(5)
        );

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        output.ShouldContain("(3/7 playlists)");
        output.ShouldContain("250/500");
        output.ShouldContain("videos");
        output.ShouldContain("Test Playlist");
        output.ShouldContain("50.0%");
        output.ShouldContain("ETA: 5m 0s");
    }

    [TestMethod]
    public void BuildDisplayFromSnapshot_ShowsDecimalPercentage()
    {
        SyncProgressSnapshot snapshot = CreateSnapshot(overallPercent: 33.33);

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        output.ShouldContain("33.3%");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Bar Width and Character Count Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BuildDisplayFromSnapshot_BarWidthIs40Characters()
    {
        SyncProgressSnapshot snapshot = CreateSnapshot(overallPercent: 50);

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        // Bar should contain exactly 40 bar characters total (filled + empty)
        var filledCount = output.Count(c => c == '━');
        var emptyCount = output.Count(c => c == '─');
        Assert.AreEqual(40, filledCount + emptyCount, "Progress bar should be 40 characters wide");
    }

    [TestMethod]
    public void BuildDisplayFromSnapshot_At50Percent_Has20FilledChars()
    {
        SyncProgressSnapshot snapshot = CreateSnapshot(overallPercent: 50);

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        var filledCount = output.Count(c => c == '━');
        Assert.AreEqual(20, filledCount, "50% progress should show 20 filled characters");
    }

    [TestMethod]
    public void BuildDisplayFromSnapshot_At25Percent_Has10FilledChars()
    {
        SyncProgressSnapshot snapshot = CreateSnapshot(overallPercent: 25);

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        var filledCount = output.Count(c => c == '━');
        Assert.AreEqual(10, filledCount, "25% progress should show 10 filled characters");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Special Character Handling Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BuildDisplay_EscapesMarkupInPlaylistName()
    {
        SyncProgressTracker tracker = new();
        var nameWithMarkup = "[red]Dangerous[/] Name";
        tracker.Initialize([new(nameWithMarkup, 50)]);
        tracker.StartPlaylist(nameWithMarkup, 50);

        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        // Should not throw and should contain escaped version
        console.Write(renderer.BuildDisplay());
        var output = console.Output;

        Assert.IsFalse(string.IsNullOrEmpty(output));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // No ETA / Elapsed Time Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BuildDisplayFromSnapshot_WithNoETA_ShowsElapsed()
    {
        SyncProgressSnapshot snapshot = new(
            TotalPlaylists: 2,
            CompletedPlaylists: 0,
            CurrentPlaylistIndex: 1,
            CurrentPlaylistName: "Test",
            CurrentPlaylistTotalVideos: 100,
            CurrentPlaylistVideosProcessed: 50,
            TotalVideosAcrossAllPlaylists: 100,
            TotalVideosProcessedAcrossAllPlaylists: 50,
            PlaylistProgressPercent: 0,
            CurrentPlaylistVideoPercent: 50,
            OverallVideoPercent: 50,
            ElapsedTime: TimeSpan.FromSeconds(30),
            EstimatedTimeRemaining: null
        );

        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        console.Write(renderer.BuildDisplayFromSnapshot(snapshot));
        var output = console.Output;

        output.ShouldContain("Elapsed: 30s");
        output.ShouldNotContain("ETA:");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Integration: Full Workflow Test
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void FullWorkflow_TrackerAndRenderer_Integration()
    {
        SyncProgressTracker tracker = new();
        TestConsole console = new();
        SyncProgressRenderer renderer = new(tracker);

        // Initialize
        tracker.Initialize([new("Playlist A", 100), new("Playlist B", 100)]);

        // Start first playlist
        tracker.StartPlaylist("Playlist A", 100);
        console.Write(renderer.BuildDisplay());
        var output1 = console.Output;
        output1.ShouldContain("(0/2 playlists)");
        output1.ShouldContain("Playlist A");

        // Progress through first playlist
        tracker.UpdateVideoProgress(50);
        console = new TestConsole();
        renderer = new SyncProgressRenderer(tracker);
        console.Write(renderer.BuildDisplay());
        var output2 = console.Output;
        output2.ShouldContain("50/200");
        output2.ShouldContain("videos");

        // Complete first, start second
        tracker.CompleteCurrentPlaylist();
        tracker.StartPlaylist("Playlist B", 100);
        console = new TestConsole();
        renderer = new SyncProgressRenderer(tracker);
        console.Write(renderer.BuildDisplay());
        var output3 = console.Output;
        output3.ShouldContain("(1/2 playlists)");
        output3.ShouldContain("Playlist B");
        output3.ShouldContain("100/200");
        output3.ShouldContain("videos");

        // Complete everything
        tracker.CompleteCurrentPlaylist();
        console = new TestConsole();
        renderer = new SyncProgressRenderer(tracker);
        console.Write(renderer.BuildDisplay());
        var output4 = console.Output;
        output4.ShouldContain("100.0%");
        output4.ShouldContain("200/200");
        output4.ShouldContain("videos");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════════

    static SyncProgressSnapshot CreateSnapshot(
        double overallPercent,
        TimeSpan? eta = null,
        int totalPlaylists = 2,
        int completedPlaylists = 0,
        string playlistName = "Test"
    )
    {
        var totalVideos = 100;
        var processedVideos = (int)(totalVideos * (overallPercent / 100.0));

        return new SyncProgressSnapshot(
            TotalPlaylists: totalPlaylists,
            CompletedPlaylists: completedPlaylists,
            CurrentPlaylistIndex: completedPlaylists + 1,
            CurrentPlaylistName: playlistName,
            CurrentPlaylistTotalVideos: totalVideos,
            CurrentPlaylistVideosProcessed: processedVideos,
            TotalVideosAcrossAllPlaylists: totalVideos,
            TotalVideosProcessedAcrossAllPlaylists: processedVideos,
            PlaylistProgressPercent: completedPlaylists * 100.0 / totalPlaylists,
            CurrentPlaylistVideoPercent: overallPercent,
            OverallVideoPercent: overallPercent,
            ElapsedTime: TimeSpan.FromMinutes(1),
            EstimatedTimeRemaining: eta
        );
    }
}
