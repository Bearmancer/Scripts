using ParkSquare.Discogs;

namespace CSharpScripts.Services.Music;

internal sealed class DiscogsClientConfig(string token) : IClientConfig
{
	public string AuthToken => token;
	public string BaseUrl { get; } = "https://api.discogs.com";
}

internal sealed class DiscogsService : IMusicService, IDisposable
{
	private readonly HttpClient HttpClient;

	public DiscogsService(string? token)
	{
		var validToken =
			token ?? throw new ArgumentException("Discogs token is required", nameof(token));
#pragma warning disable CA2000 // Ownership transferred to HttpClient via disposeHandler: true
		HttpClient = new(
			new HttpClientHandler { CheckCertificateRevocationList = true },
			disposeHandler: true
		);
#pragma warning restore CA2000
		Client = new DiscogsClient(
			HttpClient,
			new ApiQueryBuilder(new DiscogsClientConfig(validToken))
		);
	}

	internal DiscogsClient Client { get; }
	public MusicSource Source => MusicSource.Discogs;

	public void Dispose() => HttpClient.Dispose();

	public async Task<List<SearchResult>> SearchAsync(
		string query,
		int maxResults = 10,
		CancellationToken ct = default
	)
	{
		Log.Debug("SearchAsync entry {Query}", query);
		return await ExecuteSafeListAsync(
			async () =>
			{
				SearchCriteria criteria = new() { Query = query };
				SearchResults results = await Client.SearchAsync(
					criteria,
					new PageOptions { PageNumber = 1, PageSize = Math.Min(maxResults, 100) }
				);

				var result = results
					.Results.Take(maxResults)
					.Select(r => new SearchResult(
						Source: MusicSource.Discogs,
						(r.ReleaseId > 0 ? r.ReleaseId : r.MasterId).ToString()!,
						r.Title ?? "",
						DiscogsMapper.ExtractArtist(r.Title),
						DiscogsMapper.ParseYear(r.Year),
						r.Format is { } fmt ? Join(", ", fmt) : null,
						r.Label is { } lbl ? Join(", ", lbl) : null,
						ReleaseType: r.Type,
						Score: null,
						Country: r.Country,
						CatalogNumber: r.CatalogNumber,
						Status: null,
						Disambiguation: null,
						r.Genre?.ToList(),
						r.Style?.ToList()
					))
					.ToList();
				Log.Debug("SearchAsync exit {Count}", result.Count);
				return result;
			},
			ct
		);
	}

	public async Task<ReleaseData> GetReleaseAsync(
		string releaseId,
		int? maxDiscs = null,
		CancellationToken ct = default
	)
	{
		Log.Debug("GetReleaseAsync entry {ReleaseId}", releaseId);
		var id = int.Parse(releaseId);
		DiscogsRelease release =
			await GetReleaseAsync(id, ct)
			?? throw new InvalidOperationException($"Release not found: {releaseId}");

		var originalYear = await FetchMasterYearAsync(release.MasterId, ct);

		(var composer, var conductor, var orchestra, List<string> soloists) = ExtractExtraArtists(
			release.ExtraArtists
		);

		var primaryArtist = release.Artists.FirstOrDefault()?.Name;
		var label = release.Labels.FirstOrDefault()?.Name;
		var catalogNumber = release.Labels.FirstOrDefault()?.CatalogNumber;

		List<TrackInfo> tracks = BuildTracks(
			release,
			composer,
			conductor,
			orchestra,
			soloists,
			primaryArtist
		);

		TimeSpan totalDuration = CalculateTotalDuration(tracks);

		ReleaseInfo info = new(
			Source: MusicSource.Discogs,
			Id: releaseId,
			Title: release.Title,
			Artist: primaryArtist,
			Label: label,
			CatalogNumber: catalogNumber,
			originalYear ?? release.Year,
			Notes: release.Notes,
			DiscCount: tracks.Max(t => t.DiscNumber),
			TrackCount: tracks.Count,
			TotalDuration: totalDuration
		);

		return new ReleaseData(Info: info, Tracks: tracks);
	}

