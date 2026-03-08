namespace CSharpScripts.Services.Read;

using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

internal static partial class EpubWriter
{
	[GeneratedRegex(@"[^\w]")]
	private static partial Regex NonWordRegex();

	private const string JstorCss = """
		body {
		    font-family: Georgia, 'Times New Roman', serif;
		    padding: 1.5em;
		    line-height: 1.8;
		    color: #222;
		    max-width: 40em;
		    margin: 0 auto;
		}
		h1 { font-size: 1.6em; margin-bottom: 0.2em; line-height: 1.3; }
		.meta { font-size: 0.9em; color: #555; margin-bottom: 1.5em; border-bottom: 1px solid #ccc; padding-bottom: 1em; }
		.meta .authors { font-style: italic; }
		.meta .journal { margin-top: 0.3em; }
		.abstract { font-size: 0.95em; background: #f7f7f7; border-left: 3px solid #888; padding: 0.8em 1em; margin-bottom: 1.5em; }
		.page-break { page-break-before: always; margin-top: 2em; padding-top: 1em; border-top: 1px dotted #bbb; }
		p { margin: 0.6em 0; text-align: justify; }
		img { max-width: 100%; height: auto; display: block; margin: 10px auto; }
		""";

	private const string StandardCss =
		"body{font-family:sans-serif;padding:10px}"
		+ "img{max-width:100%;height:auto;display:block;margin:10px auto}"
		+ "p{line-height:1.6}";

	private const string ContainerXml = """
		<?xml version="1.0"?>
		<container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
		  <rootfiles>
		    <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/>
		  </rootfiles>
		</container>
		""";

	public static void Write(ArticleContent content, string outputPath)
	{
		ArgumentNullException.ThrowIfNull(content);

		var bookId = $"urn:uuid:{Guid.NewGuid()}";
		var escapedTitle = WebUtility.HtmlEncode(content.Title);
		var authorString =
			content.Authors.Count > 0
				? Join(", ", content.Authors.Select(WebUtility.HtmlEncode))
				: "Unknown Author";
		var isJstor = content.OriginalPdf is not null;

		using FileStream fs = new(outputPath, FileMode.Create);
		using ZipArchive zip = new(fs, ZipArchiveMode.Create);

		WriteText(zip, "mimetype", "application/epub+zip", CompressionLevel.NoCompression);
		WriteText(zip, "META-INF/container.xml", ContainerXml);
		WriteText(zip, "OEBPS/style/nav.css", isJstor ? JstorCss : StandardCss);
		WriteText(zip, "OEBPS/nav.xhtml", BuildNavXhtml(escapedTitle));

		WriteAssets(zip, content);
		WriteText(
			zip,
			"OEBPS/chap_1.xhtml",
			BuildChapterXhtml(content, escapedTitle, authorString, isJstor)
		);
		WriteText(
			zip,
			"OEBPS/content.opf",
			BuildContentOpf(content, bookId, escapedTitle, authorString)
		);
		WriteText(zip, "OEBPS/toc.ncx", BuildTocNcx(bookId, escapedTitle));
	}

	public static string SanitizeFilename(string title) => NonWordRegex().Replace(title, "_");

	private static string BuildNavXhtml(string escapedTitle) =>
		$"""
			<?xml version="1.0" encoding="utf-8"?>
			<html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
			<head>
			  <title>Navigation</title>
			  <link href="style/nav.css" rel="stylesheet" type="text/css"/>
			</head>
			<body>
			  <nav epub:type="toc" id="toc">
			    <h1>Table of Contents</h1>
			    <ol>
			      <li><a href="chap_1.xhtml">{escapedTitle}</a></li>
			    </ol>
			  </nav>
			</body>
			</html>
			""";

	private static void WriteAssets(ZipArchive zip, ArticleContent content)
	{
		foreach (KeyValuePair<string, byte[]> kvp in content.Images)
			WriteBinary(zip, $"OEBPS/images/{kvp.Key}", kvp.Value);

		if (content.OriginalPdf is not null)
			WriteBinary(zip, "OEBPS/original.pdf", content.OriginalPdf);
	}

	private static string BuildChapterXhtml(
		ArticleContent content,
		string escapedTitle,
		string authorString,
		bool includeMetaHeader
	)
	{
		StringBuilder body = new();
		body.AppendLine($"<h1>{escapedTitle}</h1>");

		if (includeMetaHeader)
			AppendMetaHeader(body, content, authorString);

		body.AppendLine(content.BodyHtml);

		return $"""
			<?xml version="1.0" encoding="utf-8"?>
			<!DOCTYPE html>
			<html xmlns="http://www.w3.org/1999/xhtml">
			<head>
			  <title>{escapedTitle}</title>
			  <link href="style/nav.css" rel="stylesheet" type="text/css"/>
			</head>
			<body>
			  {body}
			</body>
			</html>
			""";
	}

