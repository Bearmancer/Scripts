using ParkSquare.Discogs;
using ParkSquare.Discogs.Dto;

namespace Scripts.Services.Music;

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
			token
			?? throw new ArgumentException(message: "Discogs token is required", nameof(token));
		HttpClient = new HttpClient(
			new HttpClientHandler { CheckCertificateRevocationList = true },
			disposeHandler: true
		);
		Client = new DiscogsClient(
			httpClient: HttpClient,
			new ApiQueryBuilder(new DiscogsClientConfig(token: validToken))
		);
	}

	internal DiscogsClient Client { get; }

	public void Dispose() => HttpClient.Dispose();

	public MusicSource Source => MusicSource.Discogs;

	public async Task<List<SearchResult>> SearchAsync(
		string query,
		int maxResults = 10,
		CancellationToken ct = default
	)
	{
		Log.Debug(messageTemplate: "SearchAsync entry {Query}", query);
		return await ExecuteSafeListAsync(
			async () =>
			{
				SearchCriteria criteria = new() { Query = query };
				SearchResults results = await Client.SearchAsync(
					criteria: criteria,
					new PageOptions
					{
						PageNumber = 1,
						PageSize = Math.Min(val1: maxResults, val2: 100),
					}
				);

				List<SearchResult> result = results
					.Results.Take(count: maxResults)
					.Select(static r => new SearchResult(
						Source: MusicSource.Discogs,
						(r.ReleaseId > 0 ? r.ReleaseId : r.MasterId).ToString()!,
						r.Title ?? "",
						DiscogsMapper.ExtractArtist(title: r.Title),
						DiscogsMapper.ParseYear(year: r.Year),
						r.Format is { } fmt ? Join(separator: ", ", values: fmt) : null,
						r.Label is { } lbl ? lbl.FirstOrDefault() : null,
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
				Log.Debug(messageTemplate: "SearchAsync exit {Count}", result.Count);
				return result;
			},
			ct: ct
		);
	}

	public async Task<ReleaseData> GetReleaseAsync(
		string releaseId,
		int? maxDiscs = null,
		CancellationToken ct = default
	)
	{
		Log.Debug(messageTemplate: "GetReleaseAsync entry {ReleaseId}", releaseId);
		var id = int.Parse(s: releaseId);
		DiscogsRelease release =
			await GetReleaseAsync(releaseId: id, ct: ct)
			?? throw new InvalidOperationException($"Release not found: {releaseId}");

		var originalYear = await FetchMasterYearAsync(masterId: release.MasterId, ct: ct);

		(var composer, var conductor, var orchestra, List<string> soloists) = ExtractExtraArtists(
			extraArtists: release.ExtraArtists
		);

		var primaryArtist = release.Artists.FirstOrDefault()?.Name;
		DiscogsLabel? firstLabel = release.Labels.FirstOrDefault();
		var label = firstLabel?.Name;
		var catalogNumber = firstLabel?.CatalogNumber;

		List<TrackInfo> tracks;
		int maxDiscNum;
		(tracks, maxDiscNum) = BuildTracks(
			release: release,
			composer: composer,
			conductor: conductor,
			orchestra: orchestra,
			soloists: soloists,
			primaryArtist: primaryArtist
		);

		TimeSpan totalDuration = CalculateTotalDuration(tracks: tracks);

		ReleaseInfo info = new(
			Source: MusicSource.Discogs,
			Id: releaseId,
			Title: release.Title,
			Artist: primaryArtist,
			Label: label,
			CatalogNumber: catalogNumber,
			originalYear ?? release.Year,
			Notes: release.Notes,
			DiscCount: maxDiscNum,
			TrackCount: tracks.Count,
			TotalDuration: totalDuration
		);

		return new ReleaseData(Info: info, Tracks: tracks);
	}

	private async Task<int?> FetchMasterYearAsync(int? masterId, CancellationToken ct)
	{
		if (!masterId.HasValue)
			return null;

		DiscogsMaster? master = await GetMasterAsync(masterId: masterId.Value, ct: ct);
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
			.FirstOrDefault(static a =>
				a.Role?.ContainsIgnoreCase(substring: "Composed By") == true
			)
			?.Name;
		var conductor = extraArtists
			.FirstOrDefault(static a => a.Role?.ContainsIgnoreCase(substring: "Conductor") == true)
			?.Name;
		var orchestra = extraArtists
			.FirstOrDefault(static a => a.Role?.ContainsIgnoreCase(substring: "Orchestra") == true)
			?.Name;
		List<string> soloists =
		[
			.. extraArtists
				.Where(static a =>
					a.Role?.ContainsIgnoreCase(substring: "Soloist") == true
					|| a.Role?.ContainsIgnoreCase(substring: "Performer") == true
				)
				.Select(static a => a.Name)
				.Distinct(),
		];

		return (composer, conductor, orchestra, soloists);
	}

	private static (List<TrackInfo> Tracks, int MaxDiscNum) BuildTracks(
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
		var maxDiscNum = 1;

		foreach (DiscogsTrack track in release.Tracks)
		{
			if (
				track.Position.StartsWithIgnoreCase($"{discNum + 1}-", comparisonType: Ordinal)
				|| discNum == 1
					&& track.Position.StartsWithIgnoreCase(prefix: "1-", comparisonType: Ordinal)
					&& trackNum > 0
			)
			{
				discNum++;
				trackNum = 0;
			}
			if (discNum > maxDiscNum)
				maxDiscNum = discNum;
			trackNum++;

			var (recordingYear, recordingVenue) = DiscogsMapper.ParseNotesForRecordingInfo(
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

		return (tracks, maxDiscNum);
	}

	private static TimeSpan CalculateTotalDuration(List<TrackInfo> tracks)
	{
		TimeSpan total = TimeSpan.Zero;
		foreach (TrackInfo t in tracks)
		{
			if (t.Duration.HasValue)
				total += t.Duration.Value;
		}
		return total;
	}

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
					criteria: criteria,
					new PageOptions
					{
						PageNumber = 1,
						PageSize = Math.Min(val1: maxResults, val2: 100),
					}
				);

				return results
					.Results.Take(count: maxResults)
					.Select(selector: DiscogsMapper.MapSearchResult)
					.ToList();
			},
			ct: ct
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
		List<DiscogsSearchResult> results = await SearchAdvancedAsync(
			artist: artist,
			release: release,
			track: track,
			year: year,
			label: label,
			genre: genre,
			maxResults: 1,
			ct: ct
		);
		return results is [var first, ..] ? first : null;
	}

	internal async Task<DiscogsRelease?> GetReleaseAsync(
		int releaseId,
		CancellationToken ct = default
	)
	{
		Log.Debug(messageTemplate: "GetReleaseAsync entry {ReleaseId}", releaseId);
		return await ExecuteSafeAsync(
			async () =>
			{
				Release? release = await Client.GetReleaseAsync(releaseId: releaseId);
				if (release is null)
				{
					Log.Debug(messageTemplate: "GetReleaseAsync exit null");
					return null;
				}

				DiscogsRelease result = DiscogsMapper.MapRelease(r: release);
				Log.Debug(messageTemplate: "GetReleaseAsync exit {Id}", result.Id);
				return result;
			},
			ct: ct
		);
	}

	internal async Task<Dictionary<string, List<DiscogsTrack>>> GetTracksByMediaAsync(
		int releaseId,
		CancellationToken ct = default
	)
	{
		Log.Debug(messageTemplate: "GetTracksByMediaAsync entry {ReleaseId}", releaseId);
		return await ExecuteSafeDictAsync(
			async () =>
			{
				Release release = await Client.GetReleaseAsync(releaseId: releaseId);
				if (release?.Tracklist is null)
				{
					Log.Debug(messageTemplate: "GetTracksByMediaAsync exit 0 (no tracklist)");
					return [];
				}

				Dictionary<string, List<Tracklist>> mediaDict = release.Tracklist.SplitMedia();

				Dictionary<string, List<DiscogsTrack>> result = mediaDict.ToDictionary(
					static kvp => kvp.Key,
					static kvp => kvp.Value.Select(selector: DiscogsMapper.MapTrack).ToList()
				);
				Log.Debug(messageTemplate: "GetTracksByMediaAsync exit {Count}", result.Count);
				return result;
			},
			ct: ct
		);
	}

	internal async Task<DiscogsMaster?> GetMasterAsync(int masterId, CancellationToken ct = default)
	{
		Log.Debug(messageTemplate: "GetMasterAsync entry {MasterId}", masterId);
		return await ExecuteSafeAsync(
			async () =>
			{
				MasterRelease? master = await Client.GetMasterReleaseAsync(masterId: masterId);
				if (master is null)
				{
					Log.Debug(messageTemplate: "GetMasterAsync exit null");
					return null;
				}

				DiscogsMaster result = DiscogsMapper.MapMaster(m: master);
				Log.Debug(messageTemplate: "GetMasterAsync exit {Id}", result.Id);
				return result;
			},
			ct: ct
		);
	}

	internal async Task<List<DiscogsVersion>> GetVersionsAsync(
		int masterId,
		int maxResults = 50,
		CancellationToken ct = default
	)
	{
		Log.Debug(
			messageTemplate: "GetVersionsAsync entry {MasterId} {MaxResults}",
			masterId,
			maxResults
		);
		return await ExecuteSafeListAsync(
			async () =>
			{
				VersionResults results = await Client.GetVersionsAsync(
					new VersionsCriteria(masterId: masterId),
					new PageOptions
					{
						PageNumber = 1,
						PageSize = Math.Min(val1: maxResults, val2: 100),
					}
				);

				List<DiscogsVersion> versions = results
					.Versions.Take(count: maxResults)
					.Select(selector: DiscogsMapper.MapVersion)
					.ToList();
				Log.Debug(messageTemplate: "GetVersionsAsync exit {Count}", versions.Count);
				return versions;
			},
			ct: ct
		);
	}

	private static async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct)
	{
		try
		{
			return await Resilience.ExecuteMusicApiAsync(
				service: "Discogs",
				action: action,
				ct: ct
			);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Error(ex: ex, messageTemplate: "Discogs API error: {Message}", ex.Message);
			throw;
		}
	}

	private static Task<T?> ExecuteSafeAsync<T>(Func<Task<T?>> action, CancellationToken ct)
		where T : class => ExecuteAsync(action: action, ct: ct);

	private static Task<List<T>> ExecuteSafeListAsync<T>(
		Func<Task<List<T>>> action,
		CancellationToken ct
	) => ExecuteAsync(action: action, ct: ct);

	private static Task<Dictionary<TKey, TValue>> ExecuteSafeDictAsync<TKey, TValue>(
		Func<Task<Dictionary<TKey, TValue>>> action,
		CancellationToken ct
	)
		where TKey : notnull => ExecuteAsync(action: action, ct: ct);
}
