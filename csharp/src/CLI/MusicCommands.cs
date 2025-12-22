using CsvHelper;
using CsvHelper.Configuration;
using SearchResult = CSharpScripts.Models.SearchResult;

namespace CSharpScripts.CLI.Commands;

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

static class TrackCsv
{
    static string GetPath(string releaseId) =>
        Combine(Paths.DumpsDirectory, "csv", $"{releaseId}.csv");

    public static void AppendTrack(string releaseId, TrackInfo track)
    {
        string dir = Combine(Paths.DumpsDirectory, "csv");
        CreateDirectory(dir);
        string path = GetPath(releaseId);

        bool writeHeader = !File.Exists(path);
        using StreamWriter writer = new(path, append: true);
        using CsvWriter csv = new(
            writer,
            new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = writeHeader }
        );

        if (writeHeader)
        {
            csv.WriteHeader<TrackInfo>();
            csv.NextRecord();
        }
        csv.WriteRecord(track);
        csv.NextRecord();
    }

    public static List<TrackInfo> Load(string releaseId)
    {
        string path = GetPath(releaseId);
        if (!File.Exists(path))
            return [];

        using StreamReader reader = new(path);
        using CsvReader csv = new(
            reader,
            new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }
        );

        return [.. csv.GetRecords<TrackInfo>()];
    }

    public static void Delete(string releaseId)
    {
        string path = GetPath(releaseId);
        if (File.Exists(path))
            File.Delete(path);
    }
}

public sealed class MusicSearchCommand : AsyncCommand<MusicSearchCommand.Settings>
{
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

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        if (!IsNullOrEmpty(settings.Id))
        {
            return await PerformLookupAsync(settings, cancellationToken);
        }

        string? discogsToken = GetEnvironmentVariable("DISCOGS_USER_TOKEN");
        string source = settings.Source.ToLowerInvariant();

        bool searchMusicBrainz = source is "musicbrainz" or "mb" or "both";
        bool searchDiscogs = source is "discogs" or "both";

        if (searchDiscogs && IsNullOrEmpty(discogsToken))
        {
            Console.Warning("DISCOGS_USER_TOKEN not set, using MusicBrainz");
            searchDiscogs = false;
            searchMusicBrainz = true;
        }

        string sourceLabel =
            searchMusicBrainz && searchDiscogs ? "Discogs + MusicBrainz"
            : searchDiscogs ? "Discogs"
            : "MusicBrainz";

        Console.Info("Searching {0}...", sourceLabel);

        List<SearchResult> results = [];
        int filteredCount = 0;

        if (searchMusicBrainz)
        {
            MusicBrainzService mb = new();
            List<SearchResult> mbResults = await mb.SearchAsync(
                settings.Query!,
                settings.Limit,
                cancellationToken
            );
            results.AddRange(mbResults);
        }

