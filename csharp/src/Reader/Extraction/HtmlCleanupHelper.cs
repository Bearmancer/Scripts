using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace CSharpScripts.Services.Read;

internal static partial class HtmlCleanupHelper
{
	private static readonly HtmlParser SharedHtmlParser = new();

	[GeneratedRegex(pattern: @"\s+")]
	private static partial Regex WhitespaceRegex();

	[GeneratedRegex(pattern: @"\n{2,}")]
	private static partial Regex BlankLineRegex();

	internal static List<string> SplitIntoParagraphs(string text)
	{
		var parts = BlankLineRegex().Split(input: text);
		List<string> result = [];
		foreach (var part in parts)
		{
			if (!IsNullOrWhiteSpace(part))
				result.Add(WhitespaceRegex().Replace(part.Trim(), replacement: " "));
		}
		return result;
	}

	public static void RemoveUnwantedElements(IHtmlDocument doc)
	{
		foreach (
			IElement tag in doc.QuerySelectorAll(
				selectors: "script, style, iframe, form, button, nav, header, footer, aside"
			)
		)
			tag.Remove();

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
		IHtmlCollection<IElement> images = doc.QuerySelectorAll(selectors: "a > img");
		HashSet<IElement> seen = [];
		foreach (var image in images)
		{
			IElement anchor = image.ParentElement!;
			if (!seen.Add(item: anchor))
				continue;

			INode[] children = [.. anchor.ChildNodes];
			foreach (INode child in children)
				anchor.Parent!.InsertBefore(newElement: child, referenceElement: anchor);
			anchor.Remove();
		}
	}

	public static string CleanHtml(string html)
	{
		if (IsNullOrWhiteSpace(value: html))
			return "";

		IHtmlDocument document = SharedHtmlParser.ParseDocument(source: html);

		RemoveUnwantedElements(doc: document);

		IElement? body = document.Body ?? document.DocumentElement;
		var cleaned = body?.InnerHtml ?? "";

		cleaned = WhitespaceRegex().Replace(input: cleaned, replacement: " ");

		return cleaned.Trim();
	}
}
