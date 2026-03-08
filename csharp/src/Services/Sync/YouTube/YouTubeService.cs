using System.Xml;

namespace CSharpScripts.Services.Sync.YouTube;

internal sealed class YouTubeService : IDisposable
{
	private const int MaxResultsPerPage = 50;

	private const string PlaylistFields =
		"nextPageToken,items(id,snippet/title,contentDetails/itemCount,etag)";

	private const string PlaylistItemFields = "nextPageToken,items/contentDetails/videoId";

	private const string VideoFields =
		"items(id,snippet(title,description,channelTitle,channelId),contentDetails/duration)";

	private readonly YouTubeServiceApi Service;

	private YouTubeService(YouTubeServiceApi service) => Service = service;

	public static async Task<YouTubeService> CreateAsync(CancellationToken ct = default)
	{
		BaseClientService.Initializer initializer = await GoogleAuth.GetInitializerAsync(ct);
		return new YouTubeService(new YouTubeServiceApi(initializer));
	}

	public void Dispose()
	{
		Service.Dispose();
		GC.SuppressFinalize(this);
	}

	internal async Task<List<PlaylistSummary>> GetPlaylistSummariesAsync(CancellationToken ct)
	{
		Log.Debug("GetPlaylistSummariesAsync entry");
		List<Playlist> items = await FetchAllPlaylistItemsAsync(ct);
		List<PlaylistSummary> result =
		[
			.. items
				.Select(item => new PlaylistSummary(
					item.Id,
					item.Snippet?.Title ?? "Untitled",
					(int)(item.ContentDetails?.ItemCount ?? 0),
					item.ETag
				))
				.OrderBy(s => s.Title),
		];
		Log.Debug("GetPlaylistSummariesAsync exit {Count}", result.Count);
		return result;
	}

	internal async Task<PlaylistSummary?> GetPlaylistSummaryAsync(
		string playlistId,
		CancellationToken ct
	)
	{
		Log.Debug("GetPlaylistSummaryAsync entry {PlaylistId}", playlistId);
		ct.ThrowIfCancellationRequested();

		PlaylistListResponse response = await Resilience.ExecuteAsync(
			"YouTube.Playlists.List",
			async () =>
			{
				PlaylistsResource.ListRequest request = Service.Playlists.List(
					"snippet,contentDetails"
				);
				request.Id = playlistId;
				request.Fields = PlaylistFields;
				return await request.ExecuteAsync(ct);
			},
			ct
		);

		Playlist? item = response.Items?.FirstOrDefault();
		PlaylistSummary? result = item is null
			? null
			: new PlaylistSummary(
				item.Id,
				item.Snippet?.Title ?? "Untitled",
				(int)(item.ContentDetails?.ItemCount ?? 0),
				item.ETag
			);
		Log.Debug("GetPlaylistSummaryAsync exit {Found}", result is not null);
		return result;
	}

