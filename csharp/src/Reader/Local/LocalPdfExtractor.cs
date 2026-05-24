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
		if (!File.Exists(path: filePath))
			throw new FileNotFoundException($"PDF not found: {filePath}", fileName: filePath);

		ct.ThrowIfCancellationRequested();
		Ui.Info($"Reading local PDF: {filePath}");
		var pdfBytes = await File.ReadAllBytesAsync(path: filePath, cancellationToken: ct);
		Ui.Info($"File size: {pdfBytes.Length:N0} bytes");

		var isScanned = PdfTypeDetector.IsScanned(pdfBytes: pdfBytes);
		var bodyText = isScanned
			? await OcrWithFallbackAsync(pdfBytes: pdfBytes)
			: ExtractEmbeddedText(pdfBytes: pdfBytes);

		var title = Path.GetFileNameWithoutExtension(path: filePath);
		var bodyHtml = BuildBodyHtml(rawText: bodyText);
		var cleanedHtml = HtmlCleanupHelper.CleanHtml(html: bodyHtml);
		PdfContentQuality quality = ClassifyPdfContentQuality(bodyHtml: cleanedHtml);
		Ui.Info($"PDF content quality: {quality}");

		return new ArticleContent
		{
			Title = title,
			BodyHtml = cleanedHtml,
			OriginalPdf = pdfBytes,
			SourceUrl = new Uri($"file:///{Path.GetFullPath(path: filePath).Replace(oldChar: '\\', newChar: '/')}")
		};
	}

	private async Task<string> OcrWithFallbackAsync(byte[] pdfBytes)
	{
		if (AzureDocumentIntelligenceOcrProvider.IsConfigured(options: azureDocumentIntelligence))
		{
			try
			{
				return await AzureDocumentIntelligenceOcrProvider
					.CreateConfigured(options: azureDocumentIntelligence)
					.OcrPdfAsync(pdfBytes: pdfBytes, ct: ct);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Log.Error(ex, "Azure Document Intelligence failed for PDF");
			}
		}

		try
		{
			return await new GoogleVisionOcrProvider().OcrPdfAsync(pdfBytes: pdfBytes, ct: ct);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Error(ex, "Google Vision failed. Attempting Tesseract fallback...");
			return await new TesseractOcrProvider().OcrPdfAsync(pdfBytes: pdfBytes, ct: ct);
		}
	}

	private static string ExtractEmbeddedText(byte[] pdfBytes)
	{
		using MemoryStream stream = new(buffer: pdfBytes);
		using PdfDocument document = PdfDocument.Open(stream: stream);
		StringBuilder sb = new(pdfBytes.Length / 4);
		var count = 0;
		foreach (Page page in document.GetPages())
		{
			var text = page.Text;
			if (!IsNullOrWhiteSpace(value: text))
			{
				sb.AppendLine(text.Trim());
				count++;
			}
		}
		Ui.Info($"Extracted embedded text from {count} pages.");
		return sb.ToString();
	}

	private static string BuildBodyHtml(string rawText) =>
		IsNullOrWhiteSpace(value: rawText)
			? "<p><em>No content extracted.</em></p>"
			: Join(
				separator: "\n",
				HtmlCleanupHelper.SplitIntoParagraphs(text: rawText).Select(p => $"<p>{WebUtility.HtmlEncode(value: p)}</p>"
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
			_ => PdfContentQuality.Excellent
		};
	}
}
