namespace CSharpScripts.Services.Read.Ocr;

using Azure;
using Azure.AI.DocumentIntelligence;

internal sealed partial class AzureDocumentIntelligenceOcrProvider
	: IOcrProvider, IStructuredImageOcrProvider
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

	internal static bool IsConfigured =>
		!IsNullOrWhiteSpace(Secrets.AzureDocumentIntelligenceEndpoint)
		&& !IsNullOrWhiteSpace(Secrets.AzureDocumentIntelligenceKey);

	internal static AzureDocumentIntelligenceOcrProvider CreateConfigured() =>
		new(
			Secrets.AzureDocumentIntelligenceEndpoint
				?? throw new InvalidOperationException(
					"AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT is not set"
				),
			Secrets.AzureDocumentIntelligenceKey
				?? throw new InvalidOperationException("AZURE_DOCUMENT_INTELLIGENCE_KEY is not set"),
			Secrets.AzureDocumentIntelligenceModelId
		);

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
		_ = mimeType;
		AnalyzeResult result = await AnalyzeAsync(imageBytes, ct);
		return ExtractStructured(result);
	}

	internal static DocumentPageResult ExtractStructured(AnalyzeResult result)
	{
		List<string> bodyBlocks = [];
		var skippedCount = 0;
		Dictionary<int, float> pageHeights = result.Pages
			.Where(static page => page.Height is > 0)
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

				bodyBlocks.Add(CleanBlockText(paragraph.Content));
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

				bodyBlocks.Add(CleanBlockText(line.Content));
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
		IReadOnlyDictionary<int, float> pageHeights
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
			var pageHeight = pageHeights.TryGetValue(region.PageNumber, out float knownPageHeight)
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

		List<float> yCoordinates = [];
		for (var i = 1; i < polygon.Count; i += 2)
		{
			yCoordinates.Add(polygon[i]);
		}

		if (yCoordinates.Count == 0)
			return false;

		var minY = yCoordinates.Min();
		var maxY = yCoordinates.Max();

		if (pageHeight is > 0)
		{
			minY /= pageHeight.Value;
			maxY /= pageHeight.Value;
		}

		return minY < HeaderThreshold || maxY > FooterThreshold;
	}

	private static string CleanBlockText(string raw)
	{
		var deHyphenated = HyphenBreak().Replace(raw, "$1$2");
		var reflowed = InlineNewline().Replace(deHyphenated, " ");
		return reflowed.Trim();
	}

	[GeneratedRegex(@"(\w)-\s*\n\s*(\w)")]
	private static partial Regex HyphenBreak();

	[GeneratedRegex(@"\s*\n\s*")]
	private static partial Regex InlineNewline();
}
