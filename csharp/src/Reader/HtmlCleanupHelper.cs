using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace CSharpScripts.Services.Read;

internal static partial class HtmlCleanupHelper
{
	private static readonly HtmlParser SharedHtmlParser = new();

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();

	[GeneratedRegex(@"\n{2,}")]
	private static partial Regex BlankLineRegex();

	internal static List<string> SplitIntoParagraphs(string text)
	{
		var parts = BlankLineRegex().Split(text);
		List<string> result = [];
		for (var i = 0; i < parts.Length; i++)
		{
			if (!IsNullOrWhiteSpace(parts[i]))
				result.Add(WhitespaceRegex().Replace(parts[i].Trim(), " "));
		}
		return result;
	}

	public static void RemoveUnwantedElements(IHtmlDocument doc)
	{
		foreach (
			IElement tag in doc.QuerySelectorAll(
				"script, style, iframe, form, button, nav, header, footer, aside"
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
		IHtmlCollection<IElement> images = doc.QuerySelectorAll("a > img");
		HashSet<IElement> seen = [];
		for (var i = 0; i < images.Length; i++)
		{
			IElement anchor = images[i].ParentElement!;
			if (!seen.Add(anchor))
				continue;

			INode[] children = [.. anchor.ChildNodes];
			foreach (INode child in children)
				anchor.Parent!.InsertBefore(child, anchor);
			anchor.Remove();
		}
	}

	public static string CleanHtml(string html)
	{
		if (IsNullOrWhiteSpace(html))
			return Empty;

		IHtmlDocument document = SharedHtmlParser.ParseDocument(html);

		RemoveUnwantedElements(document);

		IElement? body = document.Body ?? document.DocumentElement;
		var cleaned = body?.InnerHtml ?? Empty;

		cleaned = WhitespaceRegex().Replace(cleaned, " ");

		return cleaned.Trim();
	}
}
