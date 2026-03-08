namespace CSharpScripts.Core;

internal static class DateFormat
{
	public const string TIME = "HH:mm:ss";
	public const string DATE = "yyyy/MM/dd";
	public const string DATE_TIME = "yyyy/MM/dd HH:mm:ss";

	public static string Now => DateTime.Now.ToString(TIME);
	public static string NowFull => DateTime.Now.ToString(DATE_TIME);
	public static string Today => DateTime.Now.ToString(DATE);
}

internal static class UI
{
	public static bool Suppress { get; set; }

	public static void Info(string message, params object?[] args)
	{
		if (Suppress)
			return;
		AnsiConsole.MarkupLine($"[blue]ℹ[/] {Markup.Escape(Format(message, args))}");
	}

	public static void Warn(string message, params object?[] args)
	{
		if (Suppress)
			return;
		AnsiConsole.MarkupLine($"[yellow]⚠[/] {Markup.Escape(Format(message, args))}");
	}

	public static void Error(string message, params object?[] args)
	{
		if (Suppress)
			return;
		AnsiConsole.MarkupLine($"[red]✖[/] {Markup.Escape(Format(message, args))}");
	}

	public static void Ok(string message, params object?[] args)
	{
		if (Suppress)
			return;
		AnsiConsole.MarkupLine($"[green]✔[/] {Markup.Escape(Format(message, args))}");
	}

	public static void Exception(Exception ex)
	{
		if (Suppress)
			return;
		AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
	}

	public static void Progress(string message, params object?[] args)
	{
		if (Suppress)
			return;

		var formatted = args.Length > 0 ? Format(message, args) : message;
		AnsiConsole.MarkupLine(
			$"[cyan][[PROG]][/] [dim]{DateFormat.Now}:[/] {Markup.Escape(formatted)}"
		);
	}

	public static void Starting(string message, params object?[] args)
	{
		var formatted = args.Length > 0 ? Format(message, args) : message;
		AnsiConsole.MarkupLine($"[blue]→[/] {Markup.Escape(formatted)}");
	}

