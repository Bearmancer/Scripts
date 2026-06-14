using Azure;
using Azure.AI.DocumentIntelligence;
using Scripts.Services.Read.Ocr;

namespace Scripts.Services.Language;

internal static class AzureDocumentIntelligenceService
{
	private const float HeaderThreshold = 0.06f;
	private const float FooterThreshold = 0.94f;

	private static readonly DocumentIntelligenceClient? Client = string.IsNullOrWhiteSpace(
		Secrets.AzureDocumentIntelligenceEndpoint
	)
		? null
		: new DocumentIntelligenceClient(
			new Uri(Secrets.AzureDocumentIntelligenceEndpoint),
			Core.Auth.AzureAuth.Credential
		);

	internal static bool IsConfigured => !string.IsNullOrWhiteSpace(Secrets.AzureDocumentIntelligenceEndpoint);

	internal static async Task<string?> OcrPdfAsync(
		byte[] pdfBytes,
		CancellationToken ct = default
	)
	{
		_ = pdfBytes ?? throw new ArgumentNullException(nameof(pdfBytes));
		using var track = Log.Track(new { pdfBytesLength = pdfBytes.Length });

		if (pdfBytes.Length == 0)
			throw new ArgumentException("PDF bytes cannot be empty.", nameof(pdfBytes));

		if (Client is null)
			return null;

		try
		{
			Operation<AnalyzeResult> operation = await Client
				.AnalyzeDocumentAsync(
					waitUntil: WaitUntil.Completed,
					modelId: Secrets.AzureDocumentIntelligenceModelId,
					BinaryData.FromBytes(pdfBytes),
					cancellationToken: ct
				)
				.ConfigureAwait(continueOnCapturedContext: false);

			DocumentPageResult result = ExtractStructured(operation.Value);
			return result.BodyBlocks.Count == 0
				? null
				: string.Join("\n\n", result.BodyBlocks);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning("Azure Document Intelligence PDF OCR failed: {Error}", ex.Message);
			return null;
		}
	}

	internal static async Task<DocumentPageResult?> OcrImageAsync(
		byte[] imageBytes,
		string mimeType,
		CancellationToken ct = default
	)
	{
		_ = imageBytes ?? throw new ArgumentNullException(nameof(imageBytes));
		_ = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
		using var track = Log.Track(new { imageBytesLength = imageBytes.Length, mimeType });

		if (imageBytes.Length == 0)
			throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));

		if (Client is null)
			return null;

		try
		{
			Operation<AnalyzeResult> operation = await Client
				.AnalyzeDocumentAsync(
					waitUntil: WaitUntil.Completed,
					modelId: Secrets.AzureDocumentIntelligenceModelId,
					BinaryData.FromBytes(imageBytes),
					cancellationToken: ct
				)
				.ConfigureAwait(continueOnCapturedContext: false);

			return ExtractStructured(operation.Value);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning("Azure Document Intelligence image OCR failed: {Error}", ex.Message);
			return null;
		}
	}

	private static DocumentPageResult ExtractStructured(AnalyzeResult result)
	{
		List<string> bodyBlocks = new(result.Pages.Count * 4);
		var skippedCount = 0;
		Dictionary<int, float> pageHeights = result
			.Pages.Where(static page => page.Height is > 0)
			.ToDictionary(page => page.PageNumber, page => page.Height!.Value);

		if (result.Paragraphs.Count > 0)
		{
			foreach (DocumentParagraph paragraph in result.Paragraphs)
			{
				if (IsNullOrWhiteSpace(paragraph.Content))
					continue;

				if (IsHeaderFooterParagraph(paragraph, pageHeights))
				{
					skippedCount++;
					continue;
				}

				bodyBlocks.Add(OcrTextCleanup.CleanBlockText(paragraph.Content));
			}

			return new DocumentPageResult(
				BodyBlocks: bodyBlocks,
				SkippedHeadersFooters: skippedCount
			);
		}

		foreach (DocumentPage page in result.Pages)
		{
			foreach (DocumentLine line in page.Lines)
			{
				if (IsNullOrWhiteSpace(line.Content))
					continue;

				if (IsHeaderOrFooter(line.Polygon, page.Height))
				{
					skippedCount++;
					continue;
				}

				bodyBlocks.Add(OcrTextCleanup.CleanBlockText(line.Content));
			}
		}

		return new DocumentPageResult(BodyBlocks: bodyBlocks, SkippedHeadersFooters: skippedCount);
	}

	private static bool IsHeaderFooterParagraph(
		DocumentParagraph paragraph,
		Dictionary<int, float> pageHeights
	)
	{
		if (paragraph.Role is { } role)
		{
			if (
				role == ParagraphRole.PageHeader
				|| role == ParagraphRole.PageFooter
				|| role == ParagraphRole.PageNumber
			)
				return true;
		}

		foreach (BoundingRegion region in paragraph.BoundingRegions)
		{
			var pageHeight = pageHeights.TryGetValue(region.PageNumber, out var knownPageHeight)
				? knownPageHeight
				: (float?)null;
			if (IsHeaderOrFooter(region.Polygon, pageHeight))
				return true;
		}

		return false;
	}

	private static bool IsHeaderOrFooter(IReadOnlyList<float> polygon, float? pageHeight)
	{
		if (polygon.Count < 2)
			return false;

		var coordCount = polygon.Count / 2;
		if (coordCount == 0)
			return false;

		Span<float> yCoords = stackalloc float[coordCount];
		for (var i = 0; i < coordCount; i++)
			yCoords[i] = polygon[i * 2 + 1];

		var minY = yCoords[0];
		var maxY = yCoords[0];
		for (var i = 1; i < coordCount; i++)
		{
			if (yCoords[i] < minY)
				minY = yCoords[i];
			if (yCoords[i] > maxY)
				maxY = yCoords[i];
		}

		if (pageHeight is > 0)
		{
			minY /= pageHeight.Value;
			maxY /= pageHeight.Value;
		}

		return minY < HeaderThreshold || maxY > FooterThreshold;
	}
}
