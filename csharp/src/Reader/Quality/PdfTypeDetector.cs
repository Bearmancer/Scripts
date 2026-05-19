using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace CSharpScripts.Services.Read;

internal static class PdfTypeDetector
{
	private const int ProbePages = 5;
	private const double ScannedThresholdChars = 100.0;

	public static bool IsScanned(byte[] pdfBytes)
	{
		using MemoryStream stream = new(buffer: pdfBytes);
		using PdfDocument document = PdfDocument.Open(stream: stream);
		var pageCount = document.NumberOfPages;
		var probedPages = Math.Min(val1: ProbePages, val2: pageCount);
		double totalChars = 0;

		for (var i = 1; i <= probedPages; i++)
		{
			Page page = document.GetPage(pageNumber: i);
			totalChars += page.Text.Length;
		}

		var avgCharsPerPage = probedPages > 0 ? totalChars / probedPages : 0;
		Ui.Info(
			$"PDF probe: {probedPages}/{pageCount} pages, avg {avgCharsPerPage:F0} chars/page."
		);
		var isScanned = avgCharsPerPage < ScannedThresholdChars;
		Ui.Info(
			isScanned
				? "Classification: SCANNED (OCR required)."
				: "Classification: TEXT-EMBEDDED (direct extraction)."
		);
		return isScanned;
	}
}