	public static void Complete(string message, params object?[] args)
	{
		var formatted = args.Length > 0 ? Format(message, args) : message;
		AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(formatted)}");
	}

	public static void Failed(string message, params object?[] args)
	{
		var formatted = args.Length > 0 ? Format(message, args) : message;
		AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(formatted)}");
	}

	public static void KeyValue(string key, string value) =>
		AnsiConsole.MarkupLine($"[cyan]{Markup.Escape(key)}:[/] {Markup.Escape(value)}");

	public static void Tip(string text) =>
		AnsiConsole.MarkupLine($"[dim]Tip:[/] {Markup.Escape(text)}");

	public static void Rule(string text) =>
		AnsiConsole.Write(new Rule($"[bold cyan]{Markup.Escape(text)}[/]"));

	public static void CriticalFailure(string service, string message)
	{
		NewLine();
		FigletText figlet = new("FAILED") { Color = SpectreColor.Red };
		AnsiConsole.Write(figlet);
		AnsiConsole.MarkupLine($"[bold red on black] {service.ToUpperInvariant()} ERROR [/]");
		NewLine();
		AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
		NewLine();
	}

	public static void NewLine() => AnsiConsole.WriteLine();

	public static void MarkupLine(string markup) => AnsiConsole.MarkupLine(markup);

	public static void Field(string label, string? value, int labelWidth = 12)
	{
		var paddedLabel = label.PadRight(labelWidth);
		var safeValue = Markup.Escape(value ?? "");
		AnsiConsole.MarkupLine($"[bold]{paddedLabel}[/] {safeValue}");
	}

	public static void FieldIfPresent(string label, string? value, int labelWidth = 12)
	{
		if (!IsNullOrEmpty(value))
			Field(label, value, labelWidth);
	}

	public static void LabelValue(string label, string? value, string valueColor = "cyan") =>
		AnsiConsole.MarkupLine(
			$"    {Dim(label + ":")} {(IsNullOrEmpty(value) ? Dim("-") : Colored(valueColor, value))}"
		);

	public static void MissingField(string label) =>
		AnsiConsole.MarkupLine($"    {Dim(label + ":")} {Red("(missing)")}");

	public static void FoundHeader() => AnsiConsole.MarkupLine(Green("  Found:"));

	public static void InputHeader() => AnsiConsole.MarkupLine(Dim("  Input:"));

	public static void ConfidenceResult(
		int confidence,
		string? label,
		string? catalogNumber,
		string? year,
		string source
	)
	{
		var confColor =
			confidence >= 70 ? "green"
			: confidence >= 50 ? "yellow"
			: "dim";
		var labelDisplay = IsNullOrEmpty(label) ? Dim("-") : Cyan(label);
		var catDisplay = IsNullOrEmpty(catalogNumber) ? Dim("-") : Cyan(catalogNumber);
		var yearDisplay = IsNullOrEmpty(year) ? Dim("-") : Cyan(year);

		AnsiConsole.MarkupLine(
			$"    {Colored(confColor, $"{confidence, 3}%")} Label: {labelDisplay} │ Cat: {catDisplay} │ Year: {yearDisplay} {Dim($"({source})")}"
		);
	}

	public static void TitleWithSubtitle(string title, string subtitle) =>
		AnsiConsole.MarkupLine($"{Bold(Cyan(title))} {Dim("—")} {Yellow(subtitle)}");

	public static T Prompt<T>(IPrompt<T> prompt) => AnsiConsole.Prompt(prompt);

	public static void Write(IRenderable renderable) => AnsiConsole.Write(renderable);

	public static string Colored(string color, string? text) =>
		$"[{color}]{Markup.Escape(text ?? "")}[/]";

	public static string Cyan(string? text) => Colored("cyan", text);

	public static string Green(string? text) => Colored("green", text);

	public static string Yellow(string? text) => Colored("yellow", text);

	public static string Red(string? text) => Colored("red", text);

	public static string Blue(string? text) => Colored("blue", text);

	public static string Magenta(string? text) => Colored("magenta", text);

	public static string Dim(string? text) => Colored("dim", text);

	public static string Bold(string? text) => $"[bold]{Markup.Escape(text ?? "")}[/]";

	public static string LinkText(string url, string? text) =>
		$"[link={Markup.Escape(url)}]{Markup.Escape(text ?? "")}[/]";

	public static string Combine(params string?[] parts) =>
		Join(" ", parts.Where(static p => !IsNullOrEmpty(p)));

	public static string WideProgressBar(double percent, int width = 40)
	{
		var filled = (int)(width * percent / 100.0);
		filled = Math.Clamp(filled, 0, width);
		var empty = width - filled;
		return new string('━', filled) + new string('─', empty);
	}

	public static string ProgressColor(double percent) =>
		percent switch
		{
			>= 75 => "green",
			>= 50 => "yellow",
			>= 25 => "blue",
			_ => "cyan",
		};

	public static string TaskTitle(string title) => Colored("cyan", title);

	public static string TaskDescription(
		string? prefix,
		string title,
		string? suffix = null,
		int prefixWidth = 0,
		int titleWidth = 0
	)
	{
		var result = "";
		if (!IsNullOrEmpty(prefix))
		{
			var paddedPrefix = prefixWidth > 0 ? prefix.PadLeft(prefixWidth) : prefix;
			result += Dim(paddedPrefix) + " ";
		}

		var displayTitle = titleWidth > 0 ? title.PadRight(titleWidth) : title;
		if (displayTitle.Length > titleWidth && titleWidth > 0)
			displayTitle = displayTitle[..(titleWidth - 3)] + "...";
		result += Colored("cyan", displayTitle);

		if (!IsNullOrEmpty(suffix))
			result += " " + Dim(suffix);
		return result;
	}

	public static SpectreProgress CreateStandardProgress(
		int descriptionWidth = 40,
		bool showRemaining = true,
		bool autoClear = true,
		bool hideCompleted = false
	)
	{
		SpectreProgress progress = AnsiConsole
			.Progress()
			.AutoClear(autoClear)
			.HideCompleted(hideCompleted);

		List<ProgressColumn> columns =
		[
			new FixedWidthDescriptionColumn(descriptionWidth),
			new ProgressBarColumn(),
			new PercentageColumn(),
		];

		if (showRemaining)
			columns.Add(new RemainingTimeColumn());

		columns.Add(new SpinnerColumn());

		return progress.Columns([.. columns]);
	}

	public static SpectreProgress CreateMinimalProgress(
		bool autoClear = false,
		Spinner? spinner = null
	)
	{
		return AnsiConsole
			.Progress()
			.AutoClear(autoClear)
			.Columns(
				new TaskDescriptionColumn(),
				new ProgressBarColumn(),
				new PercentageColumn(),
				new SpinnerColumn(spinner ?? Spinner.Known.Dots)
			);
	}

	public static void Link(string url, string text)
	{
		if (Suppress)
			return;

		var escaped = Markup.Escape(url);
		AnsiConsole.MarkupLine(
			$"[blue][[INFO]][/] [dim]{DateFormat.Now}:[/] {Markup.Escape(text)}: [link={escaped}]{escaped}[/]"
		);
		NewLine();
	}

	public static void Link(int number, string url, int maxLength = 80)
	{
		var truncated = url.Length <= maxLength ? url : url[..(maxLength - 3)] + "...";
		AnsiConsole.MarkupLine($"  [blue][link={url}]{number}. {Markup.Escape(truncated)}[/][/]");
	}

	private static string Format(string message, object?[] args)
	{
		try
		{
			object?[] safeArgs = [.. args.Select(static a => a ?? "null")];
			return string.Format(message, safeArgs);
		}
		catch (FormatException)
		{
			return message;
		}
	}

	public static void TranslationItem(int current, int total, string lang, string? original)
	{
		if (Suppress)
			return;
		AnsiConsole.MarkupLine(
			$"{Dim($"[{current}/{total}]")} {Dim($"[{lang.ToUpperInvariant()}]")} {Markup.Escape(original ?? "")}"
		);
	}

	public static void TranslationResult(string? translated) =>
		AnsiConsole.MarkupLine($"       {Green("→")} {Markup.Escape(translated ?? "")}");

	public static void TranslationFailed(string? reason = null)
	{
		var message = IsNullOrWhiteSpace(reason)
			? "(translation failed)"
			: $"(translation failed: {reason})";
		AnsiConsole.MarkupLine($"       {Red("→")} {Dim(message)}");
	}

	public static void TranslationSummary(Dictionary<string, int> languageCounts)
	{
		IEnumerable<string> parts = languageCounts
			.OrderByDescending(kv => kv.Value)
			.Select(kv => $"{kv.Key.ToUpperInvariant()}: {kv.Value}");
		Info("Languages: {0}", Join(", ", parts));
	}

	public static void TranslationVideoHeader(string videoId, string? language)
	{
		AnsiConsole.MarkupLine($"{Cyan("Video:")} {Markup.Escape(videoId)}");
		AnsiConsole.MarkupLine($"  {Dim("Language:")} {language?.ToUpperInvariant() ?? "-"}");
	}

	public static void TranslationOriginalTitle(string title)
	{
		AnsiConsole.MarkupLine($"  {Yellow("Original Title:")}");
		AnsiConsole.MarkupLine($"    {Markup.Escape(title)}");
	}

	public static void TranslationTranslatedTitle(string? translatedTitle)
	{
		AnsiConsole.MarkupLine($"  {Green("Translated Title:")}");
		AnsiConsole.MarkupLine($"    {Markup.Escape(translatedTitle ?? "")}");
	}

	public static void TranslationOriginalDescription(string description)
	{
		AnsiConsole.MarkupLine($"  {Yellow("Original Description:")}");
		AnsiConsole.MarkupLine($"    {Markup.Escape(Truncate(description, 100))}");
	}

	public static void TranslationTranslatedDescription(string? description)
	{
		AnsiConsole.MarkupLine($"  {Green("Translated Description:")}");
		AnsiConsole.MarkupLine($"    {Markup.Escape(Truncate(description ?? "", 100))}");
	}

	private static string Truncate(string text, int maxLength) =>
		text.Length > maxLength ? text[..(maxLength - 3)] + "..." : text;
}

file sealed class FixedWidthDescriptionColumn(int width) : ProgressColumn
{
	public Justify Alignment { get; set; } = Justify.Left;

	public override int? GetColumnWidth(RenderOptions options) => width;

	public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
	{
		var text = task.Description?.Replace("\n", " ").Replace("\r", "").Trim() ?? Empty;
		return new Markup(text).Overflow(Overflow.Ellipsis).Justify(Alignment);
	}
}
