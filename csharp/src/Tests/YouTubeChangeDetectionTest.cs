namespace CSharpScripts.Tests;

/// <summary>
/// Manual test runner for YouTube playlist change detection.
/// Tests: add video, remove video, reorder video, and ETag behavior.
/// Run via: dotnet run -- test-yt-changes
/// </summary>
public class YouTubeChangeDetectionTest
{
    private const string TestPlaylistId = "PL1zgNCoWt_7ZFpUZhv014SWeYUnRMypbw"; // "Bad" playlist
    private const string TestVideoIdToAdd = "dQw4w9WgXcQ"; // Rick Astley - Never Gonna Give You Up

    private readonly YouTubeServiceApi service;
    private readonly CancellationToken ct;

    public YouTubeChangeDetectionTest(CancellationToken ct)
    {
        this.ct = ct;

        Console.Info("Authenticating with YouTube (write scope)...");

        // Need write scope for this test - uses different user to avoid scope conflicts
        var credential = GoogleWebAuthorizationBroker
            .AuthorizeAsync(
                clientSecrets: new ClientSecrets
                {
                    ClientId = Config.GoogleClientId,
                    ClientSecret = Config.GoogleClientSecret,
                },
                scopes: [YouTubeServiceApi.Scope.Youtube], // Full access, not readonly
                user: "simplercs_user_write",
                taskCancellationToken: CancellationToken.None
            )
            .Result;

        service = new YouTubeServiceApi(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "CSharpScripts-Test",
            }
        );

