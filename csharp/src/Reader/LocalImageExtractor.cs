using System.Net;
using System.Text;
using CSharpScripts.Services.Read.Ocr;

namespace CSharpScripts.Services.Read;

internal sealed partial class LocalImageExtractor(
	string filePath,
	AzureDocumentIntelligenceOptions? azureDocumentIntelligence = null,
	CancellationToken ct = default
)
{
	public async Task<ArticleContent> ExtractAsync()
	{
		if (!File.Exists(filePath))
			throw new FileNotFoundException($"Image not found: {filePath}", filePath);

		ct.ThrowIfCancellationRequested();
		UI.Info($"Reading local image: {filePath}");
		var bytes = await File.ReadAllBytesAsync(filePath, ct);
		UI.Info($"File size: {bytes.Length:N0} bytes");

		var name = Path.GetFileName(filePath);
		var mimeType = name.GetImageMimeType();
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
				Log.Information(
					"Azure Document Intelligence: {Name} ({Bytes} bytes)...",
					name,
					bytes.Length
				);
				return await AzureDocumentIntelligenceOcrProvider
					.CreateConfigured(azureDocumentIntelligence)
					.OcrImageAsync(bytes, mimeType, ct);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Log.Warning(
					ex,
					"Failed to parse local image metadata for {FileName}",
					Path.GetFileName(filePath)
				);
			}
		}

		Log.Information("Google Document AI: {Name} ({Bytes} bytes)...", name, bytes.Length);
		return await new DocumentAiOcrProvider(Secrets.GoogleDocumentAiProcessorName).OcrImageAsync(
			bytes,
			mimeType,
			ct
		);
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
			if (IsNullOrEmpty(text))
				continue;
			var encoded = WebUtility.HtmlEncode(text);
			if (ArticleStructureDetector.IsSectionHeading(text))
			{
				sb.AppendLine($"<h2>{encoded}</h2>");
				firstBody = true;
			}
			else if (ArticleStructureDetector.IsFootnote(text))
				sb.AppendLine($"<p class=\"footnote\">{encoded}</p>");
			else
			{
				sb.AppendLine($"<p{(firstBody ? " class=\"first\"" : Empty)}>{encoded}</p>");
				firstBody = false;
			}
		}
		return sb.ToString();
	}
}


