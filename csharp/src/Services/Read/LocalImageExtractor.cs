namespace CSharpScripts.Services.Read;

using System.Net;
using System.Text;
using CSharpScripts.Services.Read.Ocr;

internal sealed partial class LocalImageExtractor(
	string filePath,
	AzureDocumentIntelligenceOptions? azureDocumentIntelligence = null,
	CancellationToken ct = default
)
{
	private const int MaxSectionHeadingLength = 100;
	private const int MaxFootnoteLength = 400;

	[GeneratedRegex(@"^\d+\.\s+[A-Z]")]
	private static partial Regex SectionHeadingPattern();

	[GeneratedRegex(@"^\d+[\.\)]\s")]
	private static partial Regex FootnotePattern();

	public async Task<ArticleContent> ExtractAsync()
	{
		if (!File.Exists(filePath))
			throw new FileNotFoundException($"Image not found: {filePath}", filePath);

		ct.ThrowIfCancellationRequested();
		UI.Info($"Reading local image: {filePath}");
		var bytes = await File.ReadAllBytesAsync(filePath, ct);
		UI.Info($"File size: {bytes.Length:N0} bytes");

		var name = Path.GetFileName(filePath);
		var mimeType = GetMimeType(name);
		DocumentPageResult result = await OcrImageWithFallbackAsync(name, bytes, mimeType);
		UI.Ok(
			$"  → {result.BodyBlocks.Count} blocks, {result.SkippedHeadersFooters} headers/footers stripped"
		);

		return new ArticleContent
		{
			Title = Path.GetFileNameWithoutExtension(filePath),
			BodyHtml = BuildBodyHtml(result.BodyBlocks),
			SourceUrl = new Uri($"file:///{Path.GetFullPath(filePath).Replace('\\', '/')}"),
		};
	}

	private async Task<DocumentPageResult> OcrImageWithFallbackAsync(
		string name,
		byte[] bytes,
		string mimeType
	)
	{
		if (AzureDocumentIntelligenceOcrProvider.IsConfigured(azureDocumentIntelligence))
		{
			try
			{
				UI.Info($"Azure Document Intelligence: {name} ({bytes.Length:N0} bytes)...");
				return await AzureDocumentIntelligenceOcrProvider
					.CreateConfigured(azureDocumentIntelligence)
					.OcrImageAsync(bytes, mimeType, ct);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				UI.Warn(
					$"Azure Document Intelligence failed ({ex.GetType().Name}: {ex.Message}). Attempting Google Document AI fallback..."
				);
			}
		}

		UI.Info($"Google Document AI: {name} ({bytes.Length:N0} bytes)...");
		return await new DocumentAiOcrProvider(Secrets.GoogleDocumentAiProcessorName)
			.OcrImageAsync(bytes, mimeType, ct);
	}

	private static string GetMimeType(string fileName) =>
		Path.GetExtension(fileName).ToLowerInvariant() switch
		{
			".png" => "image/png",
			".jpg" or ".jpeg" => "image/jpeg",
			_ => "application/octet-stream",
		};

	private static string BuildBodyHtml(IReadOnlyList<string> blocks)
	{
		if (blocks.Count == 0)
			return "<p><em>No content extracted.</em></p>";

		StringBuilder sb = new();
		var firstBody = true;

		foreach (var block in blocks)
		{
			var text = block.Trim();
			if (string.IsNullOrEmpty(text))
				continue;

			var encoded = WebUtility.HtmlEncode(text);

			if (IsSectionHeading(text))
			{
				sb.AppendLine($"<h2>{encoded}</h2>");
				firstBody = true;
			}
			else if (IsFootnote(text))
			{
				sb.AppendLine($"<p class=\"footnote\">{encoded}</p>");
			}
			else
			{
				sb.AppendLine($"<p{(firstBody ? " class=\"first\"" : string.Empty)}>{encoded}</p>");
				firstBody = false;
			}
		}

		return sb.ToString();
	}

	private static bool IsSectionHeading(string text) =>
		text.Length < MaxSectionHeadingLength && SectionHeadingPattern().IsMatch(text);

	private static bool IsFootnote(string text) =>
		text.Length < MaxFootnoteLength && FootnotePattern().IsMatch(text);
}