        Console.Success("Authenticated!");
    }

    public void RunAllTests()
    {
        Console.Info("=== YouTube Change Detection Test ===");
        Console.Info("Test playlist: Bad (ID: {0})", TestPlaylistId);
        Console.Info(
            "NOTE: YouTube API has eventual consistency - metadata may take seconds to update"
        );
        Console.Info("");

        // Get initial state
        var initialState = GetPlaylistState();
        LogPlaylistState("INITIAL STATE", initialState);

        // Test 1: Add a video
        Console.Info("");
        Console.Info("=== TEST 1: ADD VIDEO ===");
        var addResult = TestAddVideo(initialState);

        // Wait for API propagation
        WaitForPropagation();

        // Test 2: Check ETag after add
        Console.Info("");
        Console.Info("=== TEST 2: CHECK ETAG AFTER ADD ===");
        var stateAfterAdd = GetPlaylistState();
        CompareStates("After ADD", initialState, stateAfterAdd);

        // Test 3: Remove the video we just added
        Console.Info("");
        Console.Info("=== TEST 3: REMOVE VIDEO ===");
        var removeResult = TestRemoveVideo(stateAfterAdd, addResult.PlaylistItemId);

        // Wait for API propagation
        WaitForPropagation();

        // Test 4: Check ETag after remove
        Console.Info("");
        Console.Info("=== TEST 4: CHECK ETAG AFTER REMOVE ===");
        var stateAfterRemove = GetPlaylistState();
        CompareStates("After REMOVE", stateAfterAdd, stateAfterRemove);

        // Verify we're back to initial state
        Console.Info("");
        Console.Info("=== VERIFY RESTORED STATE ===");
        CompareStates("Initial vs Final", initialState, stateAfterRemove);

        // Test 5: Reorder videos (swap first two)
        Console.Info("");
        Console.Info("=== TEST 5: REORDER VIDEOS ===");
        TestReorderVideos(stateAfterRemove);

        // Wait for API propagation
        WaitForPropagation();

        // Test 6: Check ETag after reorder
        Console.Info("");
        Console.Info("=== TEST 6: CHECK ETAG AFTER REORDER ===");
        var stateAfterReorder = GetPlaylistState();
        CompareStates("After REORDER", stateAfterRemove, stateAfterReorder);

        // Test 7: Restore original order
        Console.Info("");
        Console.Info("=== TEST 7: RESTORE ORIGINAL ORDER ===");
        TestRestoreOrder(stateAfterReorder);

        // Wait for API propagation
        WaitForPropagation();

        var finalState = GetPlaylistState();
        CompareStates("Final restored", stateAfterReorder, finalState);

        Console.Info("");
        Console.Success("=== ALL TESTS COMPLETE ===");
    }

    private PlaylistState GetPlaylistState()
    {
        var request = service.Playlists.List("snippet,contentDetails");
        request.Id = TestPlaylistId;
        var response = request.Execute();
        var playlist = response.Items?.FirstOrDefault();

        if (playlist == null)
            throw new Exception($"Playlist {TestPlaylistId} not found");

        // Get video IDs in order
        List<PlaylistItemInfo> items = [];
        string? pageToken = null;

        do
        {
            var itemsRequest = service.PlaylistItems.List("snippet,contentDetails");
            itemsRequest.PlaylistId = TestPlaylistId;
            itemsRequest.MaxResults = 50;
            itemsRequest.PageToken = pageToken;
            var itemsResponse = itemsRequest.Execute();

            foreach (var item in itemsResponse.Items ?? [])
            {
                items.Add(
                    new PlaylistItemInfo(
                        PlaylistItemId: item.Id,
                        VideoId: item.ContentDetails?.VideoId ?? "",
                        Position: (int)(item.Snippet?.Position ?? 0)
                    )
                );
            }

            pageToken = itemsResponse.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return new PlaylistState(
            ETag: playlist.ETag,
            VideoCount: (int)(playlist.ContentDetails?.ItemCount ?? 0),
            Items: items.OrderBy(i => i.Position).ToList()
        );
    }

    private void LogPlaylistState(string label, PlaylistState state)
    {
        Console.Info("{0}:", label);
        Console.Info("  ETag: {0}", state.ETag ?? "NULL");
        Console.Info("  Video Count: {0}", state.VideoCount);
        Console.Info(
            "  First 3 videos: {0}",
            string.Join(", ", state.Items.Take(3).Select(i => i.VideoId))
        );
    }

    private void CompareStates(string label, PlaylistState before, PlaylistState after)
    {
        Console.Info("{0} comparison:", label);

        var etagChanged = before.ETag != after.ETag;
        var countChanged = before.VideoCount != after.VideoCount;
        var orderChanged = !before
            .Items.Select(i => i.VideoId)
            .SequenceEqual(after.Items.Select(i => i.VideoId));

        Console.Info(
            "  ETag: {0} → {1} [{2}]",
            before.ETag?[..12] ?? "NULL",
            after.ETag?[..12] ?? "NULL",
            etagChanged ? "CHANGED" : "SAME"
        );

        Console.Info(
            "  Count: {0} → {1} [{2}]",
            before.VideoCount,
            after.VideoCount,
            countChanged ? "CHANGED" : "SAME"
        );

        Console.Info("  Order: [{0}]", orderChanged ? "CHANGED" : "SAME");

        if (etagChanged)
            Console.Success("  ✓ ETag change DETECTED");
        else if (countChanged)
            Console.Warning("  ⚠ ETag unchanged but count changed - fallback to count detection");
        else if (orderChanged)
            Console.Error(
                "  ✗ ETag unchanged, count unchanged, but ORDER changed - NOT DETECTABLE via metadata"
            );
        else
            Console.Info("  No changes detected (correct)");
    }

    private static void WaitForPropagation()
    {
        Console.Info("Waiting 5s for API propagation...");
        Thread.Sleep(5000);
    }

    private AddVideoResult TestAddVideo(PlaylistState currentState)
    {
        Console.Info("Adding video {0} to playlist...", TestVideoIdToAdd);

        var insertRequest = service.PlaylistItems.Insert(
            new Google.Apis.YouTube.v3.Data.PlaylistItem
            {
                Snippet = new Google.Apis.YouTube.v3.Data.PlaylistItemSnippet
                {
                    PlaylistId = TestPlaylistId,
                    ResourceId = new Google.Apis.YouTube.v3.Data.ResourceId
                    {
                        Kind = "youtube#video",
                        VideoId = TestVideoIdToAdd,
                    },
                },
            },
            "snippet"
        );

        var response = insertRequest.Execute();
        Console.Success("Video added! PlaylistItemId: {0}", response.Id);

        return new AddVideoResult(response.Id);
    }

    private RemoveVideoResult TestRemoveVideo(PlaylistState currentState, string playlistItemId)
    {
        Console.Info("Removing video (PlaylistItemId: {0})...", playlistItemId);

        var deleteRequest = service.PlaylistItems.Delete(playlistItemId);
        deleteRequest.Execute();

        Console.Success("Video removed!");
        return new RemoveVideoResult(true);
    }

    private void TestReorderVideos(PlaylistState currentState)
    {
        if (currentState.Items.Count < 2)
        {
            Console.Warning("Not enough videos to test reorder");
            return;
        }

        var firstItem = currentState.Items[0];
        var secondItem = currentState.Items[1];

        Console.Info(
            "Swapping positions: {0} (pos 0) ↔ {1} (pos 1)",
            firstItem.VideoId,
            secondItem.VideoId
        );

        // Move first item to position 1
        var updateRequest = service.PlaylistItems.Update(
            new Google.Apis.YouTube.v3.Data.PlaylistItem
            {
                Id = firstItem.PlaylistItemId,
                Snippet = new Google.Apis.YouTube.v3.Data.PlaylistItemSnippet
                {
                    PlaylistId = TestPlaylistId,
                    ResourceId = new Google.Apis.YouTube.v3.Data.ResourceId
                    {
                        Kind = "youtube#video",
                        VideoId = firstItem.VideoId,
                    },
                    Position = 1,
                },
            },
            "snippet"
        );

        updateRequest.Execute();
        Console.Success("Videos reordered!");
    }

    private void TestRestoreOrder(PlaylistState currentState)
    {
        if (currentState.Items.Count < 2)
        {
            Console.Warning("Not enough videos to restore order");
            return;
        }

        // The second item (which was originally first) should be moved back to position 0
        var secondItem = currentState.Items[1];

        Console.Info("Restoring original order: moving {0} back to position 0", secondItem.VideoId);

        var updateRequest = service.PlaylistItems.Update(
            new Google.Apis.YouTube.v3.Data.PlaylistItem
            {
                Id = secondItem.PlaylistItemId,
                Snippet = new Google.Apis.YouTube.v3.Data.PlaylistItemSnippet
                {
                    PlaylistId = TestPlaylistId,
                    ResourceId = new Google.Apis.YouTube.v3.Data.ResourceId
                    {
                        Kind = "youtube#video",
                        VideoId = secondItem.VideoId,
                    },
                    Position = 0,
                },
            },
            "snippet"
        );

        updateRequest.Execute();
        Console.Success("Original order restored!");
    }

    private record PlaylistState(string? ETag, int VideoCount, List<PlaylistItemInfo> Items);

    private record PlaylistItemInfo(string PlaylistItemId, string VideoId, int Position);

    private record AddVideoResult(string PlaylistItemId);

    private record RemoveVideoResult(bool Success);
}