	internal async Task<List<string>> GetPlaylistVideoIdsAsync(
		string playlistId,
		CancellationToken ct
	)
	{
		Log.Debug("GetPlaylistVideoIdsAsync entry {PlaylistId}", playlistId);
		List<string> videoIds = [];
		string? pageToken = null;
		var pageCount = 0;

		do
		{
			ct.ThrowIfCancellationRequested();

			PlaylistItemListResponse response = await Resilience.ExecuteAsync(
				"YouTube.PlaylistItems.List",
				async () =>
				{
					PlaylistItemsResource.ListRequest request = Service.PlaylistItems.List(
						"contentDetails"
					);
					request.PlaylistId = playlistId;
					request.MaxResults = MaxResultsPerPage;
					request.PageToken = pageToken;
					request.Fields = PlaylistItemFields;
					return await request.ExecuteAsync(ct);
				},
				ct
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

		Log.Debug(
			"GetPlaylistVideoIdsAsync exit {Count} videoIds across {Pages} pages",
			videoIds.Count,
			pageCount
		);
		return videoIds;
	}

	internal async Task<List<YouTubePlaylist>> GetPlaylistMetadataAsync(CancellationToken ct)
	{
		Log.Debug("GetPlaylistMetadataAsync entry");
		Log.Information("Fetching playlist metadata...");
		List<Playlist> items = await FetchAllPlaylistItemsAsync(ct);
		List<YouTubePlaylist> result =
		[
			.. items
				.Select(item => new YouTubePlaylist(
					item.Id,
					item.Snippet?.Title ?? "Untitled",
					(int)(item.ContentDetails?.ItemCount ?? 0),
					[],
					item.ETag
				))
				.OrderBy(p => p.Title),
		];
		Log.Debug("GetPlaylistMetadataAsync exit {Count}", result.Count);
		return result;
	}

	private async Task<List<Playlist>> FetchAllPlaylistItemsAsync(CancellationToken ct)
	{
		Log.Debug("FetchAllPlaylistItemsAsync entry");
		List<Playlist> items = [];
		string? pageToken = null;
		var pageCount = 0;

		do
		{
			ct.ThrowIfCancellationRequested();
			pageCount++;

			PlaylistListResponse response = await Resilience.ExecuteAsync(
				"YouTube.Playlists.List",
				async () =>
				{
					PlaylistsResource.ListRequest request = Service.Playlists.List(
						"snippet,contentDetails"
					);
					request.Mine = true;
					request.MaxResults = MaxResultsPerPage;
					request.PageToken = pageToken;
					request.Fields = PlaylistFields;
					return await request.ExecuteAsync(ct);
				},
				ct
			);

			ct.ThrowIfCancellationRequested();

			items.AddRange(response.Items ?? []);
			pageToken = response.NextPageToken;
		} while (!IsNullOrEmpty(pageToken));

		Log.Debug(
			"FetchAllPlaylistItemsAsync exit {Count} items across {Pages} pages",
			items.Count,
			pageCount
		);
		return items;
	}

	internal async Task<List<YouTubePlaylist>> GetAllPlaylistsAsync(CancellationToken ct)
	{
		Log.Debug("GetAllPlaylistsAsync entry");
		Log.Information("Fetching playlists...");

		List<Playlist> items = await FetchAllPlaylistItemsAsync(ct);
		List<YouTubePlaylist> playlists =
		[
			.. items
				.Select(item => new YouTubePlaylist(
					item.Id,
					item.Snippet?.Title ?? "Untitled",
					(int)(item.ContentDetails?.ItemCount ?? 0),
					[],
					item.ETag
				))
				.OrderBy(p => p.Title),
		];

		Log.Information("Found {0} playlists, fetching video IDs...", playlists.Count);

		for (var i = 0; i < playlists.Count; i++)
		{
			ct.ThrowIfCancellationRequested();

			YouTubePlaylist playlist = playlists[i];
			List<string> videoIds = await GetPlaylistVideoIdsAsync(playlist.Id, ct);
			playlists[i] = playlist with { VideoIds = videoIds };

			Log.Debug("YouTubePlaylistIdFetch {Current} {Total}", i + 1, playlists.Count);
		}

		Log.Debug("GetAllPlaylistsAsync exit {Count}", playlists.Count);
		return playlists;
	}

	internal async Task<List<YouTubeVideo>> GetVideoDetailsForIdsAsync(
		List<string> videoIds,
		Func<List<YouTubeVideo>, Task> onBatchComplete,
		CancellationToken ct
	)
	{
		Log.Debug("GetVideoDetailsForIdsAsync entry {Count} videoIds", videoIds.Count);
		List<YouTubeVideo> videos = [];
		List<string[]> batches = [.. videoIds.Chunk(MaxResultsPerPage)];

		foreach (var batch in batches)
		{
			ct.ThrowIfCancellationRequested();

			List<YouTubeVideo> batchVideos = await GetVideoDetailsAsync([.. batch], ct);

			ct.ThrowIfCancellationRequested();

			videos.AddRange(batchVideos);
			await onBatchComplete(batchVideos);
		}

		Log.Debug(
			"GetVideoDetailsForIdsAsync exit {Count} videos in {Batches} batches",
			videos.Count,
			batches.Count
		);
		return videos;
	}

	private async Task<List<YouTubeVideo>> GetVideoDetailsAsync(
		List<string> videoIds,
		CancellationToken ct
	)
	{
		Log.Debug("GetVideoDetailsAsync entry {Count}", videoIds.Count);
		VideoListResponse response = await Resilience.ExecuteAsync(
			"YouTube.Videos.List",
			async () =>
			{
				VideosResource.ListRequest request = Service.Videos.List("snippet,contentDetails");
				request.Id = Join(",", videoIds);
				request.Fields = VideoFields;
				return await request.ExecuteAsync(ct);
			},
			ct
		);

		List<YouTubeVideo> result =
		[
			.. (response.Items ?? []).Select(item => new YouTubeVideo(
				item.Snippet?.Title ?? "Untitled",
				item.Snippet?.Description ?? "",
				ParseDuration(item.ContentDetails?.Duration),
				item.Snippet?.ChannelTitle ?? "",
				item.Id,
				item.Snippet?.ChannelId ?? ""
			)),
		];
		Log.Debug("GetVideoDetailsAsync exit {Count}", result.Count);
		return result;
	}

	private static TimeSpan ParseDuration(string? isoDuration) =>
		IsNullOrEmpty(isoDuration) ? TimeSpan.Zero : XmlConvert.ToTimeSpan(isoDuration);
}
