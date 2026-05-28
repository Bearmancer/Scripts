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
		Ui.Info(message: "OCR: Google Cloud Vision Document Text Detection...");
		ImageAnnotatorClient client = await ImageAnnotatorClient.CreateAsync(cancellationToken: ct);

		int pageCount;
		List<byte[]> chunks;
		(pageCount, chunks) = SplitIntoChunks(pdfBytes: pdfBytes);
		Ui.Info($"Vision: processing {chunks.Count} chunk(s) for {pageCount} pages.");

		var results = await Task.WhenAll(
			chunks.Select(chunk => OcrChunkAsync(client: client, chunkBytes: chunk, ct: ct))
		);

		return Join(separator: "", value: results);
	}

	private static async Task<string> OcrChunkAsync(
		ImageAnnotatorClient client,
		byte[] chunkBytes,
		CancellationToken ct
	)
	{
		AnnotateFileRequest request = new()
		{
			InputConfig = new InputConfig
			{
				Content = ByteString.CopyFrom(bytes: chunkBytes),
				MimeType = "application/pdf",
			},
			Features = { new Feature { Type = Feature.Types.Type.DocumentTextDetection } },
		};

		BatchAnnotateFilesRequest batchRequest = new() { Requests = { request } };
		BatchAnnotateFilesResponse response = await client.BatchAnnotateFilesAsync(
			request: batchRequest,
			cancellationToken: ct
		);

		StringBuilder sb = new(capacity: 4096);
		foreach (AnnotateImageResponse pageResponse in response.Responses[index: 0].Responses)
		{
			if (pageResponse.FullTextAnnotation is { })
				sb.AppendLine(value: pageResponse.FullTextAnnotation.Text);
		}
		return sb.ToString();
	}

	private static (int PageCount, List<byte[]> Chunks) SplitIntoChunks(byte[] pdfBytes)
	{
		using MemoryStream stream = new(buffer: pdfBytes);
		using PdfDocument doc = PdfDocument.Open(stream: stream);
		var totalPages = doc.NumberOfPages;

		var chunkCount = (totalPages + MaxPagesPerRequest - 1) / MaxPagesPerRequest;
		List<byte[]> chunks = [];
		for (var start = 1; start <= totalPages; start += MaxPagesPerRequest)
		{
			var end = Math.Min(start + MaxPagesPerRequest - 1, val2: totalPages);
			using PdfDocumentBuilder builder = new();
			for (var pageNum = start; pageNum <= end; pageNum++)
				builder.AddPage(document: doc, pageNumber: pageNum);
			chunks.Add(builder.Build());
		}
		return (totalPages, chunks);
	}
}
