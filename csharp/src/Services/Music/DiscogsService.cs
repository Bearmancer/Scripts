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
			token
			?? throw new ArgumentException(message: "Discogs token is required", nameof(token));
#pragma warning disable CA2000 // Ownership transferred to HttpClient via disposeHandler: true
		HttpClient = new HttpClient(
			new HttpClientHandler { CheckCertificateRevocationList = true },
			disposeHandler: true
		);
#pragma warning restore CA2000
		Client = new DiscogsClient(
			httpClient: HttpClient,
			new ApiQueryBuilder(new DiscogsClientConfig(token: validToken))
		);
	}

	// (removed pragma CA2000 suppression; ensure proper disposal of disposable resources)

	internal DiscogsClient Client { get; }

	public void Dispose() => HttpClient.Dispose();

	public MusicSource Source => MusicSource.Discogs;

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
					criteria: criteria,
					new PageOptions
					{
						PageNumber = 1,
						PageSize = Math.Min(val1: maxResults, val2: 100),
					}
				);

				var result = Enumerable.ToList(
					Enumerable.Select(
						Enumerable.Take(results.Results, count: maxResults),
						r => new SearchResult(
							Source: MusicSource.Discogs,
							(r.ReleaseId > 0 ? r.ReleaseId : r.MasterId).ToString()!,
							r.Title ?? "",
							DiscogsMapper.ExtractArtist(title: r.Title),
							DiscogsMapper.ParseYear(year: r.Year),
							r.Format is { } fmt ? Join(separator: ", ", values: fmt) : null,
							r.Label is { } lbl ? Enumerable.FirstOrDefault(lbl) : null,
							ReleaseType: r.Type,
							Score: null,
							Country: r.Country,
							CatalogNumber: r.CatalogNumber,
							Status: null,
							Disambiguation: null,
							r.Genre?.ToList(),
							r.Style?.ToList()
						)
					)
				);
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
		var id = int.Parse(s: releaseId);
		DiscogsRelease release =
			await GetReleaseAsync(releaseId: id, ct)
			?? throw new InvalidOperationException($"Release not found: {releaseId}");

		var originalYear = await FetchMasterYearAsync(masterId: release.MasterId, ct);

		(var composer, var conductor, var orchestra, List<string> soloists) = ExtractExtraArtists(
			extraArtists: release.ExtraArtists
		);

		var primaryArtist = Enumerable.FirstOrDefault(release.Artists)?.Name;
		DiscogsLabel? firstLabel = Enumerable.FirstOrDefault(release.Labels);
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
			maxDiscNum,
			TrackCount: tracks.Count,
			TotalDuration: totalDuration
		);

		return new ReleaseData(Info: info, Tracks: tracks);
	}

	private async Task<int?> FetchMasterYearAsync(int? masterId, CancellationToken ct)
	{
		if (!masterId.HasValue)
			return null;

		DiscogsMaster? master = await GetMasterAsync(masterId: masterId.Value, ct);
		return master?.Year;
	}

	private static (
		string? Composer,
		string? Conductor,
		string? Orchestra,
		List<string> Soloists
	) ExtractExtraArtists(List<DiscogsArtistRef> extraArtists)
	{
		var composer = Enumerable
			.FirstOrDefault(extraArtists, a => a.Role?.ContainsIgnoreCase("Composed By") == true)
			?.Name;
		var conductor = Enumerable
			.FirstOrDefault(extraArtists, a => a.Role?.ContainsIgnoreCase("Conductor") == true)
			?.Name;
		var orchestra = Enumerable
			.FirstOrDefault(extraArtists, a => a.Role?.ContainsIgnoreCase("Orchestra") == true)
			?.Name;
		List<string> soloists =
		[
			.. Enumerable.Distinct(
				Enumerable.Select(
					Enumerable.Where(
						extraArtists,
						a =>
							a.Role?.ContainsIgnoreCase("Soloist") == true
							|| a.Role?.ContainsIgnoreCase("Performer") == true
					),
					a => a.Name
				)
			),
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
				track.Position.StartsWithIgnoreCase($"{discNum + 1}-", Ordinal)
				|| (
					discNum == 1
					&& track.Position.StartsWithIgnoreCase("1-", Ordinal)
					&& trackNum > 0
				)
			)
			{
				discNum++;
				trackNum = 0;
			}
			if (discNum > maxDiscNum)
				maxDiscNum = discNum;
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

				return Enumerable.ToList(
					Enumerable.Select(
						Enumerable.Take(results.Results, count: maxResults),
						selector: DiscogsMapper.MapSearchResult
					)
				);
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
		return results is [var first, ..] ? first : null;
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
				Release? release = await Client.GetReleaseAsync(releaseId: releaseId);
				if (release is null)
				{
					Log.Debug("GetReleaseAsync exit null");
					return null;
				}

				DiscogsRelease result = DiscogsMapper.MapRelease(r: release);
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
				Release release = await Client.GetReleaseAsync(releaseId: releaseId);
				if (release?.Tracklist is null)
				{
					Log.Debug("GetTracksByMediaAsync exit 0 (no tracklist)");
					return [];
				}

				Dictionary<string, List<Tracklist>> mediaDict = TrackListExtensions.SplitMedia(
					release.Tracklist
				);

				var result = Enumerable.ToDictionary(
					mediaDict,
					kvp => kvp.Key,
					kvp =>
						Enumerable.ToList(
							Enumerable.Select(kvp.Value, selector: DiscogsMapper.MapTrack)
						)
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
				MasterRelease? master = await Client.GetMasterReleaseAsync(masterId: masterId);
				if (master is null)
				{
					Log.Debug("GetMasterAsync exit null");
					return null;
				}

				DiscogsMaster result = DiscogsMapper.MapMaster(m: master);
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
					new VersionsCriteria(masterId: masterId),
					new PageOptions
					{
						PageNumber = 1,
						PageSize = Math.Min(val1: maxResults, val2: 100),
					}
				);

				var versions = Enumerable.ToList(
					Enumerable.Select(
						Enumerable.Take(results.Versions, count: maxResults),
						selector: DiscogsMapper.MapVersion
					)
				);
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
			return await Resilience.ExecuteMusicApiAsync(service: "Discogs", action: action, ct);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Error(ex, "Discogs API error: {Message}", ex.Message);
			throw;
		}
	}

	private static Task<T?> ExecuteSafeAsync<T>(Func<Task<T?>> action, CancellationToken ct)
		where T : class => ExecuteAsync(action: action, ct);

	private static Task<List<T>> ExecuteSafeListAsync<T>(
		Func<Task<List<T>>> action,
		CancellationToken ct
	) => ExecuteAsync(action: action, ct);

	private static Task<Dictionary<TKey, TValue>> ExecuteSafeDictAsync<TKey, TValue>(
		Func<Task<Dictionary<TKey, TValue>>> action,
		CancellationToken ct
	)
		where TKey : notnull => ExecuteAsync(action: action, ct);
}
