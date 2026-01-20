namespace CSharpScripts.Services.Music;

public sealed class MusicBrainzService(
	string appName = "LancesUtilities",
	string appVersion = "1.0",
	string contact = "user@example.com"
) : IMusicService
{
	#region Fields & Configuration

	internal Query Query { get; } = new(appName, appVersion, contact);
	public MusicSource Source => MusicSource.MusicBrainz;

	private static readonly Lock TraceLock = new();

	#endregion

	#region Logging & Diagnostics

	private static string GetEntityDumpDirectory(string entity, string id) =>
		Combine(Paths.DumpsDirectory, entity, id);

	private static async Task<T?> ExecuteAndLogAsync<T>(
		Func<Task<T?>> action,
		string entity,
		string id,
		CancellationToken ct
	)
		where T : class
	{
		var dir = GetEntityDumpDirectory(entity, id);
		CreateDirectory(dir);

		var tracePath = Combine(dir, "http.log");
		using TextWriterTraceListener listener = new(tracePath);

		lock (TraceLock)
		{
			HttpUtils.TraceSource.Listeners.Add(listener);
			HttpUtils.TraceSource.Switch.Level = SourceLevels.All;
		}

		try
		{
			T? result = await Resilience.ExecuteAsync("MusicBrainz", action, ct);

			if (result is { })
			{
				var json = JsonSerializer.Serialize(result, StateManager.JsonIndented);
				await WriteAllTextAsync(Combine(dir, "data.json"), json, ct);
			}

			return result;
		}
		finally
		{
			lock (TraceLock)
			{
				listener.Flush();
				HttpUtils.TraceSource.Listeners.Remove(listener);
			}
		}
	}

	#endregion

	#region Work Context Cache

	private readonly Dictionary<Guid, WorkDetails> workDetailsCache = [];
	private Guid? currentWorkId;
	private MusicBrainzRecording? currentWorkRecording;
	private WorkDetails? currentWorkDetails;

	public void ClearCache()
	{
		workDetailsCache.Clear();
		currentWorkId = null;
		currentWorkRecording = null;
		currentWorkDetails = null;
	}

	private void UpdateWorkContext(
		Guid? workId,
		MusicBrainzRecording recording,
		WorkDetails? details
	)
	{
		currentWorkId = workId;
		currentWorkRecording = recording;
		currentWorkDetails = details;
	}

	#endregion

	#region Search Operations

	public async Task<List<SearchResult>> SearchAsync(
		string query,
		int maxResults = 10,
		CancellationToken ct = default
	)
	{
		if (query.Contains("artist:") || query.Contains("release:"))
			return await SearchReleasesAsync(null, query, null, null, null, maxResults, ct);

		return await SearchReleasesAsync(null, query, null, null, null, maxResults, ct);
	}

	public async Task<List<SearchResult>> SearchReleasesAsync(
		string? artist = null,
		string? release = null,
		int? year = null,
		string? label = null,
		string? genre = null,
		int maxResults = 25,
		CancellationToken ct = default
	)
	{
		var query = BuildQuery(
			artist: artist,
			release: release,
			year: year,
			label: label,
			genre: genre
		);
		if (IsNullOrEmpty(query))
			return [];

		return await ExecuteSafeListAsync(
			async () =>
			{
				ISearchResults<ISearchResult<IRelease>> results = await Query.FindReleasesAsync(
					query: query,
					limit: maxResults
				);
				return results
					.Results.Select(r => new SearchResult(
						Source: MusicSource.MusicBrainz,
						r.Item.Id.ToString(),
						r.Item.Title ?? "",
						Artist: r.Item.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
						Year: r.Item.Date?.NearestDate.Year,
						Format: r.Item.Media?.FirstOrDefault()?.Format,
						Label: r.Item.LabelInfo?.FirstOrDefault()?.Label?.Name,
						ReleaseType: r.Item.ReleaseGroup?.PrimaryType,
						Score: r.Score,
						Country: r.Item.Country,
						CatalogNumber: r.Item.LabelInfo?.FirstOrDefault()?.CatalogNumber,
						Status: r.Item.Status,
						Disambiguation: r.Item.Disambiguation,
						r.Item.Genres?.Select(g => g.Name)
							.Where(n => n is { })
							.Cast<string>()
							.ToList()
					))
					.ToList();
			},
			ct
		);
	}

	public async Task<SearchResult?> SearchFirstReleaseAsync(
		string? artist = null,
		string? release = null,
		int? year = null,
		string? label = null,
		string? genre = null,
		CancellationToken ct = default
	)
	{
		List<SearchResult> results = await SearchReleasesAsync(
			artist: artist,
			release: release,
			year: year,
			label: label,
			genre: genre,
			maxResults: 1,
			ct
		);
		return results.Count > 0 ? results[index: 0] : null;
	}

	public async Task<List<SearchResult>> SearchArtistsAsync(
		string artist,
		int maxResults = 25,
		CancellationToken ct = default
	)
	{
		return await ExecuteSafeListAsync(
			async () =>
			{
				ISearchResults<ISearchResult<IArtist>> results = await Query.FindArtistsAsync(
					$"artist:\"{artist}\"",
					limit: maxResults
				);
				return results
					.Results.Select(r => new SearchResult(
						Source: MusicSource.MusicBrainz,
						r.Item.Id.ToString(),
						r.Item.Name ?? "",
						Artist: r.Item.Name,
						Year: r.Item.LifeSpan?.Begin?.Year,
						Format: null,
						Label: null,
						ReleaseType: r.Item.Type,
						Score: r.Score,
						Country: r.Item.Country,
						Status: r.Item.Type,
						Disambiguation: r.Item.Disambiguation
					))
					.ToList();
			},
			ct
		);
	}

	public async Task<List<SearchResult>> SearchReleaseGroupsAsync(
		string? artist = null,
		string? releaseGroup = null,
		int maxResults = 25,
		CancellationToken ct = default
	)
	{
		List<string> parts = [];
		if (!IsNullOrWhiteSpace(artist))
			parts.Add($"artist:\"{artist}\"");
		if (!IsNullOrWhiteSpace(releaseGroup))
			parts.Add($"releasegroup:\"{releaseGroup}\"");

		if (parts.Count == 0)
			return [];

		var query = Join(" AND ", parts);

		return await ExecuteSafeListAsync(
			async () =>
			{
				ISearchResults<ISearchResult<IReleaseGroup>> results =
					await Query.FindReleaseGroupsAsync(query, limit: maxResults);
				return results
					.Results.Select(r => new SearchResult(
						Source: MusicSource.MusicBrainz,
						r.Item.Id.ToString(),
						r.Item.Title ?? "",
						Artist: r.Item.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
						Year: r.Item.FirstReleaseDate?.Year,
						Format: null,
						Label: null,
						ReleaseType: r.Item.PrimaryType,
						Score: r.Score,
						Status: r.Item.PrimaryType,
						Disambiguation: r.Item.Disambiguation
					))
					.ToList();
			},
			ct
		);
	}

	internal async Task<List<MusicBrainzRecording>> SearchRecordingsAsync(
		string? artist = null,
		string? recording = null,
		int maxResults = 25,
		CancellationToken ct = default
	)
	{
		List<string> parts = [];
		if (!IsNullOrWhiteSpace(artist))
			parts.Add($"artist:\"{artist}\"");
		if (!IsNullOrWhiteSpace(recording))
			parts.Add($"recording:\"{recording}\"");

		if (parts.Count == 0)
			return [];

		var query = Join(" AND ", parts);

		return await ExecuteSafeListAsync(
			async () =>
			{
				ISearchResults<ISearchResult<IRecording>> results = await Query.FindRecordingsAsync(
					query,
					limit: maxResults
				);
				return results.Results.Select(r => MapRecordingFromSearch(r.Item)).ToList();
			},
			ct
		);
	}

	#endregion

	#region Entity Lookup

	public async Task<ReleaseData> GetReleaseAsync(
		string releaseId,
		bool deepSearch = true,
		int? maxDiscs = null,
		CancellationToken ct = default
	)
	{
		var guid = Guid.Parse(releaseId);
		MusicBrainzRelease release =
			await GetReleaseAsync(guid, ct)
			?? throw new InvalidOperationException($"Release not found: {releaseId}");

		ReleaseCredits credits = ExtractReleaseCredits(release);
		List<TrackInfo> tracks = await BuildTracksAsync(
			release: release,
			credits: credits,
			deepSearch: deepSearch,
			maxDiscs: maxDiscs,
			ct
		);

		TimeSpan totalDuration = tracks
			.Where(t => t.Duration.HasValue)
			.Aggregate(TimeSpan.Zero, (sum, t) => sum + t.Duration!.Value);

		ReleaseInfo info = new(
			Source: MusicSource.MusicBrainz,
			Id: releaseId,
			Title: release.Title,
			Artist: release.Artist,
			Label: release.Labels.FirstOrDefault()?.Name,
			CatalogNumber: release.Labels.FirstOrDefault()?.CatalogNumber,
			Year: release.Date?.Year,
			Notes: release.Annotation,
			DiscCount: release.Media.Count,
			TrackCount: tracks.Count,
			TotalDuration: totalDuration
		);

		ReleaseData data = new(Info: info, Tracks: tracks);

		Logger.AppendJsonLine(
			Logger.GetLogPath(ServiceType.Music),
			new LogEntry(
				Timestamp: DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
				Level: LogLevel.Info.ToString(),
				Event: "ReleaseFetched",
				Data: new Dictionary<string, object> { ["Data"] = data }
			)
		);

		return data;
	}

	internal async Task<MusicBrainzRelease?> GetReleaseAsync(
		Guid releaseId,
		CancellationToken ct = default
	)
	{
		return await ExecuteAndLogAsync(
			async () =>
			{
				IRelease? release = await Query.LookupReleaseAsync(
					mbid: releaseId,
					Include.ArtistCredits
						| Include.Recordings
						| Include.Media
						| Include.Labels
						| Include.ArtistRelationships
						| Include.Annotation
						| Include.Tags
						| Include.Genres
						| Include.ReleaseGroups
				);
				if (release is null)
					return null;

				return MapRelease(release);
			},
			entity: "releases",
			id: releaseId.ToString(),
			ct
		);
	}

	internal async Task<MusicBrainzReleaseGroup?> GetReleaseGroupAsync(
		Guid releaseGroupId,
		CancellationToken ct = default
	)
	{
		return await ExecuteAndLogAsync(
			async () =>
			{
				IReleaseGroup? rg = await Query.LookupReleaseGroupAsync(
					mbid: releaseGroupId,
					Include.ArtistCredits
						| Include.Releases
						| Include.Annotation
						| Include.Ratings
						| Include.Tags
						| Include.Genres
				);
				if (rg is null)
					return null;

				return new MusicBrainzReleaseGroup(
					Id: rg.Id,
					Title: rg.Title ?? "",
					Artist: rg.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
					ArtistCredit: FormatArtistCredit(rg.ArtistCredit),
					PrimaryType: rg.PrimaryType,
					SecondaryTypes: rg.SecondaryTypes?.ToList() ?? [],
					FirstReleaseDate: rg.FirstReleaseDate?.NearestDate is DateTime dt
						? DateOnly.FromDateTime(dt)
						: null,
					ReleaseCount: rg.Releases?.Count ?? 0,
					Disambiguation: rg.Disambiguation,
					Tags: rg.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList()
						?? [],
					Genres: rg.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList()
						?? [],
					Rating: (double?)rg.Rating?.Value,
					RatingVotes: rg.Rating?.VoteCount,
					Annotation: rg.Annotation
				);
			},
			entity: "release-groups",
			id: releaseGroupId.ToString(),
			ct
		);
	}

	internal async Task<MusicBrainzArtist?> GetArtistAsync(
		Guid artistId,
		CancellationToken ct = default
	)
	{
		return await ExecuteAndLogAsync(
			async () =>
			{
				IArtist? artist = await Query.LookupArtistAsync(
					mbid: artistId,
					Include.Aliases
						| Include.Annotation
						| Include.Ratings
						| Include.Tags
						| Include.Genres
				);
				if (artist is null)
					return null;

				return MapArtist(artist);
			},
			entity: "artists",
			id: artistId.ToString(),
			ct
		);
	}

	internal async Task<MusicBrainzRecording?> GetRecordingAsync(
		Guid recordingId,
		CancellationToken ct = default
	)
	{
		return await ExecuteAndLogAsync(
			async () =>
			{
				IRecording? rec = await Query.LookupRecordingAsync(
					mbid: recordingId,
					Include.ArtistCredits
						| Include.Isrcs
						| Include.Annotation
						| Include.Ratings
						| Include.Tags
						| Include.Genres
						| Include.WorkRelationships
						| Include.ArtistRelationships
						| Include.PlaceRelationships
				);
				if (rec is null)
					return null;

				return MapRecording(rec);
			},
			entity: "recordings",
			id: recordingId.ToString(),
			ct
		);
	}

	internal async Task<WorkDetails?> GetWorkDetailsAsync(
		Guid workId,
		CancellationToken ct = default
	)
	{
		return await ExecuteAndLogAsync(
			async () =>
			{
				IWork work = await Query.LookupWorkAsync(
					mbid: workId,
					Include.ArtistRelationships | Include.WorkRelationships
				);
				if (work?.Relationships is null)
					return null;

				string? composerName = null;
				string? parentWorkName = null;

				foreach (IRelationship rel in work.Relationships)
				{
					var relType = rel.Type?.ToLowerInvariant();
					if (relType is null)
						continue;

					if (relType is "composer" or "writer" && rel.Artist is { } artist)
						composerName ??= artist.Name;
					else if (
						relType is "parts"
						&& rel.Direction.IsEqualTo("backward", Ordinal)
						&& rel.Work is { } parentWork
					)
						parentWorkName = parentWork.Title;
				}

				if (composerName is { } || parentWorkName is { })
					Console.Debug(
						message: "Work '{0}' → Composer: {1}, Parent: {2}",
						work.Title,
						composerName ?? "(none)",
						parentWorkName ?? "(none)"
					);

				return new WorkDetails(Composer: composerName, ParentWorkName: parentWorkName);
			},
			entity: "works",
			id: workId.ToString(),
			ct
		);
	}

	public async Task<string?> GetWorkComposerAsync(Guid workId, CancellationToken ct = default)
	{
		WorkDetails? details = await GetWorkDetailsAsync(workId, ct);
		return details?.Composer;
	}

	internal async Task<List<MusicBrainzRecording>> BrowseArtistRecordingsAsync(
		Guid artistId,
		int maxResults = 100,
		CancellationToken ct = default
	)
	{
		return await ExecuteSafeListAsync(
			async () =>
			{
				IBrowseResults<IRecording> results = await Query.BrowseArtistRecordingsAsync(
					mbid: artistId,
					limit: maxResults,
					inc: Include.ArtistCredits | Include.Isrcs
				);
				return results.Results.Select(MapRecordingFromSearch).ToList();
			},
			ct
		);
	}

	#endregion

	#region Track Building & Enrichment

	private static ReleaseCredits ExtractReleaseCredits(MusicBrainzRelease release)
	{
		var credits = release.Credits.Where(c => !ExcludedRoles.Contains(c.Role)).ToList();

		return new ReleaseCredits(
			Conductor: credits.FirstOrDefault(c => ConductorRoles.Contains(c.Role))?.Name,
			Orchestra: credits.FirstOrDefault(c => OrchestraRoles.Contains(c.Role))?.Name,
			Soloists:
			[
				.. credits
					.Where(c => SoloistRoles.Any(r => c.Role.Contains(r)))
					.Select(c => c.Name)
					.Distinct(),
			],
			Composer: release.Artist
		);
	}

	private async Task<List<TrackInfo>> BuildTracksAsync(
		MusicBrainzRelease release,
		ReleaseCredits credits,
		bool deepSearch,
		int? maxDiscs,
		CancellationToken ct
	)
	{
		List<TrackInfo> tracks = [];

		foreach (MusicBrainzMedium medium in release.Media)
		{
			if (maxDiscs.HasValue && medium.Position > maxDiscs.Value)
				break;

			foreach (MusicBrainzTrack track in medium.Tracks)
			{
				int? recordingYear = null;
				string? trackComposer = null;

				if (deepSearch && track.RecordingId.HasValue)
				{
					MusicBrainzRecording? recording = await GetRecordingAsync(
						recordingId: track.RecordingId.Value,
						ct
					);
					if (recording is { })
					{
						recordingYear = recording.FirstReleaseDate?.Year;
						trackComposer = recording.Artist;
					}
				}

				tracks.Add(
					new TrackInfo(
						DiscNumber: medium.Position,
						TrackNumber: track.Position,
						Title: track.Title,
						Duration: track.Length,
						RecordingYear: recordingYear,
						trackComposer ?? credits.Composer,
						WorkName: null,
						Conductor: credits.Conductor,
						Orchestra: credits.Orchestra,
						Soloists: credits.Soloists,
						Artist: release.Artist,
						RecordingVenue: null,
						track.RecordingId?.ToString()
					)
				);
			}
		}

		return tracks;
	}

	public async Task<TrackInfo> EnrichTrackAsync(TrackInfo track, CancellationToken ct = default)
	{
		if (IsNullOrEmpty(track.RecordingId))
			return track;

		if (!Guid.TryParse(track.RecordingId, out Guid recordingId))
			return track;

		MusicBrainzRecording? recording = await GetRecordingAsync(recordingId, ct);
		if (recording is null)
			return track;

		WorkDetails? workDetails = null;
		Guid? workId = recording.WorkId;

		if (workId.HasValue && workId == currentWorkId && currentWorkRecording is { })
		{
			workDetails = currentWorkDetails;
			Console.Debug(
				"[{0}] {1} → Work: {2} (reusing context)",
				track.TrackNumber,
				track.Title,
				recording.WorkName ?? "(none)"
			);
		}
		else
		{
			if (workId.HasValue)
			{
				if (workDetailsCache.TryGetValue(workId.Value, out WorkDetails? cached))
				{
					workDetails = cached;
				}
				else
				{
					workDetails = await GetWorkDetailsAsync(workId.Value, ct);
					if (workDetails is { })
						workDetailsCache[workId.Value] = workDetails;
				}
			}

			UpdateWorkContext(workId, recording, workDetails);

			Console.Debug(
				"[{0}] {1} → Work: {2}, Composer: {3}, Parent: {4}",
				track.TrackNumber,
				track.Title,
				recording.WorkName ?? "(none)",
				workDetails?.Composer ?? "(none)",
				workDetails?.ParentWorkName ?? "(none)"
			);
		}

		var parentWorkName = workDetails?.ParentWorkName;

		TrackInfo enriched = track with
		{
			WorkName = parentWorkName ?? track.WorkName,
			Composer = workDetails?.Composer ?? track.Composer,
			Conductor = recording.Conductor ?? track.Conductor,
			Orchestra = recording.Orchestra ?? track.Orchestra,
			RecordingVenue = recording.RecordingVenue ?? track.RecordingVenue,
			RecordingYear = recording.RecordingDate?.Year ?? track.RecordingYear,
		};

		List<string> missingFields = enriched.GetMissingFields();

		if (missingFields.Count > 0)
			Console.Warning(
				"[{0}.{1:D2}] {2} → Missing: {3}",
				track.DiscNumber,
				track.TrackNumber,
				track.Title,
				Join(", ", missingFields)
			);

		LanguageDetector.LogNonLatinScript(
			track.DiscNumber,
			track.TrackNumber,
			enriched.WorkName,
			enriched.Composer,
			enriched.Conductor,
			enriched.Orchestra
		);

		return enriched;
	}

	#endregion

	#region Entity Mappers

	private static MusicBrainzRelease MapRelease(IRelease r)
	{
		List<MusicBrainzMedium> media = [];
		if (r.Media is { } mediaList)
			foreach (IMedium medium in mediaList)
			{
				List<MusicBrainzTrack> tracks = [];
				if (medium.Tracks is { } trackList)
					foreach (ITrack track in trackList)
						tracks.Add(
							new MusicBrainzTrack(
								Id: track.Id,
								track.Title ?? track.Recording?.Title ?? "",
								track.Position ?? 0,
								Number: track.Number,
								Length: track.Length,
								RecordingId: track.Recording?.Id,
								FormatArtistCredit(track.ArtistCredit)
							)
						);

				media.Add(
					new MusicBrainzMedium(
						Position: medium.Position,
						Format: medium.Format,
						Title: medium.Title,
						TrackCount: medium.TrackCount,
						Tracks: tracks
					)
				);
			}

		List<MusicBrainzCredit> credits = [];
		if (r.Relationships is { } relationships)
			foreach (IRelationship rel in relationships)
				if (rel.Artist is { } artist && !IsNullOrEmpty(rel.Type))
					credits.Add(
						new MusicBrainzCredit(
							artist.Name ?? "",
							Role: rel.Type,
							ArtistId: artist.Id,
							rel.Attributes is { } attrs
								? Join(separator: ", ", values: attrs)
								: null
						)
					);

		List<MusicBrainzLabel> labels = [];
		if (r.LabelInfo is { } labelInfo)
			foreach (ILabelInfo li in labelInfo)
				labels.Add(
					new MusicBrainzLabel(
						Id: li.Label?.Id,
						Name: li.Label?.Name,
						CatalogNumber: li.CatalogNumber
					)
				);

		return new MusicBrainzRelease(
			Id: r.Id,
			r.Title ?? "",
			Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
			FormatArtistCredit(r.ArtistCredit),
			r.Date?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dt) : null,
			Country: r.Country,
			Status: r.Status,
			Barcode: r.Barcode,
			Asin: r.Asin,
			Quality: r.Quality,
			Packaging: r.Packaging,
			Disambiguation: r.Disambiguation,
			ReleaseGroupId: r.ReleaseGroup?.Id,
			ReleaseGroupTitle: r.ReleaseGroup?.Title,
			ReleaseGroupType: r.ReleaseGroup?.PrimaryType,
			Media: media,
			Credits: credits,
			Labels: labels,
			r.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
			r.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
			Annotation: r.Annotation
		);
	}

	private static MusicBrainzArtist MapArtist(IArtist a) =>
		new(
			Id: a.Id,
			a.Name ?? "",
			SortName: a.SortName,
			Type: a.Type,
			Gender: a.Gender,
			Country: a.Country,
			Area: a.Area?.Name,
			Disambiguation: a.Disambiguation,
			a.LifeSpan?.Begin?.NearestDate is DateTime b
				? DateOnly.FromDateTime(b)
				: null,
			a.LifeSpan?.End?.NearestDate is DateTime e ? DateOnly.FromDateTime(e) : null,
			Ended: a.LifeSpan?.Ended,
			a.Aliases?.Select(al => al.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
			a.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
			a.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
			Annotation: a.Annotation,
			(double?)a.Rating?.Value,
			RatingVotes: a.Rating?.VoteCount
		);

	private static MusicBrainzRecording MapRecording(IRecording r)
	{
		IRelationship? workRelationship = r.Relationships?.FirstOrDefault(rel => rel.Work is { });
		var workName = workRelationship?.Work?.Title;
		Guid? workId = workRelationship?.Work?.Id;

		string? conductor = null;
		string? orchestra = null;
		string? recordingVenue = null;
		DateOnly? recordingDate = null;

		if (r.Relationships is { } relationships)
			foreach (IRelationship rel in relationships)
			{
				var relType = rel.Type?.ToLowerInvariant();
				if (relType is null)
					continue;

				if (relType.IsEqualTo("conductor", Ordinal) && rel.Artist is { } conductorArtist)
				{
					conductor = conductorArtist.Name;
					if (recordingDate is null && rel.Begin?.NearestDate is DateTime beginDate)
						recordingDate = DateOnly.FromDateTime(beginDate);
				}
				else if (
					(
						relType
							is "orchestra"
								or "performing orchestra"
								or "ensemble"
								or "choir"
								or "philharmonic"
						|| (
							relType.IsEqualTo("instrument", Ordinal)
							&& rel.Artist?.Name is { } name
							&& (
								name.Has("Orchestra")
								|| name.Has("Philharmonic")
								|| name.Has("Symphony")
								|| name.Has("Choir")
							)
						)
					) && rel.Artist is { } orchestraArtist
				)
				{
					orchestra = orchestraArtist.Name;
				}
				else if (relType is "recorded at" or "recorded in" && rel.Place is { } place)
				{
					recordingVenue = place.Name;
					if (recordingDate is null && rel.Begin?.NearestDate is DateTime beginDate)
						recordingDate = DateOnly.FromDateTime(beginDate);
				}
			}

		return new MusicBrainzRecording(
			Id: r.Id,
			r.Title ?? "",
			Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
			FormatArtistCredit(r.ArtistCredit),
			Length: r.Length,
			r.FirstReleaseDate?.NearestDate is DateTime dt
				? DateOnly.FromDateTime(dt)
				: null,
			IsVideo: r.Video,
			Disambiguation: r.Disambiguation,
			r.Isrcs?.ToList() ?? [],
			r.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
			r.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
			(double?)r.Rating?.Value,
			RatingVotes: r.Rating?.VoteCount,
			Annotation: r.Annotation,
			WorkName: workName,
			WorkId: workId,
			Conductor: conductor,
			Orchestra: orchestra,
			RecordingVenue: recordingVenue,
			RecordingDate: recordingDate
		);
	}

	private static MusicBrainzRecording MapRecordingFromSearch(IRecording r) =>
		new(
			Id: r.Id,
			r.Title ?? "",
			Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
			FormatArtistCredit(r.ArtistCredit),
			Length: r.Length,
			r.FirstReleaseDate?.NearestDate is DateTime dt
				? DateOnly.FromDateTime(dt)
				: null,
			IsVideo: r.Video,
			Disambiguation: r.Disambiguation,
			r.Isrcs?.ToList() ?? [],
			[],
			[],
			Rating: null,
			RatingVotes: null,
			Annotation: null
		);

	#endregion

	#region Role Filters & Utilities

	private static readonly FrozenSet<string> ExcludedRoles = FrozenSet.ToFrozenSet(
		[
			"choir",
			"chorus",
			"chorus master",
			"choir conductor",
			"choir director",
			"vocal",
			"vocals",
			"singer",
			"soprano",
			"mezzo-soprano",
			"alto",
			"contralto",
			"tenor",
			"baritone",
			"bass",
			"bass-baritone",
			"narrator",
			"speaker",
		],
		comparer: StringComparer.OrdinalIgnoreCase
	);

	private static readonly FrozenSet<string> ConductorRoles = FrozenSet.ToFrozenSet(
		["conductor", "director"],
		comparer: StringComparer.OrdinalIgnoreCase
	);

	private static readonly FrozenSet<string> OrchestraRoles = FrozenSet.ToFrozenSet(
		["orchestra", "performing orchestra", "ensemble", "performer", "choir", "philharmonic"],
		comparer: StringComparer.OrdinalIgnoreCase
	);

	private static readonly FrozenSet<string> SoloistRoles = FrozenSet.ToFrozenSet(
		[
			"instrument",
			"piano",
			"violin",
			"viola",
			"cello",
			"double bass",
			"flute",
			"oboe",
			"clarinet",
			"bassoon",
			"horn",
			"trumpet",
			"trombone",
			"tuba",
			"harp",
			"organ",
			"harpsichord",
			"guitar",
			"percussion",
			"timpani",
			"soloist",
		],
		comparer: StringComparer.OrdinalIgnoreCase
	);

	private static string? FormatArtistCredit(IReadOnlyList<INameCredit>? credits)
	{
		if (credits is null || credits.Count == 0)
			return null;

		return Join(
			"",
			credits.Select(c => (c.Name ?? c.Artist?.Name ?? "") + (c.JoinPhrase ?? ""))
		);
	}

	private static Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct) =>
		Resilience.ExecuteAsync("MusicBrainz", action, ct);

	private static Task<List<T>> ExecuteSafeListAsync<T>(
		Func<Task<List<T>>> action,
		CancellationToken ct = default
	) => ExecuteAsync(action, ct);

	private static string BuildQuery(
		string? artist,
		string? release,
		int? year,
		string? label,
		string? genre
	)
	{
		List<string> parts = [];
		if (!IsNullOrWhiteSpace(artist))
			parts.Add($"artist:\"{artist}\"");
		if (!IsNullOrWhiteSpace(release))
			parts.Add($"release:\"{release}\"");
		if (!IsNullOrWhiteSpace(label))
			parts.Add($"label:\"{label}\"");
		if (!IsNullOrWhiteSpace(genre))
			parts.Add($"tag:\"{genre}\"");
		if (year.HasValue)
			parts.Add($"date:{year}");
		return Join(" AND ", parts);
	}

	#endregion
}

internal record ReleaseCredits(
	string? Conductor,
	string? Orchestra,
	List<string> Soloists,
	string? Composer
);
