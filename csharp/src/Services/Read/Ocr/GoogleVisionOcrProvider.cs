namespace CSharpScripts.Services.Read.Ocr;

using Google.Cloud.Vision.V1;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

internal sealed class GoogleVisionOcrProvider : IOcrProvider
{
	private const int MaxPagesPerRequest = 5;

	public async Task<string> OcrPdfAsync(byte[] pdfBytes, CancellationToken ct = default)
	{
		UI.Info("OCR: Google Cloud Vision Document Text Detection...");
		ImageAnnotatorClient client = await ImageAnnotatorClient.CreateAsync(ct);

		List<byte[]> chunks = SplitIntoChunks(pdfBytes);
		var pageCount = GetPageCount(pdfBytes);
		UI.Info($"Vision: processing {chunks.Count} chunk(s) for {pageCount} pages.");

		var tasks = chunks.Select(chunk => OcrChunkAsync(client, chunk, ct)).ToList();
		var results = await Task.WhenAll(tasks);

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
				Content = Google.Protobuf.ByteString.CopyFrom(chunkBytes),
				MimeType = "application/pdf",
			},
			Features = { new Feature { Type = Feature.Types.Type.DocumentTextDetection } },
		};

		var batchRequest = new BatchAnnotateFilesRequest { Requests = { request } };
		BatchAnnotateFilesResponse response = await client.BatchAnnotateFilesAsync(
			batchRequest,
			cancellationToken: ct
		);

		var sb = new System.Text.StringBuilder();
		foreach (AnnotateImageResponse pageResponse in response.Responses[0].Responses)
		{
			if (pageResponse.FullTextAnnotation is not null)
				sb.AppendLine(pageResponse.FullTextAnnotation.Text);
		}
		return sb.ToString();
	}

	private static int GetPageCount(byte[] pdfBytes)
	{
		using MemoryStream stream = new(pdfBytes);
		using var doc = PdfDocument.Open(stream);
		return doc.NumberOfPages;
	}

	private static List<byte[]> SplitIntoChunks(byte[] pdfBytes)
	{
		using MemoryStream stream = new(pdfBytes);
		using var doc = PdfDocument.Open(stream);
		var totalPages = doc.NumberOfPages;

		List<byte[]> chunks = [];
		for (var start = 1; start <= totalPages; start += MaxPagesPerRequest)
		{
			var end = Math.Min(start + MaxPagesPerRequest - 1, totalPages);
			using PdfDocumentBuilder builder = new();
			for (var pageNum = start; pageNum <= end; pageNum++)
			{
				builder.AddPage(doc, pageNum);
			}
			chunks.Add(builder.Build());
		}
		return chunks;
	}
}
