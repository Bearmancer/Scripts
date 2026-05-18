using System.Text;

namespace CSharpScripts.Data;

internal static class TextNormalizer
{
	public static string ToStorageKey(string input)
	{
		var normalised = input.Normalize(NormalizationForm.FormD);
		var stripped = new string([.. normalised.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)]);
		return stripped.Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
	}
}
