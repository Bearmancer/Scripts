namespace CSharpScripts.Infrastructure;

public static class StringExtensions
{
	public static bool IsEqualTo(
		this string? value,
		string? other,
		StringComparison comparison = OrdinalIgnoreCase
	) => string.Equals(value, other, comparison);

	public static bool Has(
		this string? value,
		string substring,
		StringComparison comparison = OrdinalIgnoreCase
	) => value?.Contains(substring, comparison) ?? false;

	public static bool Starts(
		this string? value,
		string prefix,
		StringComparison comparison = OrdinalIgnoreCase
	) => value?.StartsWith(prefix, comparison) ?? false;

	public static bool Ends(
		this string? value,
		string suffix,
		StringComparison comparison = OrdinalIgnoreCase
	) => value?.EndsWith(suffix, comparison) ?? false;
}
