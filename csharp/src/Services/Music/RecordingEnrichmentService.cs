namespace CSharpScripts.Services.Music;

internal static class RecordingEnrichmentService
{
	private static readonly FrozenDictionary<string, string> LabelAbbreviations =
		new Dictionary<string, string>(comparer: StringComparer.OrdinalIgnoreCase)
		{
			[key: "Deutsche Grammophon"] = "DG",
			[key: "His Master's Voice"] = "HMV",
			[key: "Columbia Masterworks"] = "Columbia",
			[key: "RCA Victor Red Seal"] = "RCA Red Seal",
			[key: "Decca Record Company"] = "Decca",
			[key: "Angel Records"] = "Angel",
			[key: "Philips Classics"] = "Philips",
			[key: "London Records"] = "London",
			[key: "EMI Classics"] = "EMI",
			[key: "Sony Classical"] = "Sony",
			[key: "Warner Classics"] = "Warner"
		}.ToFrozenDictionary(comparer: StringComparer.OrdinalIgnoreCase
		);

	internal static List<RecordingInput> ReadRecordings(string filePath)
	{
		var delimiter = filePath.EndsWithIgnoreCase(suffix: ".tsv") ? "\t" : ",";

		using StreamReader reader = new(path: filePath);
		using CsvReader csv = new(
			reader: reader,
			new CsvConfiguration(cultureInfo: CultureInfo.InvariantCulture)
			{
				Delimiter = delimiter,
				HasHeaderRecord = true,
				MissingFieldFound = args =>
					Log.Warning(
						messageTemplate: "MissingField {Header} at row {Index}",
						args.HeaderNames?.FirstOrDefault(),
						args.Index
					),
				HeaderValidated = null,
				BadDataFound = args =>
				{
					var row = args.Context?.Parser?.Row ?? -1;
					Log.Warning(
						messageTemplate: "BadData at row {Row}: {Raw}",
						row,
						args.RawRecord
					);
				},
				TrimOptions = TrimOptions.Trim,
				IgnoreBlankLines = true,
				DetectColumnCountChanges = false
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

		if (IsNullOrEmpty(value: record.Composer) && IsNullOrEmpty(value: record.Work))
			return suggestions;

		List<Func<Task>> tasks =
		[
			async () =>
			{
				var parsedYear =
					record.Year is { } && int.TryParse(record.Year.TrimEnd(trimChar: '?'), out var y)
						? (int?)y
						: null;
				List<SearchResult> mbResults = await mbService.SearchReleasesAsync(
					artist: record.Composer,
					release: record.Work,
					year: parsedYear,
					maxResults: 5,
					ct: ct
				);
				ExtractSuggestions(
					results: mbResults,
					record: record,
					suggestions: suggestions,
					source: "MusicBrainz"
				);
			}
		];

		if (discogsService is { })
		{
			tasks.Add(async () =>
			{
				var discogsQuery = BuildDiscogsQuery(record: record);
				List<SearchResult> discogsResults = await discogsService.SearchAsync(
					query: discogsQuery,
					maxResults: 5,
					ct: ct
				);
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
					catch (HttpRequestException)
					{
						Log.Warning(
							messageTemplate: "Transient music metadata lookup failure for {Composer} - {Work}",
							record.Composer,
							record.Work
						);
					}
					catch (TimeoutException)
					{
						Log.Warning(
							messageTemplate: "Timed out while looking up music metadata for {Composer} - {Work}",
							record.Composer,
							record.Work
						);
					}
					catch (TaskCanceledException) when (!ct.IsCancellationRequested)
					{
						Log.Warning(
							messageTemplate: "Lookup task was canceled by a timeout while searching music metadata for {Composer} - {Work}",
							record.Composer,
							record.Work
						);
					}
				}
			)
		);

		return suggestions;
	}

	private static string BuildDiscogsQuery(RecordingInput record)
	{
		List<string> parts = [];
		if (!IsNullOrEmpty(value: record.Composer))
			parts.Add(item: record.Composer);
		if (!IsNullOrEmpty(value: record.Work))
			parts.Add(item: record.Work);
		if (parts.Count < 2 && !IsNullOrEmpty(value: record.Orchestra))
			parts.Add(item: record.Orchestra);
		return Join(separator: " ", values: parts);
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
			var confidence = CalculateConfidence(result: result, record: record);
			if (confidence < 30)
				continue;

			var hasLabel = !IsNullOrEmpty(value: result.Label);
			var hasCat = !IsNullOrEmpty(value: result.CatalogNumber);
			var hasYear = result.Year.HasValue;

			if (hasLabel || hasCat || hasYear)
			{
				suggestions.Add(
					new SuggestionBundle(
						ShortenLabel(label: result.Label),
						CatalogNumber: result.CatalogNumber,
						result.Year?.ToString(),
						Confidence: confidence,
						Source: source,
						ReleaseId: result.Id
					)
				);
			}
		}

		suggestions.Normalize();
	}

	private static string? ShortenLabel(string? label)
	{
		if (IsNullOrEmpty(value: label))
			return null;

		foreach (var (full, abbr) in LabelAbbreviations)
		{
			if (label.Contains(value: full))
				return abbr;
		}

		return label;
	}

	private static string NormalizePersonName(string name)
	{
		var idx = name.IndexOf(value: ',');
		if (idx <= 0)
			return name;

		var last = name[..idx].Trim();
		var first = name[(idx + 1)..].Trim();
		return $"{first} {last}";
	}

	private static bool NamesMatch(string? a, string? b)
	{
		if (IsNullOrEmpty(value: a) || IsNullOrEmpty(value: b))
			return false;
		if (a.ContainsIgnoreCase(substring: b) || b.ContainsIgnoreCase(substring: a))
			return true;

		var aNorm = NormalizePersonName(name: a);
		var bNorm = NormalizePersonName(name: b);
		return aNorm.ContainsIgnoreCase(substring: bNorm) || bNorm.ContainsIgnoreCase(substring: aNorm);
	}

	private static int CalculateConfidence(SearchResult result, RecordingInput record)
	{
		var score = 0;
		var checks = 0;

		if (!IsNullOrEmpty(value: record.Composer) && !IsNullOrEmpty(value: result.Artist))
		{
			checks++;
			if (NamesMatch(a: record.Composer, b: result.Artist))
				score += 30;
		}

		if (!IsNullOrEmpty(value: record.Work) && !IsNullOrEmpty(value: result.Title))
		{
			checks++;
			if (
				result.Title.ContainsIgnoreCase(substring: record.Work)
				|| record.Work.ContainsIgnoreCase(substring: result.Title)
			)
				score += 40;
		}

		if (
			!IsNullOrEmpty(value: record.Year)
			&& int.TryParse(record.Year.TrimEnd(trimChar: '?'), out var recordYear)
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

		return checks > 0 ? Math.Min(val1: score, val2: 100) : 0;
	}
}

internal sealed class RecordingInputMap : ClassMap<RecordingInput>
{
	public RecordingInputMap()
	{
		Map(static m => m.Composer).Name("Composer").Optional();
		Map(static m => m.Work).Name("Work").Optional();
		Map(static m => m.Orchestra).Name("Orchestra").Optional();
		Map(static m => m.Conductor).Name("Conductor").Optional();
		Map(static m => m.Performers).Name("Performers").Optional();
		Map(static m => m.Label).Name("Label").Optional();
		Map(static m => m.Year).Name("Year").Optional();
		Map(static m => m.CatalogNumber).Name("Catalog Number", "CatalogNumber", "Cat No").Optional();
		Map(static m => m.Rating).Name("Rating").Optional();
		Map(static m => m.Comment).Name("Comment").Optional();
	}
}

internal sealed class RecordingInput
{
	public string? Composer { get; set; }
	public string? Work { get; set; }
	public string? Orchestra { get; set; }
	public string? Conductor { get; set; }
	public string? Performers { get; set; }
	public string? Label { get; set; }
	public string? Year { get; set; }
	public string? CatalogNumber { get; set; }
	public string? Rating { get; set; }
	public string? Comment { get; set; }
}

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
			Items.Add(item: bundle);
	}

	public void Normalize() =>
		Items =
		[
			.. Items.OrderByDescending(static i => i.Confidence).ThenBy(static i => i.Year
			).Take(count: 5
			)
		];

	public SuggestionBundle? GetBest() => Items.FirstOrDefault();

	public string GetPreviewMarkup()
	{
		SuggestionBundle? best = GetBest();
		if (best is null)
			return "[dim]No suggestions[/]";

		List<string> parts = [];
		if (!IsNullOrEmpty(value: best.Label))
			parts.Add($"[cyan]{Markup.Escape(text: best.Label)}[/]");
		if (!IsNullOrEmpty(value: best.CatalogNumber))
			parts.Add($"[cyan]{Markup.Escape(text: best.CatalogNumber)}[/]");
		if (!IsNullOrEmpty(value: best.Year))
			parts.Add($"[cyan]{best.Year}[/]");

		return $"{Join(separator: " ", values: parts)} [dim]({best.Source})[/]";
	}
}

internal record RecordingWithSuggestions(RecordingInput Original, SuggestionSet Suggestions);
