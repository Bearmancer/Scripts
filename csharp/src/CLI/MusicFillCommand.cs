namespace CSharpScripts.CLI.Commands;

public sealed class MusicFillCommand : AsyncCommand<MusicFillCommand.Settings>
{
    #region Settings

    public sealed class Settings : CommandSettings
    {
        [CommandOption("-i|--input")]
        [Description("Input TSV/CSV file with recording data")]
        public string? InputFile { get; init; }

        [CommandOption("-o|--output")]
        [Description("Output file path (optional, includes suggestions)")]
        public string? OutputFile { get; init; }
    }

    #endregion

    #region Execute

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        string? inputFile = settings.InputFile;

        if (IsNullOrEmpty(inputFile))
        {
            var files = GetFiles(".", "*.tsv").Concat(GetFiles(".", "*.csv")).ToArray();

            if (files.Length == 1)
            {
                inputFile = files[0];
                Console.Info("Auto-detected input file: {0}", inputFile);
            }
            else if (files.Length > 1)
            {
                inputFile = Console.Prompt(
                    new SelectionPrompt<string>().Title("Select input file:").AddChoices(files)
                );
            }
            else
            {
                Console.Error(
                    "No input file specified and no TSV/CSV files found in current directory."
                );
                return 1;
            }
        }

        if (!File.Exists(inputFile))
        {
            Console.Error("File not found: {0}", inputFile);
            return 1;
        }

        string? discogsToken = Config.DiscogsToken;
        if (IsNullOrEmpty(discogsToken))
            Console.Warning("DISCOGS_USER_TOKEN not set - Discogs fallback disabled");

        var records = ReadRecordings(inputFile);
        Console.Info("Loaded {0} recordings from {1}", records.Count, GetFileName(inputFile));

        List<RecordingWithSuggestions> results = [];
        MusicBrainzService mbService = new();
        DiscogsService? discogsService = IsNullOrEmpty(discogsToken)
            ? null
            : new(token: discogsToken);

        string output =
            settings.OutputFile
            ?? Combine(
                GetDirectoryName(inputFile) ?? ".",
                GetFileNameWithoutExtension(inputFile) + "-filled.csv"
            );