        if (searchDiscogs)
        {
            DiscogsService discogs = new(discogsToken);
            List<SearchResult> discogsResults = await discogs.SearchAsync(
                settings.Query!,
                settings.Limit,
                cancellationToken
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
            int beforeCount = results.Count;
            string normalizedFilter = NormalizeType(settings.Type);

            results = [.. results.Where(r => MatchesType(r, normalizedFilter))];
            filteredCount = beforeCount - results.Count;

            if (settings.Verbose)
            {
                Console.Dim(
                    $"[DEBUG] Filter '{settings.Type}' -> normalized '{normalizedFilter}', removed {filteredCount}"
                );
            }
        }

        int trackCount = results.Count(IsTrackResult);
        if (trackCount > 0)
        {
            results = [.. results.Where(r => !IsTrackResult(r))];
            filteredCount += trackCount;

            if (settings.Verbose)
                Console.Dim(
                    $"[DEBUG] Excluded {trackCount} track-level results (focusing on collections)"
                );
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

        if (settings.Output.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            string json = JsonSerializer.Serialize(results, JsonOptions.Indented);
            Console.WriteLine(json);
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

    /// <summary>
    /// Determine columns based on --fields, --mode, and --debug flags.
    /// </summary>
    private static List<string> GetColumns(Settings settings)
    {
        if (!IsNullOrEmpty(settings.Fields))
        {
            return
            [
                .. settings
                    .Fields.Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .Select(NormalizeFieldName),
            ];
        }

        bool isClassical = settings.Mode.Equals("classical", StringComparison.OrdinalIgnoreCase);

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

    /// <summary>
    /// Normalize field names to consistent casing.
    /// </summary>
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

    /// <summary>
    /// Get field value from result for display.
    /// </summary>
    private static string GetFieldValue(string column, SearchResult r)
    {
        string value = column switch
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
            "Composer" => "", // Not available from search - use lookup
            "Work" => r.Title, // Not available from search - use lookup
            "Performers" => r.Artist ?? "",
            _ => "",
        };

        return column is "ID" or "Source" or "Title" ? value : Console.Escape(value);
    }

    /// <summary>
    /// Check if a result is a track-level entry (not a collection).
    /// Filters based on MusicBrainz Recording type and Discogs track patterns.
    /// </summary>
    private static bool IsTrackResult(SearchResult r)
    {
        if (IsNullOrEmpty(r.ReleaseType))
            return false;

        string type = r.ReleaseType.ToLowerInvariant();

        return type is "recording" or "track" or "single"
            && r.Format?.Contains("Single", StringComparison.Ordinal) != true;
    }

    /// <summary>
    /// Calculate relevance score (0-100) for Discogs results using fuzzy matching.
    /// Matches MusicBrainz scoring scale for consistency.
    /// </summary>
    private static int CalculateRelevanceScore(string query, SearchResult r)
    {
        string queryLower = query.ToLowerInvariant();
        string titleLower = r.Title.ToLowerInvariant();
        string? artistLower = r.Artist?.ToLowerInvariant();

        if (titleLower == queryLower)
            return 100;

        if (artistLower is not null && $"{artistLower} {titleLower}" == queryLower)
            return 100;

        var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var resultTerms = titleLower.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (artistLower is not null)
            resultTerms.UnionWith(artistLower.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        int matchingTerms = queryTerms.Count(qt =>
            resultTerms.Any(rt =>
                rt.Contains(qt, StringComparison.Ordinal)
                || qt.Contains(rt, StringComparison.Ordinal)
            )
        );
        double termScore =
            queryTerms.Count > 0 ? (double)matchingTerms / queryTerms.Count * 100 : 0;

        double substringBonus = 0;
        if (titleLower.Contains(queryLower, StringComparison.Ordinal))
            substringBonus = 30;
        else if (artistLower?.Contains(queryLower, StringComparison.Ordinal) == true)
            substringBonus = 20;

        int score = (int)Math.Min(100, termScore + substringBonus);
        return Math.Max(1, score); // Minimum score of 1 for any result
    }

    /// <summary>
    /// Saves search results as individual JSON files in a timestamped folder.
    /// Folder structure: dumps/music-search/{timestamp}-{sanitized-query}/
    /// </summary>
    private static void SaveSearchDumps(string query, List<SearchResult> results)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string sanitizedQuery = SanitizeForFolder(query);
        string folderName = $"{timestamp}-{sanitizedQuery}";
        string dumpDir = Combine(Paths.DumpsDirectory, "music-search", folderName);

        CreateDirectory(dumpDir);

        for (int i = 0; i < results.Count; i++)
        {
            SearchResult result = results[i];
            string source = result.Source == MusicSource.Discogs ? "discogs" : "musicbrainz";
            string fileName = $"{i + 1:D3}-{source}-{result.Id}.json";
            string filePath = Combine(dumpDir, fileName);

            string json = JsonSerializer.Serialize(result, JsonOptions.Indented);
            WriteAllText(filePath, json);
        }

        string allPath = Combine(dumpDir, "_all-results.json");
        string allJson = JsonSerializer.Serialize(results, JsonOptions.Indented);
        WriteAllText(allPath, allJson);

        Console.Dim($"[DEBUG] Saved {results.Count} results to: {dumpDir}");
    }

    private static string SanitizeForFolder(string input)
    {
        char[] invalid = GetInvalidFileNameChars();
        string sanitized = new([.. input.Select(c => invalid.Contains(c) ? '_' : c)]);
        return sanitized.Length > 50 ? sanitized[..50] : sanitized;
    }

    /// <summary>
    /// Normalize user input to standard type names.
    /// </summary>
    static string NormalizeType(string input) =>
        input.ToLowerInvariant() switch
        {
            "album" => "album",
            "ep" => "ep",
            "single" => "single",
            "compilation" => "compilation",
            "master" => "master", // Discogs-specific
            "release" => "release", // Discogs-specific
            _ => input.ToLowerInvariant(),
        };

    /// <summary>
    /// Check if a result matches the normalized type filter.
    /// Handles API differences: MusicBrainz uses "Album", Discogs uses "master"/"release".
    /// </summary>
    static bool MatchesType(SearchResult r, string filter)
    {
        if (IsNullOrEmpty(r.ReleaseType))
            return false;

        string normalized = r.ReleaseType.ToLowerInvariant();

        return filter switch
        {
            "album" => normalized is "album" or "master",
            "ep" => normalized.Contains("ep", StringComparison.Ordinal),
            "single" => normalized.Contains("single", StringComparison.Ordinal),
            "compilation" => normalized.Contains("compilation", StringComparison.Ordinal),
            "master" => normalized is "master",
            "release" => normalized is "release",
            _ => normalized.Contains(filter, StringComparison.Ordinal),
        };
    }

    /// <summary>
    /// Normalize type for display consistency.
    /// </summary>
    static string? NormalizeTypeForDisplay(string? type) =>
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

    /// <summary>
    /// Create hyperlinked ID for terminal (uses ANSI escape sequences).
    /// </summary>
    static string MakeIdLink(SearchResult r)
    {
        string url =
            r.Source == MusicSource.Discogs
                ? $"https://www.discogs.com/release/{r.Id}"
                : $"https://musicbrainz.org/release/{r.Id}";

        return $"[link={url}]{r.Id}[/]";
    }

    /// <summary>
    /// Create hyperlinked title for terminal - clicking opens release page.
    /// </summary>
    static string MakeTitleLink(SearchResult r)
    {
        string url =
            r.Source == MusicSource.Discogs
                ? $"https://www.discogs.com/release/{r.Id}"
                : $"https://musicbrainz.org/release/{r.Id}";

        string escapedTitle = Console.Escape(r.Title);
        return $"[link={url}]{escapedTitle}[/]";
    }

    /// <summary>
    /// Perform lookup when --id is specified. Delegates to MusicLookupCommand logic.
    /// </summary>
    private static async Task<int> PerformLookupAsync(Settings settings, CancellationToken ct)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            ct,
            Program.Cts.Token
        );
        CancellationToken token = linkedCts.Token;

        string source = settings.Source.ToLowerInvariant();
        bool isDiscogs = source is "discogs";

        IMusicService service;

        if (isDiscogs)
        {
            if (!int.TryParse(settings.Id, out _))
            {
                Console.Error("Invalid Discogs ID (must be number)");
                return 1;
            }
            string? discogsToken = GetEnvironmentVariable("DISCOGS_USER_TOKEN");
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
        string sourceName = isDiscogs ? "Discogs" : "MusicBrainz";

        await Console
            .Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
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
            string durationText =
                td.Days > 0 ? $"{td.Days}d {td.Hours}h {td.Minutes}m"
                : td.Hours > 0 ? $"{td.Hours}h {td.Minutes}m"
                : $"{td.Minutes}m {td.Seconds}s";
            Console.Field("Duration:", durationText);
        }
        Console.NewLine();

        if (!isDiscogs)
        {
            bool deepSearch = settings.AutoConfirm;
            if (!deepSearch)
            {
                string choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Fetch full track metadata (recordings, composers, etc)?")
                        .AddChoices("Yes", "No")
                );
                deepSearch = choice == "Yes";
            }

            if (deepSearch)
            {
                List<TrackInfo> enrichedTracks = await EnrichTracksWithProgressAsync(
                    (MusicBrainzService)service,
                    settings.Id!,
                    info.Title,
                    release.Tracks,
                    settings.Fresh,
                    token
                );
                release = new ReleaseData(info, enrichedTracks);
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
                string duration = track.Duration.ToPaddedString();
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
                string duration =
                    work.TotalDuration > TimeSpan.Zero ? work.TotalDuration.ToString(@"m\:ss") : "";
                string soloists = work.Soloists.Count > 0 ? Join(", ", work.Soloists) : "";

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

        Console.Write(table);
        Console.NewLine();
        Console.NewLine();
        return 0;
    }

    /// <summary>
    /// Groups consecutive tracks by WorkName within the same disc.
    /// Tracks with the same WorkName are aggregated into a single WorkSummary.
    /// Logs warning when work hierarchy appears to be missing.
    /// </summary>
    static List<WorkSummary> GroupTracksByWork(List<TrackInfo> tracks)
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

            // Use raw WorkName from MusicBrainz - no manipulation
            string displayWork = first.WorkName ?? first.Title;

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
            // Use raw WorkName from MusicBrainz - no manipulation
            string workKey = track.WorkName ?? track.Title;

            if (track.DiscNumber != currentDisc || workKey != currentWorkName)
            {
                FlushGroup();
                currentDisc = track.DiscNumber;
                currentWorkName = workKey;
            }

            currentGroup.Add(track);
        }

        FlushGroup();

        // Detect potential missing work hierarchy
        DetectMissingWorkHierarchy(works);

        return works;
    }

