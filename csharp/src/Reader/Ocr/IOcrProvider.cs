namespace CSharpScripts.Services.Read.Ocr;

internal interface IOcrProvider
{
	Task<string> OcrPdfAsync(byte[] pdfBytes, CancellationToken ct = default);
}

internal interface IStructuredImageOcrProvider
{
	Task<DocumentPageResult> OcrImageAsync(
		byte[] imageBytes,
		string mimeType,
		CancellationToken ct = default
	);
}


