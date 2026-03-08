namespace CSharpScripts.Services.Read;

using System.Net.Http;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Playwright;
using SmartReader;
using static System.String;

internal sealed class StandardExtractor
{
	private const int NavigationTimeoutMs = 30_000;
	private const int NetworkIdleSettleMs = 10_000;
	private const int ContentReadyTimeoutMs = 20_000;
	private const int ImageDownloadTimeoutSeconds = 10;

	private static readonly HttpClient SharedHttpClient = new()
	{
		Timeout = TimeSpan.FromSeconds(ImageDownloadTimeoutSeconds),
	};

	// Polls the rendered DOM for visible article content after BPC runs.
	// Uses innerText (excludes hidden/removed paywall divs) and counts substantial
	// paragraphs, so the threshold is relative to what the page itself provides.
	private const string ContentReadyScript = """
		() => {
		    const candidates = [
		        'article', '[role="article"]', 'main',
		        '.article-body', '.story-body', '.post-content',
		        '.article-content', '.content-body', '.entry-content'
		    ];
		    for (const sel of candidates) {
		        const el = document.querySelector(sel);
		        if (!el) continue;
		        const text = el.innerText?.trim() ?? '';
		        const substantialParas = Array.from(el.querySelectorAll('p'))
		            .filter(p => (p.innerText?.trim().length ?? 0) > 50);
		        if (text.length > 1500 && substantialParas.length >= 3) return true;
		    }
		    return false;
		}
		""";
	private const string DefaultBpcRelativePath = "../bpc-ext";

	private const string ScrollScript = """
		async () => {
		    await new Promise(resolve => {
		        let totalHeight = 0;
		        const distance = 100;
		        const timer = setInterval(() => {
		            const scrollHeight = document.body.scrollHeight;
		            window.scrollBy(0, distance);
		            totalHeight += distance;
		            if (totalHeight >= scrollHeight) { clearInterval(timer); resolve(); }
		        }, 50);
		    });
		}
		""";

	private readonly Uri Url;
	private readonly string ExtensionPath;
	private readonly CancellationToken Ct;

	private IPage Page = null!;

	public StandardExtractor(Uri url, string? bpcPath = null, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(url);
		Url = url;
		ExtensionPath = Path.GetFullPath(bpcPath ?? DefaultBpcRelativePath);
		if (!Directory.Exists(ExtensionPath))
			throw new DirectoryNotFoundException($"BPC extension not found at {ExtensionPath}");
		Ct = ct;
	}

	public async Task<ArticleContent> ExtractAsync()
	{
		Ct.ThrowIfCancellationRequested();
		UI.Info("Launching Playwright with BPC extension...");

		await using BrowserSession session = await BrowserSession.CreateAsync(ExtensionPath, Ct);
		Page = await session.GetOrCreatePageAsync();

		await NavigateAndScrollAsync();
		var htmlContent = await Page.ContentAsync();

		Ct.ThrowIfCancellationRequested();
		return await ExtractContentAsync(htmlContent);
	}

	private async Task NavigateAndScrollAsync()
	{
		UI.Info($"Loading URL: {Url}");
		try
		{
			await Page.GotoAsync(
				Url.AbsoluteUri,
				new PageGotoOptions
				{
					WaitUntil = WaitUntilState.DOMContentLoaded,
					Timeout = NavigationTimeoutMs,
				}
			);
		}
		catch (TimeoutException ex)
		{
			UI.Warn($"Navigation timeout, proceeding: {ex.Message}");
		}

		// Let the network settle after DOM load without timing out on persistent connections
		try
		{
			await Page.WaitForLoadStateAsync(
				LoadState.NetworkIdle,
				new() { Timeout = NetworkIdleSettleMs }
			);
		}
		catch (TimeoutException)
		{
			UI.Info("Network still active — proceeding to scroll.");
		}

		UI.Info("Scrolling to load lazy images...");
		await Page.EvaluateAsync(ScrollScript);

		UI.Info("Waiting for article content readiness...");
		try
		{
			await Page.WaitForFunctionAsync(
				ContentReadyScript,
				options: new PageWaitForFunctionOptions
				{
					Timeout = ContentReadyTimeoutMs,
					PollingInterval = 500,
				}
			);
			UI.Info("Article content ready.");
		}
		catch (TimeoutException)
		{
			UI.Warn(
				"Content readiness check timed out \u2014 proceeding. SmartReader will make the final assessment."
			);
		}
	}

