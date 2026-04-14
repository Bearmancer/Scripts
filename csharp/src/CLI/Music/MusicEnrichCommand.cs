namespace CSharpScripts.CLI.Music;

internal sealed class MusicEnrichCommand : BaseAsyncCommand<MusicEnrichCommand.Settings>
{
	private const string EnrichSuggestCommand = "tools music enrich suggest";
	private const string EnrichExportCommand = "tools music enrich export";
	private const string EnrichPreviewCommand = "tools music enrich preview";
	private const string MusicSearchUnifiedCommand = "tools music search unified";

	private static string ResolveInputFile(string? requestedInput)
	{
		if (!IsNullOrEmpty(requestedInput))
			return requestedInput;

		var files = Directory.GetFiles(".", "*.csv");

		if (files.Length == 1)
		{
			UI.Info("Auto-detected input file for {0}: {1}", EnrichSuggestCommand, files[0]);
			return files[0];
		}

		if (files.Length > 1)
		{
			return AnsiConsole.Prompt(
				SelectionPromptExtensions.AddChoices(
					SelectionPromptExtensions.Title(
						new SelectionPrompt<string>(),
						"Select input file:"
					),
					files
				)
			);
		}

		UI.Error("No input file specified and no CSV files found in current directory.");
		throw new InvalidOperationException("No input file found");
	}

	private static string DetermineOutputPath(string inputFile, string? explicitOutput) =>
		explicitOutput
		?? Path.Combine(
			Path.GetDirectoryName(inputFile) ?? ".",
			Path.GetFileNameWithoutExtension(inputFile) + "-enriched.csv"
		);

	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			ServiceType.Music,
			async () =>
			{
				UI.Info(
					"{0} routes through {1}; use {2} to stream exports, {3} for previews, and {4} for unified searches.",
					EnrichSuggestCommand,
					nameof(RecordingEnrichmentService),
					EnrichExportCommand,
					EnrichPreviewCommand,
					MusicSearchUnifiedCommand
				);

				var inputFile = ResolveInputFile(settings.InputFile);

				if (!File.Exists(inputFile))
				{
					UI.Error("File not found: {0}", inputFile);
					throw new FileNotFoundException("Input file not found", inputFile);
				}

				var discogsToken = Secrets.DiscogsToken;
				if (IsNullOrEmpty(discogsToken))
					UI.Warn("DISCOGS_USER_TOKEN not set - Discogs fallback disabled");

				List<RecordingInput> records = RecordingEnrichmentService.ReadRecordings(inputFile);
				UI.Info(
					"{0} loaded {1} recordings from {2}",
					EnrichSuggestCommand,
					records.Count,
					Path.GetFileName(inputFile)
				);

				MusicGenreCategory genre = GenreDetector.DetectFromRecordings(records);
				UI.Info("Detected genre category: {0}", genre);

				List<RecordingWithSuggestions> results = [];
				MusicBrainzService mbService = new();
				using DiscogsService? discogsService = IsNullOrEmpty(discogsToken)
					? null
					: new DiscogsService(discogsToken);

				var output = DetermineOutputPath(inputFile, settings.OutputFile);

				using StreamWriter writer = new(output);
				writer.AutoFlush = true;
				using CsvWriter csv = new(
					writer,
					new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = "," }
				);

				csv.Context.RegisterClassMap<EnrichOutputRowMap>();
				csv.WriteHeader<EnrichOutputRow>();
				await csv.NextRecordAsync();

				UI.NewLine();
				UI.Info("Writing results in real-time to {0}", output);
				var enrichTimer = Stopwatch.StartNew();

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

							var workName = !IsNullOrEmpty(record.Work)
								? record.Work
								: "(Unknown Work)";
							var composer = !IsNullOrEmpty(record.Composer)
								? record.Composer
								: "(Unknown Composer)";
							task.Description = UI.TaskDescription(
								$"({task.Value + 1}/{records.Count})",
								workName,
								$"{composer}"
							);

							SuggestionSet suggestions =
								await RecordingEnrichmentService.SearchForSuggestionsAsync(
									record,
									mbService,
									discogsService,
									cancellationToken
								);

							if (suggestions.HasAny())
							{
								SuggestionBundle best = suggestions.GetBest()!;
								TimeSpan elapsed = enrichTimer.Elapsed;
								var shortWork =
									workName.Length > 40 ? workName[..37] + "..." : workName;

								List<string> found = [];
								if (!IsNullOrEmpty(best.Label))
									found.Add($"Label: [cyan]{Markup.Escape(best.Label)}[/]");
								if (!IsNullOrEmpty(best.CatalogNumber))
								{
									found.Add($"Cat: [cyan]{Markup.Escape(best.CatalogNumber)}[/]");
								}
								if (!IsNullOrEmpty(best.Year))
									found.Add($"Year: [cyan]{best.Year}[/]");

								AnsiConsole.MarkupLine(
									$"[green]✓[/] [dim]{elapsed:mm\\:ss}[/] [bold]{Markup.Escape(shortWork)}[/] → {Join(" │ ", found)} [dim]({best.Source})[/]"
								);
							}

							SuggestionBundle? bestSugg = suggestions.GetBest();

							var effectiveLabel = !IsNullOrEmpty(record.Label)
								? record.Label
								: bestSugg?.Label;

							var outputRow = new EnrichOutputRow(
								record.Composer,
								record.Work,
								record.Orchestra,
								record.Conductor,
								record.Performers,
								effectiveLabel,
								bestSugg?.Label ?? "",
								bestSugg?.Confidence.ToString() ?? "",
								record.Year,
								bestSugg?.Year ?? "",
								bestSugg?.Confidence.ToString() ?? "",
								record.CatalogNumber,
								bestSugg?.CatalogNumber ?? "",
								bestSugg?.Confidence.ToString() ?? "",
								record.Rating,
								record.Comment
							);
							csv.WriteRecord(outputRow);
							await csv.NextRecordAsync();

							await writer.FlushAsync(cancellationToken);

							results.Add(new RecordingWithSuggestions(record, suggestions));
							task.Increment(1);
						}
					});

				MusicOutputFormatter.DisplayFillResults(results, genre);
				UI.Complete(
					"Completed {0}! Results available in {1}; use {2} to stream exports or {3} to rerun unified searches.",
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
		[CommandOption("-i|--input")]
		[Description("Input CSV file with recording data")]
		public string? InputFile { get; init; }

		[CommandOption("-o|--output")]
		[Description("Output CSV file path (optional)")]
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
