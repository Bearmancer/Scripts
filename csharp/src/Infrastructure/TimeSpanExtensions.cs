namespace CSharpScripts.Infrastructure;

public static class TimeSpanExtensions
{
    public static string ToPaddedString(this TimeSpan? duration) =>
        duration is not { } ts ? ""
        : ts.TotalHours >= 1 ? $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
        : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
}
