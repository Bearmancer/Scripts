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
	internal Query Query { get; } = new(appName, appVersion, contact);
	public MusicSource Source => MusicSource.MusicBrainz;

	private static readonly Lock TraceLock = new();

	private static string GetEntityDumpDirectory(string entity, string id) =>
		Path.Combine(Paths.DumpsDirectory, entity, id);

	private static async Task<T?> ExecuteAndLogAsync<T>(
		Func<Task<T?>> action,
		string entity,
		string id,
		CancellationToken ct
	)
		where T : class
	{
		var dir = GetEntityDumpDirectory(entity, id);
		Directory.CreateDirectory(dir);

		var tracePath = Path.Combine(dir, "http.log");
		using TextWriterTraceListener listener = new(tracePath);

		lock (TraceLock)
		{
			HttpUtils.TraceSource.Listeners.Add(listener);
			HttpUtils.TraceSource.Switch.Level = SourceLevels.All;
		}

		try
		{
			T? result = await Resilience.ExecuteMusicApiAsync("MusicBrainz", action, ct);

			if (result is not null)
			{
				var json = JsonSerializer.Serialize(result, StateManager.JsonIndented);
				await File.WriteAllTextAsync(Path.Combine(dir, "data.json"), json, ct);
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

	private readonly ConcurrentDictionary<Guid, WorkDetails> WorkDetailsCache = new();
	private Guid? CurrentWorkId;
	private MusicBrainzRecording? CurrentWorkRecording;
	private WorkDetails? CurrentWorkDetails;

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
		Log.Debug("SearchReleasesAsync entry {Artist} {Release} {Year}", artist, release, year);
		var query = BuildQuery(
			artist: artist,
			release: release,
			year: year,
			label: label,
			genre: genre
		);
		if (IsNullOrEmpty(query))
		{
			Log.Debug("SearchReleasesAsync exit 0 (empty query)");
			return [];
		}

		return await Resilience.ExecuteAsync(
			"MusicBrainz",
			async () =>
			{
				ISearchResults<ISearchResult<IRelease>> results = await Query.FindReleasesAsync(
					query: query,
					limit: maxResults
				);
				var result = results
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
							.Where(n => n is not null)
							.Cast<string>()
							.ToList()
					))
					.ToList();
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
			"MusicBrainz",
			async () =>
			{
				ISearchResults<ISearchResult<IArtist>> results = await Query.FindArtistsAsync(
					$"artist:\"{artist}\"",
					limit: maxResults
				);
				var result = results
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
		Log.Debug("SearchRecordingsAsync entry {Artist} {Recording}", artist, recording);
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
				var result = results
					.Results.Select(r => MusicBrainzMapper.MapRecordingFromSearch(r.Item))
					.ToList();
				Log.Debug("SearchRecordingsAsync exit {Count}", result.Count);
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
		var guid = Guid.Parse(releaseId);
		MusicBrainzRelease release =
			await GetReleaseAsync(guid, ct)
			?? throw new InvalidOperationException($"Release not found: {releaseId}");

		ReleaseCredits credits = ExtractReleaseCredits(release);
		List<TrackInfo> tracks = await BuildTracksAsync(
			release: release,
			credits: credits,
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

		Log.Information("MusicBrainzReleaseFetched {@Data}", data);

		return data;
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

				return MusicBrainzMapper.MapRelease(release);
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
		Log.Debug("GetReleaseGroupAsync entry {ReleaseGroupId}", releaseGroupId);
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
						ArtistCredit: MusicBrainzMapper.FormatArtistCredit(rg.ArtistCredit),
						PrimaryType: rg.PrimaryType,
						SecondaryTypes: rg.SecondaryTypes?.ToList() ?? [],
						FirstReleaseDate: rg.FirstReleaseDate?.NearestDate is DateTime dt
							? DateOnly.FromDateTime(dt)
							: null,
						ReleaseCount: rg.Releases?.Count ?? 0,
						Disambiguation: rg.Disambiguation,
						Tags: rg.Tags?.Select(t => t.Name ?? "").Where(n => n.Length > 0).ToList()
							?? [],
						Genres: rg.Genres?.Select(g => g.Name ?? "")
							.Where(n => n.Length > 0)
							.ToList()
							?? [],
						Rating: (double?)rg.Rating?.Value,
						RatingVotes: rg.Rating?.VoteCount,
						Annotation: rg.Annotation
					);
				},
				entity: "release-groups",
				id: releaseGroupId.ToString(),
				ct
			)
			.ContinueWith(
				t =>
				{
					Log.Debug("GetReleaseGroupAsync exit {Found}", t.Result is not null);
					return t.Result;
				},
				ct,
				TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default
			);
	}

	internal async Task<MusicBrainzArtist?> GetArtistAsync(
		Guid artistId,
		CancellationToken ct = default
	)
	{
		Log.Debug("GetArtistAsync entry {ArtistId}", artistId);
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

					return MusicBrainzMapper.MapArtist(artist);
				},
				entity: "artists",
				id: artistId.ToString(),
				ct
			)
			.ContinueWith(
				t =>
				{
					Log.Debug("GetArtistAsync exit {Found}", t.Result is not null);
					return t.Result;
				},
				ct,
				TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default
			);
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

				return MusicBrainzMapper.MapRecording(rec);
			},
			entity: "recordings",
			id: recordingId.ToString(),
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
						&& rel.Direction.EqualsExact("backward")
						&& rel.Work is { } parentWork
					)
						parentWorkName = parentWork.Title;
				}

				if (composerName is not null || parentWorkName is not null)
					Log.Debug(
						"Work '{0}' → Composer: {1}, Parent: {2}",
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
		Log.Debug(
			"BrowseArtistRecordingsAsync entry {ArtistId} {MaxResults}",
			artistId,
			maxResults
		);
		return await Resilience.ExecuteAsync(
			"MusicBrainz",
			async () =>
			{
				IBrowseResults<IRecording> results = await Query.BrowseArtistRecordingsAsync(
					mbid: artistId,
					limit: maxResults,
					inc: Include.ArtistCredits | Include.Isrcs
				);
				var result = results
					.Results.Select(MusicBrainzMapper.MapRecordingFromSearch)
					.ToList();
				Log.Debug("BrowseArtistRecordingsAsync exit {Count}", result.Count);
				return result;
			},
			ct
		);
	}

	private static ReleaseCredits ExtractReleaseCredits(MusicBrainzRelease release)
	{
		var credits = release
			.Credits.Where(c => !MusicBrainzMapper.ExcludedRoles.Contains(c.Role))
			.ToList();

		return new ReleaseCredits(
			Conductor: credits
				.FirstOrDefault(c => MusicBrainzMapper.ConductorRoles.Contains(c.Role))
				?.Name,
			Orchestra: credits
				.FirstOrDefault(c => MusicBrainzMapper.OrchestraRoles.Contains(c.Role))
				?.Name,
			Soloists:
			[
				.. credits
					.Where(c => MusicBrainzMapper.SoloistRoles.Any(r => c.Role.Contains(r)))
					.Select(c => c.Name)
					.Distinct(),
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
		if (IsNullOrEmpty(track.RecordingId))
		{
			Log.Debug("EnrichTrackAsync exit (no recording ID)");
			return track;
		}

		if (!Guid.TryParse(track.RecordingId, out Guid recordingId))
		{
			Log.Debug("EnrichTrackAsync exit (invalid recording ID)");
			return track;
		}

		MusicBrainzRecording? recording = await GetRecordingAsync(recordingId, ct);
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
				if (WorkDetailsCache.TryGetValue(workId.Value, out WorkDetails? cached))
				{
					workDetails = cached;
				}
				else
				{
					workDetails = await GetWorkDetailsAsync(workId.Value, ct);
					if (workDetails is not null)
						WorkDetailsCache.TryAdd(workId.Value, workDetails);
				}
			}

			UpdateWorkContext(workId, recording, workDetails);

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
			Log.Warning(
				"[{0}.{1:D2}] {2} → Missing: {3}",
				track.DiscNumber,
				track.TrackNumber,
				track.Title,
				Join(", ", missingFields)
			);

		Log.Debug("EnrichTrackAsync exit");
		return enriched;
	}

	private static Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct) =>
		Resilience.ExecuteMusicApiAsync("MusicBrainz", action, ct);

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
}