        char delimiter = output.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) ? '\t' : ',';
        using StreamWriter writer = new(output) { AutoFlush = true };
        using CsvWriter csv = new(
            writer,
            new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter.ToString() }
        );

        csv.WriteHeader<FillOutputRow>();
        csv.NextRecord();

        Console.NewLine();
        Console.Info("Writing results in real-time to {0}", output);
        var fillTimer = Stopwatch.StartNew();

        await Console
            .CreateProgress()
            .AutoClear(false)
            .HideCompleted(false)
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
                    $"[green]Searching {records.Count} recordings...[/]",
                    maxValue: records.Count
                );

                foreach (var record in records)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    string workName = !IsNullOrEmpty(record.Work) ? record.Work : "(Unknown Work)";
                    string composer = !IsNullOrEmpty(record.Composer)
                        ? record.Composer
                        : "(Unknown Composer)";
                    task.Description = Console.TaskDescription(
                        prefix: $"({task.Value + 1}/{records.Count})",
                        title: workName,
                        $"{composer}"
                    );

                    var suggestions = await SearchForSuggestionsAsync(
                        record,
                        mbService,
                        discogsService,
                        cancellationToken
                    );

                    if (suggestions.HasAny())
                    {
                        var best = suggestions.GetBest()!;
                        var elapsed = fillTimer.Elapsed;
                        var shortWork = workName.Length > 40 ? workName[..37] + "..." : workName;

                        List<string> found = [];
                        if (!IsNullOrEmpty(best.Label))
                            found.Add($"Label: [cyan]{Console.Escape(best.Label)}[/]");
                        if (!IsNullOrEmpty(best.CatalogNumber))
                            found.Add($"Cat: [cyan]{Console.Escape(best.CatalogNumber)}[/]");
                        if (!IsNullOrEmpty(best.Year))
                            found.Add($"Year: [cyan]{best.Year}[/]");

                        Console.MarkupLine(
                            $"[green]✓[/] [dim]{elapsed:mm\\:ss}[/] [bold]{Console.Escape(shortWork)}[/] → {Join(" │ ", found)} [dim]({best.Source})[/]"
                        );
                    }

                    var bestSugg = suggestions.GetBest();
                    var outputRow = new FillOutputRow(
                        Composer: record.Composer,
                        Work: record.Work,
                        Orchestra: record.Orchestra,
                        Conductor: record.Conductor,
                        Performers: record.Performers,
                        Label: record.Label,
                        LabelSuggested: bestSugg?.Label ?? "",
                        LabelConfidence: bestSugg?.Confidence.ToString() ?? "",
                        Year: record.Year,
                        YearSuggested: bestSugg?.Year ?? "",
                        YearConfidence: bestSugg?.Confidence.ToString() ?? "",
                        CatalogNumber: record.CatalogNumber,
                        CatalogNumberSuggested: bestSugg?.CatalogNumber ?? "",
                        CatalogNumberConfidence: bestSugg?.Confidence.ToString() ?? "",
                        Rating: record.Rating,
                        Comment: record.Comment
                    );
                    csv.WriteRecord(outputRow);
                    csv.NextRecord();

                    await writer.FlushAsync();

                    results.Add(new RecordingWithSuggestions(record, suggestions));
                    task.Increment(1);
                }
            });

        DisplayResults(results);
        Console.Success("Completed! Results available in {0}", output);

        return 0;
    }

    #endregion

    #region TSV/CSV Input

    private static List<RecordingInput> ReadRecordings(string filePath)
    {
        if (!filePath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase))
        {
            using StreamReader reader = new(filePath);

            using CsvReader csv = new(
                reader,
                new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    BadDataFound = null,
                    TrimOptions = TrimOptions.Trim,
                    IgnoreBlankLines = true,
                    DetectColumnCountChanges = false,
                }
            );

            return [.. csv.GetRecords<RecordingInput>()];
        }

        string[] lines = ReadAllLines(filePath);
        if (lines.Length == 0)
            return [];

        Dictionary<string, int> headerIndex = BuildHeaderIndex(lines[0]);

        List<RecordingInput> records = [];
        for (var i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (IsNullOrWhiteSpace(line))
                continue;

            string[] fields = line.Split(['\t'], headerIndex.Count, StringSplitOptions.None);
            if (fields.Length < headerIndex.Count)
            {
                fields = SplitLooseFields(line, expectedFieldCount: headerIndex.Count);
            }

            records.Add(
                new RecordingInput(
                    Composer: GetField(fields: fields, headerIndex: headerIndex, name: "Composer"),
                    Work: GetField(fields: fields, headerIndex: headerIndex, name: "Work"),
                    Orchestra: GetField(
                        fields: fields,
                        headerIndex: headerIndex,
                        name: "Orchestra"
                    ),
                    Conductor: GetField(
                        fields: fields,
                        headerIndex: headerIndex,
                        name: "Conductor"
                    ),
                    Performers: GetField(
                        fields: fields,
                        headerIndex: headerIndex,
                        name: "Performers"
                    ),
                    Label: GetField(fields: fields, headerIndex: headerIndex, name: "Label"),
                    Year: GetField(fields: fields, headerIndex: headerIndex, name: "Year"),
                    CatalogNumber: GetField(
                        fields: fields,
                        headerIndex: headerIndex,
                        name: "CatalogNumber"
                    ),
                    Rating: GetField(fields: fields, headerIndex: headerIndex, name: "Rating"),
                    Comment: GetField(fields: fields, headerIndex: headerIndex, name: "Comment")
                )
            );
        }

        return records;
    }

    private static Dictionary<string, int> BuildHeaderIndex(string headerLine)
    {
        string[] headers = headerLine.Split('\t');
        Dictionary<string, int> dict = new(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < headers.Length; i++)
        {
            string name = headers[i].Trim();
            if (IsNullOrWhiteSpace(name))
                continue;

            dict[name] = i;
        }

        return dict;
    }

    private static string? GetField(
        string[] fields,
        Dictionary<string, int> headerIndex,
        string name
    )
    {
        if (!headerIndex.TryGetValue(name, out int idx))
            return null;

        if (idx < 0 || idx >= fields.Length)
            return null;

        string val = fields[idx].Trim();
        return IsNullOrWhiteSpace(val) ? null : val;
    }

    private static string[] SplitLooseFields(string line, int expectedFieldCount)
    {
        string normalized = Regex.Replace(line.TrimEnd(), "\\s{2,}", "\t");
        string[] parts = normalized.Split(['\t'], expectedFieldCount, StringSplitOptions.None);

        if (parts.Length >= expectedFieldCount)
            return parts;

        return
        [
            .. parts.Concat(Enumerable.Repeat(string.Empty, expectedFieldCount - parts.Length)),
        ];
    }

    #endregion

    #region Search & Suggestions

    private static async Task<SuggestionSet> SearchForSuggestionsAsync(
        RecordingInput record,
        MusicBrainzService mbService,
        DiscogsService? discogsService,
        CancellationToken ct
    )
    {
        SuggestionSet suggestions = new();

        string query = BuildSearchQuery(record);
        if (IsNullOrEmpty(query))
            return suggestions;

        List<Func<Task>> tasks = [];

        tasks.Add(async () =>
        {
            var mbResults = await mbService.SearchAsync(query, maxResults: 5, ct: ct);
            ExtractSuggestions(
                results: mbResults,
                record: record,
                suggestions: suggestions,
                source: "MusicBrainz"
            );
        });

        if (discogsService is { })
        {
            tasks.Add(async () =>
            {
                var discogsResults = await discogsService.SearchAsync(query, maxResults: 5, ct: ct);
                ExtractSuggestions(
                    results: discogsResults,
                    record: record,
                    suggestions: suggestions,
                    source: "Discogs"
                );
            });
        }

        await Task.WhenAll(
            tasks.Select(async t =>
            {
                try
                {
                    await t();
                }
                catch
                { /* ignore transient search failures */
                }
            })
        );

        return suggestions;
    }

    private static string BuildSearchQuery(RecordingInput record)
    {
        List<string> parts = [];
        if (!IsNullOrEmpty(record.Composer))
            parts.Add(record.Composer);
        if (!IsNullOrEmpty(record.Work))
            parts.Add(record.Work);
        if (parts.Count < 2)
        {
            if (!IsNullOrEmpty(record.Orchestra))
                parts.Add(record.Orchestra);
            if (!IsNullOrEmpty(record.Conductor))
                parts.Add(record.Conductor);
        }
        return Join(" ", parts);
    }

    private static void ExtractSuggestions(
        List<SearchResult> results,
        RecordingInput record,
        SuggestionSet suggestions,
        string source
    )
    {
        foreach (var result in results)
        {
            int confidence = CalculateConfidence(result, record);
            if (confidence < 30)
                continue;

            bool hasLabel = !IsNullOrEmpty(result.Label);
            bool hasCat = !IsNullOrEmpty(result.CatalogNumber);
            bool hasYear = result.Year.HasValue;

            if (hasLabel || hasCat || hasYear)
            {
                suggestions.Add(
                    new SuggestionBundle(
                        Label: ShortenLabel(result.Label),
                        CatalogNumber: result.CatalogNumber,
                        Year: result.Year?.ToString(),
                        Confidence: confidence,
                        Source: source,
                        ReleaseId: result.Id
                    )
                );
            }
        }

        suggestions.Normalize();
    }

    private static readonly FrozenDictionary<string, string> LabelAbbreviations = new Dictionary<
        string,
        string
    >(StringComparer.OrdinalIgnoreCase)
    {
        ["Deutsche Grammophon"] = "DG",
        ["His Master's Voice"] = "HMV",
        ["Columbia Masterworks"] = "Columbia",
        ["RCA Victor Red Seal"] = "RCA Red Seal",
        ["Decca Record Company"] = "Decca",
        ["Angel Records"] = "Angel",
        ["Philips Classics"] = "Philips",
        ["London Records"] = "London",
        ["EMI Classics"] = "EMI",
        ["Sony Classical"] = "Sony",
        ["Warner Classics"] = "Warner",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static string? ShortenLabel(string? label)
    {
        if (IsNullOrEmpty(label))
            return null;

        foreach (var (full, abbr) in LabelAbbreviations)
            if (label.Contains(full, StringComparison.OrdinalIgnoreCase))
                return abbr;

        return label;
    }

    private static int CalculateConfidence(SearchResult result, RecordingInput record)
    {
        int score = 0;
        int checks = 0;

        if (!IsNullOrEmpty(record.Composer) && !IsNullOrEmpty(result.Artist))
        {
            checks++;
            if (
                result.Artist.Contains(record.Composer, StringComparison.OrdinalIgnoreCase)
                || record.Composer.Contains(result.Artist, StringComparison.OrdinalIgnoreCase)
            )
                score += 30;
        }

        if (!IsNullOrEmpty(record.Work) && !IsNullOrEmpty(result.Title))
        {
            checks++;
            if (
                result.Title.Contains(record.Work, StringComparison.OrdinalIgnoreCase)
                || record.Work.Contains(result.Title, StringComparison.OrdinalIgnoreCase)
            )
                score += 40;
        }

        if (
            !IsNullOrEmpty(record.Year)
            && int.TryParse(record.Year.TrimEnd('?'), out int recordYear)
            && result.Year.HasValue
        )
        {
            checks++;
            int yearDiff = Math.Abs(recordYear - result.Year.Value);
            if (yearDiff == 0)
                score += 30;
            else if (yearDiff <= 2)
                score += 20;
            else if (yearDiff <= 5)
                score += 10;
        }

        return checks > 0 ? Math.Min(score, 100) : 0;
    }

    #endregion

    #region Results Display

    private static void DisplayResults(List<RecordingWithSuggestions> results)
    {
        Console.NewLine();
        Console.Rule("Search Results");
        Console.NewLine();

        int suggestionsFound = 0;
        foreach (var item in results)
        {
            if (!item.Suggestions.HasAny())
                continue;

            suggestionsFound++;

            string work = item.Original.Work ?? "(Unknown Work)";
            string composer = item.Original.Composer ?? "(none)";

            Console.MarkupLine(
                $"[bold cyan]{Console.Escape(work)}[/] [dim]—[/] [yellow]{Console.Escape(composer)}[/]"
            );

            Console.MarkupLine("[dim]  Input:[/]");
            if (!IsNullOrEmpty(item.Original.Orchestra))
                Console.MarkupLine(
                    $"    [dim]Orchestra:[/] {Console.Escape(item.Original.Orchestra)}"
                );
            if (!IsNullOrEmpty(item.Original.Conductor))
                Console.MarkupLine(
                    $"    [dim]Conductor:[/] {Console.Escape(item.Original.Conductor)}"
                );
            if (!IsNullOrEmpty(item.Original.Label))
                Console.MarkupLine($"    [dim]Label:[/] {Console.Escape(item.Original.Label)}");
            else
                Console.MarkupLine("    [dim]Label:[/] [red](missing)[/]");
            if (!IsNullOrEmpty(item.Original.CatalogNumber))
                Console.MarkupLine(
                    $"    [dim]Catalog #:[/] {Console.Escape(item.Original.CatalogNumber)}"
                );
            else
                Console.MarkupLine("    [dim]Catalog #:[/] [red](missing)[/]");
            if (!IsNullOrEmpty(item.Original.Year))
                Console.MarkupLine($"    [dim]Year:[/] {item.Original.Year}");
            else
                Console.MarkupLine("    [dim]Year:[/] [red](missing)[/]");

            if (item.Suggestions.Items.Count > 0)
            {
                Console.MarkupLine("[green]  Found:[/]");
                foreach (var bundle in item.Suggestions.Items)
                {
                    string conf =
                        bundle.Confidence >= 70 ? "[green]"
                        : bundle.Confidence >= 50 ? "[yellow]"
                        : "[dim]";

                    string label = IsNullOrEmpty(bundle.Label)
                        ? "[dim]-[/]"
                        : $"[cyan]{Console.Escape(bundle.Label)}[/]";
                    string cat = IsNullOrEmpty(bundle.CatalogNumber)
                        ? "[dim]-[/]"
                        : $"[cyan]{Console.Escape(bundle.CatalogNumber)}[/]";
                    string year = IsNullOrEmpty(bundle.Year)
                        ? "[dim]-[/]"
                        : $"[cyan]{bundle.Year}[/]";

                    Console.MarkupLine(
                        $"    {conf}{bundle.Confidence, 3}%[/] Label: {label} │ Cat: {cat} │ Year: {year} [dim]({bundle.Source})[/]"
                    );
                }
            }

            Console.NewLine();
        }

        Console.Info(
            "Found suggestions for {0} of {1} recordings",
            suggestionsFound,
            results.Count
        );
    }

    #endregion
}

