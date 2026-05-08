using System.Collections.Concurrent;
using MetaBrainz.Common;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Browses;
using MetaBrainz.MusicBrainz.Interfaces.Searches;

namespace CSharpScripts.Services.Music;

internal sealed class MusicBrainzService(
	string appName = "LancesUtilities",
	string appVersion = "1.0",
	string contact = "user@example.com"
) : IMusicService
{
	private static readonly Lock TraceLock = new();

	private readonly ConcurrentDictionary<Guid, WorkDetails> WorkDetailsCache = new();
	private WorkDetails? CurrentWorkDetails;
	private Guid? CurrentWorkId;
	private MusicBrainzRecording? CurrentWorkRecording;
	internal Query Query { get; } =
		new(application: appName, version: appVersion, contact: contact);
	public MusicSource Source => MusicSource.MusicBrainz;

	public async Task<List<SearchResult>> SearchAsync(
		string query,
		int maxResults = 10,
		CancellationToken ct = default
	)
	{
		if (query.Contains(value: "artist:") || query.Contains(value: "release:"))
			return await SearchReleasesAsync(
				artist: null,
				release: query,
				year: null,
				label: null,
				genre: null,
				maxResults: maxResults,
				ct
			);

		return await SearchReleasesAsync(
			artist: null,
			release: query,
			year: null,
			label: null,
			genre: null,
			maxResults: maxResults,
			ct
		);
	}

	public async Task<ReleaseData> GetReleaseAsync(
		string releaseId,
		int? maxDiscs = null,
		CancellationToken ct = default
	)
	{
		var guid = Guid.Parse(input: releaseId);
		MusicBrainzRelease release =
			await GetReleaseAsync(releaseId: guid, ct)
			?? throw new InvalidOperationException($"Release not found: {releaseId}");

		ReleaseCredits credits = ExtractReleaseCredits(release: release);
		List<TrackInfo> tracks = await BuildTracksAsync(
			release: release,
			credits: credits,
			maxDiscs: maxDiscs,
			ct
		);

		TimeSpan totalDuration = TimeSpan.Zero;
		foreach (TrackInfo t in tracks)
		{
			if (t.Duration.HasValue)
				totalDuration += t.Duration.Value;
		}

		MusicBrainzLabel? firstLabel = Enumerable.FirstOrDefault(release.Labels);
		ReleaseInfo info = new(
			Source: MusicSource.MusicBrainz,
			Id: releaseId,
			Title: release.Title,
			Artist: release.Artist,
			Label: firstLabel?.Name,
			CatalogNumber: firstLabel?.CatalogNumber,
			Year: release.Date?.Year,
			Notes: release.Annotation,
			DiscCount: release.Media.Count,
			TrackCount: tracks.Count,
			TotalDuration: totalDuration
		);

		ReleaseData data = new(Info: info, Tracks: tracks);

		Log.Information("MusicBrainzReleaseFetched {@Data}", data);

		return data;
	}

	private static string GetEntityDumpDirectory(string entity, string id) =>
		Path.Combine(path1: Paths.DumpsDirectory, path2: entity, path3: id);

	private static async Task<T?> ExecuteAndLogAsync<T>(
		Func<Task<T?>> action,
		string entity,
		string id,
		CancellationToken ct
	)
		where T : class
	{
		var dir = GetEntityDumpDirectory(entity: entity, id: id);
		Directory.CreateDirectory(path: dir);

		var tracePath = Path.Combine(path1: dir, path2: "http.jsonl");
		using TextWriterTraceListener listener = new(fileName: tracePath);

		lock (TraceLock)
		{
			HttpUtils.TraceSource.Listeners.Add(listener: listener);
			HttpUtils.TraceSource.Switch.Level = SourceLevels.All;
		}

		try
		{
			T? result = await Resilience.ExecuteMusicApiAsync(
				service: "MusicBrainz",
				action: action,
				ct
			);

			if (result is not null)
			{
				var json = JsonSerializer.Serialize(
					value: result,
					options: StateManager.JsonIndented
				);
				await File.WriteAllTextAsync(
					Path.Combine(path1: dir, path2: "data.json"),
					contents: json,
					ct
				);
			}

			return result;
		}
		finally
		{
			lock (TraceLock)
			{
				listener.Flush();
				HttpUtils.TraceSource.Listeners.Remove(listener: listener);
			}
		}
	}

	public void ClearCache()
	{
		WorkDetailsCache.Clear();
		CurrentWorkId = null;
		CurrentWorkRecording = null;
		CurrentWorkDetails = null;
	}

	private void UpdateWorkContext(
		Guid? workId,
		MusicBrainzRecording recording,
		WorkDetails? details
	)
	{
		CurrentWorkId = workId;
		CurrentWorkRecording = recording;
		CurrentWorkDetails = details;
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
		Log.Debug("SearchReleasesAsync entry {Artist} {Release} {Year}", artist, release, year);
		var query = BuildQuery(
			artist: artist,
			release: release,
			year: year,
			label: label,
			genre: genre
		);
		if (IsNullOrEmpty(value: query))
		{
			Log.Debug("SearchReleasesAsync exit 0 (empty query)");
			return [];
		}

		return await Resilience.ExecuteAsync(
			operation: "MusicBrainz",
			async () =>
			{
				ISearchResults<ISearchResult<IRelease>> results = await Query.FindReleasesAsync(
					query: query,
					limit: maxResults
				);
				var result = Enumerable.ToList(
					Enumerable.Select(
						results.Results,
						r =>
						{
							INameCredit? firstArtistCredit = r.Item.ArtistCredit?.FirstOrDefault();
							IMedium? firstMedia = r.Item.Media?.FirstOrDefault();
							ILabelInfo? firstLabelInfo = r.Item.LabelInfo?.FirstOrDefault();

							return new SearchResult(
								Source: MusicSource.MusicBrainz,
								r.Item.Id.ToString(),
								r.Item.Title ?? "",
								Artist: firstArtistCredit?.Artist?.Name,
								Year: r.Item.Date?.NearestDate.Year,
								Format: firstMedia?.Format,
								Label: firstLabelInfo?.Label?.Name,
								ReleaseType: r.Item.ReleaseGroup?.PrimaryType,
								Score: r.Score,
								Country: r.Item.Country,
								CatalogNumber: firstLabelInfo?.CatalogNumber,
								Status: r.Item.Status,
								Disambiguation: r.Item.Disambiguation,
								Genres: r.Item.Genres is { } genres
									? [.. genres.Select(g => g.Name).OfType<string>()]
									: []
							);
						}
					)
				);
				Log.Debug("SearchReleasesAsync exit {Count}", result.Count);
				return result;
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
		Log.Debug("SearchFirstReleaseAsync entry {Artist} {Release}", artist, release);
		List<SearchResult> results = await SearchReleasesAsync(
			artist: artist,
			release: release,
			year: year,
			label: label,
			genre: genre,
			maxResults: 1,
			ct
		);
		SearchResult? result = results.Count > 0 ? results[index: 0] : null;
		Log.Debug("SearchFirstReleaseAsync exit {Found}", result is not null);
		return result;
	}

	public async Task<List<SearchResult>> SearchArtistsAsync(
		string artist,
		int maxResults = 25,
		CancellationToken ct = default
	)
	{
		Log.Debug("SearchArtistsAsync entry {Artist}", artist);
		return await Resilience.ExecuteAsync(
			operation: "MusicBrainz",
			async () =>
			{
				ISearchResults<ISearchResult<IArtist>> results = await Query.FindArtistsAsync(
					$"artist:\"{artist}\"",
					limit: maxResults
				);
				var result = Enumerable.ToList(
					Enumerable.Select(
						results.Results,
						r => new SearchResult(
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
						)
					)
				);
				Log.Debug("SearchArtistsAsync exit {Count}", result.Count);
				return result;
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
		if (!IsNullOrWhiteSpace(value: artist))
			parts.Add($"artist:\"{artist}\"");
		if (!IsNullOrWhiteSpace(value: releaseGroup))
			parts.Add($"releasegroup:\"{releaseGroup}\"");

		if (parts.Count == 0)
			return [];

		var query = Join(separator: " AND ", values: parts);

		return await ExecuteSafeListAsync(
			async () =>
			{
				ISearchResults<ISearchResult<IReleaseGroup>> results =
					await Query.FindReleaseGroupsAsync(query: query, limit: maxResults);
				return Enumerable.ToList(
					Enumerable.Select(
						results.Results,
						r => new SearchResult(
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
						)
					)
				);
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
		Log.Debug("SearchRecordingsAsync entry {Artist} {Recording}", artist, recording);
		List<string> parts = [];
		if (!IsNullOrWhiteSpace(value: artist))
			parts.Add($"artist:\"{artist}\"");
		if (!IsNullOrWhiteSpace(value: recording))
			parts.Add($"recording:\"{recording}\"");

		if (parts.Count == 0)
			return [];

		var query = Join(separator: " AND ", values: parts);

		return await ExecuteSafeListAsync(
			async () =>
			{
				ISearchResults<ISearchResult<IRecording>> results = await Query.FindRecordingsAsync(
					query: query,
					limit: maxResults
				);
				var result = Enumerable.ToList(
					Enumerable.Select(
						results.Results,
						r => MusicBrainzMapper.MapRecordingFromSearch(r: r.Item)
					)
				);
				Log.Debug("SearchRecordingsAsync exit {Count}", result.Count);
				return result;
			},
			ct
		);
	}

	private async Task<MusicBrainzRelease?> GetReleaseAsync(
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

				return MusicBrainzMapper.MapRelease(r: release);
			},
			entity: "releases",
			releaseId.ToString(),
			ct
		);
	}

	internal async Task<MusicBrainzReleaseGroup?> GetReleaseGroupAsync(
		Guid releaseGroupId,
		CancellationToken ct = default
	)
	{
		Log.Debug("GetReleaseGroupAsync entry {ReleaseGroupId}", releaseGroupId);
		MusicBrainzReleaseGroup? result = await ExecuteAndLogAsync(
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
					rg.Title ?? "",
					Artist: rg.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
					MusicBrainzMapper.FormatArtistCredit(credits: rg.ArtistCredit),
					PrimaryType: rg.PrimaryType,
					rg.SecondaryTypes?.ToList() ?? [],
					rg.FirstReleaseDate?.NearestDate is DateTime dt
						? DateOnly.FromDateTime(dateTime: dt)
						: null,
					rg.Releases?.Count ?? 0,
					Disambiguation: rg.Disambiguation,
					rg.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
					rg.Genres?.Select(g => g.Name ?? "").Where(n => n.Length > 0).ToList() ?? [],
					(double?)rg.Rating?.Value,
					RatingVotes: rg.Rating?.VoteCount,
					Annotation: rg.Annotation
				);
			},
			entity: "release-groups",
			releaseGroupId.ToString(),
			ct
		);
		Log.Debug("GetReleaseGroupAsync exit {Found}", result is not null);
		return result;
	}

	internal async Task<MusicBrainzArtist?> GetArtistAsync(
		Guid artistId,
		CancellationToken ct = default
	)
	{
		Log.Debug("GetArtistAsync entry {ArtistId}", artistId);
		MusicBrainzArtist? result = await ExecuteAndLogAsync(
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

				return MusicBrainzMapper.MapArtist(a: artist);
			},
			entity: "artists",
			artistId.ToString(),
			ct
		);
		Log.Debug("GetArtistAsync exit {Found}", result is not null);
		return result;
	}

	private async Task<MusicBrainzRecording?> GetRecordingAsync(
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

				return MusicBrainzMapper.MapRecording(r: rec);
			},
			entity: "recordings",
			recordingId.ToString(),
			ct
		);
	}

	private async Task<WorkDetails?> GetWorkDetailsAsync(
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
						&& rel.Direction.EqualsIgnoreCase("backward", Ordinal)
						&& rel.Work is { } parentWork
					)
						parentWorkName = parentWork.Title;
				}

				if (composerName is not null || parentWorkName is not null)
				{
					Log.Debug(
						"Work '{0}' → Composer: {1}, Parent: {2}",
						work.Title,
						composerName ?? "(none)",
						parentWorkName ?? "(none)"
					);
				}

				return new WorkDetails(Composer: composerName, ParentWorkName: parentWorkName);
			},
			entity: "works",
			workId.ToString(),
			ct
		);
	}

	public async Task<string?> GetWorkComposerAsync(Guid workId, CancellationToken ct = default)
	{
		WorkDetails? details = await GetWorkDetailsAsync(workId: workId, ct);
		return details?.Composer;
	}

	internal async Task<List<MusicBrainzRecording>> BrowseArtistRecordingsAsync(
		Guid artistId,
		int maxResults = 100,
		CancellationToken ct = default
	)
	{
		Log.Debug(
			"BrowseArtistRecordingsAsync entry {ArtistId} {MaxResults}",
			artistId,
			maxResults
		);
		return await Resilience.ExecuteAsync(
			operation: "MusicBrainz",
			async () =>
			{
				IBrowseResults<IRecording> results = await Query.BrowseArtistRecordingsAsync(
					mbid: artistId,
					limit: maxResults,
					inc: Include.ArtistCredits | Include.Isrcs
				);
				var result = Enumerable.ToList(
					Enumerable.Select(
						results.Results,
						selector: MusicBrainzMapper.MapRecordingFromSearch
					)
				);
				Log.Debug("BrowseArtistRecordingsAsync exit {Count}", result.Count);
				return result;
			},
			ct
		);
	}

	private static ReleaseCredits ExtractReleaseCredits(MusicBrainzRelease release)
	{
		var credits = Enumerable.ToList(
			Enumerable.Where(
				release.Credits,
				c => !MusicBrainzMapper.ExcludedRoles.Contains(item: c.Role)
			)
		);

		return new ReleaseCredits(
			Conductor: Enumerable
				.FirstOrDefault(
					credits,
					c => MusicBrainzMapper.ConductorRoles.Contains(item: c.Role)
				)
				?.Name,
			Orchestra: Enumerable
				.FirstOrDefault(
					credits,
					c => MusicBrainzMapper.OrchestraRoles.Contains(item: c.Role)
				)
				?.Name,
			[
				.. Enumerable.Distinct(
					Enumerable.Select(
						Enumerable.Where(
							credits,
							c =>
								Enumerable.Any(
									MusicBrainzMapper.SoloistRoles,
									r => c.Role.Contains(value: r)
								)
						),
						c => c.Name
					)
				),
			],
			Composer: release.Artist
		);
	}

	private async Task<List<TrackInfo>> BuildTracksAsync(
		MusicBrainzRelease release,
		ReleaseCredits credits,
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

				if (track.RecordingId.HasValue)
				{
					MusicBrainzRecording? recording = await GetRecordingAsync(
						recordingId: track.RecordingId.Value,
						ct
					);
					if (recording is not null)
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
		Log.Debug("EnrichTrackAsync entry {Title} {RecordingId}", track.Title, track.RecordingId);
		if (IsNullOrEmpty(value: track.RecordingId))
		{
			Log.Debug("EnrichTrackAsync exit (no recording ID)");
			return track;
		}

		if (!Guid.TryParse(input: track.RecordingId, out Guid recordingId))
		{
			Log.Debug("EnrichTrackAsync exit (invalid recording ID)");
			return track;
		}

		MusicBrainzRecording? recording = await GetRecordingAsync(recordingId: recordingId, ct);
		if (recording is null)
			return track;

		WorkDetails? workDetails = null;
		Guid? workId = recording.WorkId;

		if (workId.HasValue && workId == CurrentWorkId && CurrentWorkRecording is not null)
		{
			workDetails = CurrentWorkDetails;
			Log.Debug(
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
				if (WorkDetailsCache.TryGetValue(key: workId.Value, out WorkDetails? cached))
					workDetails = cached;
				else
				{
					workDetails = await GetWorkDetailsAsync(workId: workId.Value, ct);
					if (workDetails is not null)
						WorkDetailsCache.TryAdd(key: workId.Value, value: workDetails);
				}
			}

			UpdateWorkContext(workId: workId, recording: recording, details: workDetails);

			Log.Debug(
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
		{
			Log.Warning(
				"[{0}.{1:D2}] {2} → Missing: {3}",
				track.DiscNumber,
				track.TrackNumber,
				track.Title,
				Join(separator: ", ", values: missingFields)
			);
		}

		Log.Debug("EnrichTrackAsync exit");
		return enriched;
	}

	private static Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct) =>
		Resilience.ExecuteMusicApiAsync(service: "MusicBrainz", action: action, ct);

	private static Task<List<T>> ExecuteSafeListAsync<T>(
		Func<Task<List<T>>> action,
		CancellationToken ct = default
	) => ExecuteAsync(action: action, ct);

	private static string BuildQuery(
		string? artist,
		string? release,
		int? year,
		string? label,
		string? genre
	)
	{
		List<string> parts = [];
		if (!IsNullOrWhiteSpace(value: artist))
			parts.Add($"artist:\"{artist}\"");
		if (!IsNullOrWhiteSpace(value: release))
			parts.Add($"release:\"{release}\"");
		if (!IsNullOrWhiteSpace(value: label))
			parts.Add($"label:\"{label}\"");
		if (!IsNullOrWhiteSpace(value: genre))
			parts.Add($"tag:\"{genre}\"");
		if (year.HasValue)
			parts.Add($"date:{year}");
		return Join(separator: " AND ", values: parts);
	}
}
