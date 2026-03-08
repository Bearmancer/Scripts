namespace CSharpScripts.Services.Read;

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Playwright;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using NetCookie = System.Net.Cookie;

internal sealed class JstorExtractor
{
	private static readonly Regex JidPattern = new(
		@"jstor\.org/stable/([^\s?#]+)",
		RegexOptions.Compiled
	);

	private const int NavigationTimeoutMs = 45_000;
	private const int DownloadTimeoutMs = 60_000;
	private const int PostNavigationDelayMs = 3000;
	private const int PostActionDelayMs = 2000;
	private const int ShortDelayMs = 1000;

	private readonly string Jid;
	private readonly string LandingUrl;
	private readonly CancellationToken Ct;

	private IPage Page = null!;
	private IBrowserContext BrowserContext = null!;

	private string Title = "";
	private List<string> Authors = [];
	private string Journal = "";
	private string PublicationDate = "";
	private string Doi = "";
	private string AbstractText = "";

	public JstorExtractor(Uri url, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(url);
		Match match = JidPattern.Match(url.AbsoluteUri);
		if (!match.Success)
			throw new ArgumentException(
				$"Cannot extract JSTOR article ID from URL: {url}",
				nameof(url)
			);
		Jid = match.Groups[1].Value;
		LandingUrl = $"https://www.jstor.org/stable/{Jid}";
		Ct = ct;
	}

	public async Task<ArticleContent> ExtractAsync()
	{
		Ct.ThrowIfCancellationRequested();
		UI.Info($"JSTOR Article ID: {Jid}");

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
		List<string> pdfPages = ExtractPdfPages(pdfBytes);

		return new ArticleContent
		{
			Title = Title,
			Authors = Authors,
			Journal = Journal,
			PublicationDate = PublicationDate,
			Doi = Doi,
			AbstractText = AbstractText,
			SourceUrl = new Uri(LandingUrl),
			BodyHtml = BuildBodyHtml(pdfPages),
			OriginalPdf = pdfBytes,
		};
	}

	private async Task NavigateAndDismissAsync()
	{
		UI.Info($"Loading landing page: {LandingUrl}");
		try
		{
			await Page.GotoAsync(
				LandingUrl,
				new PageGotoOptions
				{
					WaitUntil = WaitUntilState.NetworkIdle,
					Timeout = NavigationTimeoutMs,
				}
			);
		}
		catch (TimeoutException ex)
		{
			UI.Warn($"Navigation timeout, proceeding: {ex.Message}");
		}

		await Page.WaitForTimeoutAsync(PostNavigationDelayMs);

		await TryClickAsync(
			"button:has-text('Accept'), button:has-text('Agree'), [data-qa='accept-terms']",
			PostActionDelayMs
		);
		await TryClickAsync(
			"button:has-text('Accept Cookies'), button:has-text('Accept All'), #onetrust-accept-btn-handler",
			ShortDelayMs
		);
	}

	private async Task TryClickAsync(string selector, int delayMs)
	{
		try
		{
			ILocator locator = Page.Locator(selector);
			if (await locator.CountAsync() > 0)
			{
				await locator.First.ClickAsync();
				await Page.WaitForTimeoutAsync(delayMs);
			}
		}
		catch (PlaywrightException) { }
	}

	private async Task ExtractMetadataAsync()
	{
		UI.Info("Extracting metadata...");
		var html = await Page.ContentAsync();
		HtmlParser parser = new();
		IHtmlDocument doc = await parser.ParseDocumentAsync(html);

		Title =
			MetaContent(doc, "citation_title")
			?? MetaContent(doc, "og:title")
			?? doc.QuerySelector("h1")?.TextContent?.Trim()
			?? "JSTOR Article";

		Authors =
		[
			.. doc.QuerySelectorAll("meta[name='citation_author']")
				.Select(m => m.GetAttribute("content") ?? "")
				.Where(a => !IsNullOrWhiteSpace(a)),
		];

		Journal = MetaContent(doc, "citation_journal_title") ?? "";
		PublicationDate = MetaContent(doc, "citation_publication_date") ?? "";
		Doi = MetaContent(doc, "citation_doi") ?? "";
		AbstractText =
			doc.QuerySelector(".abstract, [data-qa='abstract'], .article-paragraph-abstract")
				?.TextContent?.Trim()
			?? "";
	}

