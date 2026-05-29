namespace CSharpScripts.CLI;

using CSharpScripts.Data.Persistence;

#region JSON Configuration

file static class JsonOptions
{
	internal static readonly JsonSerializerOptions Indented = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};
}

#endregion

public sealed class MusicSearchCommand : AsyncCommand<MusicSearchCommand.Settings>
{
	#region Settings

	public sealed class Settings : CommandSettings
	{
		[CommandOption("-q|--query")]
		[Description("Free-text search (e.g. 'Bowie Heroes 1977')")]
		public string? Query { get; init; }

		[CommandOption("-i|--id")]
		[Description("Release ID (GUID for MusicBrainz, number for Discogs)")]
		public string? Id { get; init; }

		[CommandOption("-s|--source")]
		[Description("discogs (default), musicbrainz (or mb), both")]
		[DefaultValue("discogs")]
		[AllowedValues("discogs", "musicbrainz", "mb", "both")]
		public string Source { get; init; } = "discogs";

		[CommandOption("-m|--mode")]
		[Description("pop (default) or classical (changes default columns)")]
		[DefaultValue("pop")]
		[AllowedValues("pop", "classical")]
		public string Mode { get; init; } = "pop";

		[CommandOption("-t|--type")]
		[Description("Filter: album, ep, single, compilation (normalized across APIs)")]
		[AllowedValues("album", "ep", "single", "compilation")]
		public string? Type { get; init; }

		[CommandOption("-n|--limit")]
		[Description("Max results per source (default 10)")]
		[DefaultValue(10)]
		public int Limit { get; init; } = 10;

		[CommandOption("-o|--output")]
		[Description("table (default) or json")]
		[DefaultValue("table")]
		[AllowedValues("table", "json")]
		public string Output { get; init; } = "table";

		[CommandOption("-f|--fields")]
		[Description(
			"Comma-separated field list: artist,title,year,type,id,label,format,country,genres,score,catno,barcode"
		)]
		public string? Fields { get; init; }

		[CommandOption("-v|--verbose")]
		[Description("Verbose output: filter stats, extra columns, save JSON dumps")]
		[DefaultValue(false)]
		public bool Verbose { get; init; }

		[CommandOption("-y|--yes")]
		[Description("Auto-confirm deep search for --id mode")]
		[DefaultValue(false)]
		public bool AutoConfirm { get; init; }

		[CommandOption("--fresh")]
		[Description("Clear cached state and force fresh API fetch")]
		[DefaultValue(false)]
		public bool Fresh { get; init; }

		public override ValidationResult Validate()
		{
			if (IsNullOrEmpty(Query) && IsNullOrEmpty(Id))
				return ValidationResult.Error("Must specify either --query or --id");

			if (!IsNullOrEmpty(Query) && !IsNullOrEmpty(Id))
				return ValidationResult.Error("Cannot specify both --query and --id");

			return ValidationResult.Success();
		}
	}

	#endregion

	private static readonly HashSet<string> LoggedWorkHierarchyWarnings = [];

	#region Execute - Search Mode

	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		if (!IsNullOrEmpty(settings.Id))
			return await PerformLookupAsync(settings, ct: cancellationToken);

		var discogsToken = Config.DiscogsToken;
		var source = settings.Source.ToLowerInvariant();

		var searchMusicBrainz = source is "musicbrainz" or "mb" or "both";
		var searchDiscogs = source is "discogs" or "both";

		if (searchDiscogs && IsNullOrEmpty(discogsToken))
		{
			Console.Warning("DISCOGS_USER_TOKEN not set, using MusicBrainz");
			searchDiscogs = false;
			searchMusicBrainz = true;
		}

		var sourceLabel =
			searchMusicBrainz && searchDiscogs ? "Discogs + MusicBrainz"
			: searchDiscogs ? "Discogs"
			: "MusicBrainz";

		Console.Info("Searching {0}...", sourceLabel);

		List<SearchResult> results = [];

		if (searchMusicBrainz)
		{
			MusicBrainzService mb = new();
			List<SearchResult> mbResults = await mb.SearchAsync(
				settings.Query!,
				maxResults: settings.Limit,
				ct: cancellationToken
			);
			results.AddRange(mbResults);
		}