    /// <summary>
    /// Detects when work hierarchy is likely missing from MusicBrainz data.
    /// Logs warnings for groups of consecutive single-track "works" that appear to be
    /// movements of the same parent work (based on title patterns like "Work: I.", "Work: II.").
    /// </summary>
    static readonly HashSet<string> LoggedWorkHierarchyWarnings = [];

    static void DetectMissingWorkHierarchy(List<WorkSummary> works)
    {
        List<string> suspectedMissing = [];

        for (int i = 0; i < works.Count - 1; i++)
        {
            WorkSummary current = works[i];
            WorkSummary next = works[i + 1];

            if (current.FirstTrack != current.LastTrack || next.FirstTrack != next.LastTrack)
                continue;

            if (current.Disc != next.Disc)
                continue;

            int currentColon = current.Work.IndexOf(':');
            int nextColon = next.Work.IndexOf(':');

            if (currentColon > 5 && nextColon > 5)
            {
                string currentPrefix = current.Work[..currentColon];
                string nextPrefix = next.Work[..nextColon];

                if (currentPrefix == nextPrefix && !suspectedMissing.Contains(currentPrefix))
                {
                    suspectedMissing.Add(currentPrefix);
                }
            }
        }

        foreach (string missing in suspectedMissing)
        {
            // Skip if already logged this session
            if (!LoggedWorkHierarchyWarnings.Add(missing))
                continue;

            ApiResponseDumper.LogMissingFields(
                "work-hierarchy",
                0,
                0,
                missing,
                ["ParentWork hierarchy missing - tracks not grouped"]
            );
        }
    }

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

