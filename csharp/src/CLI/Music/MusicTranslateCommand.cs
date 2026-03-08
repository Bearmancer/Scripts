namespace CSharpScripts.CLI.Music;

using System.Dynamic;

internal sealed class MusicTranslateCommand : BaseAsyncCommand<MusicTranslateCommand.Settings>
{
	private const int AzureBatchLimit = 100;
	private const double CostPerMillionChars = 10.0;
	private const int FreeMonthlyChars = 2_000_000;

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(0, "[csv-path]")]
		[Description("Path to the CSV file to translate (auto-detects if omitted)")]
		public string? CsvPath { get; init; }

		[CommandOption("--column")]
		[Description("Column name to translate (default: Title)")]
		[DefaultValue("Title")]
		public string Column { get; init; } = "Title";

		[CommandOption("--dry-run")]
		[Description("Show what would be translated without making API calls")]
		public bool DryRun { get; init; }

		[CommandOption("-o|--output")]
		[Description("Output CSV file path (default: overwrites input with -translated suffix)")]
		public string? OutputFile { get; init; }
	}

	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			ServiceType.Music,
			async () =>
			{
				if (!AzureTranslationService.IsConfigured && !settings.DryRun)
				{
					UI.Error(
						"AZURE_TRANSLATOR_KEY is not set. Set it or use --dry-run to preview."
					);
					throw new InvalidOperationException(
						"AZURE_TRANSLATOR_KEY environment variable is required"
					);
				}

				var csvPath = ResolveInputFile(settings.CsvPath);
				var outputPath =
					settings.OutputFile
					?? Path.Combine(
						Path.GetDirectoryName(csvPath) ?? ".",
						Path.GetFileNameWithoutExtension(csvPath) + "-translated.csv"
					);

				UI.Info("Reading CSV: {0}", csvPath);
				List<ExpandoObject> records = ReadCsvRecords(csvPath);

				if (records.Count == 0)
				{
					UI.Warn("No records found in CSV.");
					return;
				}

				var headers = GetHeaders(records[0]);
				if (!headers.Contains(settings.Column, StringComparer.OrdinalIgnoreCase))
				{
					UI.Error(
						"Column '{0}' not found. Available: {1}",
						settings.Column,
						string.Join(", ", headers)
					);
					throw new InvalidOperationException(
						$"Column '{settings.Column}' not found in CSV"
					);
				}

				var actualColumn = headers.First(h => h.Equals(settings.Column, OrdinalIgnoreCase));

				List<string> allTitles =
				[
					.. records
						.Select(r => GetField(r, actualColumn))
						.Where(t => !IsNullOrWhiteSpace(t))
						.Distinct(StringComparer.OrdinalIgnoreCase),
				];

				UI.Info(
					"Found {0} unique non-empty titles in column '{1}'",
					allTitles.Count,
					actualColumn
				);

				Dictionary<string, string> cachedTranslations = await LoadCacheHitsAsync(
					allTitles,
					cancellationToken
				);

				List<string> toTranslate =
				[
					.. allTitles.Where(t => !cachedTranslations.ContainsKey(t) && !IsAsciiOnly(t)),
				];

				var totalChars = toTranslate.Sum(t => t.Length);
				var estimatedCost =
					totalChars > FreeMonthlyChars
						? (totalChars - FreeMonthlyChars) / 1_000_000.0 * CostPerMillionChars
						: 0.0;

				UI.Info(
					"Cache hits: {0} | To translate: {1} | Est. chars: {2:N0} | Est. cost: ${3:F4}",
					cachedTranslations.Count,
					toTranslate.Count,
					totalChars,
					estimatedCost
				);

				if (settings.DryRun)
				{
					ShowDryRunPreview(toTranslate);
					return;
				}

				Dictionary<string, string> newTranslations = await TranslateBatchedAsync(
					toTranslate,
					cancellationToken
				);

				await TranslationCache.SetBatchCachedAsync(
					newTranslations.Select(kv => (kv.Key, "en", kv.Value)),
					cancellationToken
				);

				Dictionary<string, string> allTranslations = new(
					cachedTranslations,
					StringComparer.OrdinalIgnoreCase
				);
				foreach (KeyValuePair<string, string> kv in newTranslations)
					allTranslations[kv.Key] = kv.Value;

				ApplyTranslations(records, actualColumn, allTranslations);
				WriteCsvRecords(outputPath, records, headers);

				UI.Ok("Done. Translated {0} titles → {1}", newTranslations.Count, outputPath);
			}
		);
	}

	private static string ResolveInputFile(string? csvPath)
	{
		if (!IsNullOrEmpty(csvPath))
		{
			if (!File.Exists(csvPath))
				throw new FileNotFoundException("CSV file not found", csvPath);
			return csvPath;
		}

		var files = Directory.GetFiles(".", "*.csv");
		if (files.Length == 1)
		{
			UI.Info("Auto-detected CSV: {0}", files[0]);
			return files[0];
		}

		if (files.Length > 1)
		{
			return AnsiConsole.Prompt(
				new SelectionPrompt<string>().Title("Select CSV file:").AddChoices(files)
			);
		}

		throw new InvalidOperationException(
			"No CSV path provided and no .csv files found in current directory"
		);
	}

	private static List<ExpandoObject> ReadCsvRecords(string csvPath)
	{
		using StreamReader reader = new(csvPath);
		using CsvReader csv = new(
			reader,
			new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				TrimOptions = TrimOptions.Trim,
				IgnoreBlankLines = true,
				MissingFieldFound = null,
				HeaderValidated = null,
			}
		);

		return [.. csv.GetRecords<dynamic>().Cast<ExpandoObject>()];
	}

	private static string[] GetHeaders(ExpandoObject record) =>
		[.. ((IDictionary<string, object?>)record).Keys];

	private static string GetField(ExpandoObject record, string column)
	{
		IDictionary<string, object?> dict = record;
		var match = dict.Keys.FirstOrDefault(k => k.Equals(column, OrdinalIgnoreCase));
		return match is null ? string.Empty : dict[match]?.ToString() ?? string.Empty;
	}

	private static void SetField(ExpandoObject record, string column, string value)
	{
		IDictionary<string, object?> dict = record;
		var match = dict.Keys.FirstOrDefault(k => k.Equals(column, OrdinalIgnoreCase));
		if (match is not null)
			dict[match] = value;
		else
			dict[column] = value;
	}

	private static async Task<Dictionary<string, string>> LoadCacheHitsAsync(
		List<string> titles,
		CancellationToken ct
	)
	{
		Dictionary<string, string> hits = [with(StringComparer.OrdinalIgnoreCase)];
		foreach (var title in titles)
		{
			var cached = await TranslationCache.GetCachedAsync(title, "en", ct);
			if (cached is not null)
				hits[title] = cached;
		}
		return hits;
	}

	private static async Task<Dictionary<string, string>> TranslateBatchedAsync(
		List<string> toTranslate,
		CancellationToken ct
	)
	{
		Dictionary<string, string> results = [with(StringComparer.OrdinalIgnoreCase)];

		if (toTranslate.Count == 0)
			return results;

		await AnsiConsole
			.Progress()
			.Columns(
				new TaskDescriptionColumn(),
				new ProgressBarColumn(),
				new PercentageColumn(),
				new RemainingTimeColumn()
			)
			.StartAsync(async progressCtx =>
			{
				ProgressTask task = progressCtx.AddTask(
					"Translating titles",
					maxValue: toTranslate.Count
				);

				for (var i = 0; i < toTranslate.Count; i += AzureBatchLimit)
				{
					List<string> batch = [.. toTranslate.Skip(i).Take(AzureBatchLimit)];

					IReadOnlyList<TranslationResult> batchResults =
						await AzureTranslationService.TranslateBatchAsync(batch, ct: ct);

					for (var j = 0; j < batch.Count; j++)
					{
						// Only use the translation if the source was not already English
						if (batchResults[j].DetectedLanguage != "en")
							results[batch[j]] = batchResults[j].Translation;
					}

					task.Increment(batch.Count);
				}
			});

		return results;
	}

	private static void ApplyTranslations(
		List<ExpandoObject> records,
		string sourceColumn,
		Dictionary<string, string> translations
	)
	{
		foreach (ExpandoObject record in records)
		{
			var title = GetField(record, sourceColumn);
			if (IsNullOrWhiteSpace(title))
				continue;

			if (translations.TryGetValue(title, out var translated))
				SetField(record, "TranslatedTitle", translated);
			else
				SetField(record, "TranslatedTitle", title);
		}
	}

	private static void WriteCsvRecords(
		string outputPath,
		List<ExpandoObject> records,
		string[] originalHeaders
	)
	{
		var allHeaders = originalHeaders.Contains(
			"TranslatedTitle",
			StringComparer.OrdinalIgnoreCase
		)
			? originalHeaders
			: [.. originalHeaders, "TranslatedTitle"];

		using StreamWriter writer = new(outputPath);
		using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);

		foreach (var header in allHeaders)
			csv.WriteField(header);
		csv.NextRecord();

		foreach (ExpandoObject record in records)
		{
			foreach (var header in allHeaders)
				csv.WriteField(GetField(record, header));
			csv.NextRecord();
		}
	}

	private static void ShowDryRunPreview(List<string> toTranslate)
	{
		if (toTranslate.Count == 0)
		{
			UI.Ok("All titles appear to be ASCII/English — nothing to translate.");
			return;
		}

		SpectreTable table = new SpectreTable().AddColumn("Title (non-ASCII)");

		foreach (var title in toTranslate.Take(50))
			table.AddRow(Markup.Escape(title));

		if (toTranslate.Count > 50)
			table.AddRow($"... and {toTranslate.Count - 50} more");

		AnsiConsole.Write(table);
		UI.Info("[Dry run] No API calls were made.");
	}

	private static bool IsAsciiOnly(string text) => text.All(c => c <= 127);
}
