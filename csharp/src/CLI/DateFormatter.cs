namespace Scripts.CLI;

internal static class DateFormatter
{
	internal static string FormatForCli(DateTimeOffset dt) => dt.ToString(format: "yyyy/MM/dd");

	internal static string FormatForCli(DateTimeOffset? dt) =>
		dt.HasValue ? FormatForCli(dt: dt.Value) : "";

	internal static string FormatForCli(DateOnly dt) => dt.ToString(format: "yyyy/MM/dd");

	internal static string FormatForCli(DateOnly? dt) =>
		dt.HasValue ? FormatForCli(dt: dt.Value) : "";
}
