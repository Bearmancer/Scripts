namespace CSharpScripts.Services.Read.Ocr;

internal static partial class OcrTextCleanup
{
	internal static string CleanBlockText(string raw)
	{
		var deHyphenated = HyphenBreakRegex().Replace(raw, "$1$2");
		var reflowed = InlineNewlineRegex().Replace(deHyphenated, " ");
		return reflowed.Trim();
	}

	[GeneratedRegex(@"(\w)-\s*\n\s*(\w)")]
	private static partial Regex HyphenBreakRegex();

	[GeneratedRegex(@"\s*\n\s*")]
	private static partial Regex InlineNewlineRegex();
}
