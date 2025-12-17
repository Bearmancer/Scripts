namespace CSharpScripts.Infrastructure;

public static class Console
{
    public static LogLevel Level { get; set; } = LogLevel.Info;
    public static bool Suppress { get; set; }

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

        string formatted = args.Length > 0 ? Format(message, args) : message;
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        AnsiConsole.MarkupLine(
            $"[cyan][[PROG]][/] [dim]{timestamp}:[/] {Markup.Escape(formatted)}"
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

    /// <summary>
    /// Display a labeled field with proper escaping. Example: "Release:  Album Name"
    /// </summary>
    public static void Field(string label, string? value, int labelWidth = 12)
    {
        string paddedLabel = label.PadRight(labelWidth);
        string safeValue = Markup.Escape(value ?? "");
        AnsiConsole.MarkupLine($"[bold]{paddedLabel}[/] {safeValue}");
    }

    /// <summary>
    /// Display a labeled field only if value is not null/empty.
    /// </summary>
    public static void FieldIfPresent(string label, string? value, int labelWidth = 12)
    {
        if (!IsNullOrEmpty(value))
            Field(label, value, labelWidth);
    }

    /// <summary>
    /// Wrapper for AnsiConsole.Confirm with escaped prompt.
    /// </summary>
    public static bool Confirm(string prompt, bool defaultValue = true) =>
        AnsiConsole.Confirm(Markup.Escape(prompt), defaultValue);

    /// <summary>
    /// Wrapper for AnsiConsole.Prompt.
    /// </summary>
    public static T Prompt<T>(IPrompt<T> prompt) => AnsiConsole.Prompt(prompt);

    /// <summary>
    /// Create a Status context for spinner display.
    /// </summary>
    public static Status Status() => AnsiConsole.Status();

    /// <summary>
    /// Create a Live display for real-time updates.
    /// </summary>
    public static LiveDisplay Live(IRenderable target) => AnsiConsole.Live(target);

    // ═══════════════════════════════════════════════════════════════════════════
    // Markup Helpers - Use these instead of raw [color]...[/] syntax
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Escape text for safe use in Spectre markup.
    /// </summary>
    public static string Escape(string? text) => Markup.Escape(text ?? "");

    /// <summary>
    /// Format text as bold with escaping.
    /// </summary>
    public static string Bold(string? text) => $"[bold]{Markup.Escape(text ?? "")}[/]";

    /// <summary>
    /// Format text as dim with escaping.
    /// </summary>
    public static string DimText(string? text) => $"[dim]{Markup.Escape(text ?? "")}[/]";

    /// <summary>
    /// Format text with specified color (escapes content).
    /// </summary>
    public static string Colored(string color, string? text) =>
        $"[{color}]{Markup.Escape(text ?? "")}[/]";

    /// <summary>
    /// Format a source badge (Discogs=yellow, MusicBrainz=cyan).
    /// </summary>
    public static string SourceBadge(string source) =>
        source.Equals("Discogs", StringComparison.OrdinalIgnoreCase)
            ? "[yellow]Discogs[/]"
            : "[cyan]MusicBrainz[/]";

    /// <summary>
    /// Build a progress bar string. Example: [5/10] ██████████░░░░░░░░░░ 50% │ ETA: 1:30
    /// </summary>
    public static string ProgressBar(int completed, int total, string? eta = null)
    {
        double percent = total > 0 ? (double)completed / total * 100 : 0;
        int filled = (int)(percent / 5);
        string bar = new string('█', filled) + new string('░', 20 - filled);
        string etaPart = eta is not null ? $" │ ETA: [cyan]{eta}[/]" : "";
        return $"[blue][[{completed}/{total}]][/] {bar} [yellow]{percent:F0}%[/]{etaPart}";
    }

    /// <summary>
    /// Format a checkmark item. Example: ✓ Item description
    /// </summary>
    public static string CheckItem(string text) => $"[green]✓[/] {Markup.Escape(text)}";

    /// <summary>
    /// Create a Markup renderable for progress display.
    /// </summary>
    public static Markup ProgressMarkup(int completed, int total, string? eta = null) =>
        new(ProgressBar(completed, total, eta));

    /// <summary>
    /// Create a Markup renderable for a checkmark item.
    /// </summary>
    public static Markup CheckItemMarkup(string text) => new($"  {CheckItem(text)}");

    /// <summary>
    /// Format a task description for Spectre Progress with cyan highlighted title.
    /// </summary>
    public static string TaskTitle(string title) => Colored("cyan", title);

    /// <summary>
    /// Format a progress task description with count prefix, title, and suffix.
    /// Example: (1/5) Playlist Name (0/100 videos)
    /// </summary>
    public static string TaskDescription(string? prefix, string title, string? suffix = null)
    {
        string result = "";
        if (!IsNullOrEmpty(prefix))
            result += DimText(prefix) + " ";
        result += Colored("cyan", title);
        if (!IsNullOrEmpty(suffix))
            result += " " + DimText(suffix);
        return result;
    }

    public static void Link(string url, string text)
    {
        if (Suppress || Level > LogLevel.Info)
            return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string escaped = Markup.Escape(url);
        AnsiConsole.MarkupLine(
            $"[blue][[INFO]][/] [dim]{timestamp}:[/] {Markup.Escape(text)}: [link={escaped}]{escaped}[/]"
        );
        NewLine();
    }

    public static void Link(int number, string url, int maxLength = 80)
    {
        string truncated = url.Length <= maxLength ? url : url[..(maxLength - 3)] + "...";
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

        foreach (string col in columns)
            table.AddColumn(new TableColumn($"[cyan]{Markup.Escape(col)}[/]"));

        foreach (string[] row in rows)
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
        string[] formatted = [.. cells.Select(c => $"[steelblue1]{Markup.Escape(c)}[/]")];
        table.AddRow(formatted);
    }

    public static void Write(SpectreTable table) => AnsiConsole.Write(table);

    public static void Write(Spectre.Console.Rendering.IRenderable renderable) =>
        AnsiConsole.Write(renderable);

    internal static Panel CreatePanel(string content, string header) =>
        new Panel(Markup.Escape(content)).Header(Markup.Escape(header)).Expand();

    internal static SpectreProgress CreateProgress() => AnsiConsole.Progress();

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
        string sig = IsNullOrEmpty(shortFlag)
            ? $"    --{longFlag}"
            : $"  -{shortFlag}, --{longFlag}";

        if (!IsNullOrEmpty(valuePlaceholder))
            sig += $" [cyan]<{valuePlaceholder.ToUpperInvariant()}>[/]";

        string desc = Markup.Escape(description);

        if (allowedValues is { Length: > 0 })
            desc += $" [dim]Allowed values are {Join(", ", allowedValues)}.[/]";

        if (!IsNullOrEmpty(defaultValue))
            desc += $" [dim][[default: {Markup.Escape(defaultValue)}]][/]";

        int rawSigLen = Regex.Replace(sig, @"\[/?[^\]]+\]", "").Length;
        int padding = Math.Max(2, 44 - rawSigLen);

        AnsiConsole.MarkupLine($"[yellow]{sig}[/]{new string(' ', padding)}{desc}");
    }

    public static void HelpFlag(
        string shortFlag,
        string longFlag,
        string description,
        bool defaultValue = false
    )
    {
        string sig = IsNullOrEmpty(shortFlag)
            ? $"    --{longFlag}"
            : $"  -{shortFlag}, --{longFlag}";

        string desc = Markup.Escape(description);
        desc += $" [dim][[default: {(defaultValue ? "True" : "False")}]][/]";

        int rawSigLen = Regex.Replace(sig, @"\[/?[^\]]+\]", "").Length;
        int padding = Math.Max(2, 44 - rawSigLen);

        AnsiConsole.MarkupLine($"[yellow]{sig}[/]{new string(' ', padding)}{desc}");
    }

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

        string formatted = args.Length > 0 ? Format(message, args) : message;
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string levelCode = LevelCodes[level];
        AnsiConsole.MarkupLine(
            $"[{color}][[{levelCode}]][/] [dim]{timestamp}:[/] {Markup.Escape(formatted)}"
        );
    }

    private static string Format(string message, object?[] args)
    {
        try
        {
            object?[] safeArgs = [.. args.Select(a => a ?? "null")];
            return string.Format(message, safeArgs);
        }
        catch
        {
            return message;
        }
    }
}
