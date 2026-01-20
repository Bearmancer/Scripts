namespace CSharpScripts.Services.Music;

internal sealed class DiscogsClientConfig(string token) : IClientConfig
{
	public string AuthToken => token;
	public string BaseUrl { get; } = "https://api.discogs.com";
}

public sealed class DiscogsService : IMusicService
{
	#region Constructor & Properties

	public DiscogsService(string? token)
	{
		var validToken =
			token ?? throw new ArgumentException("Discogs token is required", nameof(token));
		HttpClient http = new(new HttpClientHandler());
		Client = new DiscogsClient(http, new ApiQueryBuilder(new DiscogsClientConfig(validToken)));
	}

	internal DiscogsClient Client { get; }
	public MusicSource Source => MusicSource.Discogs;

	#endregion

	#region Search Operations

	public async Task<List<SearchResult>> SearchAsync(
		string query,
		int maxResults = 10,
		CancellationToken ct = default
	)
	{
		return await ExecuteSafeListAsync(
			async () =>
			{
				SearchCriteria criteria = new() { Query = query };
				SearchResults results = await Client.SearchAsync(
					criteria,
					new PageOptions
					{
						PageNumber = 1,
						PageSize = Math.Min(maxResults, 100),
					}
				);

				return results
					.Results.Take(maxResults)
					.Select(r => new SearchResult(
						Source: MusicSource.Discogs,
						(r.ReleaseId > 0 ? r.ReleaseId : r.MasterId).ToString()!,
						r.Title ?? "",
						ExtractArtist(r.Title),
						ParseYear(r.Year),
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
			},
			ct
		);
	}

	#endregion

	#region Release Data Retrieval

	public async Task<ReleaseData> GetReleaseAsync(
		string releaseId,
		bool deepSearch = true,
		int? maxDiscs = null,
		CancellationToken ct = default
	)
	{
		var id = int.Parse(releaseId);
		DiscogsRelease release =
			await GetReleaseAsync(id, ct)
			?? throw new InvalidOperationException($"Release not found: {releaseId}");

		var originalYear = release.MasterId.HasValue
			? (await GetMasterAsync(release.MasterId.Value, ct))?.Year
			: null;

		var composer = release.ExtraArtists.FirstOrDefault(a => a.Role.Has("Composed By"))?.Name;
		var conductor = release.ExtraArtists.FirstOrDefault(a => a.Role.Has("Conductor"))?.Name;
		var orchestra = release.ExtraArtists.FirstOrDefault(a => a.Role.Has("Orchestra"))?.Name;
		List<string> soloists =
		[
			.. release
				.ExtraArtists.Where(a => a.Role.Has("Soloist") || a.Role.Has("Performer"))
				.Select(a => a.Name)
				.Distinct(),
		];

		var primaryArtist = release.Artists.FirstOrDefault()?.Name;
		var label = release.Labels.FirstOrDefault()?.Name;
		var catalogNumber = release.Labels.FirstOrDefault()?.CatalogNumber;

		List<TrackInfo> tracks = [];
		var discNum = 1;
		var trackNum = 0;

		foreach (DiscogsTrack track in release.Tracks)
		{
			if (
				track.Position.Starts($"{discNum + 1}-", Ordinal)
				|| (discNum == 1 && track.Position.Starts("1-", Ordinal) && trackNum > 0)
			)
			{
				discNum++;
				trackNum = 0;
			}
			trackNum++;

			(var recordingYear, var recordingVenue) = ParseNotesForRecordingInfo(
				notes: release.Notes,
				discNumber: discNum
			);

			tracks.Add(
				new TrackInfo(
					DiscNumber: discNum,
					TrackNumber: trackNum,
					Title: track.Title,
					ParseDuration(duration: track.Duration),
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

		TimeSpan totalDuration = tracks
			.Where(t => t.Duration.HasValue)
			.Aggregate(seed: TimeSpan.Zero, (sum, t) => sum + t.Duration!.Value);

		ReleaseInfo info = new(
			Source: MusicSource.Discogs,
			Id: releaseId,
			Title: release.Title,
			Artist: primaryArtist,
			Label: label,
			CatalogNumber: catalogNumber,
			originalYear ?? release.Year,
			Notes: release.Notes,
			DiscCount: discNum,
			TrackCount: tracks.Count,
			TotalDuration: totalDuration
		);

		return new ReleaseData(Info: info, Tracks: tracks);
	}

	#endregion

	#region Notes Parsing

	private static TimeSpan? ParseDuration(string? duration) =>
		TimeSpan.TryParse(duration, out TimeSpan result) ? result : null;

	internal static (int? Year, string? Venue) ParseNotesForRecordingInfo(
		string? notes,
		int discNumber
	)
	{
		if (IsNullOrWhiteSpace(notes))
			return (null, null);

		int? year = null;
		string? venue = null;

		var lines = notes.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

		foreach (var rawLine in lines)
		{
			var line = rawLine.Trim();

			var appliesToDisc = true;
			Match discRangeMatch = Regex.Match(
				line,
				@"^(?:CD|Disc)\s*(\d+)(?:\s*[-–]\s*(\d+))?:",
				RegexOptions.IgnoreCase
			);
			if (discRangeMatch.Success)
			{
				var startDisc = int.Parse(discRangeMatch.Groups[1].Value);
				var endDisc = discRangeMatch.Groups[2].Success
					? int.Parse(discRangeMatch.Groups[2].Value)
					: startDisc;
				appliesToDisc = discNumber >= startDisc && discNumber <= endDisc;
			}

			if (!appliesToDisc)
				continue;

			year ??= ExtractYearFromLine(line);

			venue ??= ExtractVenueFromLine(line);
		}

		return (year, venue);
	}

	private static int? ExtractYearFromLine(string line)
	{
		Match recordedMatch = Regex.Match(
			line,
			@"[Rr]ecorded\s+(?:\w+\s+)?(\d{4})",
			RegexOptions.IgnoreCase
		);
		if (
			recordedMatch.Success
			&& int.TryParse(recordedMatch.Groups[1].Value, out var y1)
		)
			return y1;

		Match yearMatch = Regex.Match(line, @"\b(19\d{2}|20\d{2})\b");
		if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var y2))
			return y2;

		return null;
	}

	private static string? ExtractVenueFromLine(string line)
	{
		Match venueMatch = Regex.Match(
			input: line,
			pattern: @"(?:@|at|in)\s+([A-Z][^,\.\n]+(?:,\s*[A-Z][^,\.\n]+)?)",
			options: RegexOptions.IgnoreCase
		);
		if (venueMatch.Success)
			return venueMatch.Groups[groupnum: 1].Value.Trim();

		Match commaVenueMatch = Regex.Match(
			input: line,
			pattern: @"\d{4},\s*([A-Z][^,\.\n]+(?:,\s*[A-Z][^,\.\n]+)?)",
			options: RegexOptions.None
		);
		if (commaVenueMatch.Success)
			return commaVenueMatch.Groups[groupnum: 1].Value.Trim();

		return null;
	}

	#endregion

	#region Advanced Search

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
					new PageOptions
					{
						PageNumber = 1,
						PageSize = Math.Min(maxResults, 100),
					}
				);

				return results
					.Results.Take(maxResults)
					.Select(MapSearchResult)
					.ToList();
			},
			ct
		);
	}

	public async Task<DiscogsSearchResult?> SearchFirstAsync(
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
		return results.Count > 0 ? results[index: 0] : null;
	}

	private static DiscogsSearchResult MapSearchResult(ParkSquare.Discogs.Dto.SearchResult r) =>
		new(
			ReleaseId: r.ReleaseId,
			MasterId: r.MasterId,
			Title: r.Title,
			ExtractArtist(title: r.Title),
			ParseYear(year: r.Year),
			Country: r.Country,
			r.Format is { } fmt ? Join(separator: ", ", values: fmt) : null,
			r.Label is { } lbl ? Join(separator: ", ", values: lbl) : null,
			CatalogNumber: r.CatalogNumber,
			Type: r.Type,
			Thumb: r.Thumb,
			CoverImage: r.CoverImage,
			r.Genre?.ToList(),
			r.Style?.ToList(),
			r.Barcode?.ToList()
		);

	#endregion

	#region Entity Lookup

	public async Task<DiscogsRelease?> GetReleaseAsync(
		int releaseId,
		CancellationToken ct = default
	)
	{
		return await ExecuteSafeAsync(
			async () =>
			{
				Release? release = await Client.GetReleaseAsync(releaseId);
				if (release is null)
					return null;

				return MapRelease(release);
			},
			ct
		);
	}

	private static DiscogsRelease MapRelease(Release r) =>
		new(
			Id: r.ReleaseId,
			r.Title ?? "",
			Year: r.Year,
			Country: r.Country,
			Released: r.Released,
			ReleasedFormatted: r.ReleasedFormatted,
			MasterId: r.MasterId,
			MasterUrl: r.MasterUrl,
			Status: r.Status,
			DataQuality: r.DataQuality,
			Notes: r.Notes,
			Uri: r.Uri,
			ResourceUrl: r.ResourceUrl,
			r.Artists?.Select(MapArtistRef).ToList() ?? [],
			r.ExtraArtists?.Select(MapArtistRef).ToList() ?? [],
			r.Labels?.Select(MapLabel).ToList() ?? [],
			r.Companies?.Select(MapCompany).ToList() ?? [],
			r.Genres?.ToList() ?? [],
			r.Styles?.ToList() ?? [],
			r.Tracklist?.Select(MapTrack).ToList() ?? [],
			r.Formats?.Select(MapFormat).ToList() ?? [],
			r.Identifiers?.Select(MapIdentifier).ToList() ?? [],
			r.Images?.Select(MapImage).ToList() ?? [],
			r.Videos?.Select(MapVideo).ToList() ?? [],
			r.Community is { } c ? MapCommunity(c) : null,
			EstimatedWeight: r.EstimatedWeight
		);

	public async Task<Dictionary<string, List<DiscogsTrack>>> GetTracksByMediaAsync(
		int releaseId,
		CancellationToken ct = default
	)
	{
		return await ExecuteSafeDictAsync(
			async () =>
			{
				Release release = await Client.GetReleaseAsync(releaseId);
				if (release?.Tracklist is null)
					return [];

				Dictionary<string, List<Tracklist>> mediaDict = release.Tracklist.SplitMedia();

				return mediaDict.ToDictionary(
					kvp => kvp.Key,
					kvp => kvp.Value.Select(MapTrack).ToList()
				);
			},
			ct
		);
	}

	public async Task<DiscogsMaster?> GetMasterAsync(int masterId, CancellationToken ct = default)
	{
		return await ExecuteSafeAsync(
			async () =>
			{
				MasterRelease? master = await Client.GetMasterReleaseAsync(masterId);
				if (master is null)
					return null;

				return MapMaster(master);
			},
			ct
		);
	}

	private static DiscogsMaster MapMaster(MasterRelease m) =>
		new(
			Id: m.MasterId,
			m.Title ?? "",
			Year: m.Year,
			MainReleaseId: m.MainReleaseId,
			MostRecentReleaseId: m.MostRecentReleaseId,
			MainReleaseUrl: m.MainReleaseUrl,
			MostRecentReleaseUrl: m.MostRecentReleaseUrl,
			VersionsUrl: m.VersionsUrl,
			ResourceUrl: m.ResourceUrl,
			Uri: m.Uri,
			DataQuality: m.DataQuality,
			m.Artists?.Select(MapArtistRef).ToList() ?? [],
			m.Genres?.ToList() ?? [],
			m.Styles?.ToList() ?? [],
			m.Tracklist?.Select(MapTrack).ToList() ?? [],
			m.Images?.Select(MapImage).ToList() ?? [],
			m.Videos?.Select(MapVideo).ToList() ?? [],
			QuantityForSale: m.QuantityForSale,
			(decimal?)m.LowestPrice
		);

	public async Task<List<DiscogsVersion>> GetVersionsAsync(
		int masterId,
		int maxResults = 50,
		CancellationToken ct = default
	)
	{
		return await ExecuteSafeListAsync(
			async () =>
			{
				VersionResults results = await Client.GetVersionsAsync(
					new VersionsCriteria(masterId),
					new PageOptions
					{
						PageNumber = 1,
						PageSize = Math.Min(maxResults, 100),
					}
				);

				return results
					.Versions.Take(maxResults)
					.Select(MapVersion)
					.ToList();
			},
			ct
		);
	}

	#endregion

	#region Entity Mappers

	private static DiscogsVersion MapVersion(ParkSquare.Discogs.Dto.Version v) =>
		new(
			Id: v.ReleaseId,
			v.Title ?? "",
			Format: v.Format,
			Label: v.Label,
			Country: v.Country,
			ParseYear(v.ReleaseYear),
			CatalogNumber: v.CatalogNumber,
			Status: v.Status,
			ResourceUrl: v.ResourceUrl,
			Thumb: v.Thumb
		);

	private static DiscogsTrack MapTrack(Tracklist t) =>
		new(
			t.Position ?? "",
			t.Title ?? "",
			Duration: t.Duration,
			Type: t.Type,
			t.Artists?.Select(MapArtistRef).ToList(),
			t.ExtraArtists?.Select(MapArtistRef).ToList()
		);

	private static DiscogsFormat MapFormat(Format f) =>
		new(f.Name ?? "", Quantity: f.Quantity, Text: f.Text, f.Descriptions?.ToList() ?? []);

	private static DiscogsLabel MapLabel(Label l) =>
		new(
			Id: l.Id,
			l.Name ?? "",
			CatalogNumber: l.CatalogNumber,
			EntityType: l.EntityType,
			EntityTypeName: l.EntityTypeName,
			ResourceUrl: l.ResourceUrl
		);

	private static DiscogsCompany MapCompany(Company c) =>
		new(
			Id: c.Id,
			c.Name ?? "",
			CatalogNumber: c.CatalogNumber,
			EntityType: c.EntityType,
			EntityTypeName: c.EntityTypeName,
			ResourceUrl: c.ResourceUrl
		);

	private static DiscogsArtistRef MapArtistRef(Artist a) =>
		new(
			Id: a.Id,
			a.Name ?? "",
			Anv: a.Alias,
			Join: a.Join,
			Role: a.Role,
			Tracks: a.Tracks,
			ResourceUrl: a.ResourceUrl
		);

	private static DiscogsImage MapImage(Image i) =>
		new(
			i.Type ?? "",
			Uri: i.Uri,
			Uri150: i.UriSmall,
			Width: i.Width,
			Height: i.Height,
			ResourceUrl: i.ResourceUrl
		);

	private static DiscogsVideo MapVideo(DiscogsVideoDto v) =>
		new(
			v.Uri ?? "",
			Title: v.Title,
			Description: v.Description,
			Duration: v.Duration,
			Embed: v.Embed
		);

	private static DiscogsIdentifier MapIdentifier(Identifier i) =>
		new(i.Type ?? "", i.Value ?? "", Description: i.Description);

	private static DiscogsCommunity MapCommunity(Community c) =>
		new(
			Have: c.Have,
			Want: c.Want,
			Rating: c.Rating?.Average,
			RatingCount: c.Rating?.Count,
			Status: c.Status,
			DataQuality: c.DataQuality,
			c.Submitter is { } s
				? new DiscogsSubmitter(s.Username ?? "", ResourceUrl: s.ResourceUrl)
				: null
		);

	#endregion

	#region Execution & Helpers

	private static async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct)
	{
		try
		{
			return await Resilience.ExecuteAsync("Discogs", action, ct);
		}
		catch (Exception ex)
		{
			Console.CriticalFailure("Discogs", ex.Message);
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

	private static string? ExtractArtist(string? title) =>
		title?.Contains(" - ") == true ? title.Split(" - ")[0].Trim() : null;

	private static int? ParseYear(string? year) => int.TryParse(year, out var y) ? y : null;

	#endregion
}
