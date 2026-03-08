namespace CSharpScripts.CLI.Music;

internal sealed class MusicEnrichCommand : BaseAsyncCommand<MusicEnrichCommand.Settings>
{
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
				new SelectionPrompt<string>().Title("Select input file:").AddChoices(files)
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

	private const string EnrichSuggestCommand = "tools music enrich suggest";
	private const string EnrichExportCommand = "tools music enrich export";
	private const string EnrichPreviewCommand = "tools music enrich preview";
	private const string MusicSearchUnifiedCommand = "tools music search unified";

	internal sealed class Settings : CommandSettings
	{
		[CommandOption("-i|--input")]
		[Description("Input CSV file with recording data")]
		public string? InputFile { get; init; }

		[CommandOption("-o|--output")]
		[Description("Output CSV file path (optional)")]
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
					: new(token: discogsToken);

				var output = DetermineOutputPath(inputFile, settings.OutputFile);

				using StreamWriter writer = new(output) { AutoFlush = true };
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

				await UI.CreateStandardProgress(
						60,
						showRemaining: true,
						autoClear: false,
						hideCompleted: false
					)
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
								prefix: $"({task.Value + 1}/{records.Count})",
								title: workName,
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
									found.Add($"Cat: [cyan]{Markup.Escape(best.CatalogNumber)}[/]");
								if (!IsNullOrEmpty(best.Year))
									found.Add($"Year: [cyan]{best.Year}[/]");

								AnsiConsole.MarkupLine(
									$"[green]✓[/] [dim]{elapsed:mm\\:ss}[/] [bold]{Markup.Escape(shortWork)}[/] → {Join(" │ ", found)} [dim]({best.Source})[/]"
								);
							}

							SuggestionBundle? bestSugg = suggestions.GetBest();
							var outputRow = new EnrichOutputRow(
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
							await csv.NextRecordAsync();

							await writer.FlushAsync();

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
}

file sealed class EnrichOutputRowMap : ClassMap<EnrichOutputRow>
{
	public EnrichOutputRowMap()
	{
		Map(m => m.Composer);
		Map(m => m.Work);
		Map(m => m.Orchestra);
		Map(m => m.Conductor);
		Map(m => m.Performers);
		Map(m => m.Label);
		Map(m => m.LabelSuggested).Name("Label (Suggested)");
		Map(m => m.LabelConfidence).Name("Label (Confidence)");
		Map(m => m.Year);
		Map(m => m.YearSuggested).Name("Year (Suggested)");
		Map(m => m.YearConfidence).Name("Year (Confidence)");
		Map(m => m.CatalogNumber).Name("Catalog Number");
		Map(m => m.CatalogNumberSuggested).Name("Catalog Number (Suggested)");
		Map(m => m.CatalogNumberConfidence).Name("Catalog Number (Confidence)");
		Map(m => m.Rating);
		Map(m => m.Comment);
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
