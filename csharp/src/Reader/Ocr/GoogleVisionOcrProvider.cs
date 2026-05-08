// (removed pragma warning disables; will refactor code to satisfy analyzers)

using System.Text;
using Google.Cloud.Vision.V1;
using Google.Protobuf;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace CSharpScripts.Services.Read.Ocr;

internal sealed class GoogleVisionOcrProvider : IOcrProvider
{
	private const int MaxPagesPerRequest = 5;

	public async Task<string> OcrPdfAsync(byte[] pdfBytes, CancellationToken ct = default)
	{
		UI.Info("OCR: Google Cloud Vision Document Text Detection...");
		ImageAnnotatorClient client = await ImageAnnotatorClient.CreateAsync(ct);

		int pageCount;
		List<byte[]> chunks;
		(pageCount, chunks) = SplitIntoChunks(pdfBytes);
		UI.Info($"Vision: processing {chunks.Count} chunk(s) for {pageCount} pages.");

		var results = await Task.WhenAll(chunks.Select(chunk => OcrChunkAsync(client, chunk, ct)));

		return Join("", results);
	}

	private static async Task<string> OcrChunkAsync(
		ImageAnnotatorClient client,
		byte[] chunkBytes,
		CancellationToken ct
	)
	{
		var request = new AnnotateFileRequest
		{
			InputConfig = new InputConfig
			{
				Content = ByteString.CopyFrom(chunkBytes),
				MimeType = "application/pdf",
			},
			Features = { new Feature { Type = Feature.Types.Type.DocumentTextDetection } },
		};

		var batchRequest = new BatchAnnotateFilesRequest { Requests = { request } };
		BatchAnnotateFilesResponse response = await client.BatchAnnotateFilesAsync(
			batchRequest,
			ct
		);

		var sb = new StringBuilder(4096);
		foreach (AnnotateImageResponse pageResponse in response.Responses[0].Responses)
		{
			if (pageResponse.FullTextAnnotation is not null)
				sb.AppendLine(pageResponse.FullTextAnnotation.Text);
		}
		return sb.ToString();
	}

	private static (int PageCount, List<byte[]> Chunks) SplitIntoChunks(byte[] pdfBytes)
	{
		using MemoryStream stream = new(pdfBytes);
		using var doc = PdfDocument.Open(stream);
		var totalPages = doc.NumberOfPages;

		var chunkCount = (totalPages + MaxPagesPerRequest - 1) / MaxPagesPerRequest;
		var chunks = new List<byte[]>(chunkCount);
		for (var start = 1; start <= totalPages; start += MaxPagesPerRequest)
		{
			var end = Math.Min(start + MaxPagesPerRequest - 1, totalPages);
			using PdfDocumentBuilder builder = new();
			for (var pageNum = start; pageNum <= end; pageNum++)
				builder.AddPage(doc, pageNum);
			chunks.Add(builder.Build());
		}
		return (totalPages, chunks);
	}
}
