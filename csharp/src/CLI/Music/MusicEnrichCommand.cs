using System.ComponentModel;
using CSharpScripts.Services.Music;

namespace CSharpScripts.CLI.Music;

internal sealed class MusicEnrichCommand : BaseAsyncCommand<MusicEnrichCommand.Settings>
{
	private const string EnrichSuggestCommand = "tools music enrich suggest";
	private const string EnrichExportCommand = "tools music enrich export";
	private const string EnrichPreviewCommand = "tools music enrich preview";
	private const string MusicSearchUnifiedCommand = "tools music search unified";

	private static string ResolveInputFile(string? requestedInput)
	{
		if (!IsNullOrEmpty(value: requestedInput))
			return requestedInput;

		var files = Directory.GetFiles(path: ".", searchPattern: "*.csv");

		if (files.Length == 1)
		{
			Ui.Info(
				message: "Auto-detected input file for {0}: {1}",
				EnrichSuggestCommand,
				files[0]
			);
			return files[0];
		}

		if (files.Length > 1)
		{
			return AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title(title: "Select input file:")
					.AddChoices(choices: files)
			);
		}

		Ui.Error(message: "No input file specified and no CSV files found in current directory.");
		throw new InvalidOperationException(message: "No input file found");
	}

	private static string DetermineOutputPath(string inputFile, string? explicitOutput) =>
		explicitOutput
		?? Path.Combine(
			Path.GetDirectoryName(path: inputFile) ?? ".",
			Path.GetFileNameWithoutExtension(path: inputFile) + "-enriched.csv"
		);

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
				Ui.Info(
					message: "{0} routes through {1}; use {2} to stream exports, {3} for previews, and {4} for unified searches.",
					EnrichSuggestCommand,
					nameof(RecordingEnrichmentService),
					EnrichExportCommand,
					EnrichPreviewCommand,
					MusicSearchUnifiedCommand
				);

				var inputFile = ResolveInputFile(requestedInput: settings.InputFile);

				if (!File.Exists(path: inputFile))
				{
					Ui.Error(message: "File not found: {0}", inputFile);
					throw new FileNotFoundException(
						message: "Input file not found",
						fileName: inputFile
					);
				}

				var discogsToken = Secrets.DiscogsToken;
				if (IsNullOrEmpty(value: discogsToken))
					Ui.Warn(message: "DISCOGS_USER_TOKEN not set - Discogs fallback disabled");

				List<RecordingInput> records = RecordingEnrichmentService.ReadRecordings(
					filePath: inputFile
				);
				Ui.Info(
					message: "{0} loaded {1} recordings from {2}",
					EnrichSuggestCommand,
					records.Count,
					Path.GetFileName(path: inputFile)
				);

				MusicGenreCategory genre = GenreDetector.DetectFromRecordings(records: records);
				Ui.Info(message: "Detected genre category: {0}", genre);

				List<RecordingWithSuggestions> results = [];
				MusicBrainzService mbService = new();
				using DiscogsService? discogsService = IsNullOrEmpty(value: discogsToken)
					? null
					: new DiscogsService(token: discogsToken);

				var output = DetermineOutputPath(
					inputFile: inputFile,
					explicitOutput: settings.OutputFile
				);

				using StreamWriter writer = new(path: output);
				writer.AutoFlush = true;
				using CsvWriter csv = new(
					writer: writer,
					new CsvConfiguration(cultureInfo: CultureInfo.InvariantCulture)
					{
						Delimiter = ",",
					}
				);

				csv.Context.RegisterClassMap<EnrichOutputRowMap>();
				csv.WriteHeader<EnrichOutputRow>();
				await csv.NextRecordAsync();

				Ui.NewLine();
				Ui.Info(message: "Writing results in real-time to {0}", output);
				Stopwatch enrichTimer = Stopwatch.StartNew();

				await UI.CreateStandardProgress(60, true, false, false)
					.StartAsync(async ctx =>
					{
						ProgressTask task = ctx.AddTask(
							$"[green]Searching {records.Count} recordings...[/]",
							maxValue: records.Count
						);

						foreach (RecordingInput record in records)
						{
							if (cancellationToken.IsCancellationRequested)
								break;

							var workName = !IsNullOrEmpty(value: record.Work)
								? record.Work
								: "(Unknown Work)";
							var composer = !IsNullOrEmpty(value: record.Composer)
								? record.Composer
								: "(Unknown Composer)";
							task.Description = UI.TaskDescription(
								$"({task.Value + 1}/{records.Count})",
								workName,
								$"{composer}"
							);

							SuggestionSet suggestions =
								await RecordingEnrichmentService.SearchForSuggestionsAsync(
									record: record,
									mbService: mbService,
									discogsService: discogsService,
									ct: cancellationToken
								);

							if (suggestions.HasAny())
							{
								SuggestionBundle best = suggestions.GetBest()!;
								TimeSpan elapsed = enrichTimer.Elapsed;
								var shortWork =
									workName.Length > 40 ? workName[..37] + "..." : workName;

								List<string> found = [];
								if (!IsNullOrEmpty(value: best.Label))
									found.Add($"Label: [cyan]{Markup.Escape(text: best.Label)}[/]");
								if (!IsNullOrEmpty(value: best.CatalogNumber))
									found.Add(
										$"Cat: [cyan]{Markup.Escape(text: best.CatalogNumber)}[/]"
									);
								if (!IsNullOrEmpty(value: best.Year))
									found.Add($"Year: [cyan]{best.Year}[/]");

								AnsiConsole.MarkupLine(
									$"[green]✓[/] [dim]{elapsed:mm\\:ss}[/] [bold]{Markup.Escape(text: shortWork)}[/] → {Join(separator: " │ ", values: found)} [dim]({best.Source})[/]"
								);
							}

							SuggestionBundle? bestSugg = suggestions.GetBest();

							var effectiveLabel = !IsNullOrEmpty(value: record.Label)
								? record.Label
								: bestSugg?.Label;

							EnrichOutputRow outputRow = new(
								Composer: record.Composer,
								Work: record.Work,
								Orchestra: record.Orchestra,
								Conductor: record.Conductor,
								Performers: record.Performers,
								Label: effectiveLabel,
								bestSugg?.Label ?? "",
								bestSugg?.Confidence.ToString() ?? "",
								Year: record.Year,
								bestSugg?.Year ?? "",
								bestSugg?.Confidence.ToString() ?? "",
								CatalogNumber: record.CatalogNumber,
								bestSugg?.CatalogNumber ?? "",
								bestSugg?.Confidence.ToString() ?? "",
								Rating: record.Rating,
								Comment: record.Comment
							);
							csv.WriteRecord(record: outputRow);
							await csv.NextRecordAsync();

							await writer.FlushAsync(cancellationToken: cancellationToken);

							results.Add(
								new RecordingWithSuggestions(
									Original: record,
									Suggestions: suggestions
								)
							);
							task.Increment(value: 1);
						}
					});

				MusicOutputFormatter.DisplayFillResults(results, genre);
				Ui.Complete(
					message: "Completed {0}! Results available in {1}; use {2} to stream exports or {3} to rerun unified searches.",
					EnrichSuggestCommand,
					output,
					EnrichExportCommand,
					MusicSearchUnifiedCommand
				);
			}
		);
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandOption(template: "-i|--input")]
		[Description(description: "Input CSV file with recording data")]
		public string? InputFile { get; init; }

		[CommandOption(template: "-o|--output")]
		[Description(description: "Output CSV file path (optional)")]
		public string? OutputFile { get; init; }
	}
}

file sealed class EnrichOutputRowMap : ClassMap<EnrichOutputRow>
{
	public EnrichOutputRowMap()
	{
		Map(static m => m.Composer);
		Map(static m => m.Work);
		Map(static m => m.Orchestra);
		Map(static m => m.Conductor);
		Map(static m => m.Performers);
		Map(static m => m.Label);
		Map(static m => m.LabelSuggested).Name("Label (Suggested)");
		Map(static m => m.LabelConfidence).Name("Label (Confidence)");
		Map(static m => m.Year);
		Map(static m => m.YearSuggested).Name("Year (Suggested)");
		Map(static m => m.YearConfidence).Name("Year (Confidence)");
		Map(static m => m.CatalogNumber).Name("Catalog Number");
		Map(static m => m.CatalogNumberSuggested).Name("Catalog Number (Suggested)");
		Map(static m => m.CatalogNumberConfidence).Name("Catalog Number (Confidence)");
		Map(static m => m.Rating);
		Map(static m => m.Comment);
	}
}

internal record EnrichOutputRow(
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
