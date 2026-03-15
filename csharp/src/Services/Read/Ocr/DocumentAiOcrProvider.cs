namespace CSharpScripts.Services.Read.Ocr;

using Google.Cloud.DocumentAI.V1;
using Google.Protobuf;

internal sealed partial class DocumentAiOcrProvider : IStructuredImageOcrProvider
{
	private const float HeaderThreshold = 0.06f;
	private const float FooterThreshold = 0.94f;

	private readonly string ProcessorName;

	internal DocumentAiOcrProvider(string processorName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(processorName);
		ProcessorName = processorName;
	}

	public async Task<DocumentPageResult> OcrImageAsync(
		byte[] imageBytes,
		string mimeType,
		CancellationToken ct = default
	)
	{
		DocumentProcessorServiceClient client = await DocumentProcessorServiceClient.CreateAsync(
			ct
		);

		ProcessRequest request = new()
		{
			Name = ProcessorName,
			RawDocument = new RawDocument
			{
				Content = ByteString.CopyFrom(imageBytes),
				MimeType = mimeType,
			},
		};

		ProcessResponse response = await client.ProcessDocumentAsync(
			request,
			cancellationToken: ct
		);
		return ExtractStructured(response.Document);
	}

	private static DocumentPageResult ExtractStructured(Document document)
	{
		List<string> bodyBlocks = [];
		var skippedCount = 0;

		foreach (Document.Types.Page page in document.Pages)
		{
			foreach (Document.Types.Page.Types.Block block in page.Blocks)
			{
				var text = GetBlockText(document, block.Layout.TextAnchor);
				if (IsNullOrWhiteSpace(text))
					continue;

				if (IsHeaderOrFooter(block.Layout.BoundingPoly))
				{
					skippedCount++;
					continue;
				}

				bodyBlocks.Add(CleanBlockText(text));
			}
		}

		return new DocumentPageResult(bodyBlocks, skippedCount);
	}

	private static bool IsHeaderOrFooter(BoundingPoly poly)
	{
		if (poly.NormalizedVertices.Count == 0)
			return false;

		var minY = poly.NormalizedVertices.Min(v => v.Y);
		var maxY = poly.NormalizedVertices.Max(v => v.Y);
		return minY < HeaderThreshold || maxY > FooterThreshold;
	}

	private static string GetBlockText(Document document, Document.Types.TextAnchor? anchor)
	{
		if (anchor is null || anchor.TextSegments.Count == 0)
			return Empty;

		var sb = new System.Text.StringBuilder();
		foreach (Document.Types.TextAnchor.Types.TextSegment segment in anchor.TextSegments)
		{
			var start = (int)segment.StartIndex;
			var end = (int)segment.EndIndex;
			if (end > start && end <= document.Text.Length)
				sb.Append(document.Text[start..end]);
		}
		return sb.ToString();
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

internal sealed record DocumentPageResult(
	IReadOnlyList<string> BodyBlocks,
	int SkippedHeadersFooters
);