	private async Task<int?> FetchMasterYearAsync(int? masterId, CancellationToken ct)
	{
		if (!masterId.HasValue)
			return null;

		DiscogsMaster? master = await GetMasterAsync(masterId.Value, ct);
		return master?.Year;
	}

	private static (
		string? Composer,
		string? Conductor,
		string? Orchestra,
		List<string> Soloists
	) ExtractExtraArtists(List<DiscogsArtistRef> extraArtists)
	{
		var composer = extraArtists
			.FirstOrDefault(a => a.Role.ContainsIgnoreCase("Composed By"))
			?.Name;
		var conductor = extraArtists
			.FirstOrDefault(a => a.Role.ContainsIgnoreCase("Conductor"))
			?.Name;
		var orchestra = extraArtists
			.FirstOrDefault(a => a.Role.ContainsIgnoreCase("Orchestra"))
			?.Name;
		List<string> soloists =
		[
			.. extraArtists
				.Where(a =>
					a.Role.ContainsIgnoreCase("Soloist") || a.Role.ContainsIgnoreCase("Performer")
				)
				.Select(a => a.Name)
				.Distinct(),
		];

		return (composer, conductor, orchestra, soloists);
	}

	private static List<TrackInfo> BuildTracks(
		DiscogsRelease release,
		string? composer,
		string? conductor,
		string? orchestra,
		List<string> soloists,
		string? primaryArtist
	)
	{
		List<TrackInfo> tracks = [];
		var discNum = 1;
		var trackNum = 0;

		foreach (DiscogsTrack track in release.Tracks)
		{
			if (
				track.Position.StartsWithExact($"{discNum + 1}-")
				|| (discNum == 1 && track.Position.StartsWithExact("1-") && trackNum > 0)
			)
			{
				discNum++;
				trackNum = 0;
			}
			trackNum++;

			(var recordingYear, var recordingVenue) = DiscogsMapper.ParseNotesForRecordingInfo(
				notes: release.Notes,
				discNumber: discNum
			);

			tracks.Add(
				new TrackInfo(
					DiscNumber: discNum,
					TrackNumber: trackNum,
					Title: track.Title,
					DiscogsMapper.ParseDuration(duration: track.Duration),
					RecordingYear: recordingYear,
					Composer: composer,
					WorkName: null,
					Conductor: conductor,
					Orchestra: orchestra,
					Soloists: soloists,
					Artist: primaryArtist,
					RecordingVenue: recordingVenue
				)
			);
		}

		return tracks;
	}

	private static TimeSpan CalculateTotalDuration(List<TrackInfo> tracks) =>
		tracks
			.Where(t => t.Duration.HasValue)
			.Aggregate(seed: TimeSpan.Zero, (sum, t) => sum + t.Duration!.Value);

	public async Task<List<DiscogsSearchResult>> SearchAdvancedAsync(
		string? artist = null,
		string? release = null,
		string? track = null,
		int? year = null,
		string? label = null,
		string? genre = null,
		int maxResults = 50,
		CancellationToken ct = default
	)
	{
		SearchCriteria criteria = new()
		{
			Artist = artist,
			ReleaseTitle = release,
			Track = track,
			Year = year,
			Label = label,
			Genre = genre,
		};

		return await ExecuteSafeListAsync(
			async () =>
			{
				SearchResults results = await Client.SearchAsync(
					criteria,
					new PageOptions { PageNumber = 1, PageSize = Math.Min(maxResults, 100) }
				);

				return results
					.Results.Take(maxResults)
					.Select(DiscogsMapper.MapSearchResult)
					.ToList();
			},
			ct
		);
	}

	internal async Task<DiscogsSearchResult?> SearchFirstAsync(
		string? artist = null,
		string? release = null,
		string? track = null,
		int? year = null,
		string? label = null,
		string? genre = null,
		CancellationToken ct = default
	)
	{
		Log.Debug("SearchFirstAsync entry {Artist} {Release}", artist, release);
		List<DiscogsSearchResult> results = await SearchAdvancedAsync(
			artist: artist,
			release: release,
			track: track,
			year: year,
			label: label,
			genre: genre,
			maxResults: 1,
			ct
		);
		DiscogsSearchResult? result = results.Count > 0 ? results[index: 0] : null;
		Log.Debug("SearchFirstAsync exit {Found}", result is not null);
		return result;
	}