	private static void AppendMetaHeader(
		StringBuilder body,
		ArticleContent content,
		string authorString
	)
	{
		body.AppendLine("""<div class="meta">""");

		if (content.Authors.Count > 0)
			body.AppendLine($"""  <div class="authors">{authorString}</div>""");

		if (!IsNullOrWhiteSpace(content.Journal))
		{
			var dateSuffix = IsNullOrWhiteSpace(content.PublicationDate)
				? ""
				: $" ({WebUtility.HtmlEncode(content.PublicationDate)})";
			body.AppendLine(
				$"""  <div class="journal">{WebUtility.HtmlEncode(content.Journal)}{dateSuffix}</div>"""
			);
		}

		if (!IsNullOrWhiteSpace(content.Doi))
			body.AppendLine(
				$"""  <div class="doi">DOI: {WebUtility.HtmlEncode(content.Doi)}</div>"""
			);

		body.AppendLine("</div>");

		if (!IsNullOrWhiteSpace(content.AbstractText))
			body.AppendLine(
				$"""<div class="abstract"><strong>Abstract:</strong> {WebUtility.HtmlEncode(content.AbstractText)}</div>"""
			);
	}

	private static string BuildContentOpf(
		ArticleContent content,
		string bookId,
		string escapedTitle,
		string authorString
	)
	{
		var imageManifest = Join(
			"\n    ",
			content.Images.Keys.Select(k =>
				$"""<item id="{k}" href="images/{k}" media-type="{ResolveMediaType(k)}" />"""
			)
		);

		var sourceTag =
			content.SourceUrl is null || IsNullOrWhiteSpace(content.SourceUrl.AbsoluteUri)
				? ""
				: $"<dc:source>{WebUtility.HtmlEncode(content.SourceUrl.AbsoluteUri)}</dc:source>";
		var publisherTag = IsNullOrWhiteSpace(content.Journal)
			? ""
			: $"<dc:publisher>{WebUtility.HtmlEncode(content.Journal)}</dc:publisher>";
		var dateTag = IsNullOrWhiteSpace(content.PublicationDate)
			? ""
			: $"<dc:date>{WebUtility.HtmlEncode(content.PublicationDate)}</dc:date>";

		return $"""
			<?xml version="1.0" encoding="utf-8"?>
			<package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="uuid_id">
			  <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
			    <dc:title>{escapedTitle}</dc:title>
			    <dc:language>en</dc:language>
			    <dc:identifier id="uuid_id">{bookId}</dc:identifier>
			    <dc:creator>{authorString}</dc:creator>
			    <meta property="dcterms:modified">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</meta>
			    {sourceTag}
			    {publisherTag}
			    {dateTag}
			  </metadata>
			  <manifest>
			    <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav"/>
			    <item id="chap_1" href="chap_1.xhtml" media-type="application/xhtml+xml"/>
			    <item id="css" href="style/nav.css" media-type="text/css"/>
			    <item id="toc" href="toc.ncx" media-type="application/x-dtbnc+xml"/>
			    {imageManifest}
			  </manifest>
			  <spine toc="toc">
			    <itemref idref="chap_1"/>
			  </spine>
			</package>
			""";
	}

	private static string BuildTocNcx(string bookId, string escapedTitle) =>
		$"""
			<?xml version="1.0" encoding="UTF-8"?>
			<ncx xmlns="http://www.daisy.org/z3986/2005/ncx/" version="2005-1">
			  <head>
			    <meta name="dtb:uid" content="{bookId}"/>
			    <meta name="dtb:depth" content="1"/>
			    <meta name="dtb:totalPageCount" content="0"/>
			    <meta name="dtb:maxPageNumber" content="0"/>
			  </head>
			  <docTitle><text>{escapedTitle}</text></docTitle>
			  <navMap>
			    <navPoint id="navPoint-1" playOrder="1">
			      <navLabel><text>{escapedTitle}</text></navLabel>
			      <content src="chap_1.xhtml"/>
			    </navPoint>
			  </navMap>
			</ncx>
			""";

	private static string ResolveMediaType(string filename)
	{
		var ext = Path.GetExtension(filename).ToLowerInvariant();
		return ext switch
		{
			".jpg" or ".jpeg" => "image/jpeg",
			".png" => "image/png",
			".gif" => "image/gif",
			".svg" => "image/svg+xml",
			".webp" => "image/webp",
			_ => "application/octet-stream",
		};
	}

	private static void WriteText(
		ZipArchive zip,
		string path,
		string content,
		CompressionLevel level = CompressionLevel.Optimal
	)
	{
		using Stream stream = zip.CreateEntry(path, level).Open();
		using StreamWriter writer = new(stream, new UTF8Encoding(false));
		writer.Write(content);
	}

	private static void WriteBinary(ZipArchive zip, string path, byte[] data)
	{
		using Stream stream = zip.CreateEntry(path, CompressionLevel.Optimal).Open();
		stream.Write(data);
	}
}
