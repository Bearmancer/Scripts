namespace CSharpScripts.Core;

internal static class StringExtensions
{
	public static bool EqualsIgnoreCase(this string? value, string? other) =>
		string.Equals(value, other, OrdinalIgnoreCase);

	public static bool EqualsExact(this string? value, string? other) =>
		string.Equals(value, other, Ordinal);

	public static bool ContainsIgnoreCase(this string? value, string substring) =>
		value?.Contains(substring, OrdinalIgnoreCase) ?? false;

	public static bool ContainsExact(this string? value, string substring) =>
		value?.Contains(substring, Ordinal) ?? false;

	public static bool StartsWithIgnoreCase(this string? value, string prefix) =>
		value?.StartsWith(prefix, OrdinalIgnoreCase) ?? false;

	public static bool StartsWithExact(this string? value, string prefix) =>
		value?.StartsWith(prefix, Ordinal) ?? false;

	public static bool EndsWithIgnoreCase(this string? value, string suffix) =>
		value?.EndsWith(suffix, OrdinalIgnoreCase) ?? false;

	public static bool EndsWithExact(this string? value, string suffix) =>
		value?.EndsWith(suffix, Ordinal) ?? false;
}
