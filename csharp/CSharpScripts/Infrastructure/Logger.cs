namespace CSharpScripts.Infrastructure;

internal enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
}

internal enum ServiceType
{
    LastFm,
    YouTube,
    Sheets,
}

internal static class Logger
{
    static readonly string ProjectRoot = GetDirectoryName(
        GetDirectoryName(GetDirectoryName(GetDirectoryName(AppContext.BaseDirectory)!)!)!
    )!;
    static readonly string LogDirectory = Combine(ProjectRoot, "logs");

    static readonly Lock WriteLock = new();

    internal static LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;
    internal static ServiceType? ActiveService { get; set; }

    internal static void Info(string message, params object?[] args) =>
        Log(level: LogLevel.Info, label: "Info", color: "blue", message: message, args: args);

    internal static void Warning(string message, params object?[] args) =>
        Log(
            level: LogLevel.Warning,
            label: "Warning",
            color: "yellow",
            message: message,
            args: args
        );

    internal static void Error(string message, params object?[] args) =>
        Log(
            level: LogLevel.Error,
            label: "Error",
            color: "red",
            message: message,
            args: args,
            filePrefix: "ERROR: "
        );

    internal static void Debug(string message, params object?[] args) =>
        Log(
            level: LogLevel.Debug,
            label: "Debug",
            color: "grey",
            message: message,
            args: args,
            writeToFile: false
        );

    internal static void Success(string message, params object?[] args) =>
        Log(level: LogLevel.Info, label: "Success", color: "green", message: message, args: args);

    internal static void Link(string url, string text)
    {
        if (CurrentLogLevel > LogLevel.Info)
            return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var escapedUrl = Markup.Escape(url);
        AnsiConsole.MarkupLine(
            $"[blue][[Info]][/] [dim]{timestamp}:[/] {Markup.Escape(text)}: [link={escapedUrl}]{escapedUrl}[/]"
        );
    }

    internal static void NewLine() => AnsiConsole.WriteLine();

    internal static void Progress(string message, params object?[] args)
    {
        if (CurrentLogLevel > LogLevel.Info)
            return;

        var formatted = args.Length > 0 ? Format(message, args) : message;
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        AnsiConsole.Markup(
            $"\r[cyan][[Progress]][/] [dim]{timestamp}:[/] {Markup.Escape(formatted)}"
        );
    }

    internal static void Start(ServiceType service)
    {
        ActiveService = service;
        WriteToFile("--- Session started ---");
    }

    internal static void End(bool success, string? summary = null)
    {
        if (ActiveService == null)
            return;

        var status = success ? "completed successfully" : "ended with errors";
        WriteToFile(summary != null ? $"Session {status}: {summary}" : $"Session {status}");
        WriteToFile("");
        ActiveService = null;
    }

    internal static void Interrupted(string? progress = null)
    {
        if (ActiveService == null)
            return;

        WriteToFile(
            progress != null ? $"Session interrupted: {progress}" : "Session interrupted by user"
        );
        WriteToFile("");
        ActiveService = null;
    }

    internal static void FileError(string message, Exception? ex = null)
    {
        WriteToFile($"ERROR: {message}");
        if (ex is null)
            return;

        WriteToFile($"  Type: {ex.GetType().FullName}");
        WriteToFile($"  Message: {ex.Message}");

        if (ex.InnerException is not null)
        {
            WriteToFile(
                $"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}"
            );
        }

        if (ex.StackTrace is not null)
        {
            WriteToFile("  Stack Trace:");
            foreach (var line in ex.StackTrace.Split('\n').Take(10))
                WriteToFile($"    {line.Trim()}");
        }
    }

    static void Log(
        LogLevel level,
        string label,
        string color,
        string message,
        object?[] args,
        bool writeToFile = true,
        string? filePrefix = null
    )
    {
        if (CurrentLogLevel > level)
            return;

        var formatted = args.Length > 0 ? Format(message, args) : message;
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        AnsiConsole.MarkupLine(
            $"[{color}][[{label}]][/] [dim]{timestamp}:[/] {Markup.Escape(formatted)}"
        );

        if (writeToFile)
            WriteToFile((filePrefix ?? "") + formatted);
    }

    static void WriteToFile(string message)
    {
        if (ActiveService is not ServiceType service)
            return;

        CreateDirectory(LogDirectory);

        var logLine = $"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}] {message}";
        var logPath = Combine(
            LogDirectory,
            service switch
            {
                ServiceType.LastFm => "lastfm.log",
                ServiceType.YouTube => "youtube.log",
                ServiceType.Sheets => "sheets.log",
                _ => "general.log",
            }
        );

        lock (WriteLock)
            AppendAllText(logPath, logLine + Environment.NewLine);
    }
}
