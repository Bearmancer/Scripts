using System.Net;
using System.Text;
using CSharpScripts.Services.Read.Ocr;

namespace CSharpScripts.Services.Read;

internal sealed class LocalImageExtractor(
	string filePath,
	AzureDocumentIntelligenceOptions? azureDocumentIntelligence = null,
	CancellationToken ct = default
)
{
	public async Task<ArticleContent> ExtractAsync()
	{
		if (!File.Exists(path: filePath))
			throw new FileNotFoundException($"Image not found: {filePath}", fileName: filePath);

		ct.ThrowIfCancellationRequested();
		Ui.Info($"Reading local image: {filePath}");
		var bytes = await File.ReadAllBytesAsync(path: filePath, cancellationToken: ct);
		Ui.Info($"File size: {bytes.Length:N0} bytes");

		var name = Path.GetFileName(path: filePath);
		var mimeType = name.GetImageMimeType();
		DocumentPageResult result = await OcrImageWithFallbackAsync(
			name: name,
			bytes: bytes,
			mimeType: mimeType
		);
		Ui.Ok(
			$"  → {result.BodyBlocks.Count} blocks, {result.SkippedHeadersFooters} headers/footers stripped"
		);

		return new ArticleContent
		{
			Title = Path.GetFileNameWithoutExtension(path: filePath),
			BodyHtml = BuildBodyHtml(blocks: result.BodyBlocks),
			SourceUrl = new Uri(
				$"file:///{Path.GetFullPath(path: filePath).Replace(oldChar: '\\', newChar: '/')}"
			),
		};
	}

	private async Task<DocumentPageResult> OcrImageWithFallbackAsync(
		string name,
		byte[] bytes,
		string mimeType
	)
	{
		if (AzureDocumentIntelligenceOcrProvider.IsConfigured(options: azureDocumentIntelligence))
		{
			try
			{
				Log.Information(
					messageTemplate: "Azure Document Intelligence: {Name} ({Bytes} bytes)...",
					name,
					bytes.Length
				);
				return await AzureDocumentIntelligenceOcrProvider
					.CreateConfigured(options: azureDocumentIntelligence)
					.OcrImageAsync(imageBytes: bytes, mimeType: mimeType, ct: ct);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Log.Warning(
					ex.ToString(),
					"Failed to parse local image metadata for {FileName}",
					Path.GetFileName(path: filePath)
				);
			}
		}

		Log.Information(
			messageTemplate: "Google Document AI: {Name} ({Bytes} bytes)...",
			name,
			bytes.Length
		);
		return await new DocumentAiOcrProvider(
			processorName: Secrets.GoogleDocumentAiProcessorName
		).OcrImageAsync(imageBytes: bytes, mimeType: mimeType, ct: ct);
	}

	private static string BuildBodyHtml(IReadOnlyList<string> blocks)
	{
		if (blocks.Count == 0)
			return "<p><em>No content extracted.</em></p>";

		StringBuilder sb = new(blocks.Count * 200);
		var firstBody = true;
		foreach (var block in blocks)
		{
			var text = block.Trim();
			if (IsNullOrEmpty(value: text))
				continue;

			var encoded = WebUtility.HtmlEncode(value: text);
			if (ArticleStructureDetector.IsSectionHeading(text: text))
			{
				sb.AppendLine($"<h2>{encoded}</h2>");
				firstBody = true;
			}
			else if (ArticleStructureDetector.IsFootnote(text: text))
				sb.AppendLine($"<p class=\"footnote\">{encoded}</p>");
			else
			{
				sb.AppendLine($"<p{(firstBody ? " class=\"first\"" : "")}>{encoded}</p>");
				firstBody = false;
			}
		}
		return sb.ToString();
	}
}
