using System.ComponentModel;
using System.Dynamic;

namespace Scripts.CLI.Music;

internal sealed class MusicTranslateCommand : BaseAsyncCommand<MusicTranslateCommand.Settings>
{
	private const int AzureBatchLimit = 100;
	private const double CostPerMillionChars = 10.0;
	private const int FreeMonthlyChars = 2_000_000;

	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			service: ServiceType.Music,
			async () =>
			{
				if (!AzureTranslationService.IsConfigured && !settings.DryRun)
				{
					Ui.Error(
						message: "AZURE_TRANSLATOR_KEY is not set. Set it or use --dry-run to preview."
					);
					throw new InvalidOperationException(
						message: "AZURE_TRANSLATOR_KEY environment variable is required"
					);
				}

				var csvPath = ResolveInputFile(csvPath: settings.CsvPath);
				var outputPath =
					settings.OutputFile
					?? Path.Combine(
						Path.GetDirectoryName(path: csvPath) ?? ".",
						Path.GetFileNameWithoutExtension(path: csvPath) + "-translated.csv"
					);

				Ui.Info(message: "Reading CSV: {0}", csvPath);
				List<ExpandoObject> records = ReadCsvRecords(csvPath: csvPath);

				if (records.Count == 0)
				{
					Ui.Warn(message: "No records found in CSV.");
					return;
				}

				var headers = GetHeaders(records[index: 0]);
				if (
					!headers.Contains(
						value: settings.Column,
						comparer: StringComparer.OrdinalIgnoreCase
					)
				)
				{
					Ui.Error(
						message: "Column '{0}' not found. Available: {1}",
						settings.Column,
						Join(separator: ", ", value: headers)
					);
					throw new InvalidOperationException(
						$"Column '{settings.Column}' not found in CSV"
					);
				}

				var actualColumn = headers.First(h => h.EqualsIgnoreCase(other: settings.Column));

				List<string> allTitles =
				[
					.. records
						.Select(r => GetField(record: r, column: actualColumn))
						.Where(t => !IsNullOrWhiteSpace(value: t))
						.Distinct(comparer: StringComparer.OrdinalIgnoreCase),
				];

				Ui.Info(
					message: "Found {0} unique non-empty titles in column '{1}'",
					allTitles.Count,
					actualColumn
				);

				Dictionary<string, string> cachedTranslations = await LoadCacheHitsAsync(
					titles: allTitles,
					ct: cancellationToken
				);

				List<string> toTranslate =
				[
					.. allTitles.Where(t =>
						!cachedTranslations.ContainsKey(key: t) && !IsAsciiOnly(text: t)
					),
				];

				var totalChars = toTranslate.Sum(t => t.Length);
				var estimatedCost =
					totalChars > FreeMonthlyChars
						? (totalChars - FreeMonthlyChars) / 1_000_000.0 * CostPerMillionChars
						: 0.0;

				Ui.Info(
					message: "Cache hits: {0} | To translate: {1} | Est. chars: {2:N0} | Est. cost: ${3:F4}",
					cachedTranslations.Count,
					toTranslate.Count,
					totalChars,
					estimatedCost
				);

				if (settings.DryRun)
				{
					ShowDryRunPreview(toTranslate: toTranslate);
					return;
				}

				Dictionary<string, string> newTranslations = await TranslateBatchedAsync(
					toTranslate: toTranslate,
					ct: cancellationToken
				);

				await TranslationCache.SetBatchCachedAsync(
					newTranslations.Select(kv => (kv.Key, "en", kv.Value)),
					ct: cancellationToken
				);

				Dictionary<string, string> allTranslations = new(
					dictionary: cachedTranslations,
					comparer: StringComparer.OrdinalIgnoreCase
				);
				foreach (KeyValuePair<string, string> kv in newTranslations)
					allTranslations[key: kv.Key] = kv.Value;

				ApplyTranslations(
					records: records,
					sourceColumn: actualColumn,
					translations: allTranslations
				);
				WriteCsvRecords(outputPath: outputPath, records: records, originalHeaders: headers);

				Ui.Ok(
					message: "Done. Translated {0} titles → {1}",
					newTranslations.Count,
					outputPath
				);
			}
		);
	}

	private static string ResolveInputFile(string? csvPath)
	{
		if (!IsNullOrEmpty(value: csvPath))
		{
			if (!File.Exists(path: csvPath))
				throw new FileNotFoundException(message: "CSV file not found", fileName: csvPath);

			return csvPath;
		}

		var files = Directory.GetFiles(path: ".", searchPattern: "*.csv");
		if (files.Length == 1)
		{
			Ui.Info(message: "Auto-detected CSV: {0}", files[0]);
			return files[0];
		}

		if (files.Length > 1)
		{
			return AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title(title: "Select CSV file:")
					.AddChoices(choices: files)
			);
		}

		throw new InvalidOperationException(
			message: "No CSV path provided and no .csv files found in current directory"
		);
	}

	private static List<ExpandoObject> ReadCsvRecords(string csvPath)
	{
		using StreamReader reader = new(path: csvPath);
		using CsvReader csv = new(
			reader: reader,
			new CsvConfiguration(cultureInfo: CultureInfo.InvariantCulture)
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
		var match = dict.Keys.FirstOrDefault(k => k.EqualsIgnoreCase(other: column));
		return match is null ? "" : dict[key: match]?.ToString() ?? "";
	}

	private static void SetField(ExpandoObject record, string column, string value)
	{
		IDictionary<string, object?> dict = record;
		var match = dict.Keys.FirstOrDefault(k => k.EqualsIgnoreCase(other: column));
		if (match is { })
			dict[key: match] = value;
		else
			dict[key: column] = value;
	}

	private static async Task<Dictionary<string, string>> LoadCacheHitsAsync(
		List<string> titles,
		CancellationToken ct
	)
	{
		Dictionary<string, string> hits = new(
			capacity: 0,
			comparer: StringComparer.OrdinalIgnoreCase
		);
		foreach (var title in titles)
		{
			var cached = await TranslationCache.GetCachedAsync(
				text: title,
				targetLang: "en",
				ct: ct
			);
			if (cached is { })
				hits[key: title] = cached;
		}
		return hits;
	}

	private static async Task<Dictionary<string, string>> TranslateBatchedAsync(
		List<string> toTranslate,
		CancellationToken ct
	)
	{
		Dictionary<string, string> results = new(
			capacity: 0,
			comparer: StringComparer.OrdinalIgnoreCase
		);

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
					description: "Translating titles",
					maxValue: toTranslate.Count
				);

				for (var i = 0; i < toTranslate.Count; i += AzureBatchLimit)
				{
					List<string> batch =
					[
						.. toTranslate.Skip(count: i).Take(count: AzureBatchLimit),
					];

					IReadOnlyList<TranslationResult> batchResults =
						await AzureTranslationService.TranslateBatchAsync(texts: batch, ct: ct);

					for (var j = 0; j < batch.Count; j++)
					{
						if (batchResults[index: j].DetectedLanguage != "en")
							results[batch[index: j]] = batchResults[index: j].Translation;
					}

					task.Increment(value: batch.Count);
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
			var title = GetField(record: record, column: sourceColumn);
			if (IsNullOrWhiteSpace(value: title))
				continue;

			if (translations.TryGetValue(key: title, out var translated))
				SetField(record: record, column: "TranslatedTitle", value: translated);
			else
				SetField(record: record, column: "TranslatedTitle", value: title);
		}
	}

	private static void WriteCsvRecords(
		string outputPath,
		List<ExpandoObject> records,
		string[] originalHeaders
	)
	{
		var allHeaders = originalHeaders.Contains(
			value: "TranslatedTitle",
			comparer: StringComparer.OrdinalIgnoreCase
		)
			? originalHeaders
			: [.. originalHeaders, "TranslatedTitle"];

		using StreamWriter writer = new(path: outputPath);
		using CsvWriter csv = new(writer: writer, culture: CultureInfo.InvariantCulture);

		foreach (var header in allHeaders)
			csv.WriteField(field: header);
		csv.NextRecord();

		foreach (ExpandoObject record in records)
		{
			foreach (var header in allHeaders)
				csv.WriteField(GetField(record: record, column: header));
			csv.NextRecord();
		}
	}

	private static void ShowDryRunPreview(List<string> toTranslate)
	{
		if (toTranslate.Count == 0)
		{
			Ui.Ok(message: "All titles appear to be ASCII/English — nothing to translate.");
			return;
		}

		SpectreTable table = new SpectreTable().AddColumn(column: "Title (non-ASCII)");

		foreach (var title in toTranslate.Take(count: 50))
			table.AddRow(Markup.Escape(text: title));

		if (toTranslate.Count > 50)
			table.AddRow($"... and {toTranslate.Count - 50} more");

		AnsiConsole.Write(renderable: table);
		Ui.Info(message: "[Dry run] No API calls were made.");
	}

	private static bool IsAsciiOnly(string text)
	{
		foreach (var c in text.AsSpan())
		{
			if (c > 127)
				return false;
		}
		return true;
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(position: 0, template: "[csv-path]")]
		[Description(description: "Path to the CSV file to translate (auto-detects if omitted)")]
		public string? CsvPath { get; init; }

		[CommandOption(template: "--column")]
		[Description(description: "Column name to translate (default: Title)")]
		[DefaultValue(value: "Title")]
		public string Column { get; init; } = "Title";

		[CommandOption(template: "--dry-run")]
		[Description(description: "Show what would be translated without making API calls")]
		public bool DryRun { get; init; }

		[CommandOption(template: "-o|--output")]
		[Description(
			description: "Output CSV file path (default: overwrites input with -translated suffix)"
		)]
		public string? OutputFile { get; init; }
	}
}
