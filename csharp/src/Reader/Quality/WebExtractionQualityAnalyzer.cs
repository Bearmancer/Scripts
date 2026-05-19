using AngleSharp.Html.Dom;
using SmartReader;

namespace CSharpScripts.Services.Read;

internal static partial class WebExtractionQualityAnalyzer
{
	private static readonly string[] PaywallMarkers =
	[
		"paywall",
		"subscription-required",
		"subscribe-to-read"
	];

	private static readonly string[] BpcSuccessMarkers =
	[
		"bpc-paywall-removed",
		"bpc-unlocked",
		"data-bpc"
	];

	[GeneratedRegex(pattern: "<[^>]+>")]
	private static partial Regex HtmlTagRegex();

	[GeneratedRegex(pattern: @"\s+")]
	private static partial Regex WhitespaceRegex();

	public static bool HasPaywallMarker(IHtmlDocument doc)
	{
		foreach (var marker in PaywallMarkers)
		{
			if (doc.QuerySelector($"[class*=\"{marker}\"]") is { })
				return true;
			if (doc.QuerySelector($"[id*=\"{marker}\"]") is { })
				return true;
		}

		var bodyText = doc.Body?.TextContent ?? "";
		if (bodyText.ContainsIgnoreCase(substring: "subscribe to read more"))
			return true;
		if (bodyText.ContainsIgnoreCase(substring: "sign up for full access"))
			return true;
		if (bodyText.ContainsIgnoreCase(substring: "members only"))
			return true;

		return false;
	}

	public static bool HasBpcSuccessMarker(IHtmlDocument doc)
	{
		foreach (var marker in BpcSuccessMarkers)
		{
			if (doc.QuerySelector($"[class*=\"{marker}\"]") is { })
				return true;
			if (doc.QuerySelector(selectors: "[data-bpc]") is { })
				return true;
			if (doc.Body?.ClassList.Contains(token: marker) == true)
				return true;
		}

		return false;
	}

	public static WebExtractionQuality ClassifyQuality(IHtmlDocument doc)
	{
		if (HasBpcSuccessMarker(doc: doc))
			return WebExtractionQuality.Ready;

		if (HasPaywallMarker(doc: doc))
			return WebExtractionQuality.Incomplete;

		return WebExtractionQuality.Unknown;
	}

	public static WebExtractionQuality ClassifyArticleQuality(Article article) =>
		ClassifyArticleQuality(completed: article.Completed, isReadable: article.IsReadable, charLength: article.Length);

	public static WebExtractionQuality ClassifyArticleQuality(
		bool completed,
		bool isReadable,
		int charLength
	)
	{
		if (!completed || !isReadable)
			return WebExtractionQuality.Incomplete;

		if (charLength == 0)
			return WebExtractionQuality.Incomplete;

		return WebExtractionQuality.Ready;
	}

	public static int CountWords(string html)
	{
		var text = HtmlTagRegex().Replace(input: html, replacement: " ");
		text = WhitespaceRegex().Replace(input: text, replacement: " ").Trim();
		return IsNullOrEmpty(value: text) ? 0 : CountWordsCore(text.AsSpan());

		static int CountWordsCore(ReadOnlySpan<char> span)
		{
			var count = 0;
			var inWord = false;

			foreach (var c in span)
			{
				if (c == ' ')
					inWord = false;
				else if (!inWord)
				{
					count++;
					inWord = true;
				}
			}

			return count;
		}
	}

	public static string GetDiagnosticMessage(WebExtractionQuality quality, int? charCount = null)
	{
		var charInfo = charCount.HasValue ? $" ({charCount:N0} chars)" : "";
		return quality switch
		{
			WebExtractionQuality.Ready => $"Extraction quality: COMPLETE{charInfo}",
			WebExtractionQuality.Incomplete =>
				$"Extraction quality: INCOMPLETE{charInfo} \u2014 SmartReader could not identify a readable article",
			WebExtractionQuality.Unknown =>
				"Extraction quality: UNKNOWN (no quality markers detected)",
			_ => "Extraction quality: UNRECOGNIZED"
		};
	}
}