		if (searchDiscogs)
		{
			DiscogsService discogs = new(discogsToken);
			List<SearchResult> discogsResults = await discogs.SearchAsync(
				settings.Query!,
				maxResults: settings.Limit,
				ct: cancellationToken
			);

			discogsResults =
			[
				.. discogsResults.Select(r =>
					r with
					{
						Score = CalculateRelevanceScore(settings.Query!, r),
					}
				),
			];

			results.AddRange(discogsResults);
		}

		results = [.. results.OrderByDescending(r => r.Score ?? 0)];

		if (!IsNullOrEmpty(settings.Type))
		{
			var beforeCount = results.Count;
			var normalizedFilter = NormalizeType(settings.Type);

			results = [.. results.Where(r => MatchesType(r, normalizedFilter))];
			var filteredCount = beforeCount - results.Count;

			if (settings.Verbose)
			{
				Console.Dim(
					$"[DEBUG] Filter '{settings.Type}' -> normalized '{normalizedFilter}', removed {filteredCount}"
				);
			}
		}

		var trackCount = results.Count(IsTrackResult);
		if (trackCount > 0)
		{
			results = [.. results.Where(r => !IsTrackResult(r))];

			if (settings.Verbose)
			{
				Console.Dim(
					$"[DEBUG] Excluded {trackCount} track-level results (focusing on collections)"
				);
			}
		}

		if (settings.Verbose && results.Count > 0)
		{
			SaveSearchDumps(settings.Query!, results);
		}

		if (results.Count == 0)
		{
			Console.Warning("No results found.");
			return 0;
		}

		if (settings.Output.IsEqualTo("json"))
		{
			var json = JsonSerializer.Serialize(results, JsonOptions.Indented);
			System.Console.WriteLine(json);
			return 0;
		}

		List<string> columns = GetColumns(settings);
		SpectreTable table = new();
		table.Border(TableBorder.Rounded);
		foreach (var col in columns)
			table.AddColumn(col);

		foreach (SearchResult r in results)
		{
			List<string> values = [.. columns.Select(col => GetFieldValue(col, r))];
			table.AddRow([.. values]);
		}

		Console.Write(table);

