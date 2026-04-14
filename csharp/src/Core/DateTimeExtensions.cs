namespace CSharpScripts.Core;

internal static class DateTimeExtensions
{
	extension(DateTime utcDate)
	{
		internal string ToDisplay() =>
			utcDate
				.ToLocalTime()
				.ToString(format: "yyyy/MM/dd HH:mm:ss", provider: CultureInfo.InvariantCulture);

		internal string ToDisplayDate() =>
			utcDate
				.ToLocalTime()
				.ToString(format: "yyyy/MM/dd", provider: CultureInfo.InvariantCulture);
	}
}
