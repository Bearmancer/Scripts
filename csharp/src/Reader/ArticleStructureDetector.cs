namespace CSharpScripts.Services.Read;

internal static partial class ArticleStructureDetector
{
	private const int MaxSectionHeadingLength = 100;
	private const int MaxFootnoteLength = 400;

	internal static bool IsSectionHeading(string text) =>
		text.Length < MaxSectionHeadingLength && SectionHeadingPattern().IsMatch(text);

	internal static bool IsFootnote(string text) =>
		text.Length < MaxFootnoteLength && FootnotePattern().IsMatch(text);

	[GeneratedRegex(@"^\d+\.\s+[A-Z]")]
	private static partial Regex SectionHeadingPattern();

	[GeneratedRegex(@"^\d+[\.\)]\s")]
	private static partial Regex FootnotePattern();
}
