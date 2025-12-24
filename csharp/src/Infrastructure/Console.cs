namespace CSharpScripts.Infrastructure;

public static class Console
{
    private static readonly Regex MarkupTagPattern = new(
        pattern: @"\[/?[^\]]+\]",
        options: RegexOptions.Compiled
    );
    public static LogLevel Level { get; set; } = LogLevel.Info;
    public static bool Suppress { get; set; }

    public static void Debug(string message, params object?[] args) =>
        Write(level: LogLevel.Debug, color: "grey", message: message, args: args);

    public static void Info(string message, params object?[] args) =>
        Write(level: LogLevel.Info, color: "blue", message: message, args: args);

    public static void Success(string message, params object?[] args) =>
        Write(level: LogLevel.Success, color: "green", message: message, args: args);

    public static void Warning(string message, params object?[] args) =>
        Write(level: LogLevel.Warning, color: "yellow", message: message, args: args);

    public static void Error(string message, params object?[] args) =>
        Write(level: LogLevel.Error, color: "red", message: message, args: args);

    public static void Fatal(string message, params object?[] args) =>
        Write(level: LogLevel.Fatal, color: "red bold", message: message, args: args);

    public static void Progress(string message, params object?[] args)
    {
        if (Suppress || Level > LogLevel.Info)
            return;

        string formatted = args.Length > 0 ? Format(message: message, args: args) : message;
        var timestamp = DateTime.Now.ToString(format: "HH:mm:ss");
        AnsiConsole.MarkupLine(
            $"[cyan][[PROG]][/] [dim]{timestamp}:[/] {Markup.Escape(text: formatted)}"
        );
    }

    public static void Starting(string operation) =>
        AnsiConsole.MarkupLine($"[blue]→[/] {Markup.Escape(text: operation)}");

