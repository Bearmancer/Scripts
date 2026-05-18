namespace CSharpScripts.CLI;

internal static class DateFormatter
{
	internal static string FormatForCli(System.DateTime dt) =>
		dt.ToString("yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture);

	internal static string FormatForCli(System.DateTime? dt) =>
		dt.HasValue ? FormatForCli(dt.Value) : string.Empty;

	internal static string FormatForCli(System.DateOnly dt) =>
		dt.ToString("yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture);

	internal static string FormatForCli(System.DateOnly? dt) =>
		dt.HasValue ? FormatForCli(dt.Value) : string.Empty;
}


