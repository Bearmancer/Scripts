namespace CSharpScripts.Services.Read.Ocr;

internal interface IOcrProvider
{
	Task<string> OcrPdfAsync(byte[] pdfBytes, CancellationToken ct = default);
}

internal interface IStructuredImageOcrProvider
{
	// Some OCR providers require the image MIME type explicitly, while others infer it from bytes.
	Task<DocumentPageResult> OcrImageAsync(
		byte[] imageBytes,
		string mimeType,
		CancellationToken ct = default
	);
}