    public static void Complete(string operation) =>
        AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(text: operation)}");

    public static void Failed(string operation) =>
        AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(text: operation)}");

    public static void KeyValue(string key, string value) =>
        AnsiConsole.MarkupLine(
            $"[cyan]{Markup.Escape(text: key)}:[/] {Markup.Escape(text: value)}"
        );

    public static void Dim(string text) =>
        AnsiConsole.MarkupLine($"[dim]{Markup.Escape(text: text)}[/]");

    public static void Tip(string text) =>
        AnsiConsole.MarkupLine($"[dim]Tip:[/] {Markup.Escape(text: text)}");

    public static void Rule(string text) =>
        AnsiConsole.Write(new Rule($"[bold cyan]{Markup.Escape(text: text)}[/]"));

    public static void Figlet(string text, SpectreColor? color = null)
    {
        FigletText figlet = new(text: text);
        if (color.HasValue)
            figlet.Color = color.Value;
        AnsiConsole.Write(renderable: figlet);
    }

    public static void CriticalFailure(string service, string message)
    {
        NewLine();
        Figlet(text: "FAILED", color: SpectreColor.Red);
        AnsiConsole.MarkupLine($"[bold red on black] {service.ToUpperInvariant()} ERROR [/]");
        NewLine();
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(text: message)}[/]");
        NewLine();
    }

    public static void NewLine() => AnsiConsole.WriteLine();

    public static void Clear() => AnsiConsole.Clear();

    public static void WriteLine(string text) => AnsiConsole.WriteLine(value: text);

    public static void MarkupLine(string markup) => AnsiConsole.MarkupLine(value: markup);

    public static void Field(string label, string? value, int labelWidth = 12)
    {
        string paddedLabel = label.PadRight(totalWidth: labelWidth);
        string safeValue = Markup.Escape(value ?? "");
        AnsiConsole.MarkupLine($"[bold]{paddedLabel}[/] {safeValue}");
    }

    public static void FieldIfPresent(string label, string? value, int labelWidth = 12)
    {
        if (!IsNullOrEmpty(value: value))
            Field(label: label, value: value, labelWidth: labelWidth);
    }

    public static void Confirm(string prompt) => AnsiConsole.Confirm(Markup.Escape(text: prompt));

    public static T Prompt<T>(IPrompt<T> prompt) => AnsiConsole.Prompt(prompt: prompt);

    public static Status Status() => AnsiConsole.Status();

    public static LiveDisplay Live(IRenderable target) => AnsiConsole.Live(target: target);

    public static string Escape(string? text) => Markup.Escape(text ?? "");

    public static string Bold(string? text) => $"[bold]{Markup.Escape(text ?? "")}[/]";

    public static string DimText(string? text) => $"[dim]{Markup.Escape(text ?? "")}[/]";

    public static string Colored(string color, string? text) =>
        $"[{color}]{Markup.Escape(text ?? "")}[/]";

    public static string Composer(string? text) => Colored(color: "cyan", text: text);

    public static string Conductor(string? text) => Colored(color: "yellow", text: text);

    public static string Orchestra(string? text) => Colored(color: "green", text: text);

    public static string Soloist(string? text) => Colored(color: "magenta", text: text);

    public static string Year(int? year) => year.HasValue ? $"[dim]({year})[/]" : "";

    public static string Work(string? text) => Colored(color: "steelblue1", text: text);

    public static string Venue(string? text) =>
        IsNullOrEmpty(value: text) ? "" : $"[dim italic]@ {Markup.Escape(text: text)}[/]";

    public static string CombineWith(string separator, params string?[] parts)
    {
        var nonEmpty = parts.Where(p => !IsNullOrEmpty(value: p)).ToList();
        return Join($" {separator} ", nonEmpty!);
    }

    public static string Combine(params string?[] parts)
    {
        var nonEmpty = parts.Where(p => !IsNullOrEmpty(value: p)).ToList();
        return Join(separator: " ", nonEmpty!);
    }

    public static string SourceBadge(string source) =>
        source.Equals(value: "Discogs", comparisonType: StringComparison.OrdinalIgnoreCase)
            ? "[yellow]Discogs[/]"
            : "[cyan]MusicBrainz[/]";

    public static string ProgressBar(int completed, int total, string? eta = null)
    {
        double percent = total > 0 ? (double)completed / total * 100 : 0;
        var filled = (int)(percent / 5);
        string bar = new string(c: '█', count: filled) + new string(c: '░', 20 - filled);
        string etaPart = eta is { } ? $" │ ETA: [cyan]{eta}[/]" : "";
        return $"[blue][[{completed}/{total}]][/] {bar} [yellow]{percent:F0}%[/]{etaPart}";
    }

    public static string WideProgressBar(double percent, int width = 40)
    {
        var filled = (int)(width * percent / 100.0);
        filled = Math.Clamp(value: filled, min: 0, max: width);
        int empty = width - filled;
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

    public static IRenderable LiveProgressRow(
        string description,
        int completed,
        int total,
        DateTime startTime
    )
    {
        double percent = total > 0 ? (double)completed / total * 100 : 0;
        string color = ProgressColor(percent: percent);
        string bar = WideProgressBar(percent: percent);

        string eta =
            completed > 0
                ? TimeSpan
                    .FromSeconds(
                        (DateTime.Now - startTime).TotalSeconds / completed * (total - completed)
                    )
                    .ToString(format: @"m\:ss", formatProvider: CultureInfo.InvariantCulture)
                : "?:??";

        var markup =
            $"{Escape(text: description)} [{color}]{bar}[/] [yellow]{percent:F0}%[/] ETA: [cyan]{eta}[/]";
        return new Markup(text: markup);
    }

    public static string CheckItem(string text) => $"[green]✓[/] {Markup.Escape(text: text)}";

    public static Markup ProgressMarkup(int completed, int total, string? eta = null) =>
        new(ProgressBar(completed: completed, total: total, eta: eta));

    public static Markup CheckItemMarkup(string text) => new($"  {CheckItem(text: text)}");

    public static string TaskTitle(string title) => Colored(color: "cyan", text: title);

    public static string TaskDescription(string? prefix, string title, string? suffix = null)
    {
        var result = "";
        if (!IsNullOrEmpty(value: prefix))
            result += DimText(text: prefix) + " ";
        result += Colored(color: "cyan", text: title);
        if (!IsNullOrEmpty(value: suffix))
            result += " " + DimText(text: suffix);
        return result;
    }

    public static void Link(string url, string text)
    {
        if (Suppress || Level > LogLevel.Info)
            return;

        var timestamp = DateTime.Now.ToString(format: "HH:mm:ss");
        string escaped = Markup.Escape(text: url);
        AnsiConsole.MarkupLine(
            $"[blue][[INFO]][/] [dim]{timestamp}:[/] {Markup.Escape(text: text)}: [link={escaped}]{escaped}[/]"
        );
        NewLine();
    }

    public static void Link(int number, string url, int maxLength = 80)
    {
        string truncated = url.Length <= maxLength ? url : url[..(maxLength - 3)] + "...";
        AnsiConsole.MarkupLine(
            $"  [blue][link={url}]{number}. {Markup.Escape(text: truncated)}[/][/]"
        );
    }

    public static void FileLink(string path)
    {
        if (Suppress)
            return;

        AnsiConsole.MarkupLine($"[dim]Log:[/] [link=file:///{path}]{path}[/]");
    }

    public static void Table(string title, Dictionary<string, string> data)
    {
        SpectreTable table = new() { Title = new TableTitle(Markup.Escape(text: title)) };
        table.AddColumn(new TableColumn(header: "[cyan]Key[/]"));
        table.AddColumn(new TableColumn(header: "[cyan]Value[/]"));

        foreach (var kvp in data)
            table.AddRow(Markup.Escape(text: kvp.Key), Markup.Escape(text: kvp.Value));

        AnsiConsole.Write(renderable: table);
    }

    public static void Table(string title, IEnumerable<string[]> rows, params string[] columns)
    {
        SpectreTable table = new() { Title = new TableTitle(Markup.Escape(text: title)) };

        foreach (string col in columns)
            table.AddColumn(new TableColumn($"[cyan]{Markup.Escape(text: col)}[/]"));

        foreach (string[] row in rows)
            table.AddRow(row.Select(selector: Markup.Escape).ToArray());

        AnsiConsole.Write(renderable: table);
    }

    internal static SpectreTable CreateResultTable(string title, string titleColor = "blue")
    {
        SpectreTable table = new()
        {
            Title = new TableTitle($"[bold {titleColor}]{Markup.Escape(text: title)}[/]"),
            Border = TableBorder.Rounded,
            ShowRowSeparators = true,
        };
        return table;
    }

    public static void AddResultColumn(SpectreTable table, string header) =>
        table.AddColumn($"[bold]{Markup.Escape(text: header)}[/]");

    public static void AddResultRow(SpectreTable table, params string[] cells)
    {
        string[] formatted = [.. cells.Select(c => $"[steelblue1]{Markup.Escape(text: c)}[/]")];
        table.AddRow(columns: formatted);
    }

    public static void Write(SpectreTable table) => AnsiConsole.Write(renderable: table);

    public static void Write(IRenderable renderable) => AnsiConsole.Write(renderable: renderable);

    public static void Render(IRenderable renderable) => AnsiConsole.Write(renderable: renderable);

    internal static Panel CreatePanel(string content, string header) =>
        new Panel(Markup.Escape(text: content)).Header(Markup.Escape(text: header)).Expand();

    internal static SpectreProgress CreateProgress() => AnsiConsole.Progress();

    public static void Section(string title) =>
        AnsiConsole.MarkupLine($"[bold white]{Markup.Escape(text: title)}[/]");

    public static void HelpUsage(string usage) =>
        AnsiConsole.MarkupLine($"  {Markup.Escape(text: usage)}");

    public static void HelpExample(string example) =>
        AnsiConsole.MarkupLine($"  {Markup.Escape(text: example)}");

    internal static SpectreTable CreateHelpTable()
    {
        var table = new SpectreTable().NoBorder().HideHeaders();
        table.AddColumn(new TableColumn(header: "").PadRight(right: 4));
        table.AddColumn(column: "");
        return table;
    }

    public static void AddHelpRow(SpectreTable table, string command, string description) =>
        table.AddRow($"[green]{Markup.Escape(text: command)}[/]", Markup.Escape(text: description));

    public static void HelpOption(
        string shortFlag,
        string longFlag,
        string? valuePlaceholder,
        string description,
        string[]? allowedValues = null,
        string? defaultValue = null
    )
    {
        string sig = IsNullOrEmpty(value: shortFlag)
            ? $"    --{longFlag}"
            : $"  -{shortFlag}, --{longFlag}";

        if (!IsNullOrEmpty(value: valuePlaceholder))
            sig += $" [cyan]<{valuePlaceholder.ToUpperInvariant()}>[/]";

        string desc = Markup.Escape(text: description);

        if (allowedValues is { Length: > 0 })
            desc += $" [dim]Allowed values are {Join(separator: ", ", value: allowedValues)}.[/]";

        if (!IsNullOrEmpty(value: defaultValue))
            desc += $" [dim][[default: {Markup.Escape(text: defaultValue)}]][/]";

        int rawSigLen = MarkupTagPattern.Replace(input: sig, replacement: "").Length;
        int padding = Math.Max(val1: 2, 44 - rawSigLen);

        AnsiConsole.MarkupLine($"[yellow]{sig}[/]{new string(c: ' ', count: padding)}{desc}");
    }

    public static void HelpFlag(
        string shortFlag,
        string longFlag,
        string description,
        bool defaultValue = false
    )
    {
        string sig = IsNullOrEmpty(value: shortFlag)
            ? $"    --{longFlag}"
            : $"  -{shortFlag}, --{longFlag}";

        string desc = Markup.Escape(text: description);
        desc += $" [dim][[default: {(defaultValue ? "True" : "False")}]][/]";

        int rawSigLen = MarkupTagPattern.Replace(input: sig, replacement: "").Length;
        int padding = Math.Max(val1: 2, 44 - rawSigLen);

        AnsiConsole.MarkupLine($"[yellow]{sig}[/]{new string(c: ' ', count: padding)}{desc}");
    }

    private static readonly Dictionary<LogLevel, string> LevelCodes = new()
    {
        [key: LogLevel.Debug] = "DEBG",
        [key: LogLevel.Info] = "INFO",
        [key: LogLevel.Success] = "OKAY",
        [key: LogLevel.Warning] = "WARN",
        [key: LogLevel.Error] = "ERRO",
        [key: LogLevel.Fatal] = "FATL",
    };

    private static void Write(LogLevel level, string color, string message, params object?[] args)
    {
        if (Suppress || Level > level)
            return;

        string formatted = args.Length > 0 ? Format(message: message, args: args) : message;
        var timestamp = DateTime.Now.ToString(format: "HH:mm:ss");
        string levelCode = LevelCodes[key: level];
        AnsiConsole.MarkupLine(
            $"[{color}][[{levelCode}]][/] [dim]{timestamp}:[/] {Markup.Escape(text: formatted)}"
        );
    }

    private static string Format(string message, object?[] args)
    {
        try
        {
            object?[] safeArgs = [.. args.Select(a => a ?? "null")];
            return string.Format(format: message, args: safeArgs);
        }
        catch
        {
            return message;
        }
    }
}