	private static string? MetaContent(IHtmlDocument doc, string name)
	{
		IElement? meta =
			doc.QuerySelector($"meta[name=\"{name}\"]")
			?? doc.QuerySelector($"meta[property=\"{name}\"]");
		var content = meta?.GetAttribute("content");
		return IsNullOrWhiteSpace(content) ? null : content.Trim();
	}

	private void PrintMetadata()
	{
		UI.Info($"Title   : {Title}");
		UI.Info($"Authors : {(Authors.Count > 0 ? Join("; ", Authors) : "Unknown")}");
		UI.Info($"Journal : {Journal}");
		UI.Info($"Date    : {PublicationDate}");
		UI.Info($"DOI     : {Doi}");
	}

	private async Task<byte[]> DownloadPdfAsync()
	{
		var pdfUrl = $"https://www.jstor.org/stable/pdfplus/{Jid}.pdf?acceptTC=true";
		UI.Info($"Downloading PDF from: {pdfUrl}");

		try
		{
			Task<IDownload> downloadTask = Page.WaitForDownloadAsync(
				new PageWaitForDownloadOptions { Timeout = DownloadTimeoutMs }
			);
			await Page.GotoAsync(pdfUrl, new PageGotoOptions { Timeout = DownloadTimeoutMs });
			IDownload download = await downloadTask;

			var tempPath =
				await download.PathAsync()
				?? throw new InvalidOperationException(
					"Download completed but no file path was returned."
				);
			var pdfBytes = await File.ReadAllBytesAsync(tempPath);
			UI.Info($"PDF downloaded: {pdfBytes.Length:N0} bytes");
			return pdfBytes;
		}
		catch (Exception ex) when (ex is not InvalidOperationException)
		{
			UI.Warn($"Playwright download failed ({ex.Message}), trying HTTP fallback...");
			return await DownloadPdfViaHttpAsync(pdfUrl);
		}
	}

	private async Task<byte[]> DownloadPdfViaHttpAsync(string pdfUrl)
	{
		IReadOnlyList<BrowserContextCookiesResult> cookies = await BrowserContext.CookiesAsync([
			"https://www.jstor.org",
		]);
		using HttpClientHandler handler = new()
		{
			CheckCertificateRevocationList = true,
			CookieContainer = new CookieContainer(),
		};

		foreach (BrowserContextCookiesResult cookie in cookies)
			handler.CookieContainer.Add(
				new NetCookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain)
			);

		using HttpClient httpClient = new(handler)
		{
			Timeout = TimeSpan.FromMilliseconds(DownloadTimeoutMs),
		};
		httpClient.DefaultRequestHeaders.Add(
			"User-Agent",
			"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
		);
		httpClient.DefaultRequestHeaders.Add("Referer", LandingUrl);

		HttpResponseMessage response = await httpClient.GetAsync(new Uri(pdfUrl));
		response.EnsureSuccessStatusCode();

		var pdfBytes = await response.Content.ReadAsByteArrayAsync();
		UI.Info($"PDF downloaded via HTTP: {pdfBytes.Length:N0} bytes");
		return pdfBytes;
	}

	private static List<string> ExtractPdfPages(byte[] pdfBytes)
	{
		UI.Info("Extracting text from PDF...");
		List<string> pages = [];
		using MemoryStream stream = new(pdfBytes);
		using var document = PdfDocument.Open(stream);

		foreach (Page page in document.GetPages())
		{
			var text = page.Text;
			if (!IsNullOrWhiteSpace(text))
				pages.Add(text.Trim());
		}

		UI.Info($"Extracted text from {pages.Count} page(s).");
		return pages;
	}

	private static string BuildBodyHtml(List<string> pdfPages)
	{
		StringBuilder body = new();
		for (var i = 0; i < pdfPages.Count; i++)
		{
			if (i > 0)
				body.AppendLine("""<div class="page-break"></div>""");
			foreach (var para in HtmlCleanupHelper.SplitIntoParagraphs(pdfPages[i]))
				body.AppendLine($"<p>{WebUtility.HtmlEncode(para)}</p>");
		}
		return body.ToString();
	}
}
