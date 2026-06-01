namespace CSharpScripts.Infrastructure;

public static class DateFormat
{
	public const string Time = "HH:mm:ss";
	public const string DateTime = "yyyy/MM/dd HH:mm:ss";
	public const string Date = "yyyy/MM/dd";

	public static string Now => System.DateTime.Now.ToString(Time);
	public static string NowFull => System.DateTime.Now.ToString(DateTime);
}

public static class Console
{
	private static readonly Regex MarkupTagPattern = new(@"\[/?[^\]]+\]", RegexOptions.Compiled);
	public static LogLevel Level { get; set; } = LogLevel.Info;
	public static bool Suppress { get; set; }

	#region Logging Methods

	public static void Debug(string message, params object?[] args) =>
		Write(LogLevel.Debug, "grey", message, args);

	public static void Info(string message, params object?[] args) =>
		Write(LogLevel.Info, "blue", message, args);

	public static void Success(string message, params object?[] args) =>
		Write(LogLevel.Success, "green", message, args);

	public static void Warning(string message, params object?[] args) =>
		Write(LogLevel.Warning, "yellow", message, args);

	public static void Error(string message, params object?[] args) =>
		Write(LogLevel.Error, "red", message, args);

	public static void Fatal(string message, params object?[] args) =>
		Write(LogLevel.Fatal, "red bold", message, args);

	public static void Progress(string message, params object?[] args)
	{
		if (Suppress || Level > LogLevel.Info)
			return;

		var formatted = args.Length > 0 ? Format(message, args) : message;
		AnsiConsole.MarkupLine(
			$"[cyan][[PROG]][/] [dim]{DateFormat.Now}:[/] {Markup.Escape(formatted)}"
		);
	}

	public static void Starting(string operation) =>
		AnsiConsole.MarkupLine($"[blue]→[/] {Markup.Escape(operation)}");

