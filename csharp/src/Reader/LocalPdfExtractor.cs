using System.Net;
using System.Text;
using CSharpScripts.Services.Read.Ocr;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace CSharpScripts.Services.Read;

internal sealed class LocalPdfExtractor(
	string filePath,
	AzureDocumentIntelligenceOptions? azureDocumentIntelligence = null,
	CancellationToken ct = default
)
{
	public async Task<ArticleContent> ExtractAsync()
	{
		if (!File.Exists(filePath))
			throw new FileNotFoundException($"PDF not found: {filePath}", filePath);

		ct.ThrowIfCancellationRequested();
		UI.Info($"Reading local PDF: {filePath}");
		var pdfBytes = await File.ReadAllBytesAsync(filePath, ct);
		UI.Info($"File size: {pdfBytes.Length:N0} bytes");

		var isScanned = PdfTypeDetector.IsScanned(pdfBytes);
		var bodyText = isScanned
			? await OcrWithFallbackAsync(pdfBytes)
			: ExtractEmbeddedText(pdfBytes);

		var title = Path.GetFileNameWithoutExtension(filePath);
		var bodyHtml = BuildBodyHtml(bodyText);
		var cleanedHtml = HtmlCleanupHelper.CleanHtml(bodyHtml);
		PdfContentQuality quality = ClassifyPdfContentQuality(cleanedHtml);
		UI.Info($"PDF content quality: {quality}");

		return new ArticleContent
		{
			Title = title,
			BodyHtml = cleanedHtml,
			OriginalPdf = pdfBytes,
			SourceUrl = new Uri($"file:///{Path.GetFullPath(filePath).Replace('\\', '/')}"),
		};
	}

	private async Task<string> OcrWithFallbackAsync(byte[] pdfBytes)
	{
		if (AzureDocumentIntelligenceOcrProvider.IsConfigured(azureDocumentIntelligence))
		{
			try
			{
				return await AzureDocumentIntelligenceOcrProvider
					.CreateConfigured(azureDocumentIntelligence)
					.OcrPdfAsync(pdfBytes, ct);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Log.Warning(ex, "Azure Document Intelligence failed for PDF");
			}
		}

		try
		{
			return await new GoogleVisionOcrProvider().OcrPdfAsync(pdfBytes, ct);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning(ex, "Google Vision failed. Attempting Tesseract fallback...");
			return await new TesseractOcrProvider().OcrPdfAsync(pdfBytes, ct);
		}
	}

	private static string ExtractEmbeddedText(byte[] pdfBytes)
	{
		using MemoryStream stream = new(pdfBytes);
		using var document = PdfDocument.Open(stream);
		StringBuilder sb = new(pdfBytes.Length / 4);
		var count = 0;
		foreach (Page page in document.GetPages())
		{
			var text = page.Text;
			if (!IsNullOrWhiteSpace(text))
			{
				sb.AppendLine(text.Trim());
				count++;
			}
		}
		UI.Info($"Extracted embedded text from {count} pages.");
		return sb.ToString();
	}

	private static string BuildBodyHtml(string rawText) =>
		IsNullOrWhiteSpace(rawText)
			? "<p><em>No content extracted.</em></p>"
			: Join(
				"\n",
				Enumerable.Select(
					HtmlCleanupHelper.SplitIntoParagraphs(rawText),
					p => $"<p>{WebUtility.HtmlEncode(p)}</p>"
				)
			);

	private static PdfContentQuality ClassifyPdfContentQuality(string bodyHtml)
	{
		var length = bodyHtml.Length;
		return length switch
		{
			0 => PdfContentQuality.Unknown,
			< 500 => PdfContentQuality.Poor,
			< 2000 => PdfContentQuality.Fair,
			< 10000 => PdfContentQuality.Good,
			_ => PdfContentQuality.Excellent,
		};
	}
}


