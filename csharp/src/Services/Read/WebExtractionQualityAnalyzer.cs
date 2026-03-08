namespace CSharpScripts.Services.Read;

using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;
using SmartReader;

internal static partial class WebExtractionQualityAnalyzer
{
	[GeneratedRegex("<[^>]+>")]
	private static partial Regex HtmlTagRegex();

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();

	// Markers must match blocking gate elements, not content-type labels.
	// Avoid generic substrings like "premium-content" or "members-only" that
	// remain as styling classes even after BPC successfully bypasses the paywall.
	private static readonly string[] PaywallMarkers =
	[
		"paywall",
		"subscription-required",
		"subscribe-to-read",
	];

	private static readonly string[] BpcSuccessMarkers =
	[
		"bpc-paywall-removed",
		"bpc-unlocked",
		"data-bpc",
	];

	public static bool HasPaywallMarker(IHtmlDocument doc)
	{
		// Check for paywall markers in class attributes
		foreach (var marker in PaywallMarkers)
		{
			if (doc.QuerySelector($"[class*=\"{marker}\"]") is not null)
				return true;
			if (doc.QuerySelector($"[id*=\"{marker}\"]") is not null)
				return true;
		}

		// Check for common paywall text patterns
		var bodyText = doc.Body?.TextContent?.ToLowerInvariant() ?? "";
		if (bodyText.Contains("subscribe to read more"))
			return true;
		if (bodyText.Contains("sign up for full access"))
			return true;
		if (bodyText.Contains("members only"))
			return true;

		return false;
	}

	public static bool HasBpcSuccessMarker(IHtmlDocument doc)
	{
		// Check for BPC success markers
		foreach (var marker in BpcSuccessMarkers)
		{
			if (doc.QuerySelector($"[class*=\"{marker}\"]") is not null)
				return true;
			if (doc.QuerySelector($"[data-bpc]") is not null)
				return true;
			if (doc.Body?.ClassList.Contains(marker) == true)
				return true;
		}

		return false;
	}

	public static WebExtractionQuality ClassifyQuality(IHtmlDocument doc)
	{
		if (HasBpcSuccessMarker(doc))
			return WebExtractionQuality.Ready;

		if (HasPaywallMarker(doc))
			return WebExtractionQuality.Incomplete;

		return WebExtractionQuality.Unknown;
	}

	/// <summary>
	/// Classifies extraction quality using SmartReader's own readability signals.
	/// <see cref="Article.IsReadable"/> is computed by SmartReader's content-scoring algorithm
	/// with no hardcoded external threshold — it adapts to each article's structure.
	/// </summary>
	public static WebExtractionQuality ClassifyArticleQuality(Article article) =>
		ClassifyArticleQuality(article.Completed, article.IsReadable, article.Length);

	/// <summary>
	/// Primitive overload used directly in unit tests without requiring a real Article instance.
	/// </summary>
	public static WebExtractionQuality ClassifyArticleQuality(
		bool completed,
		bool isReadable,
		int charLength
	)
	{
		if (!completed || !isReadable)
			return WebExtractionQuality.Incomplete;

		// IsReadable already encodes SmartReader's internal char threshold, but guard against
		// edge cases where SmartReader marks IsReadable on near-empty content.
		if (charLength == 0)
			return WebExtractionQuality.Incomplete;

		return WebExtractionQuality.Ready;
	}

	public static int CountWords(string html)
	{
		// Strip tags, collapse whitespace, count space-separated tokens
		var text = HtmlTagRegex().Replace(html, " ");
		text = WhitespaceRegex().Replace(text, " ").Trim();
		return IsNullOrEmpty(text) ? 0 : text.Split(' ').Length;
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
			_ => "Extraction quality: UNRECOGNIZED",
		};
	}
}
