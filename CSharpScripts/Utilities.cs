using Spectre.Console;

namespace CSharpScripts;

public static class Utilities
{
    public static void Info(string message) =>
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");

    public static void Warning(string message) =>
        AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");

    public static void Success(string message) =>
        AnsiConsole.MarkupLine($"[green]{Markup.Escape(message)}[/]");

    public static void Error(string message) =>
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");

    public static void Fail(string message)
    {
        Error(message);
        throw new InvalidOperationException(message);
    }

    public static async Task ProgressAsync(
        Func<string> descriptionFactory,
        int maxValue,
        Func<ProgressTask, Task> work
    )
    {
        await AnsiConsole
            .Progress()
            .AutoClear(enabled: false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            )
            .StartAsync(async context =>
            {
                var task = context.AddTask(description: descriptionFactory(), maxValue: maxValue);
                await work(task);
            });
    }

    public static string FormatDuration(TimeSpan duration) =>
        $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
}

public static class Logger
{
    public static void Info(string message, bool isBold = false, bool isDim = false)
    {
        var formatted =
            isBold ? $"[bold green]{Markup.Escape(message)}[/]"
            : isDim ? $"[dim]{Markup.Escape(message)}[/]"
            : $"[green]{Markup.Escape(message)}[/]";
        AnsiConsole.MarkupLine(formatted);
    }

    public static void Warning(string message) =>
        AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");

    public static void Error(string message) =>
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
}