	private async Task<ArticleContent> ExtractContentAsync(string htmlContent)
	{
		UI.Info("Extracting content with SmartReader...");

		using Reader reader = new(Url.AbsoluteUri, htmlContent);
		Article article = await reader.GetArticleAsync();

		// Classify using SmartReader's own readability signals — no hardcoded thresholds.
		WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyArticleQuality(article);
		UI.Info(WebExtractionQualityAnalyzer.GetDiagnosticMessage(quality, article.Length));

		if (quality == WebExtractionQuality.Incomplete)
		{
			var reasons = new List<string>();
			if (!article.Completed)
				reasons.Add("extraction did not complete");
			if (!article.IsReadable)
				reasons.Add("SmartReader could not identify a readable article structure");
			if (article.Errors.Count > 0)
				reasons.Add(Join(";  ", article.Errors.Select(e => e.Message)));
			if (reasons.Count == 0)
				reasons.Add($"content too short ({article.Length} chars)");

			throw new InvalidOperationException(
				$"Page did not fully load \u2014 extraction incomplete. "
					+ Join(". ", reasons)
					+ "."
			);
		}

		var title = article.Title ?? "Extracted Article";

		HtmlParser parser = new();
		IHtmlDocument doc = await parser.ParseDocumentAsync(
			article.Content ?? "<h1>No content found</h1>"
		);

		HtmlCleanupHelper.RemoveUnwantedElements(doc);

		UI.Info("Downloading images...");
		Dictionary<string, byte[]> images = await DownloadImagesAsync(doc);
		HtmlCleanupHelper.UnwrapImageAnchors(doc);

		return new ArticleContent
		{
			Title = title,
			SourceUrl = Url,
			BodyHtml = doc.Body!.InnerHtml,
			Images = images,
		};
	}

	private async Task<Dictionary<string, byte[]>> DownloadImagesAsync(IHtmlDocument doc)
	{
		var imagesBag = new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>();
		var imgIndex = 0;

		// Collect all image requests first
		var imageRequests = new List<(IElement Img, Uri Uri, int Index)>();
		foreach (IElement img in doc.QuerySelectorAll("img").ToList())
		{
			var src = img.GetAttribute("src") ?? img.GetAttribute("data-src") ?? "";
			if (
				IsNullOrWhiteSpace(src)
				|| !Uri.TryCreate(Url, src, out Uri? fullUri)
				|| fullUri is null
			)
			{
				img.Remove();
				continue;
			}

			// Skip data URIs (embedded images) - they're already in the HTML
			if (fullUri.Scheme == "data")
			{
				continue;
			}

			imageRequests.Add((img, fullUri, imgIndex));
			imgIndex++;
		}

		// Download all images in parallel
		await Parallel.ForEachAsync(
			imageRequests,
			new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = Ct },
			async (request, innerCt) =>
			{
				(string Name, byte[] Data)? result = await TryDownloadImageAsync(
					SharedHttpClient,
					request.Uri,
					request.Index
				);
				if (result is not null)
				{
					imagesBag[result.Value.Name] = result.Value.Data;
					request.Img.SetAttribute("src", $"images/{result.Value.Name}");
				}
				else
				{
					// Mark for removal
					request.Img.SetAttribute("data-remove", "true");
				}
			}
		);

		// Remove images that failed to download
		foreach (IElement img in doc.QuerySelectorAll("img[data-remove='true']").ToList())
		{
			img.Remove();
		}

		return imagesBag.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value);
	}

	private static async Task<(string Name, byte[] Data)?> TryDownloadImageAsync(
		HttpClient httpClient,
		Uri uri,
		int index
	)
	{
		try
		{
			HttpResponseMessage response = await httpClient.GetAsync(uri);
			if (!response.IsSuccessStatusCode)
				return null;

			var ext = InferImageExtension(response.Content.Headers.ContentType?.MediaType);
			var name = $"img_{index}.{ext}";
			return (name, await response.Content.ReadAsByteArrayAsync());
		}
		catch (HttpRequestException ex)
		{
			UI.Warn($"Unable to download image {uri}: {ex.Message}");
			return null;
		}
		catch (TaskCanceledException ex)
		{
			UI.Warn($"Image download timed out {uri}: {ex.Message}");
			return null;
		}
	}

	private static string InferImageExtension(string? mediaType) =>
		mediaType switch
		{
			not null when mediaType.Contains("png") => "png",
			not null when mediaType.Contains("gif") => "gif",
			not null when mediaType.Contains("svg") => "svg",
			not null when mediaType.Contains("webp") => "webp",
			_ => "jpg",
		};
}
