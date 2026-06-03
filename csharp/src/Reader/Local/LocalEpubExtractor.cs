using System.IO.Compression;
using System.Net;
using System.Text;
using Scripts.Services.Read.Ocr;

namespace Scripts.Services.Read;

internal sealed partial class LocalEpubExtractor(
	string filePath,
	AzureDocumentIntelligenceOptions? azureDocumentIntelligence = null,
	CancellationToken ct = default
)
{
	[GeneratedRegex(pattern: @"\d+")]
	private static partial Regex DigitGroups();

	public async Task<ArticleContent> ExtractAsync()
	{
		if (!File.Exists(path: filePath))
			throw new FileNotFoundException($"EPUB not found: {filePath}", fileName: filePath);

		ct.ThrowIfCancellationRequested();
		Ui.Info($"Reading local EPUB: {filePath}");

		List<(string Name, byte[] Bytes)> images = ExtractImages(epubPath: filePath);
		Ui.Info($"Found {images.Count} page images.");
		var imageCount = images.Count;

		List<string> allBodyBlocks = [];

		for (var i = 0; i < imageCount; i++)
		{
			ct.ThrowIfCancellationRequested();
			var name = images[index: i].Name;
			var bytes = images[index: i].Bytes;
			var mimeType = name.GetImageMimeType();
			DocumentPageResult result = await OcrImageWithFallbackAsync(
				i + 1,
				totalPages: imageCount,
				name: name,
				bytes: bytes,
				mimeType: mimeType
			);
			Ui.Ok(
				$"  → {result.BodyBlocks.Count} blocks, {result.SkippedHeadersFooters} headers/footers stripped"
			);

			allBodyBlocks.AddRange(collection: result.BodyBlocks);
		}

		var title = Path.GetFileNameWithoutExtension(path: filePath);
		var bodyHtml = BuildBodyHtml(blocks: allBodyBlocks);

		return new ArticleContent
		{
			Title = title,
			BodyHtml = bodyHtml,
			SourceUrl = new Uri(
				$"file:///{Path.GetFullPath(path: filePath).Replace(oldChar: '\\', newChar: '/')}"
			),
		};
	}

	private async Task<DocumentPageResult> OcrImageWithFallbackAsync(
		int pageNumber,
		int totalPages,
		string name,
		byte[] bytes,
		string mimeType
	)
	{
		if (AzureDocumentIntelligenceOcrProvider.IsConfigured(options: azureDocumentIntelligence))
		{
			try
			{
				Ui.Info(
					$"[{pageNumber}/{totalPages}] Azure Document Intelligence: {name} ({bytes.Length:N0} bytes)..."
				);
				return await AzureDocumentIntelligenceOcrProvider
					.CreateConfigured(options: azureDocumentIntelligence)
					.OcrImageAsync(imageBytes: bytes, mimeType: mimeType, ct: ct);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Log.Error(ex, "Azure Document Intelligence failed for {FileName}", name);
			}
		}

		Ui.Info(
			$"[{pageNumber}/{totalPages}] Google Document AI: {name} ({bytes.Length:N0} bytes)..."
		);
		return await new DocumentAiOcrProvider(
			processorName: Secrets.GoogleDocumentAiProcessorName
		).OcrImageAsync(imageBytes: bytes, mimeType: mimeType, ct: ct);
	}

	private static List<(string Name, byte[] Bytes)> ExtractImages(string epubPath)
	{
		using ZipArchive archive = ZipFile.OpenRead(archiveFileName: epubPath);
		return
		[
			.. archive
				.Entries.Where(e => IsContentImage(fullName: e.FullName))
				.OrderBy(e => e.Name, Comparer<string>.Create(comparison: CompareNatural))
				.Select(selector: ReadEntry),
		];
	}

	private static (string Name, byte[] Bytes) ReadEntry(ZipArchiveEntry entry)
	{
		using Stream stream = entry.Open();
		using MemoryStream ms = new();
		stream.CopyTo(destination: ms);
		return (entry.Name, ms.ToArray());
	}

	private static bool IsContentImage(string fullName)
	{
		var ext = Path.GetExtension(path: fullName);
		if (ext is not (".jpg" or ".jpeg" or ".png"))
			return false;

		return !fullName.ContainsIgnoreCase(substring: "cover");
	}

	private static int CompareNatural(string a, string b)
	{
		MatchCollection numsA = DigitGroups().Matches(input: a);
		MatchCollection numsB = DigitGroups().Matches(input: b);
		var countA = numsA.Count;
		var countB = numsB.Count;
		var limit = Math.Min(val1: countA, val2: countB);
		for (var i = 0; i < limit; i++)
		{
			var cmp = int.Parse(s: numsA[i: i].Value, provider: CultureInfo.InvariantCulture)
				.CompareTo(int.Parse(s: numsB[i: i].Value, provider: CultureInfo.InvariantCulture));
			if (cmp != 0)
				return cmp;
		}
		return countA.CompareTo(value: countB);
	}

	private static string BuildBodyHtml(List<string> blocks)
	{
		if (blocks.Count == 0)
			return "<p><em>No content extracted.</em></p>";

		StringBuilder sb = new(blocks.Count * 200);
		var firstBody = true;

		foreach (var block in blocks)
		{
			var text = block.Trim();
			if (IsNullOrEmpty(value: text))
				continue;

			var encoded = WebUtility.HtmlEncode(value: text);

			if (ArticleStructureDetector.IsSectionHeading(text: text))
			{
				sb.AppendLine($"<h2>{encoded}</h2>");
				firstBody = true;
			}
			else if (ArticleStructureDetector.IsFootnote(text: text))
				sb.AppendLine($"<p class=\"footnote\">{encoded}</p>");
			else
			{
				var cssClass = firstBody ? " class=\"first\"" : "";
				sb.AppendLine($"<p{cssClass}>{encoded}</p>");
				firstBody = false;
			}
		}

		return sb.ToString();
	}
}
