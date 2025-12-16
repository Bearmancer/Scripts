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
