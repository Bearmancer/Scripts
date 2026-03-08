namespace CSharpScripts.Core;

internal static class SheetNameHelper
{
	public static string Sanitize(string name) =>
		name.Replace(":", " -")
			.Replace("/", "-")
			.Replace("\\", "-")
			.Replace("?", "")
			.Replace("*", "")
			.Replace("[", "(")
			.Replace("]", ")");

	public static string EscapeForFormula(string name) =>
		name.Contains('\'') || name.Contains(' ') || name.Contains('-')
			? $"'{name.Replace("'", "''")}'"
			: name;
}
