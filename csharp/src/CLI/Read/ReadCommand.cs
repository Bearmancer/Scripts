using CSharpScripts.Services.Read.Ocr;
using CSharpScripts.Services.Read.Validation;

namespace CSharpScripts.CLI.Read;

internal sealed class ReadCommand : BaseAsyncCommand<ReadCommand.Settings>
{
	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			service: ServiceType.Read,
			async () =>
			{
				ArticleContent content;
				AzureDocumentIntelligenceOptions azureDocumentIntelligence = new(
					Endpoint: settings.AzureDocumentIntelligenceEndpoint,
					ApiKey: settings.AzureDocumentIntelligenceKey,
					ModelId: settings.AzureDocumentIntelligenceModel
				);

				if (File.Exists(path: settings.Source) && IsPdf(path: settings.Source))
				{
					UI.Info(message: "Local PDF detected — using LocalPdfExtractor.");
					content = await new LocalPdfExtractor(
						filePath: settings.Source,
						azureDocumentIntelligence: azureDocumentIntelligence,
						cancellationToken
					).ExtractAsync();
				}
				else if (File.Exists(path: settings.Source) && IsEpub(path: settings.Source))
				{
					UI.Info(message: "Local EPUB detected — using LocalEpubExtractor + OCR.");
					content = await new LocalEpubExtractor(
						filePath: settings.Source,
						azureDocumentIntelligence: azureDocumentIntelligence,
						cancellationToken
					).ExtractAsync();
				}
				else if (File.Exists(path: settings.Source) && IsImage(path: settings.Source))
				{
					UI.Info(message: "Local image detected — using LocalImageExtractor + OCR.");
					content = await new LocalImageExtractor(
						filePath: settings.Source,
						azureDocumentIntelligence: azureDocumentIntelligence,
						cancellationToken
					).ExtractAsync();
				}
				else if (
					Uri.TryCreate(
						uriString: settings.Source,
						uriKind: UriKind.Absolute,
						out Uri? url
					)
				)
				{
					var isJstor = settings.Source.ContainsIgnoreCase("jstor.org/stable/");
					UI.Info(
						isJstor
							? "JSTOR article detected — using PDF-based extraction."
							: "Standard article detected — using BPC + SmartReader extraction."
					);

					content = isJstor
						? await new JstorExtractor(url: url, cancellationToken).ExtractAsync()
						: await new StandardExtractor(
							url: url,
							bpcPath: settings.BpcPath,
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
					settings.Output ?? $"{EpubWriter.SanitizeFilename(title: content.Title)}.epub";
				EpubWriter.Write(content: content, outputPath: output);
				UI.Ok($"EPUB saved to {output}");

				if (!settings.SkipValidation)
				{
					EpubValidationResult validation = await EpubValidator.ValidateAsync(
						epubPath: output,
						cancellationToken
					);
					if (!validation.Passed && !validation.Skipped)
						UI.Warn(message: "EPUB validation failed — review before distributing.");
				}

				if (!IsNullOrWhiteSpace(value: settings.CalibreLibrary))
				{
					await CalibreClient.AddAsync(
						epubPath: output,
						library: settings.CalibreLibrary,
						cancellationToken
					);
				}
			}
		);
	}

	internal static bool IsPdf(string path) =>
		!IsNullOrEmpty(value: path) && path.EndsWithIgnoreCase(".pdf");

	internal static bool IsEpub(string path) =>
		!IsNullOrEmpty(value: path) && path.EndsWithIgnoreCase(".epub");

	internal static bool IsImage(string path) =>
		!IsNullOrEmpty(value: path)
		&& (
			path.EndsWithIgnoreCase(".jpg")
			|| path.EndsWithIgnoreCase(".jpeg")
			|| path.EndsWithIgnoreCase(".png")
		);

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(position: 0, template: "<source>")]
		[Description(description: "URL of the article or path to a local PDF/EPUB/image file")]
		public required string Source { get; init; }

		[CommandArgument(position: 1, template: "[output]")]
		[Description(description: "Output EPUB filename (defaults to article title)")]
		public string? Output { get; init; }

		[CommandOption(template: "--bpc-path")]
		[Description(description: "Path to the BPC extension directory (defaults to ../bpc-ext)")]
		public string? BpcPath { get; init; }

		[CommandOption(template: "--calibre-library")]
		[Description(
			description: "Calibre library path or server URL for automatic ingestion after successful validation"
		)]
		public string? CalibreLibrary { get; init; }

		[CommandOption(template: "--skip-validation")]
		[Description(description: "Skip EPUBCheck validation")]
		public bool SkipValidation { get; init; }

		[CommandOption(template: "--azure-docintel-endpoint")]
		[Description(
			description: "Azure Document Intelligence endpoint (optional; defaults to the repo's Karajan OCR resource, env var fallback still works)"
		)]
		public string? AzureDocumentIntelligenceEndpoint { get; init; }

		[CommandOption(template: "--azure-docintel-key")]
		[Description(
			description: "Azure Document Intelligence API key (pass directly or via env var)"
		)]
		public string? AzureDocumentIntelligenceKey { get; init; }

		[CommandOption(template: "--azure-docintel-model")]
		[Description(
			description: "Azure Document Intelligence model id (defaults to prebuilt-layout)"
		)]
		public string? AzureDocumentIntelligenceModel { get; init; }
	}
}
