namespace CSharpScripts.CLI.Commands;

// Cached JsonSerializerOptions per CA1869
file static class JsonOptions
{
    internal static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

public sealed class MusicSearchCommand : AsyncCommand<MusicSearchCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<query>")]
        [Description("Free-text search (e.g. 'Bowie Heroes 1977')")]
        public required string Query { get; init; }

        [CommandOption("-s|--source")]
        [Description("discogs (default), musicbrainz (or mb), both")]
        [DefaultValue("discogs")]
        public string Source { get; init; } = "discogs";

        [CommandOption("-m|--mode")]
        [Description("pop (default) or classical (changes default columns)")]
        [DefaultValue("pop")]
        public string Mode { get; init; } = "pop";

        [CommandOption("-t|--type")]
        [Description("Filter: album, ep, single, compilation (normalized across APIs)")]
        public string? Type { get; init; }

        [CommandOption("-n|--limit")]
        [Description("Max results per source (default 10)")]
        [DefaultValue(10)]
        public int Limit { get; init; } = 10;

        [CommandOption("-o|--output")]
        [Description("table (default) or json")]
        [DefaultValue("table")]
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
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
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

        List<Models.SearchResult> results = [];
        int filteredCount = 0;

        if (searchMusicBrainz)
        {
            MusicBrainzService mb = new();
            List<Models.SearchResult> mbResults = await mb.SearchAsync(
                settings.Query,
                settings.Limit,
                cancellationToken
            );
            results.AddRange(mbResults);
        }

        if (searchDiscogs)
        {
            DiscogsService discogs = new(discogsToken);
            List<Models.SearchResult> discogsResults = await discogs.SearchAsync(
                settings.Query,
                settings.Limit,
                cancellationToken
            );

            // Apply client-side relevance scoring for Discogs
            discogsResults = discogsResults
                .Select(r => r with { Score = CalculateRelevanceScore(settings.Query, r) })
                .ToList();

            results.AddRange(discogsResults);
        }

        // Sort all results by score (descending) - MusicBrainz native scores, Discogs calculated
        results = results.OrderByDescending(r => r.Score ?? 0).ToList();

        // Apply normalized type filter
        if (!IsNullOrEmpty(settings.Type))
        {
            int beforeCount = results.Count;
            string normalizedFilter = NormalizeType(settings.Type);

            results = results.Where(r => MatchesType(r, normalizedFilter)).ToList();
            filteredCount = beforeCount - results.Count;

            if (settings.Verbose)
            {
                Console.Dim(
                    $"[DEBUG] Filter '{settings.Type}' -> normalized '{normalizedFilter}', removed {filteredCount}"
                );
            }
        }

        // Filter out individual tracks - focus on collections (albums, EPs, etc.)
        int trackCount = results.Count(r => IsTrackResult(r));
        if (trackCount > 0)
        {
            results = results.Where(r => !IsTrackResult(r)).ToList();
            filteredCount += trackCount;

            if (settings.Verbose)
                Console.Dim(
                    $"[DEBUG] Excluded {trackCount} track-level results (focusing on collections)"
                );
        }

        // Save JSON dumps in debug mode
        if (settings.Verbose && results.Count > 0)
        {
            SaveSearchDumps(settings.Query, results);
        }

        if (results.Count == 0)
        {
            Console.Warning("No results found.");
            return 0;
        }

        // JSON output
        if (settings.Output.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            string json = JsonSerializer.Serialize(results, JsonOptions.Indented);
            Console.WriteLine(json);
            return 0;
        }

        // Determine which fields to display
        List<string> columns = GetColumns(settings);

        SpectreTable table = new();
        table.Border(TableBorder.Rounded);
        foreach (var col in columns)
            table.AddColumn(col);

        foreach (Models.SearchResult r in results)
        {
            List<string> values = columns.Select(col => GetFieldValue(col, r, settings)).ToList();
            table.AddRow([.. values]);
        }

        Console.Write(table);

        bool isClassical = settings.Mode.Equals("classical", StringComparison.OrdinalIgnoreCase);
        string modeLabel = isClassical ? " (classical mode)" : "";
        Console.Success(
            "Found {0} results{1}{2}",
            results.Count,
            filteredCount > 0 ? $" ({filteredCount} filtered)" : "",
            modeLabel
        );

        return 0;
    }

    /// <summary>
    /// Determine columns based on --fields, --mode, and --debug flags.
    /// </summary>
    private static List<string> GetColumns(Settings settings)
    {
        // Custom fields take priority
        if (!IsNullOrEmpty(settings.Fields))
        {
            return
            [
                .. settings
                    .Fields.Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .Select(f => NormalizeFieldName(f)),
            ];
        }

        bool isClassical = settings.Mode.Equals("classical", StringComparison.OrdinalIgnoreCase);

        // Default columns based on mode
        List<string> columns = isClassical
            ? ["Composer", "Work", "Performers", "Year", "ID"]
            : ["Artist", "Title", "Year", "Type", "ID"];

        // Add verbose columns
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
    private static string GetFieldValue(string column, Models.SearchResult r, Settings settings)
    {
        string value = column switch
        {
            "Artist" => r.Artist ?? "",
            "Title" => r.Title,
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
            "Barcode" => r.Barcode ?? "",
            "Composer" => ExtractComposer(r) ?? "",
            "Work" => ExtractWork(r) ?? r.Title,
            "Performers" => r.Artist ?? "",
            _ => "",
        };

        // Apply markup escape except for already-formatted fields
        return column is "ID" or "Source" ? value : Console.Escape(value);
    }

    /// <summary>
    /// Extract composer from title patterns (e.g., "Beethoven: Symphony No. 9").
    /// </summary>
    private static string? ExtractComposer(Models.SearchResult r)
    {
        // Pattern: "Composer: Work" or "Composer - Work"
        if (r.Title.Contains(':'))
        {
            var parts = r.Title.Split(':', 2);
            if (parts[0].Length < 50) // Likely a composer name, not part of title
                return parts[0].Trim();
        }

        // If artist looks like a composer name (contains common classical composer surnames)
        string[] classicalComposers =
        [
            "Bach",
            "Beethoven",
            "Mozart",
            "Brahms",
            "Chopin",
            "Mahler",
            "Tchaikovsky",
            "Wagner",
            "Schubert",
            "Handel",
            "Haydn",
            "Debussy",
            "Ravel",
        ];

        if (r.Artist is not null)
        {
            foreach (var composer in classicalComposers)
            {
                if (r.Artist.Contains(composer, StringComparison.OrdinalIgnoreCase))
                    return composer;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract work title from release title.
    /// </summary>
    private static string? ExtractWork(Models.SearchResult r)
    {
        // Pattern: "Composer: Work"
        if (r.Title.Contains(':'))
        {
            var parts = r.Title.Split(':', 2);
            if (parts.Length > 1)
                return parts[1].Trim();
        }

        // Pattern: "Composer - Work"
        if (r.Title.Contains(" - "))
        {
            var parts = r.Title.Split(" - ", 2);
            if (parts.Length > 1)
                return parts[1].Trim();
        }

        return null;
    }

    /// <summary>
    /// Check if a result is a track-level entry (not a collection).
    /// Filters based on MusicBrainz Recording type and Discogs track patterns.
    /// </summary>
    private static bool IsTrackResult(Models.SearchResult r)
    {
        if (IsNullOrEmpty(r.ReleaseType))
            return false;

        string type = r.ReleaseType.ToLowerInvariant();

        // MusicBrainz: "Recording" represents individual tracks
        // Discogs: Single tracks are rare in search results, but check for patterns
        return type is "recording" or "track" or "single" && r.Format?.Contains("Single") != true;
    }

    /// <summary>
    /// Calculate relevance score (0-100) for Discogs results using fuzzy matching.
    /// Matches MusicBrainz scoring scale for consistency.
    /// </summary>
    private static int CalculateRelevanceScore(string query, Models.SearchResult r)
    {
        string queryLower = query.ToLowerInvariant();
        string titleLower = r.Title.ToLowerInvariant();
        string? artistLower = r.Artist?.ToLowerInvariant();

        // Exact title match = 100
        if (titleLower == queryLower)
            return 100;

        // Exact artist + title combo match
        if (artistLower is not null && $"{artistLower} {titleLower}" == queryLower)
            return 100;

        // Calculate term overlap score
        var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var resultTerms = titleLower.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (artistLower is not null)
            resultTerms.UnionWith(artistLower.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        int matchingTerms = queryTerms.Count(qt =>
            resultTerms.Any(rt => rt.Contains(qt) || qt.Contains(rt))
        );
        double termScore =
            queryTerms.Count > 0 ? (double)matchingTerms / queryTerms.Count * 100 : 0;

        // Bonus for containing query as substring
        double substringBonus = 0;
        if (titleLower.Contains(queryLower))
            substringBonus = 30;
        else if (artistLower?.Contains(queryLower) == true)
            substringBonus = 20;

        // Calculate final score, cap at 100
        int score = (int)Math.Min(100, termScore + substringBonus);
        return Math.Max(1, score); // Minimum score of 1 for any result
    }

    /// <summary>
    /// Saves search results as individual JSON files in a timestamped folder.
    /// Folder structure: dumps/music-search/{timestamp}-{sanitized-query}/
    /// </summary>
    private static void SaveSearchDumps(string query, List<Models.SearchResult> results)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string sanitizedQuery = SanitizeForFolder(query);
        string folderName = $"{timestamp}-{sanitizedQuery}";
        string dumpDir = Combine(Paths.DumpsDirectory, "music-search", folderName);

        Directory.CreateDirectory(dumpDir);

        // Save each result as individual JSON
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            string source = result.Source == MusicSource.Discogs ? "discogs" : "musicbrainz";
            string fileName = $"{i + 1:D3}-{source}-{result.Id}.json";
            string filePath = Combine(dumpDir, fileName);

            string json = JsonSerializer.Serialize(result, JsonOptions.Indented);
            File.WriteAllText(filePath, json);
        }

        // Save combined results
        string allPath = Combine(dumpDir, "_all-results.json");
        string allJson = JsonSerializer.Serialize(results, JsonOptions.Indented);
        File.WriteAllText(allPath, allJson);

        Console.Dim($"[DEBUG] Saved {results.Count} results to: {dumpDir}");
    }

    private static string SanitizeForFolder(string input)
    {
        char[] invalid = GetInvalidFileNameChars();
        string sanitized = new(input.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
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
    static bool MatchesType(Models.SearchResult r, string filter)
    {
        if (IsNullOrEmpty(r.ReleaseType))
            return false;

        string normalized = r.ReleaseType.ToLowerInvariant();

        return filter switch
        {
            // "album" matches MusicBrainz "Album" OR Discogs "master" (canonical album)
            "album" => normalized is "album" or "master",
            "ep" => normalized.Contains("ep"),
            "single" => normalized.Contains("single"),
            "compilation" => normalized.Contains("compilation"),
            "master" => normalized is "master",
            "release" => normalized is "release",
            _ => normalized.Contains(filter),
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
    static string MakeIdLink(Models.SearchResult r)
    {
        string url =
            r.Source == MusicSource.Discogs
                ? $"https://www.discogs.com/release/{r.Id}"
                : $"https://musicbrainz.org/release/{r.Id}";

        // Spectre.Console supports [link] markup
        return $"[link={url}]{r.Id}[/]";
    }
}

public sealed class MusicLookupCommand : AsyncCommand<MusicLookupCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Release ID (GUID for MusicBrainz, number for Discogs)")]
        public required string Id { get; init; }

        [CommandOption("-s|--source")]
        [Description("musicbrainz (default), discogs")]
        [DefaultValue("musicbrainz")]
        public string Source { get; init; } = "musicbrainz";

        [CommandOption("-y|--yes")]
        [Description("Auto-confirm deep search without prompting")]
        [DefaultValue(false)]
        public bool AutoConfirm { get; init; }
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        // Link tokens so Ctrl+C cancels through Program.Cts
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            Program.Cts.Token
        );
        CancellationToken ct = linkedCts.Token;

        IMusicService service;

        // Default to MusicBrainz if not specified or explicit
        bool isDiscogs = settings.Source.Equals("discogs", StringComparison.OrdinalIgnoreCase);

        if (isDiscogs)
        {
            if (!int.TryParse(settings.Id, out _))
            {
                Console.Error("Invalid Discogs ID (must be number)");
                return 1;
            }
            string? token = GetEnvironmentVariable("DISCOGS_USER_TOKEN");
            if (IsNullOrEmpty(token))
            {
                Console.CriticalFailure("Discogs", "DISCOGS_USER_TOKEN not set");
                return 1;
            }
            service = new DiscogsService(token);
        }
        else
        {
            // MusicBrainz (default)
            if (!Guid.TryParse(settings.Id, out _))
            {
                Console.Error("Invalid MusicBrainz ID (must be GUID)");
                return 1;
            }
            service = new MusicBrainzService();
        }

        Console.Info("Fetching release info from {0}...", isDiscogs ? "Discogs" : "MusicBrainz");

        // 1. Shallow Fetch
        List<TrackMetadata> tracks = await service.GetReleaseTracksAsync(
            settings.Id,
            deepSearch: false,
            ct: ct
        );

        if (tracks.Count == 0)
        {
            Console.Warning("No tracks found.");
            return 0;
        }

        TrackMetadata header = tracks[0];

        // 2. Display Summary with all available info
        Console.NewLine();
        Console.Rule("Release Info");
        Console.Field("Release:", header.Album);
        Console.Field("Artist:", header.Artist);
        Console.Field("Year:", header.FirstIssuedYear?.ToString());
        Console.Field("Label:", header.Label);
        Console.Field("Catalog:", header.CatalogNumber);
        Console.FieldIfPresent("Conductor:", header.Conductor);
        Console.FieldIfPresent("Orchestra:", header.Orchestra);
        Console.FieldIfPresent("Venue:", header.RecordingVenue);
        if (header.Soloists.Count > 0)
            Console.Field("Soloists:", $"{header.Soloists.Count} listed");
        Console.Field("Tracks:", tracks.Count.ToString());
        Console.NewLine();

        // 3. Prompt for Deep Search (MusicBrainz only)
        if (!isDiscogs)
        {
            bool deepSearch =
                settings.AutoConfirm
                || Console.Confirm(
                    "Fetch full track metadata (recordings, composers, etc)?",
                    defaultValue: true
                );

            if (deepSearch)
            {
                tracks = await EnrichTracksWithProgressAsync(service, tracks, ct);
            }
        }

        // 4. Display Final Table
        SpectreTable table = new();
        table.Border(TableBorder.Simple);
        table.AddColumn("#");
        table.AddColumn("Title");
        table.AddColumn("Duration");
        if (!isDiscogs)
            table.AddColumn("Composer");
        if (!isDiscogs)
            table.AddColumn("Year");

        foreach (TrackMetadata track in tracks)
        {
            string duration = track.Duration?.ToString(@"m\:ss") ?? "?:??";
            var row = new List<string>
            {
                $"{track.DiscNumber}.{track.TrackNumber}",
                Console.Escape(track.Title),
                duration,
            };

            if (!isDiscogs)
            {
                row.Add(Console.Escape(track.Composer ?? ""));
                row.Add(track.FirstIssuedYear?.ToString() ?? "");
            }

            table.AddRow([.. row]);
        }

        Console.Write(table);

        // 5. Interactive export prompt
        var exportChoice = Console.Prompt(
            new SelectionPrompt<string>()
                .Title("Export to CSV?")
                .AddChoices("All fields", "Default fields (Artist, Title, Year, Label)", "Skip")
        );

        if (exportChoice != "Skip")
        {
            bool allFields = exportChoice.StartsWith("All");
            string filename = SanitizeForFileName(header.Album) + ".csv";

            ExportTracksToCsv(tracks, filename, allFields);
            Console.Success("Exported: {0} ({1} fields)", filename, allFields ? "all" : "default");
        }

        return 0;
    }

    private static async Task<List<TrackMetadata>> EnrichTracksWithProgressAsync(
        IMusicService service,
        List<TrackMetadata> tracks,
        CancellationToken ct
    )
    {
        List<TrackMetadata> enrichedTracks = new(tracks.Count);
        Queue<(string Header, string Detail)> recentTracks = new();
        int completed = 0;
        int total = tracks.Count;
        DateTime startTime = DateTime.Now;
        bool cancelled = false;

        // Create live display with progress info and rolling list
        Rows CreateDisplay()
        {
            List<IRenderable> rows = [];

            // Progress bar line
            double percent = total > 0 ? (double)completed / total * 100 : 0;
            string eta =
                completed > 0
                    ? TimeSpan
                        .FromSeconds(
                            (DateTime.Now - startTime).TotalSeconds
                                / completed
                                * (total - completed)
                        )
                        .ToString(@"m\:ss", CultureInfo.InvariantCulture)
                    : "?:??";

            rows.Add(Console.ProgressMarkup(completed, total, eta));
            rows.Add(new Text("")); // Spacer

            // Rolling list of recent tracks with multi-line detail
            foreach (var trackInfo in recentTracks)
            {
                rows.Add(new Markup($"  [green]✓[/] {Console.Escape(trackInfo.Header)}"));
                if (!IsNullOrEmpty(trackInfo.Detail))
                    rows.Add(new Text($"      {trackInfo.Detail}"));
            }

            return new Rows(rows);
        }

        // Build rich detail for track - returns header + detail lines
        static (string Header, string Detail) FormatTrackDetail(TrackMetadata t)
        {
            // Header: [1.05] Title (Duration)
            string discTrack = $"{t.DiscNumber}.{t.TrackNumber:D2}";
            string title = t.Title.Length > 55 ? t.Title[..52] + "..." : t.Title;
            string duration = t.Duration?.ToString(@"m\:ss") ?? "";
            string header = IsNullOrEmpty(duration)
                ? $"[{discTrack}] {title}"
                : $"[{discTrack}] {title} ({duration})";

            // Detail line: Composer (Year) • Performer/Orchestra | Conductor | Venue
            List<string> parts = [];

            // Composer + Year
            if (!IsNullOrEmpty(t.Composer))
            {
                string composerPart = t.Composer;
                if (t.FirstIssuedYear is { } year)
                    composerPart += $" ({year})";
                parts.Add(composerPart);
            }
            else if (t.FirstIssuedYear is { } year)
            {
                parts.Add($"({year})");
            }

            // Performer (Orchestra or Artist)
            string performer = t.Orchestra ?? t.Artist ?? "";
            if (!IsNullOrEmpty(performer) && performer != t.Composer)
                parts.Add($"• {performer}");

            // Conductor (if different from composer/performer)
            if (
                !IsNullOrEmpty(t.Conductor)
                && t.Conductor != t.Composer
                && t.Conductor != performer
            )
                parts.Add($"cond. {t.Conductor}");

            // Venue (if present)
            if (!IsNullOrEmpty(t.RecordingVenue))
                parts.Add($"@ {t.RecordingVenue}");

            return (header, Join(" ", parts));
        }

        await Console
            .Live(CreateDisplay())
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                foreach (TrackMetadata track in tracks)
                {
                    if (ct.IsCancellationRequested)
                    {
                        cancelled = true;
                        break;
                    }

                    try
                    {
                        TrackMetadata enriched = await service.EnrichTrackAsync(track, ct);
                        enrichedTracks.Add(enriched);
                        completed++;

                        // Add to rolling queue with rich detail
                        var info = FormatTrackDetail(enriched);
                        recentTracks.Enqueue(info);
                        if (recentTracks.Count > 5)
                            recentTracks.Dequeue();

                        ctx.UpdateTarget(CreateDisplay());
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled = true;
                        break;
                    }
                }
            });

        if (cancelled)
        {
            Console.Warning("Enrichment cancelled after {0}/{1} tracks", completed, total);
        }
        else
        {
            Console.Complete($"Enriched {total} tracks");
        }

        return enrichedTracks;
    }

    /// <summary>
    /// Sanitize album name for use as filename.
    /// </summary>
    private static string SanitizeForFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string sanitized = new(name.Where(c => !invalid.Contains(c)).ToArray());
        return IsNullOrWhiteSpace(sanitized) ? "export" : sanitized.Trim();
    }

    /// <summary>
    /// Export tracks to CSV file.
    /// </summary>
    private static void ExportTracksToCsv(
        List<TrackMetadata> tracks,
        string filename,
        bool allFields
    )
    {
        using StreamWriter writer = new(filename, false, System.Text.Encoding.UTF8);

        if (allFields)
        {
            // All fields header
            writer.WriteLine(
                "DiscNumber,TrackNumber,Title,Artist,Album,Year,Label,CatalogNumber,Composer,Conductor,Orchestra,Soloists,Venue,Duration,Notes"
            );

            foreach (var t in tracks)
            {
                string soloists = t.Soloists.Count > 0 ? Join("; ", t.Soloists) : "";
                string duration =
                    t.Duration?.ToString(@"m\:ss", CultureInfo.InvariantCulture) ?? "";
                writer.WriteLine(
                    $"{t.DiscNumber},{t.TrackNumber},{CsvEscape(t.Title)},{CsvEscape(t.Artist ?? "")},{CsvEscape(t.Album)},{t.FirstIssuedYear?.ToString(CultureInfo.InvariantCulture) ?? ""},{CsvEscape(t.Label ?? "")},{CsvEscape(t.CatalogNumber ?? "")},{CsvEscape(t.Composer ?? "")},{CsvEscape(t.Conductor ?? "")},{CsvEscape(t.Orchestra ?? "")},{CsvEscape(soloists)},{CsvEscape(t.RecordingVenue ?? "")},{duration},{CsvEscape(t.Notes ?? "")}"
                );
            }
        }
        else
        {
            // Default fields header
            writer.WriteLine("TrackNumber,Title,Artist,Album,Year,Label");

            foreach (var t in tracks)
            {
                writer.WriteLine(
                    $"{t.TrackNumber},{CsvEscape(t.Title)},{CsvEscape(t.Artist ?? "")},{CsvEscape(t.Album)},{t.FirstIssuedYear?.ToString(CultureInfo.InvariantCulture) ?? ""},{CsvEscape(t.Label ?? "")}"
                );
            }
        }
    }

    /// <summary>
    /// Escape value for CSV (quote if contains comma, quote, or newline).
    /// </summary>
    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

public sealed class MusicSchemaCommand : AsyncCommand<MusicSchemaCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-e|--entity")]
        [Description(
            "Filter by entity type: Artist, Release, Recording, Track, Label, ReleaseGroup, Master, Format, Credit, Image, Video, Identifier, Community"
        )]
        public string? Entity { get; init; }

        [CommandOption("-s|--source")]
        [Description("Filter by source: MusicBrainz (or mb), Discogs, Both")]
        public string? Source { get; init; }

        [CommandOption("--flat")]
        [Description("Show flat list of all fields instead of grouped by entity")]
        [DefaultValue(false)]
        public bool Flat { get; init; }

        [CommandOption("--json")]
        [Description("Output as JSON for programmatic use")]
        [DefaultValue(false)]
        public bool Json { get; init; }
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        Dictionary<string, EntitySchema> schemas = MusicMetadataSchema.GetAllSchemas();

        // Filter by entity if specified
        if (!IsNullOrWhiteSpace(settings.Entity))
        {
            string entityFilter = settings.Entity.Trim();
            schemas = schemas
                .Where(s => s.Key.Equals(entityFilter, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(s => s.Key, s => s.Value);

            if (schemas.Count == 0)
            {
                Console.Warning(
                    "Entity '{0}' not found. Available: {1}",
                    entityFilter,
                    Join(", ", MusicMetadataSchema.GetAllSchemas().Keys)
                );
                return Task.FromResult(1);
            }
        }

        // Source filter
        string? sourceFilter = settings.Source?.ToLowerInvariant() switch
        {
            "musicbrainz" or "mb" => "MusicBrainz",
            "discogs" => "Discogs",
            "both" => "Both",
            _ => null,
        };

        // JSON output
        if (settings.Json)
        {
            object output = settings.Flat
                ? FilterFields(MusicMetadataSchema.GetAllFieldsSummary(), sourceFilter)
                : schemas.ToDictionary(
                    s => s.Key,
                    s => new
                    {
                        s.Value.Description,
                        Fields = FilterFields(s.Value.Fields, sourceFilter),
                    }
                );

            string json = JsonSerializer.Serialize(output, JsonOptions.Indented);
            Console.WriteLine(json);
            return Task.FromResult(0);
        }

        // Flat list output
        if (settings.Flat)
        {
            List<MetadataFieldSummary> allFields = FilterFields(
                MusicMetadataSchema.GetAllFieldsSummary(),
                sourceFilter
            );

            SpectreTable table = new();
            table.Border(TableBorder.Rounded);
            table.AddColumn("Entity");
            table.AddColumn("Field");
            table.AddColumn("Type");
            table.AddColumn("Source");
            table.AddColumn("Description");

            foreach (MetadataFieldSummary field in allFields)
            {
                string sourceColor = GetSourceColor(field.Source);
                table.AddRow(
                    Console.Escape(field.Entity),
                    Console.Bold(field.Field),
                    Console.DimText(field.Type),
                    Console.Colored(sourceColor, field.Source),
                    Console.Escape(field.Description)
                );
            }

            Console.Write(table);
            Console.Success("Total fields: {0}", allFields.Count);
            return Task.FromResult(0);
        }

        // Grouped output (default)
        foreach ((string entityName, EntitySchema schema) in schemas)
        {
            Console.Rule(entityName);
            Console.Dim(schema.Description);
            Console.NewLine();

            List<MetadataField> fields = FilterFields(schema.Fields, sourceFilter);

            SpectreTable table = new();
            table.Border(TableBorder.Simple);
            table.AddColumn("Field");
            table.AddColumn("Type");
            table.AddColumn("Source");
            table.AddColumn("Description");

            foreach (MetadataField field in fields)
            {
                string sourceColor = GetSourceColor(field.Source);
                table.AddRow(
                    Console.Bold(field.Name),
                    Console.DimText(field.Type),
                    Console.Colored(sourceColor, field.Source),
                    Console.Escape(field.Description)
                );
            }

            Console.Write(table);
            Console.NewLine();
        }

        int totalFields = schemas.Values.Sum(s => FilterFields(s.Fields, sourceFilter).Count);
        Console.Success(
            "Showing {0} entities, {1} fields{2}",
            schemas.Count,
            totalFields,
            sourceFilter is not null ? $" (filtered to {sourceFilter})" : ""
        );

        return Task.FromResult(0);
    }

    static string GetSourceColor(string source) =>
        source switch
        {
            "MusicBrainz" => "cyan",
            "Discogs" => "yellow",
            "Both" => "green",
            _ when source.EndsWith("Role") => "magenta",
            _ => "dim",
        };

    static List<T> FilterFields<T>(List<T> fields, string? sourceFilter)
        where T : class
    {
        if (sourceFilter is null)
            return fields;

        return fields
            .Where(f =>
            {
                string source = f switch
                {
                    MetadataField mf => mf.Source,
                    MetadataFieldSummary mfs => mfs.Source,
                    _ => "",
                };
                return source.Equals(sourceFilter, StringComparison.OrdinalIgnoreCase)
                    || source.Equals("Both", StringComparison.OrdinalIgnoreCase);
            })
            .ToList();
    }
}
