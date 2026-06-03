namespace Scripts.Core;

internal static class DateFormat
{
	public const string Time = "HH:mm:ss";
	public const string Date = "yyyy/MM/dd";
	public const string DateTime = "yyyy/MM/dd HH:mm:ss";

	public static string Now => System.DateTimeOffset.Now.ToString(format: Time);
	public static string NowFull => System.DateTimeOffset.Now.ToString(format: DateTime);
	public static string Today => System.DateTimeOffset.Now.ToString(format: Date);
}

internal static class Ui
{
	public static bool Suppress { get; set; }

	public static void Info(string message, params object?[] args)
	{
		if (Suppress)
			return;

		AnsiConsole.MarkupLine($"[blue]ℹ[/] {Markup.Escape(Format(message: message, args: args))}");
	}

	public static void Warn(string message, params object?[] args)
	{
		if (Suppress)
			return;

		AnsiConsole.MarkupLine(
			$"[yellow]⚠[/] {Markup.Escape(Format(message: message, args: args))}"
		);
	}

	public static void Error(string message, params object?[] args)
	{
		if (Suppress)
			return;

		AnsiConsole.MarkupLine($"[red]✖[/] {Markup.Escape(Format(message: message, args: args))}");
	}

	public static void Ok(string message, params object?[] args)
	{
		if (Suppress)
			return;

		AnsiConsole.MarkupLine(
			$"[green]✔[/] {Markup.Escape(Format(message: message, args: args))}"
		);
	}

	public static void Exception(Exception ex)
	{
		if (Suppress)
			return;

		AnsiConsole.WriteException(exception: ex, format: ExceptionFormats.ShortenEverything);
	}

	public static void Progress(string message, params object?[] args)
	{
		if (Suppress)
			return;

		var formatted = args.Length > 0 ? Format(message: message, args: args) : message;
		AnsiConsole.MarkupLine(
			$"[cyan][[PROG]][/] [dim]{DateFormat.Now}:[/] {Markup.Escape(text: formatted)}"
		);
	}

	public static void Starting(string message, params object?[] args)
	{
		var formatted = args.Length > 0 ? Format(message: message, args: args) : message;
		AnsiConsole.MarkupLine($"[blue]→[/] {Markup.Escape(text: formatted)}");
	}

