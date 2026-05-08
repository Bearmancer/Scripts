using System.IO.Compression;
using System.Net;
using System.Text;
using CSharpScripts.Services.Read.Ocr;

namespace CSharpScripts.Services.Read;

internal sealed partial class LocalEpubExtractor(
	string filePath,
	AzureDocumentIntelligenceOptions? azureDocumentIntelligence = null,
	CancellationToken ct = default
)
{
	[GeneratedRegex(@"\d+")]
	private static partial Regex DigitGroups();

	public async Task<ArticleContent> ExtractAsync()
	{
		if (!File.Exists(filePath))
			throw new FileNotFoundException($"EPUB not found: {filePath}", filePath);

		ct.ThrowIfCancellationRequested();
		UI.Info($"Reading local EPUB: {filePath}");

		List<(string Name, byte[] Bytes)> images = ExtractImages(filePath);
		UI.Info($"Found {images.Count} page images.");
		var imageCount = images.Count;

		List<string> allBodyBlocks = [];

		for (var i = 0; i < imageCount; i++)
		{
			ct.ThrowIfCancellationRequested();
			var name = images[i].Name;
			var bytes = images[i].Bytes;
			var mimeType = name.GetImageMimeType();
			DocumentPageResult result = await OcrImageWithFallbackAsync(
				i + 1,
				imageCount,
				name,
				bytes,
				mimeType
			);
			UI.Ok(
				$"  → {result.BodyBlocks.Count} blocks, {result.SkippedHeadersFooters} headers/footers stripped"
			);

			allBodyBlocks.AddRange(result.BodyBlocks);
		}

		var title = Path.GetFileNameWithoutExtension(filePath);
		var bodyHtml = BuildBodyHtml(allBodyBlocks);

		return new ArticleContent
		{
			Title = title,
			BodyHtml = bodyHtml,
			SourceUrl = new Uri($"file:///{Path.GetFullPath(filePath).Replace('\\', '/')}"),
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
		if (AzureDocumentIntelligenceOcrProvider.IsConfigured(azureDocumentIntelligence))
		{
			try
			{
				UI.Info(
					$"[{pageNumber}/{totalPages}] Azure Document Intelligence: {name} ({bytes.Length:N0} bytes)..."
				);
				return await AzureDocumentIntelligenceOcrProvider
					.CreateConfigured(azureDocumentIntelligence)
					.OcrImageAsync(bytes, mimeType, ct);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Log.Warning(ex, "Azure Document Intelligence failed for {FileName}", name);
			}
		}

		UI.Info(
			$"[{pageNumber}/{totalPages}] Google Document AI: {name} ({bytes.Length:N0} bytes)..."
		);
		return await new DocumentAiOcrProvider(Secrets.GoogleDocumentAiProcessorName).OcrImageAsync(
			bytes,
			mimeType,
			ct
		);
	}

	private static List<(string Name, byte[] Bytes)> ExtractImages(string epubPath)
	{
		using ZipArchive archive = ZipFile.OpenRead(epubPath);
		return
		[
			.. Enumerable.Select(
				Enumerable.OrderBy(
					Enumerable.Where(archive.Entries, e => IsContentImage(e.FullName)),
					e => e.Name,
					Comparer<string>.Create(CompareNatural)
				),
				ReadEntry
			),
		];
	}

	private static (string Name, byte[] Bytes) ReadEntry(ZipArchiveEntry entry)
	{
		using Stream stream = entry.Open();
		using MemoryStream ms = new();
		stream.CopyTo(ms);
		return (entry.Name, ms.ToArray());
	}

	private static bool IsContentImage(string fullName)
	{
		var ext = Path.GetExtension(fullName);
		if (ext is not (".jpg" or ".jpeg" or ".png"))
			return false;

		return !fullName.ContainsIgnoreCase("cover");
	}

	private static int CompareNatural(string a, string b)
	{
		MatchCollection numsA = DigitGroups().Matches(a);
		MatchCollection numsB = DigitGroups().Matches(b);
		var countA = numsA.Count;
		var countB = numsB.Count;
		var limit = Math.Min(countA, countB);
		for (var i = 0; i < limit; i++)
		{
			var cmp = int.Parse(numsA[i].Value, CultureInfo.InvariantCulture)
				.CompareTo(int.Parse(numsB[i].Value, CultureInfo.InvariantCulture));
			if (cmp != 0)
				return cmp;
		}
		return countA.CompareTo(countB);
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
			if (IsNullOrEmpty(text))
				continue;

			var encoded = WebUtility.HtmlEncode(text);

			if (ArticleStructureDetector.IsSectionHeading(text))
			{
				sb.AppendLine($"<h2>{encoded}</h2>");
				firstBody = true;
			}
			else if (ArticleStructureDetector.IsFootnote(text))
				sb.AppendLine($"<p class=\"footnote\">{encoded}</p>");
			else
			{
				var cssClass = firstBody ? " class=\"first\"" : Empty;
				sb.AppendLine($"<p{cssClass}>{encoded}</p>");
				firstBody = false;
			}
		}

		return sb.ToString();
	}
}
