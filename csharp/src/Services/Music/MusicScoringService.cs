using CSharpScripts.CLI.Music;

namespace CSharpScripts.Services.Music;

internal sealed class MusicScoringService
{
	private const int SubstringBonusTitleMatch = 30;
	private const int SubstringBonusArtistMatch = 20;
	private const int PerfectScore = 100;
	private const int MinimumScore = 1;

	internal static int CalculateRelevanceScore(string query, SearchResult result)
	{
		if (query.EqualsIgnoreCase(result.Title))
			return PerfectScore;

		if (result.Artist is not null)
		{
			var artistLen = result.Artist.Length;
			var titleLen = result.Title.Length;
			if (
				query.Length == artistLen + 1 + titleLen
				&& query.AsSpan(0, artistLen).Equals(result.Artist.AsSpan(), OrdinalIgnoreCase)
				&& query[artistLen] == ' '
				&& query.AsSpan(artistLen + 1).Equals(result.Title.AsSpan(), OrdinalIgnoreCase)
			)
				return PerfectScore;
		}

		ReadOnlySpan<char> querySpan = query.AsSpan();
		ReadOnlySpan<char> titleSpan = result.Title.AsSpan();

		Span<Range> queryRanges = stackalloc Range[querySpan.Count(' ') + 1];
		var queryTermCount = MemoryExtensions.Split(
			querySpan,
			queryRanges,
			' ',
			StringSplitOptions.RemoveEmptyEntries
		);

		Span<Range> titleRanges = stackalloc Range[titleSpan.Count(' ') + 1];
		var titleTermCount = MemoryExtensions.Split(
			titleSpan,
			titleRanges,
			' ',
			StringSplitOptions.RemoveEmptyEntries
		);

		ReadOnlySpan<char> artistSpan = (result.Artist ?? "").AsSpan();
		Span<Range> artistRanges = stackalloc Range[artistSpan.Count(' ') + 1];
		var artistTermCount = result.Artist is not null
			? MemoryExtensions.Split(
				artistSpan,
				artistRanges,
				' ',
				StringSplitOptions.RemoveEmptyEntries
			)
			: 0;

		var matchingTerms = 0;
		for (var qi = 0; qi < queryTermCount; qi++)
		{
			ReadOnlySpan<char> qt = querySpan[queryRanges[qi]];
			var found = false;
			for (var ti = 0; ti < titleTermCount && !found; ti++)
			{
				ReadOnlySpan<char> rt = titleSpan[titleRanges[ti]];
				if (rt.Contains(qt, OrdinalIgnoreCase) || qt.Contains(rt, OrdinalIgnoreCase))
					found = true;
			}
			for (var ai = 0; ai < artistTermCount && !found; ai++)
			{
				ReadOnlySpan<char> rt = artistSpan[artistRanges[ai]];
				if (rt.Contains(qt, OrdinalIgnoreCase) || qt.Contains(rt, OrdinalIgnoreCase))
					found = true;
			}
			if (found)
				matchingTerms++;
		}

		var termScore =
			queryTermCount > 0 ? (double)matchingTerms / queryTermCount * PerfectScore : 0;

		double substringBonus = 0;
		if (result.Title.ContainsIgnoreCase(query))
			substringBonus = SubstringBonusTitleMatch;
		else if (result.Artist?.ContainsIgnoreCase(query) == true)
			substringBonus = SubstringBonusArtistMatch;

		var score = (int)Math.Min(PerfectScore, termScore + substringBonus);
		return Math.Max(MinimumScore, score);
	}

	internal static bool IsTrackResult(SearchResult result)
	{
		if (IsNullOrEmpty(value: result.ReleaseType))
			return false;

		var type = result.ReleaseType;

		return (
				type.EqualsIgnoreCase("recording")
				|| type.EqualsIgnoreCase("track")
				|| type.EqualsIgnoreCase("single")
			)
			&& result.Format?.Contains(value: "Single") != true;
	}

	internal static bool MatchesType(SearchResult result, string filter)
	{
		if (IsNullOrEmpty(value: result.ReleaseType))
			return false;

		var normalized = result.ReleaseType;

		return filter switch
		{
			"album" => normalized.EqualsIgnoreCase("album")
				|| normalized.EqualsIgnoreCase("master"),
			"ep" => normalized.ContainsIgnoreCase("ep"),
			"single" => normalized.ContainsIgnoreCase("single"),
			"compilation" => normalized.ContainsIgnoreCase("compilation"),
			"master" => normalized.EqualsIgnoreCase("master"),
			"release" => normalized.EqualsIgnoreCase("release"),
			_ => normalized.ContainsIgnoreCase(filter),
		};
	}