#region Supporting Types

internal record RecordingInput(
    string? Composer,
    string? Work,
    string? Orchestra,
    string? Conductor,
    string? Performers,
    string? Label,
    string? Year,
    string? CatalogNumber,
    string? Rating,
    string? Comment
);

internal record SuggestionBundle(
    string? Label,
    string? CatalogNumber,
    string? Year,
    int Confidence,
    string Source,
    string ReleaseId
)
{
    public string Summary =>
        $"{Confidence}% - {Label ?? "(no label)"} / {CatalogNumber ?? "(no cat)"} [{Year ?? "????"}] ({Source})";
}

internal class SuggestionSet
{
    public List<SuggestionBundle> Items { get; private set; } = [];

    public bool HasAny() => Items.Count > 0;

    public void Add(SuggestionBundle bundle)
    {
        if (
            !Items.Any(i =>
                i.Label == bundle.Label
                && i.CatalogNumber == bundle.CatalogNumber
                && i.Year == bundle.Year
            )
        )
        {
            Items.Add(bundle);
        }
    }

    public void Normalize()
    {
        Items = [.. Items.OrderByDescending(i => i.Confidence).ThenBy(i => i.Year).Take(5)];
    }

    public SuggestionBundle? GetBest() => Items.FirstOrDefault();

    public string GetPreviewMarkup()
    {
        var best = GetBest();
        if (best is null)
            return "[dim]No suggestions[/]";

        List<string> parts = [];
        if (!IsNullOrEmpty(best.Label))
            parts.Add($"[cyan]{Console.Escape(best.Label)}[/]");
        if (!IsNullOrEmpty(best.CatalogNumber))
            parts.Add($"[cyan]{Console.Escape(best.CatalogNumber)}[/]");
        if (!IsNullOrEmpty(best.Year))
            parts.Add($"[cyan]{best.Year}[/]");

        return $"{Join(" ", parts)} [dim]({best.Source})[/]";
    }
}

internal record RecordingWithSuggestions(RecordingInput Original, SuggestionSet Suggestions);

internal record FillOutputRow(
    string? Composer,
    string? Work,
    string? Orchestra,
    string? Conductor,
    string? Performers,
    string? Label,
    string LabelSuggested,
    string LabelConfidence,
    string? Year,
    string YearSuggested,
    string YearConfidence,
    string? CatalogNumber,
    string CatalogNumberSuggested,
    string CatalogNumberConfidence,
    string? Rating,
    string? Comment
);

#endregion