	public static void Complete(string operation) =>
		AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(operation)}");

	public static void Failed(string operation) =>
		AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(operation)}");

	public static void KeyValue(string key, string value) =>
		AnsiConsole.MarkupLine($"[cyan]{Markup.Escape(key)}:[/] {Markup.Escape(value)}");

	public static void Dim(string text) => AnsiConsole.MarkupLine($"[dim]{Markup.Escape(text)}[/]");

	public static void Tip(string text) =>
		AnsiConsole.MarkupLine($"[dim]Tip:[/] {Markup.Escape(text)}");

	#endregion

	#region Display Helpers

	public static void Rule(string text) =>
		AnsiConsole.Write(new Rule($"[bold cyan]{Markup.Escape(text)}[/]"));

	public static void Figlet(string text, SpectreColor? color = null)
	{
		FigletText figlet = new(text);
		if (color.HasValue)
			figlet.Color = color.Value;
		AnsiConsole.Write(figlet);
	}

	public static void CriticalFailure(string service, string message)
	{
		NewLine();
		Figlet("FAILED", SpectreColor.Red);
		AnsiConsole.MarkupLine($"[bold red on black] {service.ToUpperInvariant()} ERROR [/]");
		NewLine();
		AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
		NewLine();
	}

	public static void NewLine() => AnsiConsole.WriteLine();

	public static void Clear() => AnsiConsole.Clear();

	public static void WriteLine(string text) => AnsiConsole.WriteLine(text);

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

	public static void Confirm(string prompt) => AnsiConsole.Confirm(Markup.Escape(prompt));

	public static T Prompt<T>(IPrompt<T> prompt) => AnsiConsole.Prompt(prompt);

	public static Status Status() => AnsiConsole.Status();

	public static LiveDisplay Live(IRenderable target) => AnsiConsole.Live(target);

	#endregion

	#region Text Formatters

	public static string Escape(string? text) => Markup.Escape(text ?? "");

	public static string Bold(string? text) => $"[bold]{Markup.Escape(text ?? "")}[/]";

	public static string DimText(string? text) => $"[dim]{Markup.Escape(text ?? "")}[/]";

	public static string Colored(string color, string? text) =>
		$"[{color}]{Markup.Escape(text ?? "")}[/]";

	public static string Composer(string? text) => Colored("cyan", text);

	public static string Conductor(string? text) => Colored("yellow", text);

	public static string Orchestra(string? text) => Colored("green", text);

	public static string Soloist(string? text) => Colored("magenta", text);

	public static string Year(int? year) => year.HasValue ? $"[dim]({year})[/]" : "";

	public static string Work(string? text) => Colored("steelblue1", text);

	public static string Venue(string? text) =>
		IsNullOrEmpty(text) ? "" : $"[dim italic]@ {Markup.Escape(text)}[/]";

	public static string CombineWith(string separator, params string?[] parts)
	{
		var nonEmpty = parts.Where(static p => !IsNullOrEmpty(p)).ToList();
		return Join($" {separator} ", nonEmpty);
	}

	public static string Combine(params string?[] parts)
	{
		var nonEmpty = parts.Where(static p => !IsNullOrEmpty(p)).ToList();
		return Join(" ", nonEmpty);
	}

	public static string SourceBadge(string source) =>
		source.Equals("Discogs") ? "[yellow]Discogs[/]" : "[cyan]MusicBrainz[/]";

	#endregion

	#region Progress Rendering

	public static string ProgressBar(int completed, int total, string? eta = null)
	{
		var percent = total > 0 ? (double)completed / total * 100 : 0;
		var filled = (int)(percent / 5);
		var bar = new string('█', filled) + new string('░', 20 - filled);
		var etaPart = eta is { } ? $" │ ETA: [cyan]{eta}[/]" : "";
		return $"[blue][[{completed}/{total}]][/] {bar} [yellow]{percent:F0}%[/]{etaPart}";
	}

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

	public static IRenderable LiveProgressRow(
		string description,
		int completed,
		int total,
		DateTime startTime
	)
	{
		var percent = total > 0 ? (double)completed / total * 100 : 0;
		var color = ProgressColor(percent);
		var bar = WideProgressBar(percent);

		var eta =
			completed > 0
				? TimeSpan
					.FromSeconds(
						(DateTime.Now - startTime).TotalSeconds / completed * (total - completed)
					)
					.ToString(@"m\:ss", CultureInfo.InvariantCulture)
				: "?:??";

		var markup =
			$"{Escape(description)} [{color}]{bar}[/] [yellow]{percent:F0}%[/] ETA: [cyan]{eta}[/]";
		return new Markup(markup);
	}

	public static string CheckItem(string text) => $"[green]✓[/] {Markup.Escape(text)}";

	public static Markup ProgressMarkup(int completed, int total, string? eta = null) =>
		new(ProgressBar(completed, total, eta));

	public static Markup CheckItemMarkup(string text) => new($"  {CheckItem(text)}");

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
			result += DimText(paddedPrefix) + " ";
		}

		var displayTitle = titleWidth > 0 ? title.PadRight(titleWidth) : title;
		if (displayTitle.Length > titleWidth && titleWidth > 0)
			displayTitle = displayTitle[..(titleWidth - 3)] + "...";
		result += Colored("cyan", displayTitle);

		if (!IsNullOrEmpty(suffix))
			result += " " + DimText(suffix);
		return result;
	}

	#endregion

	#region Links & Tables

	public static void Link(string url, string text)
	{
		if (Suppress || Level > LogLevel.Info)
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

	public static void FileLink(string path)
	{
		if (Suppress)
			return;

		AnsiConsole.MarkupLine($"[dim]Log:[/] [link=file:///{path}]{path}[/]");
	}

	public static void Table(string title, Dictionary<string, string> data)
	{
		SpectreTable table = new() { Title = new TableTitle(Markup.Escape(title)) };
		table.AddColumn(new TableColumn("[cyan]Key[/]"));
		table.AddColumn(new TableColumn("[cyan]Value[/]"));

		foreach (KeyValuePair<string, string> kvp in data)
			table.AddRow(Markup.Escape(kvp.Key), Markup.Escape(kvp.Value));

		AnsiConsole.Write(table);
	}

	public static void Table(string title, IEnumerable<string[]> rows, params string[] columns)
	{
		SpectreTable table = new() { Title = new TableTitle(Markup.Escape(title)) };

		foreach (var col in columns)
			table.AddColumn(new TableColumn($"[cyan]{Markup.Escape(col)}[/]"));

		foreach (var row in rows)
			table.AddRow(row.Select(Markup.Escape).ToArray());

		AnsiConsole.Write(table);
	}

	internal static SpectreTable CreateResultTable(string title, string titleColor = "blue")
	{
		SpectreTable table = new()
		{
			Title = new TableTitle($"[bold {titleColor}]{Markup.Escape(title)}[/]"),
			Border = TableBorder.Rounded,
			ShowRowSeparators = true,
		};
		return table;
	}

	public static void AddResultColumn(SpectreTable table, string header) =>
		table.AddColumn($"[bold]{Markup.Escape(header)}[/]");

	public static void AddResultRow(SpectreTable table, params string[] cells)
	{
		string[] formatted = [.. cells.Select(static c => $"[steelblue1]{Markup.Escape(c)}[/]")];
		table.AddRow(formatted);
	}

	public static void Write(SpectreTable table) => AnsiConsole.Write(table);

	public static void Write(IRenderable renderable) => AnsiConsole.Write(renderable);

	public static void Render(IRenderable renderable) => AnsiConsole.Write(renderable);

	internal static Panel CreatePanel(string content, string header) =>
		new Panel(Markup.Escape(content)).Header(Markup.Escape(header)).Expand();

	public static void WritePanel(string header, string markupContent)
	{
		var panel = new Panel(new Markup(markupContent))
		{
			Border = BoxBorder.Rounded,
			Padding = new Spectre.Console.Padding(1, 1, 1, 1),
			Header = new PanelHeader($"[blue]{Markup.Escape(header)}[/]"),
		};
		AnsiConsole.Write(panel);
	}

	internal static SpectreProgress CreateProgress() => AnsiConsole.Progress();

	#endregion

	#region Help System

	public static void Section(string title) =>
		AnsiConsole.MarkupLine($"[bold white]{Markup.Escape(title)}[/]");

	public static void HelpUsage(string usage) =>
		AnsiConsole.MarkupLine($"  {Markup.Escape(usage)}");

	public static void HelpExample(string example) =>
		AnsiConsole.MarkupLine($"  {Markup.Escape(example)}");

	internal static SpectreTable CreateHelpTable()
	{
		SpectreTable table = new SpectreTable().NoBorder().HideHeaders();
		table.AddColumn(new TableColumn("").PadRight(4));
		table.AddColumn("");
		return table;
	}

	public static void AddHelpRow(SpectreTable table, string command, string description) =>
		table.AddRow($"[green]{Markup.Escape(command)}[/]", Markup.Escape(description));

	public static void HelpOption(
		string shortFlag,
		string longFlag,
		string? valuePlaceholder,
		string description,
		string[]? allowedValues = null,
		string? defaultValue = null
	)
	{
		var sig = IsNullOrEmpty(shortFlag) ? $"    --{longFlag}" : $"  -{shortFlag}, --{longFlag}";

		if (!IsNullOrEmpty(valuePlaceholder))
			sig += $" [cyan]<{valuePlaceholder.ToUpperInvariant()}>[/]";

		var desc = Markup.Escape(description);

		if (allowedValues is { Length: > 0 })
			desc += $" [dim]Allowed values are {Join(", ", allowedValues)}.[/]";

		if (!IsNullOrEmpty(defaultValue))
			desc += $" [dim][[default: {Markup.Escape(defaultValue)}]][/]";

		var rawSigLen = MarkupTagPattern.Replace(sig, "").Length;
		var padding = Math.Max(2, 44 - rawSigLen);

		AnsiConsole.MarkupLine($"[yellow]{sig}[/]{new string(' ', padding)}{desc}");
	}

	public static void HelpFlag(
		string shortFlag,
		string longFlag,
		string description,
		bool defaultValue = false
	)
	{
		var sig = IsNullOrEmpty(shortFlag) ? $"    --{longFlag}" : $"  -{shortFlag}, --{longFlag}";

		var desc = Markup.Escape(description);
		desc += $" [dim][[default: {(defaultValue ? "True" : "False")}]][/]";

		var rawSigLen = MarkupTagPattern.Replace(sig, "").Length;
		var padding = Math.Max(2, 44 - rawSigLen);

		AnsiConsole.MarkupLine($"[yellow]{sig}[/]{new string(' ', padding)}{desc}");
	}

	#endregion

	#region Private Helpers

	private static readonly Dictionary<LogLevel, string> LevelCodes = new()
	{
		[LogLevel.Debug] = "DEBG",
		[LogLevel.Info] = "INFO",
		[LogLevel.Success] = "OKAY",
		[LogLevel.Warning] = "WARN",
		[LogLevel.Error] = "ERRO",
		[LogLevel.Fatal] = "FATL",
	};

	private static void Write(LogLevel level, string color, string message, params object?[] args)
	{
		if (Suppress || Level > level)
			return;

		var formatted = args.Length > 0 ? Format(message, args) : message;
		var levelCode = LevelCodes[level];
		AnsiConsole.MarkupLine(
			$"[{color}][[{levelCode}]][/] [dim]{DateFormat.Now}:[/] {Markup.Escape(formatted)}"
		);
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

	#endregion
}

/// <summary>
/// A progress column that displays task descriptions with a fixed width for vertical alignment.
/// </summary>
public sealed class FixedWidthDescriptionColumn(int width) : ProgressColumn
{
	public Justify Alignment { get; set; } = Justify.Left;

	public override int? GetColumnWidth(RenderOptions options) => width;

	public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
	{
		var text = task.Description?.Replace("\n", " ").Replace("\r", "").Trim() ?? string.Empty;
		return new Markup(text).Overflow(Overflow.Ellipsis).Justify(Alignment);
	}
}
