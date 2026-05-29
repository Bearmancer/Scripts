namespace CSharpScripts.Core;

internal static class StringExtensions
{
	extension(string? value)
	{
		internal bool EqualsIgnoreCase(
			string? other,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => string.Equals(a: value, b: other, comparisonType: comparisonType);

		internal bool ContainsIgnoreCase(
			string substring,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => value?.Contains(value: substring, comparisonType: comparisonType) ?? false;

		internal bool StartsWithIgnoreCase(
			string prefix,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => value?.StartsWith(value: prefix, comparisonType: comparisonType) ?? false;

		internal bool EndsWithIgnoreCase(
			string suffix,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => value?.EndsWith(value: suffix, comparisonType: comparisonType) ?? false;
	}

	extension(string value)
	{
		internal int IndexOfIgnoreCase(
			string substring,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => value.IndexOf(value: substring, comparisonType: comparisonType);

		internal string GetImageMimeType() =>
			value.EndsWithIgnoreCase(suffix: ".png") ? "image/png"
			: value.EndsWithIgnoreCase(suffix: ".gif") ? "image/gif"
			: value.EndsWithIgnoreCase(suffix: ".jpg") || value.EndsWithIgnoreCase(suffix: ".jpeg")
				? "image/jpeg"
			: value.EndsWithIgnoreCase(suffix: ".webp") ? "image/webp"
			: value.EndsWithIgnoreCase(suffix: ".svg") ? "image/svg+xml"
			: "application/octet-stream";

		internal string SanitizeFileName(int maxLength = int.MaxValue)
		{
			if (IsNullOrWhiteSpace(value: value))
				return "unnamed";

			ReadOnlySpan<char> invalid = Path.GetInvalidFileNameChars().AsSpan();
			var needsSanitizing = false;
			foreach (var c in value)
			{
				if (invalid.Contains(value: c))
				{
					needsSanitizing = true;
					break;
				}
			}

			var result = !needsSanitizing
				? value.Trim().TrimEnd(trimChar: '.')
				: Create(
						length: value.Length,
						state: value,
						static (span, src) =>
						{
							ReadOnlySpan<char> invalidChars = Path.GetInvalidFileNameChars()
								.AsSpan();
							for (var i = 0; i < src.Length; i++)
								span[index: i] = invalidChars.Contains(src[index: i])
									? '_'
									: src[index: i];
						}
					)
					.Trim()
					.TrimEnd(trimChar: '.');

			return result.Length <= maxLength ? result : result[..maxLength];
		}
	}

	extension(ReadOnlySpan<char> value)
	{
		internal bool ContainsIgnoreCase(
			ReadOnlySpan<char> substring,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => value.Contains(value: substring, comparisonType: comparisonType);

		internal int IndexOfIgnoreCase(
			ReadOnlySpan<char> substring,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => value.IndexOf(value: substring, comparisonType: comparisonType);
	}
}
