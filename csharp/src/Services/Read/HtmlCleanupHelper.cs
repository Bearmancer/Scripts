namespace CSharpScripts.Services.Read;

using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

internal static partial class HtmlCleanupHelper
{
	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();

	[GeneratedRegex(@"\n{2,}")]
	private static partial Regex BlankLineRegex();

	/// <summary>
	/// Splits raw extracted text on blank lines and collapses internal whitespace.
	/// Shared by <see cref="LocalPdfExtractor"/> and <see cref="JstorExtractor"/>.
	/// </summary>
	internal static IEnumerable<string> SplitIntoParagraphs(string text) =>
		BlankLineRegex()
			.Split(text)
			.Where(p => !IsNullOrWhiteSpace(p))
			.Select(p => WhitespaceRegex().Replace(p.Trim(), " "));

	public static void RemoveUnwantedElements(IHtmlDocument doc)
	{
		// Remove standard unwanted elements
		foreach (
			IElement tag in doc.QuerySelectorAll(
				"script, style, iframe, form, button, nav, header, footer, aside"
			)
		)
			tag.Remove();

		// Remove advertisement and promotional content common across multiple publishers
		foreach (
			IElement tag in doc.QuerySelectorAll(
				"[class*='ad-'], [class*='advertisement'], [id*='ad-'], "
					+ "[class*='promo'], [class*='sponsor'], aside[class*='promo']"
			)
		)
			tag.Remove();
	}

	public static void UnwrapImageAnchors(IHtmlDocument doc)
	{
		foreach (
			IElement anchor in doc.QuerySelectorAll("a > img")
				.Select(x => x.ParentElement!)
				.Distinct()
				.ToList()
		)
		{
			INode[] children = [.. anchor.ChildNodes];
			foreach (INode child in children)
				anchor.Parent!.InsertBefore(child, anchor);
			anchor.Remove();
		}
	}

	/// <summary>
	/// Cleans an HTML string by removing unwanted structural tags and collapsing whitespace.
	/// Used for PDF-extracted content where the input is a raw HTML string, not a live DOM.
	/// </summary>
	public static string CleanHtml(string html)
	{
		if (IsNullOrWhiteSpace(html))
			return Empty;

		HtmlParser parser = new();
		IHtmlDocument document = parser.ParseDocument(html);

		RemoveUnwantedElements(document);

		IElement? body = document.Body ?? document.DocumentElement;
		var cleaned = body?.InnerHtml ?? Empty;

		// Collapse excessive whitespace
		cleaned = WhitespaceRegex().Replace(cleaned, " ");

		return cleaned.Trim();
	}
}
