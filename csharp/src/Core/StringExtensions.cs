namespace CSharpScripts.Core;

internal static class StringExtensions
{
	extension(string? value)
	{
		internal bool EqualsIgnoreCase(
			string? other,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => string.Equals(value, other, comparisonType);

		internal bool ContainsIgnoreCase(
			string substring,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => value?.Contains(substring, comparisonType) ?? false;

		internal bool StartsWithIgnoreCase(
			string prefix,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => value?.StartsWith(prefix, comparisonType) ?? false;

		internal bool EndsWithIgnoreCase(
			string suffix,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => value?.EndsWith(suffix, comparisonType) ?? false;
	}

	extension(string value)
	{
		internal int IndexOfIgnoreCase(
			string substring,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => value.IndexOf(substring, comparisonType);

		internal string GetImageMimeType() =>
			value.EndsWithIgnoreCase(".png") ? "image/png"
			: value.EndsWithIgnoreCase(".gif") ? "image/gif"
			: value.EndsWithIgnoreCase(".jpg") || value.EndsWithIgnoreCase(".jpeg") ? "image/jpeg"
			: value.EndsWithIgnoreCase(".webp") ? "image/webp"
			: value.EndsWithIgnoreCase(".svg") ? "image/svg+xml"
			: "application/octet-stream";

		internal string SanitizeFileName(int maxLength = int.MaxValue)
		{
			if (IsNullOrWhiteSpace(value))
				return "unnamed";

			ReadOnlySpan<char> invalid = Path.GetInvalidFileNameChars().AsSpan();
			var needsSanitizing = false;
			foreach (var c in value)
			{
				if (invalid.Contains(c))
				{
					needsSanitizing = true;
					break;
				}
			}

			var result = !needsSanitizing
				? value.Trim().TrimEnd('.')
				: Create(
						value.Length,
						value,
						static (span, src) =>
						{
							ReadOnlySpan<char> invalidChars = Path.GetInvalidFileNameChars()
								.AsSpan();
							for (var i = 0; i < src.Length; i++)
								span[i] = invalidChars.Contains(src[i]) ? '_' : src[i];
						}
					)
					.Trim()
					.TrimEnd('.');

			return result.Length <= maxLength ? result : result[..maxLength];
		}
	}

	extension(ReadOnlySpan<char> value)
	{
		internal bool ContainsIgnoreCase(
			ReadOnlySpan<char> substring,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => value.Contains(substring, comparisonType);

		internal int IndexOfIgnoreCase(
			ReadOnlySpan<char> substring,
			StringComparison comparisonType = OrdinalIgnoreCase
		) => value.IndexOf(substring, comparisonType);
	}
}
