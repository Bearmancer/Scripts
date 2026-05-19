using System.Net;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Playwright;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace CSharpScripts.Services.Read;

using NetCookie = System.Net.Cookie;

internal sealed class JstorExtractor
{
	private const int NavigationTimeoutMs = 45_000;
	private const int DownloadTimeoutMs = 60_000;
	private const int PostNavigationDelayMs = 3000;
	private const int PostActionDelayMs = 2000;
	private const int ShortDelayMs = 1000;

	private static readonly Regex JidPattern = new(
		pattern: @"jstor\.org/stable/([^\s?#]+)",
		options: RegexOptions.Compiled
	);

	private static readonly HtmlParser Parser = new();

	private readonly CancellationToken Ct;

	private readonly string Jid;
	private readonly string LandingUrl;
	private string AbstractText = "";
	private List<string> Authors = [];
	private IBrowserContext BrowserContext = null!;
	private string Doi = "";
	private string Journal = "";

	private IPage Page = null!;
	private string PublicationDate = "";

	private string Title = "";

	public JstorExtractor(Uri url, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(argument: url);
		Match match = JidPattern.Match(input: url.AbsoluteUri);
		if (!match.Success)
		{
			throw new ArgumentException(
				$"Cannot extract JSTOR article ID from URL: {url}",
				nameof(url)
			);
		}
		Jid = match.Groups[groupnum: 1].Value;
		LandingUrl = $"https://www.jstor.org/stable/{Jid}";
		Ct = ct;
	}

	public async Task<ArticleContent> ExtractAsync()
	{
		Ct.ThrowIfCancellationRequested();
		Ui.Info($"JSTOR Article ID: {Jid}");

		await using BrowserSession session = await BrowserSession.CreateAsync(
			cancellationToken: Ct
		);
		Page = await session.GetOrCreatePageAsync();
		BrowserContext = session.Browser;

		await NavigateAndDismissAsync();
		await ExtractMetadataAsync();
		PrintMetadata();

		Ct.ThrowIfCancellationRequested();

		var pdfBytes = await DownloadPdfAsync();
		List<string> pdfPages = ExtractPdfPages(pdfBytes: pdfBytes);

		return new ArticleContent
		{
			Title = Title,
			Authors = Authors,
			Journal = Journal,
			PublicationDate = PublicationDate,
			Doi = Doi,
			AbstractText = AbstractText,
			SourceUrl = new Uri(uriString: LandingUrl),
			BodyHtml = BuildBodyHtml(pdfPages: pdfPages),
			OriginalPdf = pdfBytes
		};
	}

	private async Task NavigateAndDismissAsync()
	{
		Ui.Info($"Loading landing page: {LandingUrl}");
		try
		{
			await Page.GotoAsync(
				url: LandingUrl,
				new PageGotoOptions
				{
					WaitUntil = WaitUntilState.NetworkIdle,
					Timeout = NavigationTimeoutMs
				}
			);
		}
		catch (TimeoutException ex)
		{
			Ui.Warn($"Navigation timeout, proceeding: {ex.Message}");
		}

		await Page.WaitForTimeoutAsync(timeout: PostNavigationDelayMs);

		await TryClickAsync(
			selector: "button:has-text('Accept'), button:has-text('Agree'), [data-qa='accept-terms']",
			delayMs: PostActionDelayMs
		);
		await TryClickAsync(
			selector: "button:has-text('Accept Cookies'), button:has-text('Accept All'), #onetrust-accept-btn-handler",
			delayMs: ShortDelayMs
		);
	}

	private async Task TryClickAsync(string selector, int delayMs)
	{
		try
		{
			ILocator locator = Page.Locator(selector: selector);
			if (await locator.CountAsync() > 0)
			{
				await locator.First.ClickAsync();
				await Page.WaitForTimeoutAsync(timeout: delayMs);
			}
		}
		catch (PlaywrightException ex)
		{
			Log.Debug(
				messageTemplate: "Optional JSTOR click target unavailable for selector {Selector}: {Message}",
				selector,
				ex.Message
			);
		}
	}

	private async Task ExtractMetadataAsync()
	{
		Ui.Info(message: "Extracting metadata...");
		var html = await Page.ContentAsync();
		IHtmlDocument doc = await Parser.ParseDocumentAsync(source: html);

		Title =
			MetaContent(doc: doc, name: "citation_title")
			?? MetaContent(doc: doc, name: "og:title")
			?? doc.QuerySelector(selectors: "h1")?.TextContent?.Trim()
			?? "JSTOR Article";

		Authors =
		[
			.. doc.QuerySelectorAll(selectors: "meta[name='citation_author']").Select(m => m.GetAttribute(name: "content") ?? ""
			).Where(a => !IsNullOrWhiteSpace(value: a)
			)
		];

		Journal = MetaContent(doc: doc, name: "citation_journal_title") ?? "";
		PublicationDate = MetaContent(doc: doc, name: "citation_publication_date") ?? "";
		Doi = MetaContent(doc: doc, name: "citation_doi") ?? "";
		AbstractText =
			doc.QuerySelector(selectors: ".abstract, [data-qa='abstract'], .article-paragraph-abstract")
				?.TextContent?.Trim()
			?? "";
	}

