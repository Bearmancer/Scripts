namespace CSharpScripts.CLI.Music;

internal sealed class MusicSearchCommand : BaseAsyncCommand<MusicSearchCommand.Settings>
{
	private static readonly JsonSerializerOptions IndentedJson = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(0, "[input]")]
		[Description("Search query or release ID (GUID for MusicBrainz, number for Discogs)")]
		public string? Input { get; init; }

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

		[CommandOption("-f|--fields")]
		[Description(
			"Comma-separated field list: artist,title,year,type,id,label,format,country,genres,score,catno,barcode"
		)]
		public string? Fields { get; init; }

		[CommandOption("-v|--verbose")]
		[Description("Verbose output: filter stats, extra columns, save JSON dumps")]
		public bool Verbose { get; init; }

		[CommandOption("-y|--yes")]
		[Description("Auto-confirm deep search for --id mode")]
		public bool AutoConfirm { get; init; }

		[CommandOption("--fresh")]
		[Description("Clear cached state and force fresh API fetch")]
		public bool Fresh { get; init; }
	}

	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		if (IsNullOrEmpty(settings.Input))
		{
			UI.Error("Input required: provide a search query or release ID");
			return 1;
		}

		if (Guid.TryParse(settings.Input, out _))
			return await PerformLookupAsync(
				settings,
				settings.Input,
				isMusicBrainz: true,
				ct: cancellationToken
			);

		if (int.TryParse(settings.Input, out _))
			return await PerformLookupAsync(
				settings,
				settings.Input,
				isMusicBrainz: false,
				ct: cancellationToken
			);

		return await ExecuteWithErrorHandlingAsync(
			ServiceType.Music,
			async () =>
			{
				List<SearchResult> results = await RunSearchAsync(settings, cancellationToken);

				if (results.Count == 0)
				{
					UI.Warn("No results found.");
					return;
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

				AnsiConsole.Write(table);
			}
		);
	}

	private static async Task<List<SearchResult>> RunSearchAsync(
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		var discogsToken = Secrets.DiscogsToken;
		var source = settings.Source.ToLowerInvariant();

		var searchMusicBrainz = source is "musicbrainz" or "mb" or "both";
		var searchDiscogs = source is "discogs" or "both";

		if (searchDiscogs && IsNullOrEmpty(discogsToken))
		{
			UI.Warn("DISCOGS_USER_TOKEN not set, using MusicBrainz");
			searchDiscogs = false;
			searchMusicBrainz = true;
		}

		var sourceLabel =
			searchMusicBrainz && searchDiscogs ? "Discogs + MusicBrainz"
			: searchDiscogs ? "Discogs"
			: "MusicBrainz";

		UI.Info("Searching {0}...", sourceLabel);

		List<SearchResult> results = [];

		if (searchMusicBrainz)
		{
			MusicBrainzService mb = new();
			List<SearchResult> mbResults = await mb.SearchAsync(
				settings.Input!,
				maxResults: settings.Limit,
				ct: cancellationToken
			);
			results.AddRange(mbResults);
		}

		if (searchDiscogs)
		{
			using DiscogsService discogs = new(discogsToken);
			List<SearchResult> discogsResults = await discogs.SearchAsync(
				settings.Input!,
				maxResults: settings.Limit,
				ct: cancellationToken
			);

			discogsResults =
			[
				.. discogsResults.Select(r =>
					r with
					{
						Score = MusicScoringService.CalculateRelevanceScore(settings.Input!, r),
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

			results = [.. results.Where(r => MusicScoringService.MatchesType(r, normalizedFilter))];
			var filteredCount = beforeCount - results.Count;

			if (settings.Verbose)
			{
				Log.Debug(
					"MusicSearch_Filter {FilterType} {NormalizedFilter} {RemovedCount}",
					settings.Type,
					normalizedFilter,
					filteredCount
				);
			}
		}

		var trackCount = results.Count(MusicScoringService.IsTrackResult);
		if (trackCount > 0)
		{
			results = [.. results.Where(r => !MusicScoringService.IsTrackResult(r))];

			if (settings.Verbose)
			{
				Log.Debug("MusicSearch_ExcludedTracks {TrackCount}", trackCount);
			}
		}

		if (settings.Verbose && results.Count > 0)
		{
			SaveSearchDumps(settings.Input!, results);
		}

		return results;
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
			"Source" => r.Source == MusicSource.Discogs
				? UI.Yellow("Discogs")
				: UI.Cyan("MusicBrainz"),
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

		return column is "ID" or "Source" or "Title" ? value : Markup.Escape(value);
	}

	private static void SaveSearchDumps(string query, List<SearchResult> results)
	{
		var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
		var sanitizedQuery = SanitizeForFolder(query);
		var folderName = $"{timestamp}-{sanitizedQuery}";
		var dumpDir = Path.Combine(Paths.DumpsDirectory, "music-search", folderName);

		Directory.CreateDirectory(dumpDir);

		for (var i = 0; i < results.Count; i++)
		{
			SearchResult result = results[i];
			var source = result.Source == MusicSource.Discogs ? "discogs" : "musicbrainz";
			var fileName = $"{i + 1:D3}-{source}-{result.Id}.json";
			var filePath = Path.Combine(dumpDir, fileName);

			var json = JsonSerializer.Serialize(result, IndentedJson);
			File.WriteAllText(filePath, json);
		}

		var allPath = Path.Combine(dumpDir, "_all-results.json");
		var allJson = JsonSerializer.Serialize(results, IndentedJson);
		File.WriteAllText(allPath, allJson);

		Log.Debug("Saved {0} results to: {1}", results.Count, dumpDir);
	}

	private static string SanitizeForFolder(string input)
	{
		var invalid = Path.GetInvalidFileNameChars();
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

		return UI.LinkText(url, r.Id);
	}

	private static string MakeTitleLink(SearchResult r)
	{
		var url =
			r.Source == MusicSource.Discogs
				? $"https://www.discogs.com/release/{r.Id}"
				: $"https://musicbrainz.org/release/{r.Id}";

		return UI.LinkText(url, r.Title);
	}

	private static async Task<int> PerformLookupAsync(
		Settings settings,
		string id,
		bool isMusicBrainz,
		CancellationToken ct
	)
	{
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
			ct,
			Program.Cts.Token
		);
		CancellationToken token = linkedCts.Token;

		var isDiscogs = !isMusicBrainz;

		(ReleaseData? release, var wasEnriched) = await FetchAndEnrichAsync(
			id,
			isDiscogs,
			settings.Fresh,
			token
		);
		if (release is null)
			return 1;

		if (release.Tracks.Count == 0)
		{
			UI.Warn("No tracks found.");
			return 0;
		}

		RenderResultTable(release, isDiscogs);
		await ExportToSheetsIfRequestedAsync(release, wasEnriched, token);

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
			if (!int.TryParse(id, out _))
			{
				UI.Error("Invalid Discogs ID (must be number)");
				return (null, false);
			}
			var discogsToken = Secrets.DiscogsToken;
			if (IsNullOrEmpty(discogsToken))
			{
				UI.CriticalFailure("Discogs", "DISCOGS_USER_TOKEN not set");
				return (null, false);
			}
			service = new DiscogsService(discogsToken);
		}
		else
		{
			if (!Guid.TryParse(id, out _))
			{
				UI.Error("Invalid MusicBrainz ID (must be GUID)");
				return (null, false);
			}
			service = new MusicBrainzService();
		}

		try
		{
			ReleaseData? release = null;
			var sourceName = isDiscogs ? "Discogs" : "MusicBrainz";

			await AnsiConsole
				.Status()
				.Spinner(Spinner.Known.Dots)
				.SpinnerStyle(Style.Parse("cyan"))
				.StartAsync(
					UI.Cyan($"Fetching release info from {sourceName}..."),
					async _ => release = await service.GetReleaseAsync(id, ct: ct)
				);

			if (release is null)
				return (null, false);

			var wasEnriched = !isDiscogs;
			if (!isDiscogs)
			{
				List<TrackInfo> enrichedTracks =
					await MusicScoringService.EnrichTracksWithProgressAsync(
						service,
						id,
						release.Info.Title,
						release.Tracks,
						fresh,
						ct
					);
				release = new ReleaseData(release.Info, enrichedTracks);
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
		TrackInfo header = release.Tracks[0];

		UI.NewLine();
		UI.Rule("Release Info");
		UI.NewLine();
		UI.Field("Release:", info.Title);
		UI.Field("Artist:", info.Artist);
		UI.Field("Year:", info.Year?.ToString());
		UI.Field("Label:", info.Label);
		UI.Field("Catalog:", info.CatalogNumber);
		UI.FieldIfPresent("Conductor:", header.Conductor);
		UI.FieldIfPresent("Orchestra:", header.Orchestra);
		UI.FieldIfPresent("Venue:", header.RecordingVenue);
		if (header.Soloists.Count > 0)
			UI.Field("Soloists:", $"{header.Soloists.Count} listed");

		UI.Field("Discs:", info.DiscCount.ToString());
		UI.Field("Tracks:", info.TrackCount.ToString());
		if (info.TotalDuration.HasValue && info.TotalDuration.Value > TimeSpan.Zero)
		{
			TimeSpan td = info.TotalDuration.Value;
			var durationText =
				td.Days > 0 ? $"{td.Days}d {td.Hours}h {td.Minutes}m"
				: td.Hours > 0 ? $"{td.Hours}h {td.Minutes}m"
				: $"{td.Minutes}m {td.Seconds}s";
			UI.Field("Duration:", durationText);
		}
		UI.NewLine();

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
					Markup.Escape(track.Title),
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

			List<WorkSummary> works = WorkGrouper.Group(release.Tracks);
			foreach (WorkSummary work in works)
			{
				var duration =
					work.TotalDuration > TimeSpan.Zero ? work.TotalDuration.ToString(@"m\:ss") : "";
				var soloists = work.Soloists.Count > 0 ? Join(", ", work.Soloists) : "";

				table.AddRow(
					work.Disc.ToString(),
					work.TrackRange,
					Markup.Escape(work.Work),
					Markup.Escape(work.Composer ?? ""),
					work.YearDisplay,
					duration,
					Markup.Escape(work.Conductor ?? ""),
					Markup.Escape(work.Orchestra ?? ""),
					Markup.Escape(soloists)
				);
			}
		}

		AnsiConsole.Write(table);
		UI.NewLine();
	}

	private static async Task ExportToSheetsIfRequestedAsync(
		ReleaseData release,
		bool wasDeepSearchPerformed,
		CancellationToken ct
	)
	{
		if (wasDeepSearchPerformed)
			await MusicExporter.ExportToSheetsAsync(release, ct);
	}
}
