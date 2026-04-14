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

	public void Dispose()
	{
		Service.Dispose();
		GC.SuppressFinalize(this);
	}

	public static async Task<YouTubeService> CreateAsync(CancellationToken ct = default)
	{
		BaseClientService.Initializer initializer = await GoogleAuth.GetInitializerAsync(ct);
		return new YouTubeService(new YouTubeServiceApi(initializer: initializer));
	}

	internal async Task<List<PlaylistSummary>> GetPlaylistSummariesAsync(CancellationToken ct)
	{
		Log.Debug("GetPlaylistSummariesAsync entry");
		List<Playlist> items = await FetchAllPlaylistItemsAsync(ct);
		List<PlaylistSummary> result =
		[
			.. Enumerable.OrderBy(
				Enumerable.Select(
					items,
					item => new PlaylistSummary(
						Id: item.Id,
						item.Snippet?.Title ?? "Untitled",
						(int)(item.ContentDetails?.ItemCount ?? 0),
						ETag: item.ETag
					)
				),
				s => s.Title
			),
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
			operation: "YouTube.Playlists.List",
			async () =>
			{
				PlaylistsResource.ListRequest request = Service.Playlists.List(
					part: "snippet,contentDetails"
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
				Id: item.Id,
				item.Snippet?.Title ?? "Untitled",
				(int)(item.ContentDetails?.ItemCount ?? 0),
				ETag: item.ETag
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
			pageCount++;

			PlaylistItemListResponse response = await Resilience.ExecuteAsync(
				operation: "YouTube.PlaylistItems.List",
				async () =>
				{
					PlaylistItemsResource.ListRequest request = Service.PlaylistItems.List(
						part: "contentDetails"
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

			IList<PlaylistItem>? items = response.Items;
			if (items is not null)
			{
				foreach (PlaylistItem? item in items)
				{
					var videoId = item.ContentDetails?.VideoId;
					if (!IsNullOrEmpty(value: videoId))
						videoIds.Add(videoId);
				}
			}

			pageToken = response.NextPageToken;
		} while (!IsNullOrEmpty(value: pageToken));

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
			.. Enumerable.OrderBy(
				Enumerable.Select(
					items,
					item => new YouTubePlaylist(
						Id: item.Id,
						item.Snippet?.Title ?? "Untitled",
						(int)(item.ContentDetails?.ItemCount ?? 0),
						[],
						ETag: item.ETag
					)
				),
				p => p.Title
			),
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
				operation: "YouTube.Playlists.List",
				async () =>
				{
					PlaylistsResource.ListRequest request = Service.Playlists.List(
						part: "snippet,contentDetails"
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
		} while (!IsNullOrEmpty(value: pageToken));

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
			.. Enumerable.OrderBy(
				Enumerable.Select(
					items,
					item => new YouTubePlaylist(
						Id: item.Id,
						item.Snippet?.Title ?? "Untitled",
						(int)(item.ContentDetails?.ItemCount ?? 0),
						[],
						ETag: item.ETag
					)
				),
				p => p.Title
			),
		];

		Log.Information("Found {0} playlists, fetching video IDs...", playlists.Count);

		var playlistCount = playlists.Count;
		for (var i = 0; i < playlistCount; i++)
		{
			ct.ThrowIfCancellationRequested();

			YouTubePlaylist playlist = playlists[index: i];
			List<string> videoIds = await GetPlaylistVideoIdsAsync(playlistId: playlist.Id, ct);
			playlists[index: i] = playlist with { VideoIds = videoIds };

			Log.Debug("YouTubePlaylistIdFetch {Current} {Total}", i + 1, playlistCount);
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
		var videoIdCount = videoIds.Count;
		var batchCount = 0;

		for (var i = 0; i < videoIdCount; i += MaxResultsPerPage)
		{
			ct.ThrowIfCancellationRequested();

			List<string> batchIds = videoIds.GetRange(
				i,
				Math.Min(MaxResultsPerPage, videoIdCount - i)
			);
			List<YouTubeVideo> batchVideos = await GetVideoDetailsAsync(batchIds, ct);

			ct.ThrowIfCancellationRequested();

			videos.AddRange(collection: batchVideos);
			await onBatchComplete(arg: batchVideos);
			batchCount++;
		}

		Log.Debug(
			"GetVideoDetailsForIdsAsync exit {Count} videos in {Batches} batches",
			videos.Count,
			batchCount
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
			operation: "YouTube.Videos.List",
			async () =>
			{
				VideosResource.ListRequest request = Service.Videos.List(
					part: "snippet,contentDetails"
				);
				request.Id = Join(separator: ",", values: videoIds);
				request.Fields = VideoFields;
				return await request.ExecuteAsync(ct);
			},
			ct
		);

		IList<global::Google.Apis.YouTube.v3.Data.Video>? items = response.Items;
		List<YouTubeVideo> result = [];
		result.EnsureCapacity(items?.Count ?? 0);
		if (items is not null)
		{
			foreach (global::Google.Apis.YouTube.v3.Data.Video? item in items)
			{
				result.Add(
					new YouTubeVideo(
						item.Snippet?.Title ?? "Untitled",
						item.Snippet?.Description ?? "",
						ParseDuration(isoDuration: item.ContentDetails?.Duration),
						item.Snippet?.ChannelTitle ?? "",
						VideoId: item.Id,
						item.Snippet?.ChannelId ?? ""
					)
				);
			}
		}
		Log.Debug("GetVideoDetailsAsync exit {Count}", result.Count);
		return result;
	}

	private static TimeSpan ParseDuration(string? isoDuration) =>
		IsNullOrEmpty(value: isoDuration) ? TimeSpan.Zero : XmlConvert.ToTimeSpan(s: isoDuration);
}
