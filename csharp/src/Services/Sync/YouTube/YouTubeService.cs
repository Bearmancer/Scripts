namespace CSharpScripts.Services.Sync.YouTube;

public class YouTubeService(string clientId, string clientSecret) : IDisposable
{
    private const int MaxResultsPerPage = 50;

    private const string PLAYLIST_FIELDS =
        "nextPageToken,items(id,snippet/title,contentDetails/itemCount,etag)";
    private const string PLAYLIST_ITEM_FIELDS = "nextPageToken,items/contentDetails/videoId";
    private const string VIDEO_FIELDS =
        "items(id,snippet(title,description,channelTitle,channelId),contentDetails/duration)";

    private readonly YouTubeServiceApi service = new(
        new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredentialService.GetCredential(clientId, clientSecret),
            ApplicationName = "CSharpScripts",
        }
    );

    internal async Task<List<PlaylistSummary>> GetPlaylistSummariesAsync(CancellationToken ct)
    {
        List<global::Google.Apis.YouTube.v3.Data.Playlist> items = await FetchAllPlaylistItemsAsync(
            ct
        );
        return
        [
            .. items
                .Select(item => new PlaylistSummary(
                    Id: item.Id,
                    Title: item.Snippet?.Title ?? "Untitled",
                    VideoCount: (int)(item.ContentDetails?.ItemCount ?? 0),
                    ETag: item.ETag
                ))
                .OrderBy(s => s.Title),
        ];
    }

    internal async Task<PlaylistSummary?> GetPlaylistSummaryAsync(
        string playlistId,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        var response = await Resilience.ExecuteAsync(
            operation: "YouTube.Playlists.List",
            action: async () =>
            {
                var request = service.Playlists.List("snippet,contentDetails");
                request.Id = playlistId;
                request.Fields = PLAYLIST_FIELDS;
                return await request.ExecuteAsync(ct);
            },
            ct: ct
        );

        var item = response.Items?.FirstOrDefault();
        return item is null
            ? null
            : new PlaylistSummary(
                Id: item.Id,
                Title: item.Snippet?.Title ?? "Untitled",
                VideoCount: (int)(item.ContentDetails?.ItemCount ?? 0),
                ETag: item.ETag
            );
    }

    internal async Task<List<string>> GetPlaylistVideoIdsAsync(
        string playlistId,
        CancellationToken ct
    )
    {
        List<string> videoIds = [];
        string? pageToken = null;

        do
        {
            ct.ThrowIfCancellationRequested();

            var response = await Resilience.ExecuteAsync(
                operation: "YouTube.PlaylistItems.List",
                action: async () =>
                {
                    var request = service.PlaylistItems.List("contentDetails");
                    request.PlaylistId = playlistId;
                    request.MaxResults = MaxResultsPerPage;
                    request.PageToken = pageToken;
                    request.Fields = PLAYLIST_ITEM_FIELDS;
                    return await request.ExecuteAsync(ct);
                },
                ct: ct
            );

            ct.ThrowIfCancellationRequested();

            videoIds.AddRange(
                response
                    .Items?.Select(i => i.ContentDetails?.VideoId)
                    .Where(id => !IsNullOrEmpty(id))
                    .Cast<string>()
                    ?? []
            );

            pageToken = response.NextPageToken;
        } while (!IsNullOrEmpty(pageToken));

        return videoIds;
    }

    internal async Task<List<YouTubePlaylist>> GetPlaylistMetadataAsync(CancellationToken ct)
    {
        Console.Info("Fetching playlist metadata...");
        List<global::Google.Apis.YouTube.v3.Data.Playlist> items = await FetchAllPlaylistItemsAsync(
            ct
        );
        return
        [
            .. items
                .Select(item => new YouTubePlaylist(
                    Id: item.Id,
                    Title: item.Snippet?.Title ?? "Untitled",
                    VideoCount: (int)(item.ContentDetails?.ItemCount ?? 0),
                    VideoIds: [],
                    ETag: item.ETag
                ))
                .OrderBy(p => p.Title),
        ];
    }

    private async Task<
        List<global::Google.Apis.YouTube.v3.Data.Playlist>
    > FetchAllPlaylistItemsAsync(CancellationToken ct)
    {
        List<global::Google.Apis.YouTube.v3.Data.Playlist> items = [];
        string? pageToken = null;

        do
        {
            ct.ThrowIfCancellationRequested();

            var response = await Resilience.ExecuteAsync(
                operation: "YouTube.Playlists.List",
                action: async () =>
                {
                    var request = service.Playlists.List("snippet,contentDetails");
                    request.Mine = true;
                    request.MaxResults = MaxResultsPerPage;
                    request.PageToken = pageToken;
                    request.Fields = PLAYLIST_FIELDS;
                    return await request.ExecuteAsync(ct);
                },
                ct: ct
            );

            ct.ThrowIfCancellationRequested();

            items.AddRange(response.Items ?? []);
            pageToken = response.NextPageToken;
        } while (!IsNullOrEmpty(pageToken));

        return items;
    }

    internal async Task<List<YouTubePlaylist>> GetAllPlaylistsAsync(CancellationToken ct)
    {
        Console.Info("Fetching playlists...");

        List<global::Google.Apis.YouTube.v3.Data.Playlist> items = await FetchAllPlaylistItemsAsync(
            ct
        );
        List<YouTubePlaylist> playlists = items
            .Select(item => new YouTubePlaylist(
                Id: item.Id,
                Title: item.Snippet?.Title ?? "Untitled",
                VideoCount: (int)(item.ContentDetails?.ItemCount ?? 0),
                VideoIds: [],
                ETag: item.ETag
            ))
            .OrderBy(p => p.Title)
            .ToList();

        Console.Info("Found {0} playlists, fetching video IDs...", playlists.Count);

        for (var i = 0; i < playlists.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var playlist = playlists[i];
            var videoIds = await GetPlaylistVideoIdsAsync(playlist.Id, ct);
            playlists[i] = playlist with { VideoIds = videoIds };

            Console.Progress("Playlist IDs: {0}/{1}", i + 1, playlists.Count);
        }

        Console.NewLine();
        return playlists;
    }

    internal async Task<List<YouTubeVideo>> GetVideoDetailsForIdsAsync(
        List<string> videoIds,
        Func<List<YouTubeVideo>, Task> onBatchComplete,
        CancellationToken ct
    )
    {
        List<YouTubeVideo> videos = [];
        List<string[]> batches = [.. videoIds.Chunk(MaxResultsPerPage)];

        foreach (var batch in batches)
        {
            ct.ThrowIfCancellationRequested();

            var batchVideos = await GetVideoDetailsAsync([.. batch], ct);

            ct.ThrowIfCancellationRequested();

            videos.AddRange(batchVideos);
            await onBatchComplete(batchVideos);
        }

        return videos;
    }

    private async Task<List<YouTubeVideo>> GetVideoDetailsAsync(
        List<string> videoIds,
        CancellationToken ct
    )
    {
        var response = await Resilience.ExecuteAsync(
            operation: "YouTube.Videos.List",
            action: async () =>
            {
                var request = service.Videos.List("snippet,contentDetails");
                request.Id = Join(",", videoIds);
                request.Fields = VIDEO_FIELDS;
                return await request.ExecuteAsync(ct);
            },
            ct: ct
        );

        return
        [
            .. (response.Items ?? []).Select(item => new YouTubeVideo(
                Title: item.Snippet?.Title ?? "Untitled",
                Description: item.Snippet?.Description ?? "",
                Duration: ParseDuration(item.ContentDetails?.Duration),
                ChannelName: item.Snippet?.ChannelTitle ?? "",
                VideoId: item.Id,
                ChannelId: item.Snippet?.ChannelId ?? ""
            )),
        ];
    }

    private static TimeSpan ParseDuration(string? isoDuration) =>
        IsNullOrEmpty(isoDuration) ? TimeSpan.Zero : XmlConvert.ToTimeSpan(isoDuration);

    public void Dispose()
    {
        service?.Dispose();
        GC.SuppressFinalize(this);
    }
}
