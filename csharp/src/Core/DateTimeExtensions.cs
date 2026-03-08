namespace CSharpScripts.Core;

internal static class DateTimeExtensions
{
	internal static string ToDisplay(this DateTime utcDate) =>
		utcDate.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

	internal static string ToDisplayDate(this DateTime utcDate) =>
		utcDate.ToLocalTime().ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
}