	private static string? MetaContent(IHtmlDocument doc, string name)
	{
		IElement? meta =
			doc.QuerySelector($"meta[name=\"{name}\"]")
			?? doc.QuerySelector($"meta[property=\"{name}\"]");
		var content = meta?.GetAttribute(name: "content");
		return IsNullOrWhiteSpace(value: content) ? null : content.Trim();
	}

	private void PrintMetadata()
	{
		Ui.Info($"Title   : {Title}");
		Ui.Info($"Authors : {(Authors.Count > 0 ? Join(separator: "; ", values: Authors) : "Unknown")}");
		Ui.Info($"Journal : {Journal}");
		Ui.Info($"Date    : {PublicationDate}");
		Ui.Info($"DOI     : {Doi}");
	}

	private async Task<byte[]> DownloadPdfAsync()
	{
		var pdfUrl = $"https://www.jstor.org/stable/pdfplus/{Jid}.pdf?acceptTC=true";
		Ui.Info($"Downloading PDF from: {pdfUrl}");

		try
		{
			Task<IDownload> downloadTask = Page.WaitForDownloadAsync(
				new PageWaitForDownloadOptions { Timeout = DownloadTimeoutMs }
			);
			await Page.GotoAsync(url: pdfUrl, new PageGotoOptions { Timeout = DownloadTimeoutMs });
			IDownload download = await downloadTask;

			var tempPath =
				await download.PathAsync()
				?? throw new InvalidOperationException(
					message: "Download completed but no file path was returned."
				);
			var pdfBytes = await File.ReadAllBytesAsync(path: tempPath);
			Ui.Info($"PDF downloaded: {pdfBytes.Length:N0} bytes");
			return pdfBytes;
		}
		catch (Exception ex) when (ex is not InvalidOperationException)
		{
			Log.Warning(
				messageTemplate: ex,
				"Playwright download failed ({Message}), trying HTTP fallback...",
				ex.Message
			);
			Ui.Warn($"Playwright download failed ({ex.Message}), trying HTTP fallback...");
			return await DownloadPdfViaHttpAsync(pdfUrl: pdfUrl);
		}
	}

	private async Task<byte[]> DownloadPdfViaHttpAsync(string pdfUrl)
	{
		IReadOnlyList<BrowserContextCookiesResult> cookies = await BrowserContext.CookiesAsync([
			"https://www.jstor.org"
		]);
		using HttpClientHandler handler = new()
		{
			CheckCertificateRevocationList = true,
			CookieContainer = new CookieContainer()
		};

		foreach (BrowserContextCookiesResult cookie in cookies)
		{
			handler.CookieContainer.Add(
				new NetCookie(name: cookie.Name, value: cookie.Value, path: cookie.Path, domain: cookie.Domain)
			);
		}

		using HttpClient httpClient = new(handler: handler)
		{
			Timeout = TimeSpan.FromMilliseconds(milliseconds: DownloadTimeoutMs)
		};
		httpClient.DefaultRequestHeaders.Add(
			name: "User-Agent",
			value: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
		);
		httpClient.DefaultRequestHeaders.Add(name: "Referer", value: LandingUrl);

		HttpResponseMessage response = await httpClient.GetAsync(new Uri(uriString: pdfUrl), cancellationToken: Ct);
		response.EnsureSuccessStatusCode();

		var pdfBytes = await response.Content.ReadAsByteArrayAsync();
		Ui.Info($"PDF downloaded via HTTP: {pdfBytes.Length:N0} bytes");
		return pdfBytes;
	}

	private static List<string> ExtractPdfPages(byte[] pdfBytes)
	{
		Ui.Info(message: "Extracting text from PDF...");
		List<string> pages = [];
		using MemoryStream stream = new(buffer: pdfBytes);
		using PdfDocument document = PdfDocument.Open(stream: stream);

		foreach (Page page in document.GetPages())
		{
			var text = page.Text;
			if (!IsNullOrWhiteSpace(value: text))
				pages.Add(text.Trim());
		}

		Ui.Info($"Extracted text from {pages.Count} page(s).");
		return pages;
	}

	private static string BuildBodyHtml(List<string> pdfPages)
	{
		StringBuilder body = new(pdfPages.Count * 2000);
		for (var i = 0; i < pdfPages.Count; i++)
		{
			if (i > 0)
				body.AppendLine(value: """<div class="page-break"></div>""");
			foreach (var para in HtmlCleanupHelper.SplitIntoParagraphs(pdfPages[index: i]))
				body.AppendLine($"<p>{WebUtility.HtmlEncode(value: para)}</p>");
		}
		return body.ToString();
	}
}