		return 0;
	}

	private static List<string> GetColumns(Settings settings)
	{
		if (!IsNullOrEmpty(settings.Fields))
			return
			[
				.. settings
					.Fields.Split(
						',',
						StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
					)
					.Select(NormalizeFieldName),
			];

		var isClassical = settings.Mode.Equals("classical");

		List<string> columns = isClassical
			? ["Composer", "Work", "Performers", "Year", "ID"]
			: ["Artist", "Title", "Year", "Type", "ID"];

		if (settings.Verbose)
			columns.AddRange([
				"Source",
				"Score",
				"Label",
				"Format",
				"Country",
				"Genres",
				"CatNo",
				"Barcode",
			]);

		return columns;
	}

	private static string NormalizeFieldName(string field) =>
		field.ToLowerInvariant() switch
		{
			"artist" => "Artist",
			"title" => "Title",
			"year" => "Year",
			"type" => "Type",
			"id" => "ID",
			"source" => "Source",
			"score" => "Score",
			"label" => "Label",
			"format" => "Format",
			"country" => "Country",
			"genres" => "Genres",
			"styles" => "Styles",
			"catno" or "catalognumber" => "CatNo",
			"barcode" => "Barcode",
			"composer" => "Composer",
			"work" => "Work",
			"performers" => "Performers",
			_ => field,
		};

	private static string GetFieldValue(string column, SearchResult r)
	{
		var value = column switch
		{
			"Artist" => r.Artist ?? "",
			"Title" => MakeTitleLink(r),
			"Year" => r.Year?.ToString(CultureInfo.InvariantCulture) ?? "",
			"Type" => NormalizeTypeForDisplay(r.ReleaseType) ?? "",
			"ID" => MakeIdLink(r),
			"Source" => Console.SourceBadge(r.Source.ToString()),
			"Score" => r.Score?.ToString(CultureInfo.InvariantCulture) ?? "",
			"Label" => r.Label ?? "",
			"Format" => r.Format ?? "",
			"Country" => r.Country ?? "",
			"Genres" => r.Genres is { Count: > 0 } ? Join(", ", r.Genres) : "",
			"Styles" => r.Styles is { Count: > 0 } ? Join(", ", r.Styles) : "",
			"CatNo" => r.CatalogNumber ?? "",
			"Composer" => "",
			"Work" => r.Title,
			"Performers" => r.Artist ?? "",
			_ => "",
		};

		return column is "ID" or "Source" or "Title" ? value : Console.Escape(value);
	}

	#endregion

	#region Type Filtering & Scoring

	private static bool IsTrackResult(SearchResult r)
	{
		if (IsNullOrEmpty(r.ReleaseType))
			return false;

		var type = r.ReleaseType.ToLowerInvariant();

		return type is "recording" or "track" or "single" && r.Format?.Contains("Single") != true;
	}

	private static int CalculateRelevanceScore(string query, SearchResult r)
	{
		var queryLower = query.ToLowerInvariant();
		var titleLower = r.Title.ToLowerInvariant();
		var artistLower = r.Artist?.ToLowerInvariant();

		if (titleLower == queryLower)
			return 100;

		if (artistLower is { } && $"{artistLower} {titleLower}" == queryLower)
			return 100;

		HashSet<string> queryTerms =
		[
			.. queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries),
		];
		HashSet<string> resultTerms =
		[
			.. titleLower.Split(' ', StringSplitOptions.RemoveEmptyEntries),
		];
		if (artistLower is { })
			resultTerms.UnionWith(artistLower.Split(' ', StringSplitOptions.RemoveEmptyEntries));

		var matchingTerms = queryTerms.Count(qt =>
			resultTerms.Any(rt => rt.Contains(qt) || qt.Contains(rt))
		);
		var termScore = queryTerms.Count > 0 ? (double)matchingTerms / queryTerms.Count * 100 : 0;

		double substringBonus = 0;
		if (titleLower.Contains(queryLower))
			substringBonus = 30;
		else if (artistLower?.Contains(queryLower) == true)
			substringBonus = 20;

		var score = (int)Math.Min(100, termScore + substringBonus);
		return Math.Max(1, score);
	}

	private static void SaveSearchDumps(string query, List<SearchResult> results)
	{
		var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
		var sanitizedQuery = SanitizeForFolder(query);
		var folderName = $"{timestamp}-{sanitizedQuery}";
		var dumpDir = Combine(Paths.DumpsDirectory, "music-search", folderName);

		CreateDirectory(dumpDir);

		for (var i = 0; i < results.Count; i++)
		{
			SearchResult result = results[i];
			var source = result.Source == MusicSource.Discogs ? "discogs" : "musicbrainz";
			var fileName = $"{i + 1:D3}-{source}-{result.Id}.json";
			var filePath = Combine(dumpDir, fileName);

			var json = JsonSerializer.Serialize(result, JsonOptions.Indented);
			WriteAllText(filePath, json);
		}

		var allPath = Combine(dumpDir, "_all-results.json");
		var allJson = JsonSerializer.Serialize(results, JsonOptions.Indented);
		WriteAllText(allPath, allJson);

		Console.Dim($"[DEBUG] Saved {results.Count} results to: {dumpDir}");
	}

	private static string SanitizeForFolder(string input)
	{
		var invalid = GetInvalidFileNameChars();
		string sanitized = new([.. input.Select(c => invalid.Contains(c) ? '_' : c)]);
		return sanitized.Length > 50 ? sanitized[..50] : sanitized;
	}

	private static string NormalizeType(string input) =>
		input.ToLowerInvariant() switch
		{
			"album" => "album",
			"ep" => "ep",
			"single" => "single",
			"compilation" => "compilation",
			"master" => "master",
			"release" => "release",
			_ => input.ToLowerInvariant(),
		};

	private static bool MatchesType(SearchResult r, string filter)
	{
		if (IsNullOrEmpty(r.ReleaseType))
			return false;

		var normalized = r.ReleaseType.ToLowerInvariant();

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

	private static string? NormalizeTypeForDisplay(string? type) =>
		type?.ToLowerInvariant() switch
		{
			"album" => "Album",
			"ep" => "EP",
			"single" => "Single",
			"compilation" => "Compilation",
			"master" => "Master",
			"release" => "Release",
			_ => type,
		};

	private static string MakeIdLink(SearchResult r)
	{
		var url =
			r.Source == MusicSource.Discogs
				? $"https://www.discogs.com/release/{r.Id}"
				: $"https://musicbrainz.org/release/{r.Id}";

		return $"[link={url}]{r.Id}[/]";
	}

	private static string MakeTitleLink(SearchResult r)
	{
		var url =
			r.Source == MusicSource.Discogs
				? $"https://www.discogs.com/release/{r.Id}"
				: $"https://musicbrainz.org/release/{r.Id}";

		var escapedTitle = Console.Escape(r.Title);
		return $"[link={url}]{escapedTitle}[/]";
	}

	#endregion

	#region Execute - Lookup Mode (--id)

	private static async Task<int> PerformLookupAsync(Settings settings, CancellationToken ct)
	{
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
			ct,
			Program.cts.Token
		);
		CancellationToken token = linkedCts.Token;

		var source = settings.Source.ToLowerInvariant();
		var isDiscogs = source is "discogs";

		IMusicService service;

		if (isDiscogs)
		{
			if (!int.TryParse(settings.Id, out _))
			{
				Console.Error("Invalid Discogs ID (must be number)");
				return 1;
			}
			var discogsToken = Config.DiscogsToken;
			if (IsNullOrEmpty(discogsToken))
			{
				Console.CriticalFailure("Discogs", "DISCOGS_USER_TOKEN not set");
				return 1;
			}
			service = new DiscogsService(discogsToken);
		}
		else
		{
			if (!Guid.TryParse(settings.Id, out _))
			{
				Console.Error("Invalid MusicBrainz ID (must be GUID)");
				return 1;
			}
			service = new MusicBrainzService();
		}

		ReleaseData? release = null;
		var sourceName = isDiscogs ? "Discogs" : "MusicBrainz";

		await Console
			.Status()
			.Spinner(Spinner.Known.Dots)
			.SpinnerStyle(Style.Parse("cyan"))
			.StartAsync(
				$"[cyan]Fetching release info from {sourceName}...[/]",
				async _ =>
					release = await service.GetReleaseAsync(
						settings.Id,
						deepSearch: false,
						ct: token
					)
			);

		if (release is null || release.Tracks.Count == 0)
		{
			Console.Warning("No tracks found.");
			return 0;
		}

		ReleaseInfo info = release.Info;
		TrackInfo header = release.Tracks[0];

		Console.NewLine();
		Console.Rule("Release Info");
		Console.NewLine();
		Console.Field("Release:", info.Title);
		Console.Field("Artist:", info.Artist);
		Console.Field("Year:", info.Year?.ToString());
		Console.Field("Label:", info.Label);
		Console.Field("Catalog:", info.CatalogNumber);
		Console.FieldIfPresent("Conductor:", header.Conductor);
		Console.FieldIfPresent("Orchestra:", header.Orchestra);
		Console.FieldIfPresent("Venue:", header.RecordingVenue);
		if (header.Soloists.Count > 0)
			Console.Field("Soloists:", $"{header.Soloists.Count} listed");

		Console.Field("Discs:", info.DiscCount.ToString());
		Console.Field("Tracks:", info.TrackCount.ToString());
		if (info.TotalDuration.HasValue && info.TotalDuration.Value > TimeSpan.Zero)
		{
			TimeSpan td = info.TotalDuration.Value;
			var durationText =
				td.Days > 0 ? $"{td.Days}d {td.Hours}h {td.Minutes}m"
				: td.Hours > 0 ? $"{td.Hours}h {td.Minutes}m"
				: $"{td.Minutes}m {td.Seconds}s";
			Console.Field("Duration:", durationText);
		}
		Console.NewLine();

		if (!isDiscogs)
		{
			var deepSearch = settings.AutoConfirm;
			if (!deepSearch)
			{
				var choice = Console.Prompt(
					new SelectionPrompt<string>()
						.Title("Fetch full track metadata (recordings, composers, etc)?")
						.AddChoices("Yes", "No")
				);
				deepSearch = choice.IsEqualTo("Yes", Ordinal);
			}

			if (deepSearch)
			{
				List<TrackInfo> enrichedTracks = await EnrichTracksWithProgressAsync(
					(MusicBrainzService)service,
					settings.Id,
					info.Title,
					release.Tracks,
					settings.Fresh,
					token
				);
				release = new ReleaseData(info, enrichedTracks);
				MusicExporter.ExportToSheets(release);
			}
		}

		SpectreTable table = new();
		table.Border(TableBorder.Simple);

		if (isDiscogs)
		{
			table.AddColumn("Disc");
			table.AddColumn("Track");
			table.AddColumn("Title");
			table.AddColumn("Duration");

			foreach (TrackInfo track in release.Tracks)
			{
				var duration =
					track.Duration is { } d && d > TimeSpan.Zero ? d.ToString(@"m\:ss") : "";
				table.AddRow(
					track.DiscNumber.ToString(),
					track.TrackNumber.ToString(),
					Console.Escape(track.Title),
					duration
				);
			}
		}
		else
		{
			table.AddColumn(new TableColumn("Disc").NoWrap().Centered());
			table.AddColumn(new TableColumn("Tracks").NoWrap().Centered());
			table.AddColumn(new TableColumn("Work").NoWrap());
			table.AddColumn("Composer");
			table.AddColumn(new TableColumn("Year").NoWrap().Centered());
			table.AddColumn(new TableColumn("Duration").NoWrap().RightAligned());
			table.AddColumn("Conductor");
			table.AddColumn("Orchestra");
			table.AddColumn("Soloists");

			List<WorkSummary> works = GroupTracksByWork(release.Tracks);
			foreach (WorkSummary work in works)
			{
				var duration =
					work.TotalDuration > TimeSpan.Zero ? work.TotalDuration.ToString(@"m\:ss") : "";
				var soloists = work.Soloists.Count > 0 ? Join(", ", work.Soloists) : "";

				table.AddRow(
					work.Disc.ToString(),
					work.TrackRange,
					Console.Escape(work.Work),
					Console.Escape(work.Composer ?? ""),
					work.YearDisplay,
					duration,
					Console.Escape(work.Conductor ?? ""),
					Console.Escape(work.Orchestra ?? ""),
					Console.Escape(soloists)
				);
			}
		}

		Console.NewLine();
		return 0;
	}

	#endregion

	#region Work Grouping & Display

	internal static List<WorkSummary> GroupTracksByWork(List<TrackInfo> tracks)
	{
		List<WorkSummary> works = [];
		if (tracks.Count == 0)
			return works;

		var currentDisc = -1;
		string? currentWorkName = null;
		List<TrackInfo> currentGroup = [];

		void FlushGroup()
		{
			if (currentGroup.Count == 0)
				return;

			TrackInfo first = currentGroup[0];
			List<int> years =
			[
				.. currentGroup
					.Select(t => t.RecordingYear)
					.Where(y => y.HasValue)
					.Select(y => y!.Value)
					.Distinct()
					.OrderBy(y => y),
			];

			TimeSpan totalDuration = currentGroup
				.Where(t => t.Duration.HasValue)
				.Aggregate(TimeSpan.Zero, (sum, t) => sum + t.Duration!.Value);

			List<string> soloists = [.. currentGroup.SelectMany(t => t.Soloists).Distinct()];

			var displayWork = first.WorkName ?? first.Title;

			works.Add(
				new WorkSummary(
					Disc: first.DiscNumber,
					FirstTrack: currentGroup[0].TrackNumber,
					LastTrack: currentGroup[^1].TrackNumber,
					Work: displayWork,
					Composer: first.Composer,
					Years: years,
					Conductor: first.Conductor,
					Orchestra: first.Orchestra,
					Soloists: soloists,
					TotalDuration: totalDuration
				)
			);

			currentGroup.Clear();
		}

		foreach (TrackInfo track in tracks)
		{
			var workKey = track.WorkName ?? track.Title;

			if (track.DiscNumber != currentDisc || workKey != currentWorkName)
			{
				FlushGroup();
				currentDisc = track.DiscNumber;
				currentWorkName = workKey;
			}

			currentGroup.Add(track);
		}

		FlushGroup();

		DetectMissingWorkHierarchy(works);

		return works;
	}

	private static void DetectMissingWorkHierarchy(List<WorkSummary> works)
	{
		List<string> suspectedMissing = [];

		for (var i = 0; i < works.Count - 1; i++)
		{
			WorkSummary current = works[i];
			WorkSummary next = works[i + 1];

			if (current.FirstTrack != current.LastTrack || next.FirstTrack != next.LastTrack)
				continue;

			if (current.Disc != next.Disc)
				continue;

			var currentColon = current.Work.IndexOf(':');
			var nextColon = next.Work.IndexOf(':');

			if (currentColon > 5 && nextColon > 5)
			{
				var currentPrefix = current.Work[..currentColon];
				var nextPrefix = next.Work[..nextColon];

				if (currentPrefix == nextPrefix && !suspectedMissing.Contains(currentPrefix))
					suspectedMissing.Add(currentPrefix);
			}
		}

		foreach (var missing in suspectedMissing)
		{
			if (!LoggedWorkHierarchyWarnings.Add(missing))
				continue;

			Console.Warning("Work hierarchy missing for '{0}' - tracks not grouped", missing);
		}
	}

	#endregion

	#region Track Enrichment with Progress

	private static async Task<List<TrackInfo>> EnrichTracksWithProgressAsync(
		IMusicService service,
		string releaseId,
		string releaseTitle,
		List<TrackInfo> tracks,
		bool fresh,
		CancellationToken ct
	)
	{
		var total = tracks.Count;

		Logger.Start(ServiceType.Music);
		Logger.Event(
			"ReleaseStart",
			new Dictionary<string, object>
			{
				["ReleaseId"] = releaseId,
				["ReleaseTitle"] = releaseTitle,
				["TotalTracks"] = total,
			}
		);

		var connStr = Environment.GetEnvironmentVariable("PGCONNSTR") 
			?? throw new InvalidOperationException("PGCONNSTR environment variable is not set.");
		var progressService = new ReleaseProgressService(new CommandDbContextFactory(connStr));

		if (fresh)
		{
			await progressService.DeleteAsync(releaseId, ct);
			StateManager.DeleteReleaseCache(releaseId);
			Console.Info("Cleared cached state for fresh fetch");
		}

		List<TrackInfo> enrichedTracks = await progressService.LoadAsync(releaseId, ct);
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
			if (cachedState is { } && cachedState.TotalTracks == total)
			{
				enrichedTracks = cachedState.EnrichedTracks;
				startIndex = enrichedTracks.Count;
				resumeSource = "JSON";
			}
		}

		if (startIndex > 0)
		{
			Console.Info("Resuming from {0} (track {1}/{2})", resumeSource, startIndex + 1, total);
			Logger.Event(
				"ReleaseResume",
				new Dictionary<string, object>
				{
					["Source"] = resumeSource,
					["TracksEnriched"] = startIndex,
				}
			);

			foreach (TrackInfo t in enrichedTracks.TakeLast(3))
			{
				Console.MarkupLine(
					$"  [dim]└[/] {t.DiscNumber}.{t.TrackNumber:D2} {Console.Escape(t.Title)}"
				);
			}
			Console.NewLine();
		}

		if (startIndex >= total)
		{
			Console.Success("All tracks already enriched from cache");
			StateManager.DeleteReleaseCache(releaseId);
			return enrichedTracks;
		}

		Queue<(string Header, string Detail)> recentTracks = new();
		var completed = startIndex;
		var cancelled = false;

		static (string Header, string Detail) FormatTrackDetail(TrackInfo t)
		{
			var discTrack = $"{t.DiscNumber}.{t.TrackNumber:D2}";
			var title = t.Title;
			var duration = t.Duration?.ToString(@"m\:ss") ?? "";
			var header = IsNullOrEmpty(duration)
				? $"[{discTrack}] {title}"
				: $"[{discTrack}] {title} ({duration})";

			List<string> parts = [];

			if (!IsNullOrEmpty(t.WorkName))
				parts.Add(Console.Work(t.WorkName));

			var year = t.RecordingYear;
			if (!IsNullOrEmpty(t.Composer))
				parts.Add(Console.Combine(Console.Composer(t.Composer), Console.Year(year)));
			else if (year is { } y)
				parts.Add($"({y})");

			var performer = t.Orchestra ?? t.Artist ?? "";
			if (!IsNullOrEmpty(performer) && performer != t.Composer)
				parts.Add($"• {Console.Orchestra(performer)}");

			if (
				!IsNullOrEmpty(t.Conductor)
				&& t.Conductor != t.Composer
				&& t.Conductor != performer
			)
				parts.Add($"cond. {Console.Conductor(t.Conductor)}");

			if (!IsNullOrEmpty(t.RecordingVenue))
				parts.Add(Console.Venue(t.RecordingVenue));

			if (t.Soloists.Count > 0)
				parts.Add($"feat. {Join(", ", t.Soloists)}");

			return (header, Join(" ", parts));
		}

		void SaveState()
		{
			StateManager.SaveReleaseCache(
				releaseId,
				new MusicBrainzEnrichmentState(releaseId, total, enrichedTracks, DateTime.Now)
			);
		}

		Console.Suppress = true;

		await Console
			.CreateProgress()
			.AutoClear(true)
			.HideCompleted(false)
			.Columns(
				new FixedWidthDescriptionColumn(60),
				new ProgressBarColumn(),
				new PercentageColumn(),
				new RemainingTimeColumn(),
				new SpinnerColumn()
			)
			.StartAsync(async ctx =>
			{
				ProgressTask task = ctx.AddTask(
					Console.TaskDescription(
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
						SaveState();
						break;
					}

					try
					{
						TrackInfo enriched = await ((MusicBrainzService)service).EnrichTrackAsync(
							track,
							ct
						);
						enrichedTracks.Add(enriched);
						await progressService.AppendTrackAsync(releaseId, enriched, ct);
						completed++;

						(string Header, string Detail) info = FormatTrackDetail(enriched);
						recentTracks.Enqueue(info);
						if (recentTracks.Count > 5)
							recentTracks.Dequeue();

						if (completed % 10 == 0)
							Logger.Event(
								"TrackProgress",
								new Dictionary<string, object>
								{
									["Completed"] = completed,
									["Total"] = total,
								}
							);

						if (completed % 10 == 0)
							SaveState();

						task.Value = completed;
						task.Description = Console.TaskDescription(
							prefix: $"({completed}/{total})",
							title: releaseTitle,
							$"({completed}/{total} tracks)"
						);
					}
					catch (OperationCanceledException)
					{
						cancelled = true;
						SaveState();
						break;
					}
					catch (Exception ex)
					{
						SaveState();
						Console.Suppress = false;
						Console.Error("Error: {0}", ex.Message);
						cancelled = true;
						break;
					}
				}
			});

		Console.Suppress = false;
		Console.NewLine();

		if (cancelled)
		{
			Console.Warning("Enrichment interrupted at {0}/{1} tracks", completed, total);
			Console.Info("Run the same command again to resume from track {0}", completed + 1);
			Logger.Interrupted($"{completed}/{total} tracks");
		}
		else
		{
			Console.Complete($"Enriched {total} tracks");

			List<WorkSummary> works = GroupTracksByWork(enrichedTracks);
			Logger.End(true, $"{total} tracks, {works.Count} works");

			MusicExporter.ExportWorksToCSV(releaseTitle, works);

			StateManager.DeleteReleaseCache(releaseId);
			await progressService.DeleteAsync(releaseId, CancellationToken.None);
		}

		return enrichedTracks;
	}

	#endregion

	private sealed class CommandDbContextFactory(string connectionString) : IDbContextFactory<ScriptsDbContext>
	{
		public ScriptsDbContext CreateDbContext()
		{
			var options = new DbContextOptionsBuilder<ScriptsDbContext>()
				.UseNpgsql(connectionString)
				.Options;
			return new ScriptsDbContext(options);
		}
	}
}