	public static void Complete(string message, params object?[] args)
	{
		var formatted = args.Length > 0 ? Format(message: message, args: args) : message;
		AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(text: formatted)}");
	}

	public static void Failed(string message, params object?[] args)
	{
		var formatted = args.Length > 0 ? Format(message: message, args: args) : message;
		AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(text: formatted)}");
	}

	public static void KeyValue(string key, string value) =>
		AnsiConsole.MarkupLine(
			$"[cyan]{Markup.Escape(text: key)}:[/] {Markup.Escape(text: value)}"
		);

	public static void Tip(string text) =>
		AnsiConsole.MarkupLine($"[dim]Tip:[/] {Markup.Escape(text: text)}");

	public static void Rule(string text) =>
		AnsiConsole.Write(new Rule($"[bold cyan]{Markup.Escape(text: text)}[/]"));

	public static void CriticalFailure(string service, string message)
	{
		NewLine();
		FigletText figlet = new(text: "FAILED") { Color = SpectreColor.Red };
		AnsiConsole.Write(renderable: figlet);
		AnsiConsole.MarkupLine($"[bold red on black] {service.ToUpperInvariant()} ERROR [/]");
		NewLine();
		AnsiConsole.MarkupLine($"[red]{Markup.Escape(text: message)}[/]");
		NewLine();
	}

	public static void NewLine() => AnsiConsole.WriteLine();

	public static void MarkupLine(string markup) => AnsiConsole.MarkupLine(value: markup);

	public static void Field(string label, string? value, int labelWidth = 12)
	{
		var paddedLabel = label.PadRight(totalWidth: labelWidth);
		var safeValue = Markup.Escape(value ?? "");
		AnsiConsole.MarkupLine($"[bold]{paddedLabel}[/] {safeValue}");
	}

	public static void FieldIfPresent(string label, string? value, int labelWidth = 12)
	{
		if (!IsNullOrEmpty(value: value))
			Field(label: label, value: value, labelWidth: labelWidth);
	}

	public static void LabelValue(string label, string? value, string valueColor = "cyan") =>
		AnsiConsole.MarkupLine(
			$"    {Dim(label + ":")} {(IsNullOrEmpty(value: value) ? Dim(text: "-") : Colored(color: valueColor, text: value))}"
		);

	public static void MissingField(string label) =>
		AnsiConsole.MarkupLine($"    {Dim(label + ":")} {Red(text: "(missing)")}");

	public static void FoundHeader() => AnsiConsole.MarkupLine(Green(text: "  Found:"));

	public static void InputHeader() => AnsiConsole.MarkupLine(Dim(text: "  Input:"));

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
		var labelDisplay = IsNullOrEmpty(value: label) ? Dim(text: "-") : Cyan(text: label);
		var catDisplay = IsNullOrEmpty(value: catalogNumber)
			? Dim(text: "-")
			: Cyan(text: catalogNumber);
		var yearDisplay = IsNullOrEmpty(value: year) ? Dim(text: "-") : Cyan(text: year);

		AnsiConsole.MarkupLine(
			$"    {Colored(color: confColor, $"{confidence, 3}%")} Label: {labelDisplay} │ Cat: {catDisplay} │ Year: {yearDisplay} {Dim($"({source})")}"
		);
	}

	public static void TitleWithSubtitle(string title, string subtitle) =>
		AnsiConsole.MarkupLine(
			$"{Bold(Cyan(text: title))} {Dim(text: "—")} {Yellow(text: subtitle)}"
		);

	public static T Prompt<T>(IPrompt<T> prompt) => AnsiConsole.Prompt(prompt: prompt);

	public static void Write(IRenderable renderable) => AnsiConsole.Write(renderable: renderable);

	public static string Colored(string color, string? text) =>
		$"[{color}]{Markup.Escape(text ?? "")}[/]";

	public static string Cyan(string? text) => Colored(color: "cyan", text: text);

	public static string Green(string? text) => Colored(color: "green", text: text);

	public static string Yellow(string? text) => Colored(color: "yellow", text: text);

	public static string Red(string? text) => Colored(color: "red", text: text);

	public static string Blue(string? text) => Colored(color: "blue", text: text);

	public static string Magenta(string? text) => Colored(color: "magenta", text: text);

	public static string Dim(string? text) => Colored(color: "dim", text: text);

	public static string Bold(string? text) => $"[bold]{Markup.Escape(text ?? "")}[/]";

	public static string LinkText(string url, string? text) =>
		$"[link={Markup.Escape(text: url)}]{Markup.Escape(text ?? "")}[/]";

	public static string Combine(params string?[] parts) =>
		Join(separator: " ", parts.Where(static p => !IsNullOrEmpty(value: p)));

	public static string WideProgressBar(double percent, int width = 40)
	{
		var filled = (int)(width * percent / 100.0);
		filled = Math.Clamp(value: filled, min: 0, max: width);
		var empty = width - filled;
		return new string(c: '━', count: filled) + new string(c: '─', count: empty);
	}

	public static string ProgressColor(double percent) =>
		percent switch
		{
			>= 75 => "green",
			>= 50 => "yellow",
			>= 25 => "blue",
			_ => "cyan",
		};

	public static string TaskTitle(string title) => Colored(color: "cyan", text: title);

	public static string TaskDescription(
		string? prefix,
		string title,
		string? suffix = null,
		int prefixWidth = 0,
		int titleWidth = 0
	)
	{
		var result = "";
		if (!IsNullOrEmpty(value: prefix))
		{
			var paddedPrefix = prefixWidth > 0 ? prefix.PadLeft(totalWidth: prefixWidth) : prefix;
			result += Dim(text: paddedPrefix) + " ";
		}

		var displayTitle = titleWidth > 0 ? title.PadRight(totalWidth: titleWidth) : title;
		if (displayTitle.Length > titleWidth && titleWidth > 0)
			displayTitle = displayTitle[..(titleWidth - 3)] + "...";
		result += Colored(color: "cyan", text: displayTitle);

		if (!IsNullOrEmpty(value: suffix))
			result += " " + Dim(text: suffix);
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
			.AutoClear(enabled: autoClear)
			.HideCompleted(enabled: hideCompleted);

		List<ProgressColumn> columns =
		[
			new FixedWidthDescriptionColumn(width: descriptionWidth),
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
			.AutoClear(enabled: autoClear)
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

		var escaped = Markup.Escape(text: url);
		AnsiConsole.MarkupLine(
			$"[blue][[INFO]][/] [dim]{DateFormat.Now}:[/] {Markup.Escape(text: text)}: [link={escaped}]{escaped}[/]"
		);
		NewLine();
	}

	public static void Link(int number, string url, int maxLength = 80)
	{
		var truncated = url.Length <= maxLength ? url : url[..(maxLength - 3)] + "...";
		AnsiConsole.MarkupLine(
			$"  [blue][link={url}]{number}. {Markup.Escape(text: truncated)}[/][/]"
		);
	}

	private static string Format(string message, object?[] args)
	{
		try
		{
			object?[] safeArgs = [.. args.Select(static a => a ?? "null")];
			return string.Format(format: message, args: safeArgs);
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
		AnsiConsole.MarkupLine($"       {Green(text: "→")} {Markup.Escape(translated ?? "")}");

	public static void TranslationFailed(string? reason = null)
	{
		var message = IsNullOrWhiteSpace(value: reason)
			? "(translation failed)"
			: $"(translation failed: {reason})";
		AnsiConsole.MarkupLine($"       {Red(text: "→")} {Dim(text: message)}");
	}

	public static void TranslationSummary(Dictionary<string, int> languageCounts)
	{
		IEnumerable<string> parts = languageCounts
			.OrderByDescending(kv => kv.Value)
			.Select(kv => $"{kv.Key.ToUpperInvariant()}: {kv.Value}");
		Info(message: "Languages: {0}", Join(separator: ", ", values: parts));
	}

	public static void TranslationVideoHeader(string videoId, string? language)
	{
		AnsiConsole.MarkupLine($"{Cyan(text: "Video:")} {Markup.Escape(text: videoId)}");
		AnsiConsole.MarkupLine($"  {Dim(text: "Language:")} {language?.ToUpperInvariant() ?? "-"}");
	}

	public static void TranslationOriginalTitle(string title)
	{
		AnsiConsole.MarkupLine($"  {Yellow(text: "Original Title:")}");
		AnsiConsole.MarkupLine($"    {Markup.Escape(text: title)}");
	}

	public static void TranslationTranslatedTitle(string? translatedTitle)
	{
		AnsiConsole.MarkupLine($"  {Green(text: "Translated Title:")}");
		AnsiConsole.MarkupLine($"    {Markup.Escape(translatedTitle ?? "")}");
	}

	public static void TranslationOriginalDescription(string description)
	{
		AnsiConsole.MarkupLine($"  {Yellow(text: "Original Description:")}");
		AnsiConsole.MarkupLine($"    {Markup.Escape(Truncate(text: description, maxLength: 100))}");
	}

	public static void TranslationTranslatedDescription(string? description)
	{
		AnsiConsole.MarkupLine($"  {Green(text: "Translated Description:")}");
		AnsiConsole.MarkupLine($"    {Markup.Escape(Truncate(description ?? "", maxLength: 100))}");
	}

	private static string Truncate(string text, int maxLength) =>
		text.Length > maxLength ? text[..(maxLength - 3)] + "..." : text;
}

file sealed class FixedWidthDescriptionColumn(int width) : ProgressColumn
{
	public static Justify Alignment => Justify.Left;

	public override int? GetColumnWidth(RenderOptions options) => width;

	public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
	{
		var text =
			task.Description?.Replace(oldValue: "\n", newValue: " ")
				.Replace(oldValue: "\r", newValue: "")
				.Trim()
			?? "";
		return new Markup(text: text)
			.Overflow(overflow: Overflow.Ellipsis)
			.Justify(alignment: Alignment);
	}
}