        // Start logging session
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

        // Clear caches if fresh flag is set
        if (fresh)
        {
            TrackCsv.Delete(releaseId);
            StateManager.DeleteReleaseCache(releaseId);
            Console.Info("Cleared cached state for fresh fetch");
        }

        // Try to resume from CSV first (more reliable - append-only)
        List<TrackInfo> enrichedTracks = TrackCsv.Load(releaseId);
        int startIndex = enrichedTracks.Count;
        string resumeSource = "none";

        if (startIndex > 0)
        {
            resumeSource = "CSV";
        }
        else
        {
            // Fall back to JSON state cache
            MusicBrainzEnrichmentState? cachedState =
                StateManager.LoadReleaseCache<MusicBrainzEnrichmentState>(releaseId);
            if (cachedState is not null && cachedState.TotalTracks == total)
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
            // Show last 3 enriched tracks as preview
            foreach (TrackInfo t in enrichedTracks.TakeLast(3))
            {
                Console.MarkupLine(
                    $"  [dim]└[/] {t.DiscNumber}.{t.TrackNumber:D2} {Console.Escape(t.Title)}"
                );
            }
            Console.NewLine();
        }

        // If already complete, return cached data
        if (startIndex >= total)
        {
            Console.Success("All tracks already enriched from cache");
            StateManager.DeleteReleaseCache(releaseId);
            return enrichedTracks;
        }

        Queue<(string Header, string Detail)> recentTracks = new();
        int completed = startIndex;
        bool cancelled = false;