	internal static async Task<List<TrackInfo>> EnrichTracksWithProgressAsync(
		IMusicService service,
		string releaseId,
		string releaseTitle,
		List<TrackInfo> tracks,
		bool fresh,
		CancellationToken ct
	)
	{
		var total = tracks.Count;

		Log.Information(
			"MusicReleaseStart {ReleaseId} {ReleaseTitle} {TotalTracks}",
			releaseId,
			releaseTitle,
			total
		);

		if (fresh)
		{
			ReleaseProgressCache.Delete(releaseId: releaseId);
			StateManager.DeleteReleaseCache(releaseId: releaseId);
			UI.Info(message: "Cleared cached state for fresh fetch");
		}

		(List<TrackInfo> enrichedTracks, var startIndex, var resumeSource) =
			TryLoadResumeStateAsync(releaseId: releaseId, expectedTotal: total);

		if (startIndex > 0)
		{
			UI.Info(
				message: "Resuming from {0} (track {1}/{2})",
				resumeSource,
				startIndex + 1,
				total
			);
			Log.Information(
				"MusicReleaseResume {Source} {TracksEnriched}",
				resumeSource,
				startIndex
			);

			var startIdx = Math.Max(0, enrichedTracks.Count - 3);
			for (var i = startIdx; i < enrichedTracks.Count; i++)
			{
				TrackInfo t = enrichedTracks[i];
				AnsiConsole.MarkupLine(
					$"  {UI.Dim(text: "└")} {t.DiscNumber}.{t.TrackNumber:D2} {Markup.Escape(text: t.Title)}"
				);
			}
			UI.NewLine();
		}

		if (startIndex >= total)
		{
			UI.Complete(message: "All tracks already enriched from cache");
			StateManager.DeleteReleaseCache(releaseId: releaseId);
			return enrichedTracks;
		}

		Queue<(string Header, string Detail)> recentTracks = new();
		var completed = startIndex;
		var cancelled = false;

		UI.Suppress = true;

		await UI.CreateStandardProgress(descriptionWidth: 60)
			.StartAsync(async ctx =>
			{
				ProgressTask task = ctx.AddTask(
					UI.TaskDescription(
						$"({completed}/{total})",
						title: releaseTitle,
						$"(0/{total} tracks)"
					),
					maxValue: total
				);
				task.Value = startIndex;

				for (var i = startIndex; i < tracks.Count; i++)
				{
					TrackInfo track = tracks[index: i];

					if (ct.IsCancellationRequested)
					{
						cancelled = true;
						SaveResumeCheckpointAsync(
							releaseId: releaseId,
							total: total,
							enrichedTracks: enrichedTracks
						);
						break;
					}

					try
					{
						TrackInfo enriched = await EnrichSingleTrackAsync(
							service: service,
							releaseId: releaseId,
							track: track,
							ct
						);
						enrichedTracks.Add(item: enriched);
						completed++;

						(string Header, string Detail) info = FormatTrackDetail(t: enriched);
						recentTracks.Enqueue(item: info);
						if (recentTracks.Count > 5)
							recentTracks.Dequeue();

						if (completed % 10 == 0)
							Log.Debug("MusicTrackProgress {Completed} {Total}", completed, total);

						if (completed % 10 == 0)
							SaveResumeCheckpointAsync(
								releaseId: releaseId,
								total: total,
								enrichedTracks: enrichedTracks
							);

						task.Value = completed;
						task.Description = UI.TaskDescription(
							$"({completed}/{total})",
							title: releaseTitle,
							$"({completed}/{total} tracks)"
						);
					}
					catch (OperationCanceledException)
					{
						cancelled = true;
						SaveResumeCheckpointAsync(
							releaseId: releaseId,
							total: total,
							enrichedTracks: enrichedTracks
						);
						break;
					}
					catch (HttpRequestException ex)
					{
						SaveResumeCheckpointAsync(
							releaseId: releaseId,
							total: total,
							enrichedTracks: enrichedTracks
						);
						UI.Suppress = false;
						Log.Error(ex, "HTTP error during track enrichment");
						UI.Error(message: "Network Error: {0}", ex.Message);
						cancelled = true;
						break;
					}
					catch (InvalidOperationException ex)
					{
						SaveResumeCheckpointAsync(
							releaseId: releaseId,
							total: total,
							enrichedTracks: enrichedTracks
						);
						UI.Suppress = false;
						Log.Error(ex, "Validation error during track enrichment");
						UI.Error(message: "Validation Error: {0}", ex.Message);
						cancelled = true;
						break;
					}
				}
			});

		UI.Suppress = false;
		UI.NewLine();

		if (cancelled)
		{
			UI.Warn(message: "Enrichment interrupted at {0}/{1} tracks", completed, total);
			UI.Info(message: "Run the same command again to resume from track {0}", completed + 1);
			Log.Warning("SyncInterrupted {Reason}", $"{completed}/{total} tracks");
		}
		else
			FinalizeAndExportAsync(
				releaseId: releaseId,
				releaseTitle: releaseTitle,
				enrichedTracks: enrichedTracks,
				total: total
			);

		return enrichedTracks;
	}

