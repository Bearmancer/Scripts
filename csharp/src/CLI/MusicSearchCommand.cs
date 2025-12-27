namespace CSharpScripts.CLI.Commands;

#region JSON Configuration

file static class JsonOptions
{
    internal static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
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
        if (!IsNullOrEmpty(value: settings.Id))
            return await PerformLookupAsync(settings: settings, ct: cancellationToken);

        string? discogsToken = Config.DiscogsToken;
        string source = settings.Source.ToLowerInvariant();

        bool searchMusicBrainz = source is "musicbrainz" or "mb" or "both";
        bool searchDiscogs = source is "discogs" or "both";

        if (searchDiscogs && IsNullOrEmpty(value: discogsToken))
        {
            Console.Warning(message: "DISCOGS_USER_TOKEN not set, using MusicBrainz");
            searchDiscogs = false;
            searchMusicBrainz = true;
        }

        string sourceLabel =
            searchMusicBrainz && searchDiscogs ? "Discogs + MusicBrainz"
            : searchDiscogs ? "Discogs"
            : "MusicBrainz";

        Console.Info(message: "Searching {0}...", sourceLabel);

        List<SearchResult> results = [];
        var filteredCount = 0;

        if (searchMusicBrainz)
        {
            MusicBrainzService mb = new();
            var mbResults = await mb.SearchAsync(
                settings.Query!,
                maxResults: settings.Limit,
                ct: cancellationToken
            );
            results.AddRange(collection: mbResults);
        }

        if (searchDiscogs)
        {
            DiscogsService discogs = new(token: discogsToken);
            var discogsResults = await discogs.SearchAsync(
                settings.Query!,
                maxResults: settings.Limit,
                ct: cancellationToken
            );

            discogsResults =
            [
                .. discogsResults.Select(r =>
                    r with
                    {
                        Score = CalculateRelevanceScore(settings.Query!, r: r),
                    }
                ),
            ];

            results.AddRange(collection: discogsResults);
        }

        results = [.. results.OrderByDescending(r => r.Score ?? 0)];

        if (!IsNullOrEmpty(value: settings.Type))
        {
            int beforeCount = results.Count;
            string normalizedFilter = NormalizeType(input: settings.Type);

            results = [.. results.Where(r => MatchesType(r: r, filter: normalizedFilter))];
            filteredCount = beforeCount - results.Count;

            if (settings.Verbose)
                Console.Dim(
                    $"[DEBUG] Filter '{settings.Type}' -> normalized '{normalizedFilter}', removed {filteredCount}"
                );
        }

        int trackCount = results.Count(predicate: IsTrackResult);
        if (trackCount > 0)
        {
            results = [.. results.Where(r => !IsTrackResult(r: r))];
            filteredCount += trackCount;

            if (settings.Verbose)
                Console.Dim(
                    $"[DEBUG] Excluded {trackCount} track-level results (focusing on collections)"
                );
        }

        if (settings.Verbose && results.Count > 0)
            SaveSearchDumps(settings.Query!, results: results);

        if (results.Count == 0)
        {
            Console.Warning(message: "No results found.");
            return 0;
        }

        if (
            settings.Output.Equals(
                value: "json",
                comparisonType: StringComparison.OrdinalIgnoreCase
            )
        )
        {
            string json = JsonSerializer.Serialize(value: results, options: JsonOptions.Indented);
            Console.WriteLine(text: json);
            return 0;
        }

        var columns = GetColumns(settings: settings);

        SpectreTable table = new();
        table.Border(border: TableBorder.Rounded);
        foreach (string col in columns)
            table.AddColumn(column: col);

        foreach (var r in results)
        {
            List<string> values = [.. columns.Select(col => GetFieldValue(column: col, r: r))];
            table.AddRow([.. values]);
        }

        Console.Write(table: table);

        return 0;
    }

    private static List<string> GetColumns(Settings settings)
    {
        if (!IsNullOrEmpty(value: settings.Fields))
            return
            [
                .. settings
                    .Fields.Split(
                        separator: ',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .Select(selector: NormalizeFieldName),
            ];

        bool isClassical = settings.Mode.Equals(
            value: "classical",
            comparisonType: StringComparison.OrdinalIgnoreCase
        );

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
        string value = column switch
        {
            "Artist" => r.Artist ?? "",
            "Title" => MakeTitleLink(r: r),
            "Year" => r.Year?.ToString(provider: CultureInfo.InvariantCulture) ?? "",
            "Type" => NormalizeTypeForDisplay(type: r.ReleaseType) ?? "",
            "ID" => MakeIdLink(r: r),
            "Source" => Console.SourceBadge(r.Source.ToString()),
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

        return column is "ID" or "Source" or "Title" ? value : Console.Escape(text: value);
    }

    #endregion

    #region Type Filtering & Scoring

    private static bool IsTrackResult(SearchResult r)
    {
        if (IsNullOrEmpty(value: r.ReleaseType))
            return false;

        string type = r.ReleaseType.ToLowerInvariant();

        return type is "recording" or "track" or "single"
            && r.Format?.Contains(value: "Single", comparisonType: StringComparison.Ordinal)
                != true;
    }

    private static int CalculateRelevanceScore(string query, SearchResult r)
    {
        string queryLower = query.ToLowerInvariant();
        string titleLower = r.Title.ToLowerInvariant();
        string? artistLower = r.Artist?.ToLowerInvariant();

        if (titleLower == queryLower)
            return 100;

        if (artistLower is { } && $"{artistLower} {titleLower}" == queryLower)
            return 100;

        var queryTerms = queryLower
            .Split(separator: ' ', options: StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
        var resultTerms = titleLower
            .Split(separator: ' ', options: StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
        if (artistLower is { })
            resultTerms.UnionWith(
                artistLower.Split(separator: ' ', options: StringSplitOptions.RemoveEmptyEntries)
            );

        int matchingTerms = queryTerms.Count(qt =>
            resultTerms.Any(rt =>
                rt.Contains(value: qt, comparisonType: StringComparison.Ordinal)
                || qt.Contains(value: rt, comparisonType: StringComparison.Ordinal)
            )
        );
        double termScore =
            queryTerms.Count > 0 ? (double)matchingTerms / queryTerms.Count * 100 : 0;

        double substringBonus = 0;
        if (titleLower.Contains(value: queryLower, comparisonType: StringComparison.Ordinal))
            substringBonus = 30;
        else if (
            artistLower?.Contains(value: queryLower, comparisonType: StringComparison.Ordinal)
            == true
        )
            substringBonus = 20;

        var score = (int)Math.Min(val1: 100, termScore + substringBonus);
        return Math.Max(val1: 1, val2: score);
    }

    private static void SaveSearchDumps(string query, List<SearchResult> results)
    {
        var timestamp = DateTime.Now.ToString(
            format: "yyyyMMdd-HHmmss",
            provider: CultureInfo.InvariantCulture
        );
        string sanitizedQuery = SanitizeForFolder(input: query);
        var folderName = $"{timestamp}-{sanitizedQuery}";
        string dumpDir = Combine(
            path1: Paths.DumpsDirectory,
            path2: "music-search",
            path3: folderName
        );

        CreateDirectory(path: dumpDir);

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[index: i];
            string source = result.Source == MusicSource.Discogs ? "discogs" : "musicbrainz";
            var fileName = $"{i + 1:D3}-{source}-{result.Id}.json";
            string filePath = Combine(path1: dumpDir, path2: fileName);

            string json = JsonSerializer.Serialize(value: result, options: JsonOptions.Indented);
            WriteAllText(path: filePath, contents: json);
        }

        string allPath = Combine(path1: dumpDir, path2: "_all-results.json");
        string allJson = JsonSerializer.Serialize(value: results, options: JsonOptions.Indented);
        WriteAllText(path: allPath, contents: allJson);

        Console.Dim($"[DEBUG] Saved {results.Count} results to: {dumpDir}");
    }

    private static string SanitizeForFolder(string input)
    {
        char[] invalid = GetInvalidFileNameChars();
        string sanitized = new([.. input.Select(c => invalid.Contains(value: c) ? '_' : c)]);
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
        if (IsNullOrEmpty(value: r.ReleaseType))
            return false;

        string normalized = r.ReleaseType.ToLowerInvariant();

        return filter switch
        {
            "album" => normalized is "album" or "master",
            "ep" => normalized.Contains(value: "ep", comparisonType: StringComparison.Ordinal),
            "single" => normalized.Contains(
                value: "single",
                comparisonType: StringComparison.Ordinal
            ),
            "compilation" => normalized.Contains(
                value: "compilation",
                comparisonType: StringComparison.Ordinal
            ),
            "master" => normalized is "master",
            "release" => normalized is "release",
            _ => normalized.Contains(value: filter, comparisonType: StringComparison.Ordinal),
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
        string url =
            r.Source == MusicSource.Discogs
                ? $"https://www.discogs.com/release/{r.Id}"
                : $"https://musicbrainz.org/release/{r.Id}";

        return $"[link={url}]{r.Id}[/]";
    }

    private static string MakeTitleLink(SearchResult r)
    {
        string url =
            r.Source == MusicSource.Discogs
                ? $"https://www.discogs.com/release/{r.Id}"
                : $"https://musicbrainz.org/release/{r.Id}";

        string escapedTitle = Console.Escape(text: r.Title);
        return $"[link={url}]{escapedTitle}[/]";
    }

    #endregion

    #region Execute - Lookup Mode (--id)

    private static async Task<int> PerformLookupAsync(Settings settings, CancellationToken ct)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            token1: ct,
            token2: Program.Cts.Token
        );
        var token = linkedCts.Token;

        string source = settings.Source.ToLowerInvariant();
        bool isDiscogs = source is "discogs";

        IMusicService service;

        if (isDiscogs)
        {
            if (!int.TryParse(s: settings.Id, result: out _))
            {
                Console.Error(message: "Invalid Discogs ID (must be number)");
                return 1;
            }
            string? discogsToken = Config.DiscogsToken;
            if (IsNullOrEmpty(value: discogsToken))
            {
                Console.CriticalFailure(service: "Discogs", message: "DISCOGS_USER_TOKEN not set");
                return 1;
            }
            service = new DiscogsService(token: discogsToken);
        }
        else
        {
            if (!Guid.TryParse(input: settings.Id, result: out _))
            {
                Console.Error(message: "Invalid MusicBrainz ID (must be GUID)");
                return 1;
            }
            service = new MusicBrainzService();
        }

        ReleaseData? release = null;
        string sourceName = isDiscogs ? "Discogs" : "MusicBrainz";

        await Console
            .Status()
            .Spinner(spinner: Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(text: "cyan"))
            .StartAsync(
                $"[cyan]Fetching release info from {sourceName}...[/]",
                async ctx =>
                {
                    release = await service.GetReleaseAsync(
                        settings.Id!,
                        deepSearch: false,
                        ct: token
                    );
                }
            );

        if (release is null || release.Tracks.Count == 0)
        {
            Console.Warning(message: "No tracks found.");
            return 0;
        }

        var info = release.Info;
        var header = release.Tracks[index: 0];

        Console.NewLine();
        Console.Rule(text: "Release Info");
        Console.NewLine();
        Console.Field(label: "Release:", value: info.Title);
        Console.Field(label: "Artist:", value: info.Artist);
        Console.Field(label: "Year:", info.Year?.ToString());
        Console.Field(label: "Label:", value: info.Label);
        Console.Field(label: "Catalog:", value: info.CatalogNumber);
        Console.FieldIfPresent(label: "Conductor:", value: header.Conductor);
        Console.FieldIfPresent(label: "Orchestra:", value: header.Orchestra);
        Console.FieldIfPresent(label: "Venue:", value: header.RecordingVenue);
        if (header.Soloists.Count > 0)
            Console.Field(label: "Soloists:", $"{header.Soloists.Count} listed");

        Console.Field(label: "Discs:", info.DiscCount.ToString());
        Console.Field(label: "Tracks:", info.TrackCount.ToString());
        if (info.TotalDuration.HasValue && info.TotalDuration.Value > TimeSpan.Zero)
        {
            var td = info.TotalDuration.Value;
            string durationText =
                td.Days > 0 ? $"{td.Days}d {td.Hours}h {td.Minutes}m"
                : td.Hours > 0 ? $"{td.Hours}h {td.Minutes}m"
                : $"{td.Minutes}m {td.Seconds}s";
            Console.Field(label: "Duration:", value: durationText);
        }
        Console.NewLine();

        if (!isDiscogs)
        {
            bool deepSearch = settings.AutoConfirm;
            if (!deepSearch)
            {
                string choice = Console.Prompt(
                    new SelectionPrompt<string>()
                        .Title(title: "Fetch full track metadata (recordings, composers, etc)?")
                        .AddChoices("Yes", "No")
                );
                deepSearch = choice == "Yes";
            }

            if (deepSearch)
            {
                var enrichedTracks = await EnrichTracksWithProgressAsync(
                    (MusicBrainzService)service,
                    settings.Id!,
                    releaseTitle: info.Title,
                    tracks: release.Tracks,
                    fresh: settings.Fresh,
                    ct: token
                );
                release = new ReleaseData(Info: info, Tracks: enrichedTracks);
                string sheetUrl = MusicExporter.ExportToSheets(release);
            }
        }

        SpectreTable table = new();
        table.Border(border: TableBorder.Simple);

        if (isDiscogs)
        {
            table.AddColumn(column: "Disc");
            table.AddColumn(column: "Track");
            table.AddColumn(column: "Title");
            table.AddColumn(column: "Duration");

            foreach (var track in release.Tracks)
            {
                string duration =
                    track.Duration is { } d && d > TimeSpan.Zero ? d.ToString(@"m\:ss") : "";
                table.AddRow(
                    track.DiscNumber.ToString(),
                    track.TrackNumber.ToString(),
                    Console.Escape(text: track.Title),
                    duration
                );
            }
        }
        else
        {
            table.AddColumn(new TableColumn(header: "Disc").NoWrap().Centered());
            table.AddColumn(new TableColumn(header: "Tracks").NoWrap().Centered());
            table.AddColumn(new TableColumn(header: "Work").NoWrap());
            table.AddColumn(column: "Composer");
            table.AddColumn(new TableColumn(header: "Year").NoWrap().Centered());
            table.AddColumn(new TableColumn(header: "Duration").NoWrap().RightAligned());
            table.AddColumn(column: "Conductor");
            table.AddColumn(column: "Orchestra");
            table.AddColumn(column: "Soloists");

            var works = GroupTracksByWork(tracks: release.Tracks);
            foreach (var work in works)
            {
                string duration =
                    work.TotalDuration > TimeSpan.Zero
                        ? work.TotalDuration.ToString(format: @"m\:ss")
                        : "";
                string soloists =
                    work.Soloists.Count > 0 ? Join(separator: ", ", values: work.Soloists) : "";

                table.AddRow(
                    work.Disc.ToString(),
                    work.TrackRange,
                    Console.Escape(text: work.Work),
                    Console.Escape(work.Composer ?? ""),
                    work.YearDisplay,
                    duration,
                    Console.Escape(work.Conductor ?? ""),
                    Console.Escape(work.Orchestra ?? ""),
                    Console.Escape(text: soloists)
                );
            }
        }

        Console.Write(table: table);
        Console.NewLine();
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

        int currentDisc = -1;
        string? currentWorkName = null;
        List<TrackInfo> currentGroup = [];

        void FlushGroup()
        {
            if (currentGroup.Count == 0)
                return;

            var first = currentGroup[index: 0];
            List<int> years =
            [
                .. currentGroup
                    .Select(t => t.RecordingYear)
                    .Where(y => y.HasValue)
                    .Select(y => y!.Value)
                    .Distinct()
                    .OrderBy(y => y),
            ];

            var totalDuration = currentGroup
                .Where(t => t.Duration.HasValue)
                .Aggregate(seed: TimeSpan.Zero, (sum, t) => sum + t.Duration!.Value);

            List<string> soloists = [.. currentGroup.SelectMany(t => t.Soloists).Distinct()];

            string displayWork = first.WorkName ?? first.Title;

            works.Add(
                new WorkSummary(
                    Disc: first.DiscNumber,
                    FirstTrack: currentGroup[index: 0].TrackNumber,
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

        foreach (var track in tracks)
        {
            string workKey = track.WorkName ?? track.Title;

            if (track.DiscNumber != currentDisc || workKey != currentWorkName)
            {
                FlushGroup();
                currentDisc = track.DiscNumber;
                currentWorkName = workKey;
            }

            currentGroup.Add(item: track);
        }

        FlushGroup();

        DetectMissingWorkHierarchy(works: works);

        return works;
    }

    private static void DetectMissingWorkHierarchy(List<WorkSummary> works)
    {
        List<string> suspectedMissing = [];

        for (var i = 0; i < works.Count - 1; i++)
        {
            var current = works[index: i];
            var next = works[i + 1];

            if (current.FirstTrack != current.LastTrack || next.FirstTrack != next.LastTrack)
                continue;

            if (current.Disc != next.Disc)
                continue;

            int currentColon = current.Work.IndexOf(
                value: ':',
                comparisonType: StringComparison.Ordinal
            );
            int nextColon = next.Work.IndexOf(value: ':', comparisonType: StringComparison.Ordinal);

            if (currentColon > 5 && nextColon > 5)
            {
                string currentPrefix = current.Work[..currentColon];
                string nextPrefix = next.Work[..nextColon];

                if (currentPrefix == nextPrefix && !suspectedMissing.Contains(item: currentPrefix))
                    suspectedMissing.Add(item: currentPrefix);
            }
        }

        foreach (string missing in suspectedMissing)
        {
            if (!LoggedWorkHierarchyWarnings.Add(item: missing))
                continue;

            Console.Warning(
                message: "Work hierarchy missing for '{0}' - tracks not grouped",
                missing
            );
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
        int total = tracks.Count;

        Logger.Start(service: ServiceType.Music);
        Logger.Event(
            eventName: "ReleaseStart",
            new Dictionary<string, object>
            {
                [key: "ReleaseId"] = releaseId,
                [key: "ReleaseTitle"] = releaseTitle,
                [key: "TotalTracks"] = total,
            }
        );

        if (fresh)
        {
            ReleaseProgressCache.Delete(releaseId: releaseId);
            StateManager.DeleteReleaseCache(releaseId: releaseId);
            Console.Info(message: "Cleared cached state for fresh fetch");
        }

        var enrichedTracks = ReleaseProgressCache.Load(releaseId: releaseId);
        int startIndex = enrichedTracks.Count;
        var resumeSource = "none";

        if (startIndex > 0)
        {
            resumeSource = "CSV";
        }
        else
        {
            var cachedState = StateManager.LoadReleaseCache<MusicBrainzEnrichmentState>(
                releaseId: releaseId
            );
            if (cachedState is { } && cachedState.TotalTracks == total)
            {
                enrichedTracks = cachedState.EnrichedTracks;
                startIndex = enrichedTracks.Count;
                resumeSource = "JSON";
            }
        }

        if (startIndex > 0)
        {
            Console.Info(
                message: "Resuming from {0} (track {1}/{2})",
                resumeSource,
                startIndex + 1,
                total
            );
            Logger.Event(
                eventName: "ReleaseResume",
                new Dictionary<string, object>
                {
                    [key: "Source"] = resumeSource,
                    [key: "TracksEnriched"] = startIndex,
                }
            );

            foreach (var t in enrichedTracks.TakeLast(count: 3))
                Console.MarkupLine(
                    $"  [dim]└[/] {t.DiscNumber}.{t.TrackNumber:D2} {Console.Escape(text: t.Title)}"
                );
            Console.NewLine();
        }

        if (startIndex >= total)
        {
            Console.Success(message: "All tracks already enriched from cache");
            StateManager.DeleteReleaseCache(releaseId: releaseId);
            return enrichedTracks;
        }

        Queue<(string Header, string Detail)> recentTracks = new();
        int completed = startIndex;
        var cancelled = false;

        static (string Header, string Detail) FormatTrackDetail(TrackInfo t)
        {
            var discTrack = $"{t.DiscNumber}.{t.TrackNumber:D2}";
            string title = t.Title;
            string duration = t.Duration?.ToString(format: @"m\:ss") ?? "";
            string header = IsNullOrEmpty(value: duration)
                ? $"[{discTrack}] {title}"
                : $"[{discTrack}] {title} ({duration})";

            List<string> parts = [];

            if (!IsNullOrEmpty(value: t.WorkName))
                parts.Add(Console.Work(text: t.WorkName));

            int? year = t.RecordingYear;
            if (!IsNullOrEmpty(value: t.Composer))
                parts.Add(
                    Console.Combine(Console.Composer(text: t.Composer), Console.Year(year: year))
                );
            else if (year is { } y)
                parts.Add($"({y})");

            string performer = t.Orchestra ?? t.Artist ?? "";
            if (!IsNullOrEmpty(value: performer) && performer != t.Composer)
                parts.Add($"• {Console.Orchestra(text: performer)}");

            if (
                !IsNullOrEmpty(value: t.Conductor)
                && t.Conductor != t.Composer
                && t.Conductor != performer
            )
                parts.Add($"cond. {Console.Conductor(text: t.Conductor)}");

            if (!IsNullOrEmpty(value: t.RecordingVenue))
                parts.Add(Console.Venue(text: t.RecordingVenue));

            if (t.Soloists.Count > 0)
                parts.Add($"feat. {Join(separator: ", ", values: t.Soloists)}");

            return (header, Join(separator: " ", values: parts));
        }

        void SaveState()
        {
            StateManager.SaveReleaseCache(
                releaseId: releaseId,
                new MusicBrainzEnrichmentState(
                    ReleaseId: releaseId,
                    TotalTracks: total,
                    EnrichedTracks: enrichedTracks,
                    LastUpdated: DateTime.Now
                )
            );
        }

        Console.Suppress = true;

        var fillTimer = System.Diagnostics.Stopwatch.StartNew();
        await Console
            .CreateProgress()
            .AutoClear(enabled: true)
            .HideCompleted(enabled: false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            )
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(
                    Console.TaskDescription(
                        prefix: $"({completed}/{total})",
                        title: releaseTitle,
                        $"(0/{total} tracks)"
                    ),
                    maxValue: total
                );
                task.Value = startIndex;

                for (int i = startIndex; i < tracks.Count; i++)
                {
                    var track = tracks[index: i];

                    if (ct.IsCancellationRequested)
                    {
                        cancelled = true;
                        SaveState();
                        break;
                    }

                    try
                    {
                        var enriched = await ((MusicBrainzService)service).EnrichTrackAsync(
                            track: track,
                            ct: ct
                        );
                        enrichedTracks.Add(item: enriched);
                        ReleaseProgressCache.AppendTrack(releaseId: releaseId, track: enriched);
                        completed++;

                        var info = FormatTrackDetail(t: enriched);
                        recentTracks.Enqueue(item: info);
                        if (recentTracks.Count > 5)
                            recentTracks.Dequeue();

                        if (completed % 10 == 0)
                            Logger.Event(
                                eventName: "TrackProgress",
                                new Dictionary<string, object>
                                {
                                    [key: "Completed"] = completed,
                                    [key: "Total"] = total,
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
                        Console.Error(message: "Error: {0}", ex.Message);
                        cancelled = true;
                        break;
                    }
                }
            });

        Console.Suppress = false;
        Console.NewLine();

        if (cancelled)
        {
            Console.Warning(message: "Enrichment interrupted at {0}/{1} tracks", completed, total);
            Console.Info(
                message: "Run the same command again to resume from track {0}",
                completed + 1
            );
            Logger.Interrupted($"{completed}/{total} tracks");
        }
        else
        {
            Console.Complete($"Enriched {total} tracks");

            var works = GroupTracksByWork(tracks: enrichedTracks);
            Logger.End(success: true, $"{total} tracks, {works.Count} works");

            MusicExporter.ExportWorksToCSV(releaseTitle: releaseTitle, works: works);

            StateManager.DeleteReleaseCache(releaseId: releaseId);
            ReleaseProgressCache.Delete(releaseId: releaseId);
        }

        return enrichedTracks;
    }

    #endregion
}