        static (string Header, string Detail) FormatTrackDetail(TrackInfo t)
        {
            string discTrack = $"{t.DiscNumber}.{t.TrackNumber:D2}";
            string title = t.Title;
            string duration = t.Duration?.ToString(@"m\:ss") ?? "";
            string header = IsNullOrEmpty(duration)
                ? $"[{discTrack}] {title}"
                : $"[{discTrack}] {title} ({duration})";

            List<string> parts = [];

            if (!IsNullOrEmpty(t.WorkName))
                parts.Add(Console.Work(t.WorkName));

            int? year = t.RecordingYear;
            if (!IsNullOrEmpty(t.Composer))
            {
                parts.Add(Console.Combine(Console.Composer(t.Composer), Console.Year(year)));
            }
            else if (year is { } y)
            {
                parts.Add($"({y})");
            }

            string performer = t.Orchestra ?? t.Artist ?? "";
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

        DateTime startTime = DateTime.Now;

        IRenderable CreateDisplay()
        {
            List<IRenderable> rows = [];

            // Progress bar (YT-orchestrator style)
            string desc = $"({completed}/{total})";
            rows.Add(Console.LiveProgressRow(desc, completed, total, startTime));
            rows.Add(new Text(""));

            // Growing table of grouped works (show first, so it scrolls up)
            if (enrichedTracks.Count > 0)
            {
                SpectreTable table = new();
                table.Border(TableBorder.Simple);
                table.AddColumn(new TableColumn("Disc").NoWrap().Centered());
                table.AddColumn(new TableColumn("Tracks").NoWrap().Centered());
                table.AddColumn(new TableColumn("Work").NoWrap());
                table.AddColumn("Composer");
                table.AddColumn(new TableColumn("Year").NoWrap().Centered());
                table.AddColumn(new TableColumn("Duration").NoWrap().RightAligned());
                table.AddColumn("Conductor");
                table.AddColumn("Orchestra");
                table.AddColumn("Soloists");

                List<WorkSummary> works = GroupTracksByWork(enrichedTracks);
                foreach (WorkSummary work in works)
                {
                    string duration =
                        work.TotalDuration > TimeSpan.Zero
                            ? work.TotalDuration.ToString(@"m\:ss")
                            : "";
                    string soloists = work.Soloists.Count > 0 ? Join(", ", work.Soloists) : "";

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
                rows.Add(table);
                rows.Add(new Text(""));
            }

            // Rolling last 5 tracks (show at bottom, always visible)
            if (recentTracks.Count > 0)
            {
                rows.Add(new Markup("[dim]Recently parsed:[/]"));
                foreach (var (Header, Detail) in recentTracks)
                {
                    rows.Add(new Markup($"  [green]✓[/] {Console.Escape(Header)}"));
                    if (!IsNullOrEmpty(Detail))
                        rows.Add(new Markup($"      {Detail}"));
                }
            }

            return new Rows(rows);
        }

        void SaveState()
        {
            StateManager.SaveReleaseCache(
                releaseId,
                new MusicBrainzEnrichmentState(releaseId, total, enrichedTracks, DateTime.Now)
            );
        }

        await Console
            .Live(CreateDisplay())
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                // Skip already enriched tracks
                for (int i = startIndex; i < tracks.Count; i++)
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
                        TrackCsv.AppendTrack(releaseId, enriched);
                        completed++;

                        var info = FormatTrackDetail(enriched);
                        recentTracks.Enqueue(info);
                        if (recentTracks.Count > 5)
                            recentTracks.Dequeue();

                        // Log track completion (only every 10th for less noise)
                        if (completed % 10 == 0)
                        {
                            Logger.Event(
                                "TrackProgress",
                                new Dictionary<string, object>
                                {
                                    ["Completed"] = completed,
                                    ["Total"] = total,
                                }
                            );
                        }

                        // Save state every 10 tracks
                        if (completed % 10 == 0)
                            SaveState();

                        ctx.UpdateTarget(CreateDisplay());
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled = true;
                        SaveState();
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Save state on any error so we can resume
                        SaveState();
                        Console.Error("Error: {0}", ex.Message);
                        cancelled = true;
                        break;
                    }
                }
            });

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
            // Log work summary before clearing cache
            List<WorkSummary> works = GroupTracksByWork(enrichedTracks);
            Logger.End(success: true, summary: $"{total} tracks, {works.Count} works");

            // Export works to CSV
            MusicExporter.ExportWorksToCSV(releaseTitle, works);

            // Clear caches on successful completion
            StateManager.DeleteReleaseCache(releaseId);
            TrackCsv.Delete(releaseId);
        }

        return enrichedTracks;
    }
}
