namespace CSharpScripts.Services.Read.Ocr;

internal static partial class OcrTextCleanup
{
	internal static string CleanBlockText(string raw)
	{
		var deHyphenated = HyphenBreakRegex().Replace(input: raw, replacement: "$1$2");
		var reflowed = InlineNewlineRegex().Replace(input: deHyphenated, replacement: " ");
		return reflowed.Trim();
	}

	[GeneratedRegex(pattern: @"(\w)-\s*\n\s*(\w)")]
	private static partial Regex HyphenBreakRegex();

	[GeneratedRegex(pattern: @"\s*\n\s*")]
	private static partial Regex InlineNewlineRegex();
}
