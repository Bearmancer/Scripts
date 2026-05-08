using System.Text;
using Google.Cloud.DocumentAI.V1;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace CSharpScripts.Services.Read.Ocr;

internal sealed partial class DocumentAiOcrProvider : IStructuredImageOcrProvider
{
	private const float HeaderThreshold = 0.06f;
	private const float FooterThreshold = 0.94f;

	private static readonly Lazy<Task<DocumentProcessorServiceClient>> ClientFactory = new(() =>
		DocumentProcessorServiceClient.CreateAsync()
	);

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
		DocumentProcessorServiceClient client = await ClientFactory.Value;

		ProcessRequest request = new()
		{
			Name = ProcessorName,
			RawDocument = new RawDocument
			{
				Content = ByteString.CopyFrom(imageBytes),
				MimeType = mimeType,
			},
		};

		ProcessResponse response = await client.ProcessDocumentAsync(request, ct);
		return ExtractStructured(response.Document);
	}

	private static DocumentPageResult ExtractStructured(Document document)
	{
		var bodyBlocks = new List<string>(document.Pages.Count * 4);
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

				bodyBlocks.Add(OcrTextCleanup.CleanBlockText(text));
			}
		}

		return new DocumentPageResult(bodyBlocks, skippedCount);
	}

	private static bool IsHeaderOrFooter(BoundingPoly poly)
	{
		RepeatedField<NormalizedVertex> vertices = poly.NormalizedVertices;
		if (vertices.Count == 0)
			return false;

		var minY = vertices[0].Y;
		var maxY = vertices[0].Y;
		for (var i = 1; i < vertices.Count; i++)
		{
			var y = vertices[i].Y;
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
			return Empty;

		var text = document.Text;
		var textLength = text.Length;
		StringBuilder sb = new(256);
		foreach (Document.Types.TextAnchor.Types.TextSegment segment in anchor.TextSegments)
		{
			var start = (int)segment.StartIndex;
			var end = (int)segment.EndIndex;
			if (end > start && end <= textLength)
				sb.Append(text.AsSpan(start, end - start));
		}
		return sb.ToString();
	}
}

internal sealed record DocumentPageResult(
	IReadOnlyList<string> BodyBlocks,
	int SkippedHeadersFooters
);
