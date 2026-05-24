using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Identity;

namespace CSharpScripts.Services.Read.Ocr;

internal sealed class AzureDocumentIntelligenceOcrProvider
	: IOcrProvider,
		IStructuredImageOcrProvider
{
	private const float HeaderThreshold = 0.06f;
	private const float FooterThreshold = 0.94f;

	private readonly DocumentIntelligenceClient Client;
	private readonly string ModelId;

	internal AzureDocumentIntelligenceOcrProvider(
		string endpoint,
		string? modelId = null
	)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(argument: endpoint);

		Client = new DocumentIntelligenceClient(new Uri(uriString: endpoint), new DefaultAzureCredential());
		
		ModelId = IsNullOrWhiteSpace(value: modelId) ? "prebuilt-layout" : modelId;
	}

	public async Task<string> OcrPdfAsync(byte[] pdfBytes, CancellationToken ct = default)
	{
		Ui.Info(message: "OCR: Azure Document Intelligence ({0})...", ModelId);
		AnalyzeResult result = await AnalyzeAsync(bytes: pdfBytes, ct: ct);
		return Join(separator: "\n\n", values: ExtractStructured(result: result).BodyBlocks);
	}

	public async Task<DocumentPageResult> OcrImageAsync(
		byte[] imageBytes,
		string mimeType,
		CancellationToken ct = default
	)
	{
		AnalyzeResult result = await AnalyzeAsync(bytes: imageBytes, ct: ct);
		return ExtractStructured(result: result);
	}

	internal static bool IsConfigured(AzureDocumentIntelligenceOptions? options = null) =>
		!IsNullOrWhiteSpace(options?.Endpoint ?? Secrets.AzureDocumentIntelligenceEndpoint);

	internal static AzureDocumentIntelligenceOcrProvider CreateConfigured(
		AzureDocumentIntelligenceOptions? options = null
	) =>
		new(
			options?.Endpoint
			?? Secrets.AzureDocumentIntelligenceEndpoint
			?? throw new InvalidOperationException(
				message: "Azure Document Intelligence endpoint is not set. Pass --azure-docintel-endpoint or set AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT."
			),
			options?.ModelId ?? Secrets.AzureDocumentIntelligenceModelId
		);

	internal static DocumentPageResult ExtractStructured(AnalyzeResult result)
	{
		List<string> bodyBlocks = new(result.Pages.Count * 4);
		var skippedCount = 0;
		Dictionary<int, float> pageHeights = result.Pages.Where(static page => page.Height is > 0).ToDictionary(page => page.PageNumber,
			page => page.Height!.Value
		);

		if (result.Paragraphs.Count > 0)
		{
			foreach (DocumentParagraph paragraph in result.Paragraphs)
			{
				if (IsNullOrWhiteSpace(value: paragraph.Content))
					continue;

				if (IsHeaderFooterParagraph(paragraph: paragraph, pageHeights: pageHeights))
				{
					skippedCount++;
					continue;
				}

				bodyBlocks.Add(OcrTextCleanup.CleanBlockText(raw: paragraph.Content));
			}

			return new DocumentPageResult(BodyBlocks: bodyBlocks, SkippedHeadersFooters: skippedCount);
		}

		foreach (DocumentPage page in result.Pages)
		{
			foreach (DocumentLine line in page.Lines)
			{
				if (IsNullOrWhiteSpace(value: line.Content))
					continue;

				if (IsHeaderOrFooter(polygon: line.Polygon, pageHeight: page.Height))
				{
					skippedCount++;
					continue;
				}

				bodyBlocks.Add(OcrTextCleanup.CleanBlockText(raw: line.Content));
			}
		}

		return new DocumentPageResult(BodyBlocks: bodyBlocks, SkippedHeadersFooters: skippedCount);
	}

	private async Task<AnalyzeResult> AnalyzeAsync(byte[] bytes, CancellationToken ct)
	{
		Operation<AnalyzeResult> operation = await Client.AnalyzeDocumentAsync(
			waitUntil: WaitUntil.Completed,
			modelId: ModelId,
			BinaryData.FromBytes(data: bytes),
			cancellationToken: ct
		);
		return operation.Value;
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
			var pageHeight = pageHeights.TryGetValue(key: region.PageNumber, out var knownPageHeight)
				? knownPageHeight
				: (float?)null;
			if (IsHeaderOrFooter(polygon: region.Polygon, pageHeight: pageHeight))
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
			yCoords[index: i] = polygon[i * 2 + 1];

		var minY = yCoords[index: 0];
		var maxY = yCoords[index: 0];
		for (var i = 1; i < coordCount; i++)
		{
			if (yCoords[index: i] < minY)
				minY = yCoords[index: i];
			if (yCoords[index: i] > maxY)
				maxY = yCoords[index: i];
		}

		if (pageHeight is > 0)
		{
			minY /= pageHeight.Value;
			maxY /= pageHeight.Value;
		}

		return minY < HeaderThreshold || maxY > FooterThreshold;
	}
}

internal sealed record AzureDocumentIntelligenceOptions(
	string? Endpoint,
	string? ModelId
);
