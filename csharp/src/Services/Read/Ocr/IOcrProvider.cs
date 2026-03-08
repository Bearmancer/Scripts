namespace CSharpScripts.Services.Read.Ocr;

internal interface IOcrProvider
{
	Task<string> OcrPdfAsync(byte[] pdfBytes, CancellationToken ct = default);
}