	internal async Task<DiscogsRelease?> GetReleaseAsync(
		int releaseId,
		CancellationToken ct = default
	)
	{
		Log.Debug("GetReleaseAsync entry {ReleaseId}", releaseId);
		return await ExecuteSafeAsync(
			async () =>
			{
				Release? release = await Client.GetReleaseAsync(releaseId);
				if (release is null)
				{
					Log.Debug("GetReleaseAsync exit null");
					return null;
				}

				DiscogsRelease result = DiscogsMapper.MapRelease(release);
				Log.Debug("GetReleaseAsync exit {Id}", result.Id);
				return result;
			},
			ct
		);
	}

	internal async Task<Dictionary<string, List<DiscogsTrack>>> GetTracksByMediaAsync(
		int releaseId,
		CancellationToken ct = default
	)
	{
		Log.Debug("GetTracksByMediaAsync entry {ReleaseId}", releaseId);
		return await ExecuteSafeDictAsync(
			async () =>
			{
				Release release = await Client.GetReleaseAsync(releaseId);
				if (release?.Tracklist is null)
				{
					Log.Debug("GetTracksByMediaAsync exit 0 (no tracklist)");
					return [];
				}

				Dictionary<string, List<Tracklist>> mediaDict = release.Tracklist.SplitMedia();

				var result = mediaDict.ToDictionary(
					kvp => kvp.Key,
					kvp => kvp.Value.Select(DiscogsMapper.MapTrack).ToList()
				);
				Log.Debug("GetTracksByMediaAsync exit {Count}", result.Count);
				return result;
			},
			ct
		);
	}

	internal async Task<DiscogsMaster?> GetMasterAsync(int masterId, CancellationToken ct = default)
	{
		Log.Debug("GetMasterAsync entry {MasterId}", masterId);
		return await ExecuteSafeAsync(
			async () =>
			{
				MasterRelease? master = await Client.GetMasterReleaseAsync(masterId);
				if (master is null)
				{
					Log.Debug("GetMasterAsync exit null");
					return null;
				}

				DiscogsMaster result = DiscogsMapper.MapMaster(master);
				Log.Debug("GetMasterAsync exit {Id}", result.Id);
				return result;
			},
			ct
		);
	}

	internal async Task<List<DiscogsVersion>> GetVersionsAsync(
		int masterId,
		int maxResults = 50,
		CancellationToken ct = default
	)
	{
		Log.Debug("GetVersionsAsync entry {MasterId} {MaxResults}", masterId, maxResults);
		return await ExecuteSafeListAsync(
			async () =>
			{
				VersionResults results = await Client.GetVersionsAsync(
					new VersionsCriteria(masterId),
					new PageOptions { PageNumber = 1, PageSize = Math.Min(maxResults, 100) }
				);

				var versions = results
					.Versions.Take(maxResults)
					.Select(DiscogsMapper.MapVersion)
					.ToList();
				Log.Debug("GetVersionsAsync exit {Count}", versions.Count);
				return versions;
			},
			ct
		);
	}

	private static async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct)
	{
		try
		{
			return await Resilience.ExecuteMusicApiAsync("Discogs", action, ct);
		}
		catch (Exception ex)
		{
			Log.Error("Discogs", ex.Message);
			throw;
		}
	}

	private static Task<T?> ExecuteSafeAsync<T>(Func<Task<T?>> action, CancellationToken ct)
		where T : class => ExecuteAsync(action, ct);

	private static Task<List<T>> ExecuteSafeListAsync<T>(
		Func<Task<List<T>>> action,
		CancellationToken ct
	) => ExecuteAsync(action, ct);

	private static Task<Dictionary<TKey, TValue>> ExecuteSafeDictAsync<TKey, TValue>(
		Func<Task<Dictionary<TKey, TValue>>> action,
		CancellationToken ct
	)
		where TKey : notnull => ExecuteAsync(action, ct);
}
