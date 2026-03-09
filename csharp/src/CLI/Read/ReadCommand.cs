namespace CSharpScripts.CLI.Read;

using CSharpScripts.Services.Read.Validation;

internal sealed class ReadCommand : BaseAsyncCommand<ReadCommand.Settings>
{
	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(0, "<source>")]
		[Description("URL of the article or path to a local PDF/EPUB/image file")]
		public required string Source { get; init; }

		[CommandArgument(1, "[output]")]
		[Description("Output EPUB filename (defaults to article title)")]
		public string? Output { get; init; }

		[CommandOption("--bpc-path")]
		[Description("Path to the BPC extension directory (defaults to ../bpc-ext)")]
		public string? BpcPath { get; init; }

		[CommandOption("--calibre-library")]
		[Description(
			"Calibre library path or server URL for automatic ingestion after successful validation"
		)]
		public string? CalibreLibrary { get; init; }

		[CommandOption("--skip-validation")]
		[Description("Skip EPUBCheck validation")]
		public bool SkipValidation { get; init; }

		[CommandOption("--azure-docintel-endpoint")]
		[Description(
			"Azure Document Intelligence endpoint (optional; defaults to the repo's Karajan OCR resource, env var fallback still works)"
		)]
		public string? AzureDocumentIntelligenceEndpoint { get; init; }

		[CommandOption("--azure-docintel-key")]
		[Description("Azure Document Intelligence API key (pass directly or via env var)")]
		public string? AzureDocumentIntelligenceKey { get; init; }

		[CommandOption("--azure-docintel-model")]
		[Description("Azure Document Intelligence model id (defaults to prebuilt-layout)")]
		public string? AzureDocumentIntelligenceModel { get; init; }
	}

	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			ServiceType.Read,
			async () =>
			{
				ArticleContent content;
				AzureDocumentIntelligenceOptions azureDocumentIntelligence = new(
					settings.AzureDocumentIntelligenceEndpoint,
					settings.AzureDocumentIntelligenceKey,
					settings.AzureDocumentIntelligenceModel
				);

				// Detect local file path first
				if (File.Exists(settings.Source) && IsPdf(settings.Source))
				{
					UI.Info("Local PDF detected — using LocalPdfExtractor.");
					content = await new LocalPdfExtractor(
						settings.Source,
						azureDocumentIntelligence,
						cancellationToken
					).ExtractAsync();
				}
				else if (File.Exists(settings.Source) && IsEpub(settings.Source))
				{
					UI.Info("Local EPUB detected — using LocalEpubExtractor + OCR.");
					content = await new LocalEpubExtractor(
						settings.Source,
						azureDocumentIntelligence,
						cancellationToken
					).ExtractAsync();
				}
				else if (File.Exists(settings.Source) && IsImage(settings.Source))
				{
					UI.Info("Local image detected — using LocalImageExtractor + OCR.");
					content = await new LocalImageExtractor(
						settings.Source,
						azureDocumentIntelligence,
						cancellationToken
					).ExtractAsync();
				}
				else if (Uri.TryCreate(settings.Source, UriKind.Absolute, out Uri? url))
				{
					var isJstor = settings.Source.Contains("jstor.org/stable/", OrdinalIgnoreCase);
					UI.Info(
						isJstor
							? "JSTOR article detected — using PDF-based extraction."
							: "Standard article detected — using BPC + SmartReader extraction."
					);

					content = isJstor
						? await new JstorExtractor(url, cancellationToken).ExtractAsync()
						: await new StandardExtractor(
							url,
							settings.BpcPath,
							cancellationToken
						).ExtractAsync();
				}
				else
				{
					throw new ArgumentException(
						$"Source must be a valid URL or an existing .pdf/.epub/.jpg/.jpeg/.png file path: {settings.Source}",
						nameof(settings)
					);
				}

				var output =
					settings.Output ?? $"{EpubWriter.SanitizeFilename(content.Title)}.epub";
				EpubWriter.Write(content, output);
				UI.Ok($"EPUB saved to {output}");

				// Validate (unless skipped)
				if (!settings.SkipValidation)
				{
					EpubValidationResult validation = await EpubValidator.ValidateAsync(
						output,
						cancellationToken
					);
					if (!validation.Passed && !validation.Skipped)
						UI.Warn("EPUB validation failed — review before distributing.");
				}

				// Ingest into Calibre (if library specified)
				if (!IsNullOrWhiteSpace(settings.CalibreLibrary))
					await CalibreClient.AddAsync(
						output,
						settings.CalibreLibrary,
						cancellationToken
					);
			}
		);
	}

	internal static bool IsPdf(string path) =>
		!string.IsNullOrEmpty(path) && path.EndsWith(".pdf", OrdinalIgnoreCase);

	internal static bool IsEpub(string path) =>
		!string.IsNullOrEmpty(path) && path.EndsWith(".epub", OrdinalIgnoreCase);

	internal static bool IsImage(string path) =>
		!string.IsNullOrEmpty(path)
		&& (
			path.EndsWith(".jpg", OrdinalIgnoreCase)
			|| path.EndsWith(".jpeg", OrdinalIgnoreCase)
			|| path.EndsWith(".png", OrdinalIgnoreCase)
		);
}
