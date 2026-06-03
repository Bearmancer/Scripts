using System.Text;
using Google.Cloud.DocumentAI.V1;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace Scripts.Services.Read.Ocr;

internal sealed class DocumentAiOcrProvider : IStructuredImageOcrProvider
{
	private const float HeaderThreshold = 0.06f;
	private const float FooterThreshold = 0.94f;

	private static readonly Lazy<Task<DocumentProcessorServiceClient>> ClientFactory = new(() =>
		DocumentProcessorServiceClient.CreateAsync()
	);

	private readonly string ProcessorName;

	internal DocumentAiOcrProvider(string processorName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(argument: processorName);
		ProcessorName = processorName;
	}

	public async Task<DocumentPageResult> OcrImageAsync(
		byte[] imageBytes,
		string mimeType,
		CancellationToken ct = default
	)
	{
		DocumentProcessorServiceClient client = await ClientFactory.Value;

		ProcessRequest request = new()
		{
			Name = ProcessorName,
			RawDocument = new RawDocument
			{
				Content = ByteString.CopyFrom(bytes: imageBytes),
				MimeType = mimeType,
			},
		};

		ProcessResponse response = await client.ProcessDocumentAsync(
			request: request,
			cancellationToken: ct
		);
		return ExtractStructured(document: response.Document);
	}

	private static DocumentPageResult ExtractStructured(Document document)
	{
		List<string> bodyBlocks = new(document.Pages.Count * 4);
		var skippedCount = 0;

		foreach (Document.Types.Page page in document.Pages)
		{
			foreach (Document.Types.Page.Types.Block block in page.Blocks)
			{
				var text = GetBlockText(document: document, anchor: block.Layout.TextAnchor);
				if (IsNullOrWhiteSpace(value: text))
					continue;

				if (IsHeaderOrFooter(poly: block.Layout.BoundingPoly))
				{
					skippedCount++;
					continue;
				}

				bodyBlocks.Add(OcrTextCleanup.CleanBlockText(raw: text));
			}
		}

		return new DocumentPageResult(BodyBlocks: bodyBlocks, SkippedHeadersFooters: skippedCount);
	}

	private static bool IsHeaderOrFooter(BoundingPoly poly)
	{
		RepeatedField<NormalizedVertex> vertices = poly.NormalizedVertices;
		if (vertices.Count == 0)
			return false;

		var minY = vertices[index: 0].Y;
		var maxY = vertices[index: 0].Y;
		for (var i = 1; i < vertices.Count; i++)
		{
			var y = vertices[index: i].Y;
			if (y < minY)
				minY = y;

			if (y > maxY)
				maxY = y;
		}

		return minY < HeaderThreshold || maxY > FooterThreshold;
	}

	private static string GetBlockText(Document document, Document.Types.TextAnchor? anchor)
	{
		if (anchor is null || anchor.TextSegments.Count == 0)
			return "";

		var text = document.Text;
		var textLength = text.Length;
		StringBuilder sb = new(capacity: 256);
		foreach (Document.Types.TextAnchor.Types.TextSegment segment in anchor.TextSegments)
		{
			var start = (int)segment.StartIndex;
			var end = (int)segment.EndIndex;
			if (end > start && end <= textLength)
				sb.Append(text.AsSpan(start: start, end - start));
		}
		return sb.ToString();
	}
}

internal sealed record DocumentPageResult(
	IReadOnlyList<string> BodyBlocks,
	int SkippedHeadersFooters
);
