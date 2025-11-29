namespace CSharpScripts.Services;

internal class YouTubeService(string clientId, string clientSecret)
{
    const int MaxResultsPerPage = 50;

    readonly Google.Apis.YouTube.v3.YouTubeService service = new(
        new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredentialService.GetCredential(clientId, clientSecret),
            ApplicationName = "CSharpScripts",
        }
    );

    internal List<PlaylistSummary> GetPlaylistSummaries(CancellationToken ct)
    {
        List<PlaylistSummary> summaries = [];
        string? pageToken = null;

        do
        {
            if (ct.IsCancellationRequested)
                break;

            var response = ApiConfig.ExecuteWithRetry(
                operationName: "YouTube.Playlists.List",
                action: () =>
                {
                    var request = service.Playlists.List("snippet,contentDetails");
                    request.Mine = true;
                    request.MaxResults = MaxResultsPerPage;
                    request.PageToken = pageToken;
                    return request.Execute();
                },
                postAction: () => ApiConfig.Delay(ServiceType.YouTube),
                ct: ct
            );

            if (ct.IsCancellationRequested)
                break;

            foreach (var item in response.Items ?? [])
            {
                PlaylistSummary summary = new(
                    Id: item.Id,
                    Title: item.Snippet?.Title ?? "Untitled",
                    VideoCount: (int)(item.ContentDetails?.ItemCount ?? 0),
                    ETag: item.ETag
                );
                summaries.Add(summary);
            }

            pageToken = response.NextPageToken;
        } while (!IsNullOrEmpty(pageToken) && !ct.IsCancellationRequested);

        return [.. summaries.OrderBy(s => s.Title)];
    }

    internal PlaylistSummary? GetPlaylistSummary(string playlistId, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return null;

        var response = ApiConfig.ExecuteWithRetry(
            operationName: "YouTube.Playlists.List",
            action: () =>
            {
                var request = service.Playlists.List("snippet,contentDetails");
                request.Id = playlistId;
                return request.Execute();
            },
            postAction: () => ApiConfig.Delay(ServiceType.YouTube),
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

            var response = ApiConfig.ExecuteWithRetry(
                operationName: "YouTube.PlaylistItems.List",
                action: () =>
                {
                    var request = service.PlaylistItems.List("contentDetails");
                    request.PlaylistId = playlistId;
                    request.MaxResults = MaxResultsPerPage;
                    request.PageToken = pageToken;
                    return request.Execute();
                },
                postAction: () => ApiConfig.Delay(ServiceType.YouTube),
                ct: ct
            );

            if (ct.IsCancellationRequested)
                break;

            var ids =
                response
                    .Items?.Select(i => i.ContentDetails?.VideoId)
                    .Where(id => !IsNullOrEmpty(id))
                    .Cast<string>()
                    .ToList() ?? [];

            videoIds.AddRange(ids);
            pageToken = response.NextPageToken;
        } while (!IsNullOrEmpty(pageToken) && !ct.IsCancellationRequested);

        return videoIds;
    }

    internal List<YouTubePlaylist> GetPlaylistMetadata(CancellationToken ct)
    {
        Logger.Info("Fetching playlist metadata...");
        List<YouTubePlaylist> playlists = [];
        string? pageToken = null;

        do
        {
            if (ct.IsCancellationRequested)
                break;

            var response = ApiConfig.ExecuteWithRetry(
                operationName: "YouTube.Playlists.List",
                action: () =>
                {
                    var request = service.Playlists.List("snippet,contentDetails");
                    request.Mine = true;
                    request.MaxResults = MaxResultsPerPage;
                    request.PageToken = pageToken;
                    return request.Execute();
                },
                postAction: () => ApiConfig.Delay(ServiceType.YouTube),
                ct: ct
            );

            if (ct.IsCancellationRequested)
                break;

            foreach (var item in response.Items ?? [])
            {
                YouTubePlaylist playlist = new(
                    Id: item.Id,
                    Title: item.Snippet?.Title ?? "Untitled",
                    VideoCount: (int)(item.ContentDetails?.ItemCount ?? 0),
                    VideoIds: [],
                    ETag: item.ETag
                );
                playlists.Add(playlist);
                Logger.Debug("Found: {0} ({1} videos)", playlist.Title, playlist.VideoCount);
            }

            pageToken = response.NextPageToken;
        } while (!IsNullOrEmpty(pageToken) && !ct.IsCancellationRequested);

        return [.. playlists.OrderBy(p => p.Title)];
    }

    internal List<YouTubePlaylist> GetAllPlaylists(CancellationToken ct)
    {
        Logger.Info("Fetching playlists...");
        List<YouTubePlaylist> playlists = [];
        string? pageToken = null;

        do
        {
            if (ct.IsCancellationRequested)
                break;

            var response = ApiConfig.ExecuteWithRetry(
                operationName: "YouTube.Playlists.List",
                action: () =>
                {
                    var request = service.Playlists.List("snippet,contentDetails");
                    request.Mine = true;
                    request.MaxResults = MaxResultsPerPage;
                    request.PageToken = pageToken;
                    return request.Execute();
                },
                postAction: () => ApiConfig.Delay(ServiceType.YouTube),
                ct: ct
            );

            if (ct.IsCancellationRequested)
                break;

            foreach (var item in response.Items ?? [])
            {
                YouTubePlaylist playlist = new(
                    Id: item.Id,
                    Title: item.Snippet?.Title ?? "Untitled",
                    VideoCount: (int)(item.ContentDetails?.ItemCount ?? 0),
                    VideoIds: [],
                    ETag: item.ETag
                );
                playlists.Add(playlist);
                Logger.Debug("Found: {0} ({1} videos)", playlist.Title, playlist.VideoCount);
            }

            pageToken = response.NextPageToken;
        } while (!IsNullOrEmpty(pageToken) && !ct.IsCancellationRequested);

        playlists = [.. playlists.OrderBy(p => p.Title)];
        Logger.Info("Found {0} playlists, fetching video IDs...", playlists.Count);

        for (var i = 0; i < playlists.Count; i++)
        {
            if (ct.IsCancellationRequested)
                break;

            var playlist = playlists[i];
            var videoIds = GetPlaylistVideoIds(playlistId: playlist.Id, ct: ct);
            playlists[i] = playlist with { VideoIds = videoIds };

            Logger.Progress("Playlist IDs: {0}/{1}", i + 1, playlists.Count);
        }

        Logger.NewLine();
        return playlists;
    }

    internal List<YouTubeVideo> GetVideoDetailsForIds(
        List<string> videoIds,
        Action<List<YouTubeVideo>> onBatchComplete,
        CancellationToken ct
    )
    {
        List<YouTubeVideo> videos = [];
        var batches = videoIds.Chunk(MaxResultsPerPage).ToList();

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

    List<YouTubeVideo> GetVideoDetails(List<string> videoIds, CancellationToken ct)
    {
        var response = ApiConfig.ExecuteWithRetry(
            operationName: "YouTube.Videos.List",
            action: () =>
            {
                var request = service.Videos.List("snippet,contentDetails");
                request.Id = Join(",", videoIds);
                return request.Execute();
            },
            postAction: () => ApiConfig.Delay(ServiceType.YouTube),
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
                ChannelName: item.Snippet?.ChannelTitle ?? "Unknown",
                VideoId: item.Id,
                ChannelId: item.Snippet?.ChannelId ?? ""
            );
            videos.Add(video);
        }

        return videos;
    }

    static TimeSpan ParseDuration(string? isoDuration) =>
        IsNullOrEmpty(isoDuration) ? TimeSpan.Zero : XmlConvert.ToTimeSpan(isoDuration);
}
