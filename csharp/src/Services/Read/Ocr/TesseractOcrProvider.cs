namespace CSharpScripts.Services.Read.Ocr;

/// <summary>Offline fallback OCR using TesseractOCR NuGet (Sicos1977).</summary>
/// <remarks>
/// Requires tessdata directory. Activated only when Google Vision credentials are unavailable.
/// Currently a stub — PDF page rasterization (PDFtoImage or similar) is not yet wired.
/// </remarks>
internal sealed class TesseractOcrProvider(string tessdataPath = "./tessdata") : IOcrProvider
{
	public Task<string> OcrPdfAsync(byte[] pdfBytes, CancellationToken ct = default)
	{
		UI.Info($"OCR: Tesseract fallback (tessdata: {tessdataPath})...");
		if (!Directory.Exists(tessdataPath))
			throw new DirectoryNotFoundException(
				$"tessdata directory not found at '{tessdataPath}'. "
					+ "Download eng.traineddata from https://github.com/tesseract-ocr/tessdata"
			);

		// PdfPig does not render bitmaps; Tesseract path requires rendered images.
		// This fallback is a stub — real implementation needs a PDF renderer (PDFtoImage or similar).
		throw new NotSupportedException(
			"Tesseract fallback requires a PDF page image renderer (not yet wired). "
				+ "Ensure Google Vision credentials are configured."
		);
	}
}
