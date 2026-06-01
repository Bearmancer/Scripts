using System.Text;

namespace Scripts.Data;

internal static class TextNormalizer
{
	public static string ToStorageKey(string input)
	{
		var normalised = input.Normalize(normalizationForm: NormalizationForm.FormD);
		var stripped = new string([
			.. normalised.Where(static c =>
				CharUnicodeInfo.GetUnicodeCategory(ch: c) != UnicodeCategory.NonSpacingMark
			),
		]);
		return stripped
			.Normalize(normalizationForm: NormalizationForm.FormC)
			.ToLowerInvariant()
			.Trim();
	}
}
