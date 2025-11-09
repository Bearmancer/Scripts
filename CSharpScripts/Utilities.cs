using Spectre.Console;
using static System.String;
using Polly;

namespace CSharpScripts;

public static class Utilities
{
	// ============================== Logging ==============================

	public static void Info(string message)
	{
		AnsiConsole.MarkupLine($"[grey]{EscapeMarkup(message)}[/]");
	}

	public static void Warning(string message)
	{
		AnsiConsole.MarkupLine($"[yellow]{EscapeMarkup(message)}[/]");
	}

	public static void Success(string message)
	{
		AnsiConsole.MarkupLine($"[green]{EscapeMarkup(message)}[/]");
	}

	public static void Fail(string message)
	{
		var rendered = $"[red]{EscapeMarkup(message)}[/]";
		AnsiConsole.MarkupLine(rendered);
		throw new InvalidOperationException(message);
	}

	private static string EscapeMarkup(string text)
		=> Markup.Escape(text ?? Empty);

	// ============================== Progress ==============================

	/*
	Usage: Provide a task title supplier to avoid unescaped content and run work inside the callback.

	await Utilities.ProgressAsync(
		descriptionFactory: () => $"Processing: {Utilities.EscapeMarkup(title)}",
		maxValue: total,
		work: async (task) => { /* increment task safely */
	public static async Task ProgressAsync(Func<string> descriptionFactory, int maxValue, Func<ProgressTask, Task> work)
	{
		await AnsiConsole
			.Progress()
			.AutoClear(false)
			.Columns(
				new TaskDescriptionColumn(),
				new ProgressBarColumn(),
				new PercentageColumn(),
				new RemainingTimeColumn(),
				new SpinnerColumn()
			)
			.StartAsync(async ctx =>
			{
				var task = ctx.AddTask(descriptionFactory(), maxValue: maxValue);
				await work(task);
			});
	}

	// ============================== Retry (Polly) ==============================

	private static IAsyncPolicy CreateExponentialBackoffPolicy(int maxRetries = 5, double jitterSeconds = 0.25)
	{
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetries);

        return Policy
            .Handle<Exception>()
			.WaitAndRetryAsync(
				retryCount: maxRetries,
				sleepDurationProvider: attempt =>
				{
					var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
					var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, (int)(jitterSeconds * 1000)));
					return baseDelay + jitter;
				},
				onRetryAsync: (ex, delay, attempt, ctx) =>
				{
					Info($"Retry {attempt} in {delay:hh\\:mm\\:ss} :: {ex.Message}");
					return Task.CompletedTask;
				}
			);
	}
}