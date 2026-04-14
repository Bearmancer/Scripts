namespace CSharpScripts.CLI.Music;

internal sealed class MusicSearchCommand : BaseAsyncCommand<MusicSearchCommand.Settings>
{
	private static readonly JsonSerializerOptions IndentedJson = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		if (IsNullOrEmpty(value: settings.Input))
		{
			UI.Error(message: "Input required: provide a search query or release ID");
			return 1;
		}

		if (Guid.TryParse(input: settings.Input, result: out _))
		{
			return await PerformLookupAsync(
				settings: settings,
				id: settings.Input,
				isMusicBrainz: true,
				ct: cancellationToken
			);
		}

		if (int.TryParse(s: settings.Input, result: out _))
		{
			return await PerformLookupAsync(
				settings: settings,
				id: settings.Input,
				isMusicBrainz: false,
				ct: cancellationToken
			);
		}

		return await ExecuteWithErrorHandlingAsync(
			service: ServiceType.Music,
			async () =>
			{
				List<SearchResult> results = await RunSearchAsync(
					settings: settings,
					cancellationToken: cancellationToken
				);

				if (results.Count == 0)
				{
					UI.Warn(message: "No results found.");
					return;
				}

				List<string> columns = GetColumns(settings: settings);
				SpectreTable table = new();
				HasTableBorderExtensions.Border(table, border: TableBorder.Rounded);
				foreach (var col in columns)
					TableExtensions.AddColumn(table, column: col);

				foreach (SearchResult r in results)
				{
					List<string> values =
					[
						.. Enumerable.Select(columns, col => GetFieldValue(column: col, r: r)),
					];
					TableExtensions.AddRow(table, [.. values]);
				}

				AnsiConsole.Write(renderable: table);
			}
		);
	}

	private static async Task<List<SearchResult>> RunSearchAsync(
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		var discogsToken = Secrets.DiscogsToken;

		var searchMusicBrainz =
			settings.Source.EqualsIgnoreCase("musicbrainz")
			|| settings.Source.EqualsIgnoreCase("mb")
			|| settings.Source.EqualsIgnoreCase("both");
		var searchDiscogs =
			settings.Source.EqualsIgnoreCase("discogs") || settings.Source.EqualsIgnoreCase("both");

		if (searchDiscogs && IsNullOrEmpty(value: discogsToken))
		{
			UI.Warn(message: "DISCOGS_USER_TOKEN not set, using MusicBrainz");
			searchDiscogs = false;
			searchMusicBrainz = true;
		}

		var sourceLabel =
			searchMusicBrainz && searchDiscogs ? "Discogs + MusicBrainz"
			: searchDiscogs ? "Discogs"
			: "MusicBrainz";

		UI.Info(message: "Searching {0}...", sourceLabel);

		List<SearchResult> results = [];

		if (searchMusicBrainz)
		{
			MusicBrainzService mb = new();
			List<SearchResult> mbResults = await mb.SearchAsync(
				settings.Input!,
				maxResults: settings.Limit,
				ct: cancellationToken
			);
			results.AddRange(collection: mbResults);
		}

		if (searchDiscogs)
		{
			using DiscogsService discogs = new(token: discogsToken);
			List<SearchResult> discogsResults = await discogs.SearchAsync(
				settings.Input!,
				maxResults: settings.Limit,
				ct: cancellationToken
			);

			discogsResults =
			[
				.. Enumerable.Select(
					discogsResults,
					r =>
						r with
						{
							Score = MusicScoringService.CalculateRelevanceScore(
								settings.Input!,
								result: r
							),
						}
				),
			];

			results.AddRange(collection: discogsResults);
		}

		results = [.. Enumerable.OrderByDescending(results, r => r.Score ?? 0)];

		if (!IsNullOrEmpty(value: settings.Type))
		{
			var beforeCount = results.Count;
			var normalizedFilter = NormalizeType(input: settings.Type);

			results =
			[
				.. Enumerable.Where(
					results,
					r => MusicScoringService.MatchesType(result: r, filter: normalizedFilter)
				),
			];
			var filteredCount = beforeCount - results.Count;

			if (settings.Verbose)
			{
				Log.Debug(
					messageTemplate: "MusicSearch_Filter {FilterType} {NormalizedFilter} {RemovedCount}",
					settings.Type,
					normalizedFilter,
					filteredCount
				);
			}
		}

		var trackCount = Enumerable.Count(results, predicate: MusicScoringService.IsTrackResult);
		if (trackCount > 0)
		{
			results =
			[
				.. Enumerable.Where(results, r => !MusicScoringService.IsTrackResult(result: r)),
			];

			if (settings.Verbose)
				Log.Debug(messageTemplate: "MusicSearch_ExcludedTracks {TrackCount}", trackCount);
		}

		if (settings.Verbose && results.Count > 0)
			SaveSearchDumps(settings.Input!, results: results);

		return results;
	}

	private static List<string> GetColumns(Settings settings)
	{
		if (!IsNullOrEmpty(value: settings.Fields))
		{
			return
			[
				.. Enumerable.Select(
					settings.Fields.Split(
						separator: ',',
						StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
					),
					selector: NormalizeFieldName
				),
			];
		}

		var isClassical = settings.Mode.EqualsIgnoreCase("classical");

		List<string> columns = isClassical
			? ["Composer", "Work", "Performers", "Year", "ID"]
			: ["Artist", "Title", "Year", "Type", "ID"];

		if (settings.Verbose)
		{
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
		}

		return columns;
	}

	private static readonly FrozenDictionary<string, string> FieldNameMap = new Dictionary<
		string,
		string
	>(StringComparer.OrdinalIgnoreCase)
	{
		["artist"] = "Artist",
		["title"] = "Title",
		["year"] = "Year",
		["type"] = "Type",
		["id"] = "ID",
		["source"] = "Source",
		["score"] = "Score",
		["label"] = "Label",
		["format"] = "Format",
		["country"] = "Country",
		["genres"] = "Genres",
		["styles"] = "Styles",
		["catno"] = "CatNo",
		["catalognumber"] = "CatNo",
		["barcode"] = "Barcode",
		["composer"] = "Composer",
		["work"] = "Work",
		["performers"] = "Performers",
	}.ToFrozenDictionary();

	private static string NormalizeFieldName(string field) =>
		FieldNameMap.TryGetValue(field, out var normalized) ? normalized : field;

	private static string GetFieldValue(string column, SearchResult r)
	{
		var value = column switch
		{
			"Artist" => r.Artist ?? "",
			"Title" => MakeTitleLink(r: r),
			"Year" => r.Year?.ToString(provider: CultureInfo.InvariantCulture) ?? "",
			"Type" => NormalizeTypeForDisplay(type: r.ReleaseType) ?? "",
			"ID" => MakeIdLink(r: r),
			"Source" => r.Source == MusicSource.Discogs
				? UI.Yellow(text: "Discogs")
				: UI.Cyan(text: "MusicBrainz"),
			"Score" => r.Score?.ToString(provider: CultureInfo.InvariantCulture) ?? "",
			"Label" => r.Label ?? "",
			"Format" => r.Format ?? "",
			"Country" => r.Country ?? "",
			"Genres" => r.Genres is { Count: > 0 } ? Join(separator: ", ", values: r.Genres) : "",
			"Styles" => r.Styles is { Count: > 0 } ? Join(separator: ", ", values: r.Styles) : "",
			"CatNo" => r.CatalogNumber ?? "",
			"Composer" => "",
			"Work" => r.Title,
			"Performers" => r.Artist ?? "",
			_ => "",
		};

		return column is "ID" or "Source" or "Title" ? value : Markup.Escape(text: value);
	}

	private static void SaveSearchDumps(string query, List<SearchResult> results)
	{
		var timestamp = DateTime.UtcNow.ToString(format: "yyyyMMdd-HHmmss");
		var sanitizedQuery = SanitizeForFolder(input: query);
		var folderName = $"{timestamp}-{sanitizedQuery}";
		var dumpDir = Path.Combine(
			path1: Paths.DumpsDirectory,
			path2: "music-search",
			path3: folderName
		);

		Directory.CreateDirectory(path: dumpDir);

		for (var i = 0; i < results.Count; i++)
		{
			SearchResult result = results[index: i];
			var source = result.Source == MusicSource.Discogs ? "discogs" : "musicbrainz";
			var fileName = $"{i + 1:D3}-{source}-{result.Id}.json";
			var filePath = Path.Combine(path1: dumpDir, path2: fileName);

			var json = JsonSerializer.Serialize(value: result, options: IndentedJson);
			File.WriteAllText(path: filePath, contents: json);
		}

		var allPath = Path.Combine(path1: dumpDir, path2: "_all-results.json");
		var allJson = JsonSerializer.Serialize(value: results, options: IndentedJson);
		File.WriteAllText(path: allPath, contents: allJson);

		Log.Debug(messageTemplate: "Saved {0} results to: {1}", results.Count, dumpDir);
	}

	private static string SanitizeForFolder(string input)
	{
		var invalid = Path.GetInvalidFileNameChars();
		string sanitized = new([
			.. Enumerable.Select(
				input,
				c => MemoryExtensions.Contains(invalid, value: c) ? '_' : c
			),
		]);
		return sanitized.Length > 50 ? sanitized[..50] : sanitized;
	}

	private static readonly FrozenDictionary<string, string> TypeMap = new Dictionary<
		string,
		string
	>(StringComparer.OrdinalIgnoreCase)
	{
		["album"] = "album",
		["ep"] = "ep",
		["single"] = "single",
		["compilation"] = "compilation",
		["master"] = "master",
		["release"] = "release",
	}.ToFrozenDictionary();

	private static string NormalizeType(string input) =>
		TypeMap.TryGetValue(input, out var normalized) ? normalized : input;

	private static readonly FrozenDictionary<string, string> TypeDisplayMap = new Dictionary<
		string,
		string
	>(StringComparer.OrdinalIgnoreCase)
	{
		["album"] = "Album",
		["ep"] = "EP",
		["single"] = "Single",
		["compilation"] = "Compilation",
		["master"] = "Master",
		["release"] = "Release",
	}.ToFrozenDictionary();

	private static string? NormalizeTypeForDisplay(string? type) =>
		type is not null && TypeDisplayMap.TryGetValue(type, out var display) ? display : type;

	private static string MakeIdLink(SearchResult r)
	{
		var url =
			r.Source == MusicSource.Discogs
				? $"https://www.discogs.com/release/{r.Id}"
				: $"https://musicbrainz.org/release/{r.Id}";

		return UI.LinkText(url: url, text: r.Id);
	}

	private static string MakeTitleLink(SearchResult r)
	{
		var url =
			r.Source == MusicSource.Discogs
				? $"https://www.discogs.com/release/{r.Id}"
				: $"https://musicbrainz.org/release/{r.Id}";

		return UI.LinkText(url: url, text: r.Title);
	}

	private static async Task<int> PerformLookupAsync(
		Settings settings,
		string id,
		bool isMusicBrainz,
		CancellationToken ct
	)
	{
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
			token1: ct,
			token2: Program.Cts.Token
		);
		CancellationToken token = linkedCts.Token;

		var isDiscogs = !isMusicBrainz;

		(ReleaseData? release, var wasEnriched) = await FetchAndEnrichAsync(
			id: id,
			isDiscogs: isDiscogs,
			fresh: settings.Fresh,
			ct: token
		);
		if (release is null)
			return 1;

		if (release.Tracks.Count == 0)
		{
			UI.Warn(message: "No tracks found.");
			return 0;
		}

		RenderResultTable(release: release, isDiscogs: isDiscogs);
		await ExportToSheetsIfRequestedAsync(
			release: release,
			wasDeepSearchPerformed: wasEnriched,
			ct: token
		);

		return 0;
	}

	private static async Task<(ReleaseData?, bool)> FetchAndEnrichAsync(
		string id,
		bool isDiscogs,
		bool fresh,
		CancellationToken ct
	)
	{
		IMusicService service;

		if (isDiscogs)
		{
			if (!int.TryParse(s: id, result: out _))
			{
				UI.Error(message: "Invalid Discogs ID (must be number)");
				return (null, false);
			}
			var discogsToken = Secrets.DiscogsToken;
			if (IsNullOrEmpty(value: discogsToken))
			{
				UI.CriticalFailure(service: "Discogs", message: "DISCOGS_USER_TOKEN not set");
				return (null, false);
			}
			service = new DiscogsService(token: discogsToken);
		}
		else
		{
			if (!Guid.TryParse(input: id, result: out _))
			{
				UI.Error(message: "Invalid MusicBrainz ID (must be GUID)");
				return (null, false);
			}
			service = new MusicBrainzService();
		}

		try
		{
			ReleaseData? release = null;
			var sourceName = isDiscogs ? "Discogs" : "MusicBrainz";

			await StatusExtensions
				.SpinnerStyle(
					StatusExtensions.Spinner(AnsiConsole.Status(), spinner: Spinner.Known.Dots),
					Style.Parse(text: "cyan")
				)
				.StartAsync(
					UI.Cyan($"Fetching release info from {sourceName}..."),
					async _ => release = await service.GetReleaseAsync(releaseId: id, ct: ct)
				);

			if (release is null)
				return (null, false);

			var wasEnriched = !isDiscogs;
			if (!isDiscogs)
			{
				List<TrackInfo> enrichedTracks =
					await MusicScoringService.EnrichTracksWithProgressAsync(
						service: service,
						releaseId: id,
						releaseTitle: release.Info.Title,
						tracks: release.Tracks,
						fresh: fresh,
						ct: ct
					);
				release = new ReleaseData(Info: release.Info, Tracks: enrichedTracks);
			}

			return (release, wasEnriched);
		}
		finally
		{
			(service as IDisposable)?.Dispose();
		}
	}

	private static void RenderResultTable(ReleaseData release, bool isDiscogs)
	{
		ReleaseInfo info = release.Info;
		TrackInfo header = release.Tracks[index: 0];

		UI.NewLine();
		UI.Rule(text: "Release Info");
		UI.NewLine();
		UI.Field(label: "Release:", value: info.Title);
		UI.Field(label: "Artist:", value: info.Artist);
		UI.Field(label: "Year:", info.Year?.ToString());
		UI.Field(label: "Label:", value: info.Label);
		UI.Field(label: "Catalog:", value: info.CatalogNumber);
		UI.FieldIfPresent(label: "Conductor:", value: header.Conductor);
		UI.FieldIfPresent(label: "Orchestra:", value: header.Orchestra);
		UI.FieldIfPresent(label: "Venue:", value: header.RecordingVenue);
		if (header.Soloists.Count > 0)
			UI.Field(label: "Soloists:", $"{header.Soloists.Count} listed");

		UI.Field(label: "Discs:", info.DiscCount.ToString());
		UI.Field(label: "Tracks:", info.TrackCount.ToString());
		if (info.TotalDuration.HasValue && info.TotalDuration.Value > TimeSpan.Zero)
		{
			TimeSpan td = info.TotalDuration.Value;
			var durationText =
				td.Days > 0 ? $"{td.Days}d {td.Hours}h {td.Minutes}m"
				: td.Hours > 0 ? $"{td.Hours}h {td.Minutes}m"
				: $"{td.Minutes}m {td.Seconds}s";
			UI.Field(label: "Duration:", value: durationText);
		}
		UI.NewLine();

		SpectreTable table = new();
		HasTableBorderExtensions.Border(table, border: TableBorder.Simple);

		if (isDiscogs)
		{
			TableExtensions.AddColumn(table, column: "Disc");
			TableExtensions.AddColumn(table, column: "Track");
			TableExtensions.AddColumn(table, column: "Title");
			TableExtensions.AddColumn(table, column: "Duration");

			foreach (TrackInfo track in release.Tracks)
			{
				var duration =
					track.Duration is { } d && d > TimeSpan.Zero
						? d.ToString(format: @"m\:ss")
						: "";
				TableExtensions.AddRow(
					table,
					track.DiscNumber.ToString(),
					track.TrackNumber.ToString(),
					Markup.Escape(text: track.Title),
					duration
				);
			}
		}
		else
		{
			table.AddColumn(
				AlignableExtensions.Centered(
					ColumnExtensions.NoWrap(new TableColumn(header: "Disc"))
				)
			);
			table.AddColumn(
				AlignableExtensions.Centered(
					ColumnExtensions.NoWrap(new TableColumn(header: "Tracks"))
				)
			);
			table.AddColumn(ColumnExtensions.NoWrap(new TableColumn(header: "Work")));
			TableExtensions.AddColumn(table, column: "Composer");
			table.AddColumn(
				AlignableExtensions.Centered(
					ColumnExtensions.NoWrap(new TableColumn(header: "Year"))
				)
			);
			table.AddColumn(
				AlignableExtensions.RightAligned(
					ColumnExtensions.NoWrap(new TableColumn(header: "Duration"))
				)
			);
			TableExtensions.AddColumn(table, column: "Conductor");
			TableExtensions.AddColumn(table, column: "Orchestra");
			TableExtensions.AddColumn(table, column: "Soloists");

			List<WorkSummary> works = WorkGrouper.Group(tracks: release.Tracks);
			foreach (WorkSummary work in works)
			{
				var duration =
					work.TotalDuration > TimeSpan.Zero
						? work.TotalDuration.ToString(format: @"m\:ss")
						: "";
				var soloists =
					work.Soloists.Count > 0 ? Join(separator: ", ", values: work.Soloists) : "";

				TableExtensions.AddRow(
					table,
					work.Disc.ToString(),
					work.TrackRange,
					Markup.Escape(text: work.Work),
					Markup.Escape(work.Composer ?? ""),
					work.YearDisplay,
					duration,
					Markup.Escape(work.Conductor ?? ""),
					Markup.Escape(work.Orchestra ?? ""),
					Markup.Escape(text: soloists)
				);
			}
		}

		AnsiConsole.Write(renderable: table);
		UI.NewLine();
	}

	private static async Task ExportToSheetsIfRequestedAsync(
		ReleaseData release,
		bool wasDeepSearchPerformed,
		CancellationToken ct
	)
	{
		if (wasDeepSearchPerformed)
			await MusicExporter.ExportToSheetsAsync(release: release, ct: ct);
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(position: 0, template: "[input]")]
		[Description(
			description: "Search query or release ID (GUID for MusicBrainz, number for Discogs)"
		)]
		public string? Input { get; init; }

		[CommandOption(template: "-s|--source")]
		[Description(description: "discogs (default), musicbrainz (or mb), both")]
		[DefaultValue(value: "discogs")]
		[AllowedValues("discogs", "musicbrainz", "mb", "both")]
		public string Source { get; init; } = "discogs";

		[CommandOption(template: "-m|--mode")]
		[Description(description: "pop (default) or classical (changes default columns)")]
		[DefaultValue(value: "pop")]
		[AllowedValues("pop", "classical")]
		public string Mode { get; init; } = "pop";

		[CommandOption(template: "-t|--type")]
		[Description(
			description: "Filter: album, ep, single, compilation (normalized across APIs)"
		)]
		[AllowedValues("album", "ep", "single", "compilation")]
		public string? Type { get; init; }

		[CommandOption(template: "-n|--limit")]
		[Description(description: "Max results per source (default 10)")]
		[DefaultValue(value: 10)]
		public int Limit { get; init; } = 10;

		[CommandOption(template: "-f|--fields")]
		[Description(
			description: "Comma-separated field list: artist,title,year,type,id,label,format,country,genres,score,catno,barcode"
		)]
		public string? Fields { get; init; }

		[CommandOption(template: "-v|--verbose")]
		[Description(description: "Verbose output: filter stats, extra columns, save JSON dumps")]
		public bool Verbose { get; init; }

		[CommandOption(template: "-y|--yes")]
		[Description(description: "Auto-confirm deep search for --id mode")]
		public bool AutoConfirm { get; init; }

		[CommandOption(template: "--fresh")]
		[Description(description: "Clear cached state and force fresh API fetch")]
		public bool Fresh { get; init; }
	}
}
