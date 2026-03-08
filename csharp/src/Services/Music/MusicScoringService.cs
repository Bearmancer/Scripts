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
		var queryLower = query.ToLowerInvariant();
		var titleLower = result.Title.ToLowerInvariant();
		var artistLower = result.Artist?.ToLowerInvariant();

		if (titleLower == queryLower)
			return PerfectScore;

		if (artistLower is not null && $"{artistLower} {titleLower}" == queryLower)
			return PerfectScore;

		HashSet<string> queryTerms =
		[
			.. queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries),
		];
		HashSet<string> resultTerms =
		[
			.. titleLower.Split(' ', StringSplitOptions.RemoveEmptyEntries),
		];
		if (artistLower is not null)
			resultTerms.UnionWith(artistLower.Split(' ', StringSplitOptions.RemoveEmptyEntries));

		var matchingTerms = queryTerms.Count(qt =>
			resultTerms.Any(rt => rt.Contains(qt) || qt.Contains(rt))
		);
		var termScore =
			queryTerms.Count > 0 ? (double)matchingTerms / queryTerms.Count * PerfectScore : 0;

		double substringBonus = 0;
		if (titleLower.Contains(queryLower))
			substringBonus = SubstringBonusTitleMatch;
		else if (artistLower?.Contains(queryLower) == true)
			substringBonus = SubstringBonusArtistMatch;

		var score = (int)Math.Min(PerfectScore, termScore + substringBonus);
		return Math.Max(MinimumScore, score);
	}

	internal static bool IsTrackResult(SearchResult result)
	{
		if (IsNullOrEmpty(result.ReleaseType))
			return false;

		var type = result.ReleaseType.ToLowerInvariant();

		return type is "recording" or "track" or "single"
			&& result.Format?.Contains("Single") != true;
	}

	internal static bool MatchesType(SearchResult result, string filter)
	{
		if (IsNullOrEmpty(result.ReleaseType))
			return false;

		var normalized = result.ReleaseType.ToLowerInvariant();

		return filter switch
		{
			"album" => normalized is "album" or "master",
			"ep" => normalized.Contains("ep"),
			"single" => normalized.Contains("single"),
			"compilation" => normalized.Contains("compilation"),
			"master" => normalized is "master",
			"release" => normalized is "release",
			_ => normalized.Contains(filter),
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
			ReleaseProgressCache.Delete(releaseId);
			StateManager.DeleteReleaseCache(releaseId);
			UI.Info("Cleared cached state for fresh fetch");
		}

		(List<TrackInfo> enrichedTracks, var startIndex, var resumeSource) =
			TryLoadResumeStateAsync(releaseId, total);

		if (startIndex > 0)
		{
			UI.Info("Resuming from {0} (track {1}/{2})", resumeSource, startIndex + 1, total);
			Log.Information(
				"MusicReleaseResume {Source} {TracksEnriched}",
				resumeSource,
				startIndex
			);

			foreach (TrackInfo t in enrichedTracks.TakeLast(3))
			{
				AnsiConsole.MarkupLine(
					$"  {UI.Dim("└")} {t.DiscNumber}.{t.TrackNumber:D2} {Markup.Escape(t.Title)}"
				);
			}
			UI.NewLine();
		}

		if (startIndex >= total)
		{
			UI.Complete("All tracks already enriched from cache");
			StateManager.DeleteReleaseCache(releaseId);
			return enrichedTracks;
		}

		Queue<(string Header, string Detail)> recentTracks = new();
		var completed = startIndex;
		var cancelled = false;

		UI.Suppress = true;

		await UI.CreateStandardProgress(60)
			.StartAsync(async ctx =>
			{
				ProgressTask task = ctx.AddTask(
					UI.TaskDescription(
						prefix: $"({completed}/{total})",
						title: releaseTitle,
						$"(0/{total} tracks)"
					),
					maxValue: total
				);
				task.Value = startIndex;

				for (var i = startIndex; i < tracks.Count; i++)
				{
					TrackInfo track = tracks[i];

					if (ct.IsCancellationRequested)
					{
						cancelled = true;
						SaveResumeCheckpointAsync(releaseId, total, enrichedTracks);
						break;
					}

					try
					{
						TrackInfo enriched = await EnrichSingleTrackAsync(
							service,
							releaseId,
							track,
							ct
						);
						enrichedTracks.Add(enriched);
						completed++;

						(string Header, string Detail) info = FormatTrackDetail(enriched);
						recentTracks.Enqueue(info);
						if (recentTracks.Count > 5)
							recentTracks.Dequeue();

						if (completed % 10 == 0)
							Log.Debug("MusicTrackProgress {Completed} {Total}", completed, total);

						if (completed % 10 == 0)
							SaveResumeCheckpointAsync(releaseId, total, enrichedTracks);

						task.Value = completed;
						task.Description = UI.TaskDescription(
							prefix: $"({completed}/{total})",
							title: releaseTitle,
							$"({completed}/{total} tracks)"
						);
					}
					catch (OperationCanceledException)
					{
						cancelled = true;
						SaveResumeCheckpointAsync(releaseId, total, enrichedTracks);
						break;
					}
					catch (HttpRequestException ex)
					{
						SaveResumeCheckpointAsync(releaseId, total, enrichedTracks);
						UI.Suppress = false;
						UI.Error("Error: {0}", ex.Message);
						cancelled = true;
						break;
					}
					catch (InvalidOperationException ex)
					{
						SaveResumeCheckpointAsync(releaseId, total, enrichedTracks);
						UI.Suppress = false;
						UI.Error("Error: {0}", ex.Message);
						cancelled = true;
						break;
					}
				}
			});

		UI.Suppress = false;
		UI.NewLine();

		if (cancelled)
		{
			UI.Warn("Enrichment interrupted at {0}/{1} tracks", completed, total);
			UI.Info("Run the same command again to resume from track {0}", completed + 1);
			Log.Warning("SyncInterrupted {Reason}", $"{completed}/{total} tracks");
		}
		else
		{
			FinalizeAndExportAsync(releaseId, releaseTitle, enrichedTracks, total);
		}

		return enrichedTracks;
	}

	private static (
		List<TrackInfo> EnrichedTracks,
		int StartIndex,
		string ResumeSource
	) TryLoadResumeStateAsync(string releaseId, int expectedTotal)
	{
		List<TrackInfo> enrichedTracks = ReleaseProgressCache.Load(releaseId);
		var startIndex = enrichedTracks.Count;
		var resumeSource = "none";

		if (startIndex > 0)
		{
			resumeSource = "CSV";
		}
		else
		{
			MusicBrainzEnrichmentState? cachedState =
				StateManager.LoadReleaseCache<MusicBrainzEnrichmentState>(releaseId);
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
		TrackInfo enriched = await ((MusicBrainzService)service).EnrichTrackAsync(track, ct);
		ReleaseProgressCache.AppendTrack(releaseId, enriched);
		return enriched;
	}

	private static void SaveResumeCheckpointAsync(
		string releaseId,
		int total,
		List<TrackInfo> enrichedTracks
	)
	{
		StateManager.SaveReleaseCache(
			releaseId,
			new MusicBrainzEnrichmentState(releaseId, total, enrichedTracks, DateTime.UtcNow)
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

		List<WorkSummary> works = WorkGrouper.Group(enrichedTracks);

		MusicExporter.ExportWorksToCSV(releaseTitle, works);

		StateManager.DeleteReleaseCache(releaseId);
		ReleaseProgressCache.Delete(releaseId);
	}

	private static (string Header, string Detail) FormatTrackDetail(TrackInfo t)
	{
		var discTrack = $"{t.DiscNumber}.{t.TrackNumber:D2}";
		var title = t.Title;
		var duration = t.Duration?.ToString(@"m\:ss") ?? "";
		var header = IsNullOrEmpty(duration)
			? $"[{discTrack}] {title}"
			: $"[{discTrack}] {title} ({duration})";

		List<string> parts = [];

		if (!IsNullOrEmpty(t.WorkName))
			parts.Add(UI.Colored("steelblue1", t.WorkName));

		var year = t.RecordingYear;
		if (!IsNullOrEmpty(t.Composer))
		{
			var yearPart = year.HasValue ? UI.Dim($"({year})") : "";
			parts.Add(UI.Combine(UI.Cyan(t.Composer), yearPart));
		}
		else if (year is { } y)
			parts.Add($"({y})");

		var performer = t.Orchestra ?? t.Artist ?? "";
		if (!IsNullOrEmpty(performer) && performer != t.Composer)
			parts.Add($"• {UI.Green(performer)}");

		if (!IsNullOrEmpty(t.Conductor) && t.Conductor != t.Composer && t.Conductor != performer)
			parts.Add($"cond. {UI.Yellow(t.Conductor)}");

		if (!IsNullOrEmpty(t.RecordingVenue))
			parts.Add($"[dim italic]@ {Markup.Escape(t.RecordingVenue)}[/]");

		if (t.Soloists.Count > 0)
			parts.Add($"feat. {Join(", ", t.Soloists)}");

		return (header, Join(" ", parts));
	}
}
