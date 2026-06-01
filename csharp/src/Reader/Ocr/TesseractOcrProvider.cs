namespace Scripts.Services.Read.Ocr;

internal sealed class TesseractOcrProvider(string tessdataPath = "./tessdata") : IOcrProvider
{
	public Task<string> OcrPdfAsync(byte[] pdfBytes, CancellationToken ct = default)
	{
		Ui.Info($"OCR: Tesseract fallback (tessdata: {tessdataPath})...");
		if (!Directory.Exists(path: tessdataPath))
		{
			throw new DirectoryNotFoundException(
				$"tessdata directory not found at '{tessdataPath}'. "
					+ "Download eng.traineddata from https://github.com/tesseract-ocr/tessdata"
			);
		}

		throw new NotSupportedException(
			"Tesseract fallback requires a PDF page image renderer (not yet wired). "
				+ "Ensure Google Vision credentials are configured."
		);
	}
}
