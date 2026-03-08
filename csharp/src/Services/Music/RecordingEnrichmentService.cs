namespace CSharpScripts.Services.Music;

internal static class RecordingEnrichmentService
{
	internal static List<RecordingInput> ReadRecordings(string filePath)
	{
		var delimiter = filePath.EndsWith(".tsv", OrdinalIgnoreCase) ? "\t" : ",";

		using StreamReader reader = new(filePath);
		using CsvReader csv = new(
			reader,
			new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				Delimiter = delimiter,
				HasHeaderRecord = true,
				MissingFieldFound = args =>
					Log.Warning(
						"MissingField {Header} at row {Index}",
						args.HeaderNames?.FirstOrDefault(),
						args.Index
					),
				HeaderValidated = null,
				BadDataFound = args =>
				{
					var row = args.Context?.Parser?.Row ?? -1;
					Log.Warning("BadData at row {Row}: {Raw}", row, args.RawRecord);
				},
				TrimOptions = TrimOptions.Trim,
				IgnoreBlankLines = true,
				DetectColumnCountChanges = false,
			}
		);

		csv.Context.RegisterClassMap<RecordingInputMap>();
		return [.. csv.GetRecords<RecordingInput>()];
	}

	internal static async Task<SuggestionSet> SearchForSuggestionsAsync(
		RecordingInput record,
		MusicBrainzService mbService,
		DiscogsService? discogsService,
		CancellationToken ct
	)
	{
		SuggestionSet suggestions = new();

		var query = BuildSearchQuery(record);
		if (IsNullOrEmpty(query))
			return suggestions;

		List<Func<Task>> tasks = [];

		tasks.Add(async () =>
		{
			List<SearchResult> mbResults = await mbService.SearchAsync(query, maxResults: 5, ct);
			ExtractSuggestions(mbResults, record, suggestions, "MusicBrainz");
		});

		if (discogsService is { })
		{
			tasks.Add(async () =>
			{
				List<SearchResult> discogsResults = await discogsService.SearchAsync(
					query,
					maxResults: 5,
					ct
				);
				ExtractSuggestions(discogsResults, record, suggestions, "Discogs");
			});
		}

		await Task.WhenAll(
			tasks.Select(async t =>
			{
				try
				{
					await t();
				}
				catch (Exception ex) when (ex is HttpRequestException or TimeoutException)
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
		foreach (SearchResult result in results)
		{
			var confidence = CalculateConfidence(result, record);
			if (confidence < 30)
				continue;

			var hasLabel = !IsNullOrEmpty(result.Label);
			var hasCat = !IsNullOrEmpty(result.CatalogNumber);
			var hasYear = result.Year.HasValue;

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

		foreach ((var full, var abbr) in LabelAbbreviations)
			if (label.Contains(full))
				return abbr;

		return label;
	}

	private static int CalculateConfidence(SearchResult result, RecordingInput record)
	{
		var score = 0;
		var checks = 0;

		if (!IsNullOrEmpty(record.Composer) && !IsNullOrEmpty(result.Artist))
		{
			checks++;
			if (result.Artist.Contains(record.Composer) || record.Composer.Contains(result.Artist))
				score += 30;
		}

		if (!IsNullOrEmpty(record.Work) && !IsNullOrEmpty(result.Title))
		{
			checks++;
			if (result.Title.Contains(record.Work) || record.Work.Contains(result.Title))
				score += 40;
		}

		if (
			!IsNullOrEmpty(record.Year)
			&& int.TryParse(record.Year.TrimEnd('?'), out var recordYear)
			&& result.Year.HasValue
		)
		{
			checks++;
			var yearDiff = Math.Abs(recordYear - result.Year.Value);
			if (yearDiff == 0)
				score += 30;
			else if (yearDiff <= 2)
				score += 20;
			else if (yearDiff <= 5)
				score += 10;
		}

		return checks > 0 ? Math.Min(score, 100) : 0;
	}
}

internal sealed class RecordingInputMap : ClassMap<RecordingInput>
{
	public RecordingInputMap()
	{
		Map(m => m.Composer).Name("Composer").Optional();
		Map(m => m.Work).Name("Work").Optional();
		Map(m => m.Orchestra).Name("Orchestra").Optional();
		Map(m => m.Conductor).Name("Conductor").Optional();
		Map(m => m.Performers).Name("Performers").Optional();
		Map(m => m.Label).Name("Label").Optional();
		Map(m => m.Year).Name("Year").Optional();
		Map(m => m.CatalogNumber).Name("Catalog Number", "CatalogNumber", "Cat No").Optional();
		Map(m => m.Rating).Name("Rating").Optional();
		Map(m => m.Comment).Name("Comment").Optional();
	}
}

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
);

internal sealed class SuggestionSet
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

	public void Normalize() =>
		Items = [.. Items.OrderByDescending(i => i.Confidence).ThenBy(i => i.Year).Take(5)];

	public SuggestionBundle? GetBest() => Items.FirstOrDefault();

	public string GetPreviewMarkup()
	{
		SuggestionBundle? best = GetBest();
		if (best is null)
			return "[dim]No suggestions[/]";

		List<string> parts = [];
		if (!IsNullOrEmpty(best.Label))
			parts.Add($"[cyan]{Markup.Escape(best.Label)}[/]");
		if (!IsNullOrEmpty(best.CatalogNumber))
			parts.Add($"[cyan]{Markup.Escape(best.CatalogNumber)}[/]");
		if (!IsNullOrEmpty(best.Year))
			parts.Add($"[cyan]{best.Year}[/]");

		return $"{Join(" ", parts)} [dim]({best.Source})[/]";
	}
}

internal record RecordingWithSuggestions(RecordingInput Original, SuggestionSet Suggestions);
