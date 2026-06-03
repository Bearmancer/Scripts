using System;
using System.Globalization;

namespace Scripts.Core;

public static class DateTimeFormats
{
	public const string Iso8601 = "yyyy-MM-ddTHH:mm:sszzz";
	public const string UiTime = "HH:mm:ss";
	public const string UiDate = "yyyy-MM-dd";
	public const string UiDateTime = "yyyy-MM-dd HH:mm:ss";
}

public static class TimeZoneHelper
{
	private static readonly TimeZoneInfo Ist = TimeZoneInfo.FindSystemTimeZoneById(
		"India Standard Time"
	);

	public static DateTimeOffset ToIst(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, Ist);

	public static string FormatIst(DateTimeOffset utc, string format = DateTimeFormats.Iso8601) =>
		ToIst(utc).ToString(format, CultureInfo.InvariantCulture);
}
