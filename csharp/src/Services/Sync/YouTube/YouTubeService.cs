namespace CSharpScripts.Services.Sync.YouTube;

public class YouTubeService : IDisposable
{
    private const int MaxResultsPerPage = 50;

    private const string PLAYLIST_FIELDS =
        "nextPageToken,items(id,snippet/title,contentDetails/itemCount,etag)";

    private const string PLAYLIST_ITEM_FIELDS = "nextPageToken,items/contentDetails/videoId";

    private const string VIDEO_FIELDS =
        "items(id,snippet(title,description,channelTitle,channelId),contentDetails/duration)";

    private readonly YouTubeServiceApi service = new(Config.GoogleInitializer);

    public void Dispose()
    {
        service?.Dispose();
        GC.SuppressFinalize(this);
    }

    internal async Task<List<PlaylistSummary>> GetPlaylistSummariesAsync(CancellationToken ct)
    {
        var items = await FetchAllPlaylistItemsAsync(ct: ct);
        return
        [
            .. items
                .Select(item => new PlaylistSummary(
                    Id: item.Id,
                    item.Snippet?.Title ?? "Untitled",
                    (int)(item.ContentDetails?.ItemCount ?? 0),
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
            async () =>
            {
                var request = service.Playlists.List(part: "snippet,contentDetails");
                request.Id = playlistId;
                request.Fields = PLAYLIST_FIELDS;
                return await request.ExecuteAsync(cancellationToken: ct);
            },
            ct: ct
        );

        var item = response.Items?.FirstOrDefault();
        return item is null
            ? null
            : new PlaylistSummary(
                Id: item.Id,
                item.Snippet?.Title ?? "Untitled",
                (int)(item.ContentDetails?.ItemCount ?? 0),
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
                async () =>
                {
                    var request = service.PlaylistItems.List(part: "contentDetails");
                    request.PlaylistId = playlistId;
                    request.MaxResults = MaxResultsPerPage;
                    request.PageToken = pageToken;
                    request.Fields = PLAYLIST_ITEM_FIELDS;
                    return await request.ExecuteAsync(cancellationToken: ct);
                },
                ct: ct
            );

            ct.ThrowIfCancellationRequested();

            videoIds.AddRange(
                response
                    .Items?.Select(i => i.ContentDetails?.VideoId)
                    .Where(id => !IsNullOrEmpty(value: id))
                    .Cast<string>()
                    ?? []
            );

            pageToken = response.NextPageToken;
        } while (!IsNullOrEmpty(value: pageToken));

        return videoIds;
    }

    internal async Task<List<YouTubePlaylist>> GetPlaylistMetadataAsync(CancellationToken ct)
    {
        Console.Info(message: "Fetching playlist metadata...");
        var items = await FetchAllPlaylistItemsAsync(ct: ct);
        return
        [
            .. items
                .Select(item => new YouTubePlaylist(
                    Id: item.Id,
                    item.Snippet?.Title ?? "Untitled",
                    (int)(item.ContentDetails?.ItemCount ?? 0),
                    [],
                    ETag: item.ETag
                ))
                .OrderBy(p => p.Title),
        ];
    }

    private async Task<List<Playlist>> FetchAllPlaylistItemsAsync(CancellationToken ct)
    {
        List<Playlist> items = [];
        string? pageToken = null;

        do
        {
            ct.ThrowIfCancellationRequested();

            var response = await Resilience.ExecuteAsync(
                operation: "YouTube.Playlists.List",
                async () =>
                {
                    var request = service.Playlists.List(part: "snippet,contentDetails");
                    request.Mine = true;
                    request.MaxResults = MaxResultsPerPage;
                    request.PageToken = pageToken;
                    request.Fields = PLAYLIST_FIELDS;
                    return await request.ExecuteAsync(cancellationToken: ct);
                },
                ct: ct
            );

            ct.ThrowIfCancellationRequested();

            items.AddRange(response.Items ?? []);
            pageToken = response.NextPageToken;
        } while (!IsNullOrEmpty(value: pageToken));

        return items;
    }

    internal async Task<List<YouTubePlaylist>> GetAllPlaylistsAsync(CancellationToken ct)
    {
        Console.Info(message: "Fetching playlists...");

        var items = await FetchAllPlaylistItemsAsync(ct: ct);
        List<YouTubePlaylist> playlists =
        [
            .. items
                .Select(item => new YouTubePlaylist(
                    Id: item.Id,
                    item.Snippet?.Title ?? "Untitled",
                    (int)(item.ContentDetails?.ItemCount ?? 0),
                    [],
                    ETag: item.ETag
                ))
                .OrderBy(p => p.Title),
        ];

        Console.Info(message: "Found {0} playlists, fetching video IDs...", playlists.Count);

        for (var i = 0; i < playlists.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var playlist = playlists[index: i];
            var videoIds = await GetPlaylistVideoIdsAsync(playlistId: playlist.Id, ct: ct);
            playlists[index: i] = playlist with { VideoIds = videoIds };

            Console.Progress(message: "Playlist IDs: {0}/{1}", i + 1, playlists.Count);
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
        List<string[]> batches = [.. videoIds.Chunk(size: MaxResultsPerPage)];

        foreach (string[] batch in batches)
        {
            ct.ThrowIfCancellationRequested();

            var batchVideos = await GetVideoDetailsAsync([.. batch], ct: ct);

            ct.ThrowIfCancellationRequested();

            videos.AddRange(collection: batchVideos);
            await onBatchComplete(arg: batchVideos);
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
            async () =>
            {
                var request = service.Videos.List(part: "snippet,contentDetails");
                request.Id = Join(separator: ",", values: videoIds);
                request.Fields = VIDEO_FIELDS;
                return await request.ExecuteAsync(cancellationToken: ct);
            },
            ct: ct
        );

        return
        [
            .. (response.Items ?? []).Select(item => new YouTubeVideo(
                item.Snippet?.Title ?? "Untitled",
                item.Snippet?.Description ?? "",
                ParseDuration(isoDuration: item.ContentDetails?.Duration),
                item.Snippet?.ChannelTitle ?? "",
                VideoId: item.Id,
                item.Snippet?.ChannelId ?? ""
            )),
        ];
    }

    private static TimeSpan ParseDuration(string? isoDuration) =>
        IsNullOrEmpty(value: isoDuration) ? TimeSpan.Zero : XmlConvert.ToTimeSpan(s: isoDuration);
}
