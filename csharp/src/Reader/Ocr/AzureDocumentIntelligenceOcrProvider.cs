using Azure;
using Azure.AI.DocumentIntelligence;

namespace CSharpScripts.Services.Read.Ocr;

internal sealed partial class AzureDocumentIntelligenceOcrProvider
	: IOcrProvider,
		IStructuredImageOcrProvider
{
	private const float HeaderThreshold = 0.06f;
	private const float FooterThreshold = 0.94f;

	private readonly DocumentIntelligenceClient Client;
	private readonly string ModelId;

	internal AzureDocumentIntelligenceOcrProvider(
		string endpoint,
		string apiKey,
		string? modelId = null
	)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
		ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

		Client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
		ModelId = IsNullOrWhiteSpace(modelId) ? "prebuilt-layout" : modelId;
	}

	public async Task<string> OcrPdfAsync(byte[] pdfBytes, CancellationToken ct = default)
	{
		UI.Info("OCR: Azure Document Intelligence ({0})...", ModelId);
		AnalyzeResult result = await AnalyzeAsync(pdfBytes, ct);
		return Join("\n\n", ExtractStructured(result).BodyBlocks);
	}

	public async Task<DocumentPageResult> OcrImageAsync(
		byte[] imageBytes,
		string mimeType,
		CancellationToken ct = default
	)
	{
		AnalyzeResult result = await AnalyzeAsync(imageBytes, ct);
		return ExtractStructured(result);
	}

	internal static bool IsConfigured(AzureDocumentIntelligenceOptions? options = null) =>
		!IsNullOrWhiteSpace(options?.Endpoint ?? Secrets.AzureDocumentIntelligenceEndpoint)
		&& !IsNullOrWhiteSpace(options?.ApiKey ?? Secrets.AzureDocumentIntelligenceKey);

	internal static AzureDocumentIntelligenceOcrProvider CreateConfigured(
		AzureDocumentIntelligenceOptions? options = null
	) =>
		new(
			options?.Endpoint
				?? Secrets.AzureDocumentIntelligenceEndpoint
				?? throw new InvalidOperationException(
					"Azure Document Intelligence endpoint is not set. Pass --azure-docintel-endpoint or set AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT."
				),
			options?.ApiKey
				?? Secrets.AzureDocumentIntelligenceKey
				?? throw new InvalidOperationException(
					"Azure Document Intelligence API key is not set. Pass --azure-docintel-key or set AZURE_DOCUMENT_INTELLIGENCE_KEY."
				),
			options?.ModelId ?? Secrets.AzureDocumentIntelligenceModelId
		);

	internal static DocumentPageResult ExtractStructured(AnalyzeResult result)
	{
		var bodyBlocks = new List<string>(result.Pages.Count * 4);
		var skippedCount = 0;
		var pageHeights = Enumerable.ToDictionary(
			Enumerable.Where(result.Pages, static page => page.Height is > 0),
			page => page.PageNumber,
			page => page.Height!.Value
		);

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

			return new DocumentPageResult(bodyBlocks, skippedCount);
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

		return new DocumentPageResult(bodyBlocks, skippedCount);
	}

	private async Task<AnalyzeResult> AnalyzeAsync(byte[] bytes, CancellationToken ct)
	{
		Operation<AnalyzeResult> operation = await Client.AnalyzeDocumentAsync(
			WaitUntil.Completed,
			ModelId,
			BinaryData.FromBytes(bytes),
			ct
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
			yCoords[i] = polygon[(i * 2) + 1];

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

internal sealed record AzureDocumentIntelligenceOptions(
	string? Endpoint,
	string? ApiKey,
	string? ModelId
);