	private static (
		List<TrackInfo> EnrichedTracks,
		int StartIndex,
		string ResumeSource
	) TryLoadResumeStateAsync(string releaseId, int expectedTotal)
	{
		List<TrackInfo> enrichedTracks = ReleaseProgressCache.Load(releaseId: releaseId);
		var startIndex = enrichedTracks.Count;
		var resumeSource = "none";

		if (startIndex > 0)
			resumeSource = "CSV";
		else
		{
			MusicBrainzEnrichmentState? cachedState =
				StateManager.LoadReleaseCache<MusicBrainzEnrichmentState>(releaseId: releaseId);
			if (cachedState is not null && cachedState.TotalTracks == expectedTotal)
			{
				enrichedTracks = cachedState.EnrichedTracks;
				startIndex = enrichedTracks.Count;
				resumeSource = "JSON";
			}
		}

		return (enrichedTracks, startIndex, resumeSource);
	}

	private static async Task<TrackInfo> EnrichSingleTrackAsync(
		IMusicService service,
		string releaseId,
		TrackInfo track,
		CancellationToken ct
	)
	{
		TrackInfo enriched = await ((MusicBrainzService)service).EnrichTrackAsync(track: track, ct);
		ReleaseProgressCache.AppendTrack(releaseId: releaseId, track: enriched);
		return enriched;
	}

	private static void SaveResumeCheckpointAsync(
		string releaseId,
		int total,
		List<TrackInfo> enrichedTracks
	)
	{
		StateManager.SaveReleaseCache(
			releaseId: releaseId,
			new MusicBrainzEnrichmentState(
				ReleaseId: releaseId,
				TotalTracks: total,
				EnrichedTracks: enrichedTracks,
				LastUpdated: DateTime.UtcNow
			)
		);
	}

	private static void FinalizeAndExportAsync(
		string releaseId,
		string releaseTitle,
		List<TrackInfo> enrichedTracks,
		int total
	)
	{
		UI.Complete($"Enriched {total} tracks");

		List<WorkSummary> works = WorkGrouper.Group(tracks: enrichedTracks);

		MusicExporter.ExportWorksToCSV(releaseTitle: releaseTitle, works: works);

		StateManager.DeleteReleaseCache(releaseId: releaseId);
		ReleaseProgressCache.Delete(releaseId: releaseId);
	}

	private static (string Header, string Detail) FormatTrackDetail(TrackInfo t)
	{
		var discTrack = $"{t.DiscNumber}.{t.TrackNumber:D2}";
		var title = t.Title;
		var duration = t.Duration?.ToString(format: @"m\:ss") ?? "";
		var header = IsNullOrEmpty(value: duration)
			? $"[{discTrack}] {title}"
			: $"[{discTrack}] {title} ({duration})";

		List<string> parts = [];

		if (!IsNullOrEmpty(value: t.WorkName))
			parts.Add(UI.Colored(color: "steelblue1", text: t.WorkName));

		var year = t.RecordingYear;
		if (!IsNullOrEmpty(value: t.Composer))
		{
			var yearPart = year.HasValue ? UI.Dim($"({year})") : "";
			parts.Add(UI.Combine(UI.Cyan(text: t.Composer), yearPart));
		}
		else if (year is { } y)
			parts.Add($"({y})");

		var performer = t.Orchestra ?? t.Artist ?? "";
		if (!IsNullOrEmpty(value: performer) && performer != t.Composer)
			parts.Add($"• {UI.Green(text: performer)}");

		if (
			!IsNullOrEmpty(value: t.Conductor)
			&& t.Conductor != t.Composer
			&& t.Conductor != performer
		)
			parts.Add($"cond. {UI.Yellow(text: t.Conductor)}");

		if (!IsNullOrEmpty(value: t.RecordingVenue))
			parts.Add($"[dim italic]@ {Markup.Escape(text: t.RecordingVenue)}[/]");

		if (t.Soloists.Count > 0)
			parts.Add($"feat. {Join(separator: ", ", values: t.Soloists)}");

		return (header, Join(separator: " ", values: parts));
	}
}
