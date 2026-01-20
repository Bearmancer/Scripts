namespace CSharpScripts.Services.Sync.YouTube;

public class YouTubeService : IDisposable
{
	#region Configuration

	private const int MaxResultsPerPage = 50;

	private const string PLAYLIST_FIELDS =
		"nextPageToken,items(id,snippet/title,contentDetails/itemCount,etag)";

	private const string PLAYLIST_ITEM_FIELDS = "nextPageToken,items/contentDetails/videoId";

	private const string VIDEO_FIELDS =
		"items(id,snippet(title,description,channelTitle,channelId),contentDetails/duration)";

	private readonly YouTubeServiceApi service = new(GoogleCredentials.Initializer);

	public void Dispose()
	{
		service.Dispose();
		GC.SuppressFinalize(this);
	}

	#endregion

	#region Playlist Summaries

	internal async Task<List<PlaylistSummary>> GetPlaylistSummariesAsync(CancellationToken ct)
	{
		List<Playlist> items = await FetchAllPlaylistItemsAsync(ct);
		return
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
	}

	internal async Task<PlaylistSummary?> GetPlaylistSummaryAsync(
		string playlistId,
		CancellationToken ct
	)
	{
		ct.ThrowIfCancellationRequested();

		PlaylistListResponse response = await Resilience.ExecuteAsync(
			"YouTube.Playlists.List",
			async () =>
			{
				PlaylistsResource.ListRequest request = service.Playlists.List(
					"snippet,contentDetails"
				);
				request.Id = playlistId;
				request.Fields = PLAYLIST_FIELDS;
				return await request.ExecuteAsync(ct);
			},
			ct
		);

		Playlist? item = response.Items?.FirstOrDefault();
		return item is null
			? null
			: new PlaylistSummary(
				item.Id,
				item.Snippet?.Title ?? "Untitled",
				(int)(item.ContentDetails?.ItemCount ?? 0),
				item.ETag
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

			PlaylistItemListResponse response = await Resilience.ExecuteAsync(
				"YouTube.PlaylistItems.List",
				async () =>
				{
					PlaylistItemsResource.ListRequest request = service.PlaylistItems.List(
						"contentDetails"
					);
					request.PlaylistId = playlistId;
					request.MaxResults = MaxResultsPerPage;
					request.PageToken = pageToken;
					request.Fields = PLAYLIST_ITEM_FIELDS;
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

		return videoIds;
	}

	#endregion

	#region Playlist Fetching

	internal async Task<List<YouTubePlaylist>> GetPlaylistMetadataAsync(CancellationToken ct)
	{
		Console.Info("Fetching playlist metadata...");
		List<Playlist> items = await FetchAllPlaylistItemsAsync(ct);
		return
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
	}

	private async Task<List<Playlist>> FetchAllPlaylistItemsAsync(CancellationToken ct)
	{
		List<Playlist> items = [];
		string? pageToken = null;

		do
		{
			ct.ThrowIfCancellationRequested();

			PlaylistListResponse response = await Resilience.ExecuteAsync(
				"YouTube.Playlists.List",
				async () =>
				{
					PlaylistsResource.ListRequest request = service.Playlists.List(
						"snippet,contentDetails"
					);
					request.Mine = true;
					request.MaxResults = MaxResultsPerPage;
					request.PageToken = pageToken;
					request.Fields = PLAYLIST_FIELDS;
					return await request.ExecuteAsync(ct);
				},
				ct
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

		Console.Info("Found {0} playlists, fetching video IDs...", playlists.Count);

		for (var i = 0; i < playlists.Count; i++)
		{
			ct.ThrowIfCancellationRequested();

			YouTubePlaylist playlist = playlists[i];
			List<string> videoIds = await GetPlaylistVideoIdsAsync(playlist.Id, ct);
			playlists[i] = playlist with { VideoIds = videoIds };

			Console.Progress("Playlist IDs: {0}/{1}", i + 1, playlists.Count);
		}

		Console.NewLine();
		return playlists;
	}

	#endregion

	#region Video Details

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

			List<YouTubeVideo> batchVideos = await GetVideoDetailsAsync([.. batch], ct);

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
		VideoListResponse response = await Resilience.ExecuteAsync(
			"YouTube.Videos.List",
			async () =>
			{
				VideosResource.ListRequest request = service.Videos.List("snippet,contentDetails");
				request.Id = Join(",", videoIds);
				request.Fields = VIDEO_FIELDS;
				return await request.ExecuteAsync(ct);
			},
			ct
		);

		return
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
	}

	private static TimeSpan ParseDuration(string? isoDuration) =>
		IsNullOrEmpty(isoDuration) ? TimeSpan.Zero : XmlConvert.ToTimeSpan(isoDuration);

	#endregion
}
