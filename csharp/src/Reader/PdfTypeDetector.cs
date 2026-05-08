using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace CSharpScripts.Services.Read;

internal static class PdfTypeDetector
{
	private const int ProbePages = 5;
	private const double ScannedThresholdChars = 100.0;

	public static bool IsScanned(byte[] pdfBytes)
	{
		using MemoryStream stream = new(pdfBytes);
		using var document = PdfDocument.Open(stream);
		var pageCount = document.NumberOfPages;
		var probedPages = Math.Min(ProbePages, pageCount);
		double totalChars = 0;

		for (var i = 1; i <= probedPages; i++)
		{
			Page page = document.GetPage(i);
			totalChars += page.Text.Length;
		}

		var avgCharsPerPage = probedPages > 0 ? totalChars / probedPages : 0;
		UI.Info(
			$"PDF probe: {probedPages}/{pageCount} pages, avg {avgCharsPerPage:F0} chars/page."
		);
		var isScanned = avgCharsPerPage < ScannedThresholdChars;
		UI.Info(
			isScanned
				? "Classification: SCANNED (OCR required)."
				: "Classification: TEXT-EMBEDDED (direct extraction)."
		);
		return isScanned;
	}
}
