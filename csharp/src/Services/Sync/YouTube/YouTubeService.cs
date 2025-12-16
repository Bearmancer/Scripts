namespace CSharpScripts.Services.Sync.YouTube;

public class YouTubeService(string clientId, string clientSecret)
{
    private const int MaxResultsPerPage = 50;

    // Field filters to reduce API response size
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

    internal List<PlaylistSummary> GetPlaylistSummaries(CancellationToken ct) =>
        [
            .. FetchAllPlaylistItems(ct)
                .Select(item => new PlaylistSummary(
                    Id: item.Id,
                    Title: item.Snippet?.Title ?? "Untitled",
                    VideoCount: (int)(item.ContentDetails?.ItemCount ?? 0),
                    ETag: item.ETag
                ))
                .OrderBy(s => s.Title),
        ];

    internal PlaylistSummary? GetPlaylistSummary(string playlistId, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return null;

        var response = Resilience.Execute(
            operation: "YouTube.Playlists.List",
            action: () =>
            {
                var request = service.Playlists.List("snippet,contentDetails");
                request.Id = playlistId;
                request.Fields = PLAYLIST_FIELDS;
                return request.Execute();
            },
            ct: ct
        );

        var item = response.Items?.FirstOrDefault();
        if (item == null)
            return null;

        return new PlaylistSummary(
            Id: item.Id,
            Title: item.Snippet?.Title ?? "Untitled",
            VideoCount: (int)(item.ContentDetails?.ItemCount ?? 0),
            ETag: item.ETag
        );
    }

    internal List<string> GetPlaylistVideoIds(string playlistId, CancellationToken ct)
    {
        List<string> videoIds = [];
        string? pageToken = null;

        do
        {
            if (ct.IsCancellationRequested)
                break;

            var response = Resilience.Execute(
                operation: "YouTube.PlaylistItems.List",
                action: () =>
                {
                    var request = service.PlaylistItems.List("contentDetails");
                    request.PlaylistId = playlistId;
                    request.MaxResults = MaxResultsPerPage;
                    request.PageToken = pageToken;
                    request.Fields = PLAYLIST_ITEM_FIELDS;
                    return request.Execute();
                },
                ct: ct
            );

            if (ct.IsCancellationRequested)
                break;

            List<string> ids =
            [
                .. response
                    .Items?.Select(i => i.ContentDetails?.VideoId)
                    .Where(id => !IsNullOrEmpty(id))
                    .Cast<string>()
                    ?? [],
            ];

            videoIds.AddRange(ids);
            pageToken = response.NextPageToken;
        } while (!IsNullOrEmpty(pageToken) && !ct.IsCancellationRequested);

        return videoIds;
    }

    internal List<YouTubePlaylist> GetPlaylistMetadata(CancellationToken ct)
    {
        Console.Info("Fetching playlist metadata...");
        return
        [
            .. FetchAllPlaylistItems(ct)
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

    /// <summary>
    /// Fetches all playlist items with pagination - shared by GetPlaylistSummaries, GetPlaylistMetadata, GetAllPlaylists.
    /// </summary>
    private List<global::Google.Apis.YouTube.v3.Data.Playlist> FetchAllPlaylistItems(
        CancellationToken ct
    )
    {
        List<global::Google.Apis.YouTube.v3.Data.Playlist> items = [];
        string? pageToken = null;

        do
        {
            if (ct.IsCancellationRequested)
                break;

            var response = Resilience.Execute(
                operation: "YouTube.Playlists.List",
                action: () =>
                {
                    var request = service.Playlists.List("snippet,contentDetails");
                    request.Mine = true;
                    request.MaxResults = MaxResultsPerPage;
                    request.PageToken = pageToken;
                    request.Fields = PLAYLIST_FIELDS;
                    return request.Execute();
                },
                ct: ct
            );

            if (ct.IsCancellationRequested)
                break;

            items.AddRange(response.Items ?? []);
            pageToken = response.NextPageToken;
        } while (!IsNullOrEmpty(pageToken) && !ct.IsCancellationRequested);

        return items;
    }

    internal List<YouTubePlaylist> GetAllPlaylists(CancellationToken ct)
    {
        Console.Info("Fetching playlists...");

        var playlists = FetchAllPlaylistItems(ct)
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
            if (ct.IsCancellationRequested)
                break;

            var playlist = playlists[i];
            var videoIds = GetPlaylistVideoIds(playlist.Id, ct);
            playlists[i] = playlist with { VideoIds = videoIds };

            Console.Progress("Playlist IDs: {0}/{1}", i + 1, playlists.Count);
        }

        Console.NewLine();
        return playlists;
    }

    internal List<YouTubeVideo> GetVideoDetailsForIds(
        List<string> videoIds,
        Action<List<YouTubeVideo>> onBatchComplete,
        CancellationToken ct
    )
    {
        List<YouTubeVideo> videos = [];
        List<string[]> batches = [.. videoIds.Chunk(MaxResultsPerPage)];

        foreach (var batch in batches)
        {
            if (ct.IsCancellationRequested)
                break;

            var batchVideos = GetVideoDetails([.. batch], ct);

            if (ct.IsCancellationRequested)
                break;

            videos.AddRange(batchVideos);
            onBatchComplete(batchVideos);
        }

        return videos;
    }

    private List<YouTubeVideo> GetVideoDetails(List<string> videoIds, CancellationToken ct)
    {
        var response = Resilience.Execute(
            operation: "YouTube.Videos.List",
            action: () =>
            {
                var request = service.Videos.List("snippet,contentDetails");
                request.Id = Join(",", videoIds);
                request.Fields = VIDEO_FIELDS;
                return request.Execute();
            },
            ct: ct
        );

        List<YouTubeVideo> videos = [];

        foreach (var item in response.Items ?? [])
        {
            var duration = ParseDuration(item.ContentDetails?.Duration);

            YouTubeVideo video = new(
                Title: item.Snippet?.Title ?? "Untitled",
                Description: item.Snippet?.Description ?? "",
                Duration: duration,
                ChannelName: item.Snippet?.ChannelTitle ?? "",
                VideoId: item.Id,
                ChannelId: item.Snippet?.ChannelId ?? ""
            );
            videos.Add(video);
        }

        return videos;
    }

    private static TimeSpan ParseDuration(string? isoDuration) =>
        IsNullOrEmpty(isoDuration) ? TimeSpan.Zero : XmlConvert.ToTimeSpan(isoDuration);
}
