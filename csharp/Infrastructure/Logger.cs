using System.Text.Json;

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
    static readonly Lock WriteLock = new();
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    internal static LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;
    internal static ServiceType? ActiveService { get; set; }
    internal static bool SuppressConsole { get; set; }
    static string? SessionId { get; set; }

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
            operation: "error"
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
        SessionId = Guid.NewGuid().ToString("N")[..8];
        WriteJsonLog(level: "Info", operation: "session_start", message: "Session started");
    }

    internal static void End(bool success, string? summary = null)
    {
        if (ActiveService == null)
            return;

        var status = success ? "completed" : "failed";
        WriteJsonLog(
            level: "Info",
            operation: "session_end",
            message: summary ?? "Session ended",
            data: new { status }
        );
        ActiveService = null;
        SessionId = null;
    }

    internal static void Interrupted(string? progress = null)
    {
        if (ActiveService == null)
            return;

        WriteJsonLog(
            level: "Warning",
            operation: "session_interrupted",
            message: progress ?? "Session interrupted by user"
        );
        ActiveService = null;
        SessionId = null;
    }

    internal static void FileError(string message, Exception? ex = null)
    {
        var data = ex is null
            ? null
            : new
            {
                exceptionType = ex.GetType().FullName,
                exceptionMessage = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace?.Split('\n').Take(5).Select(l => l.Trim()).ToArray(),
            };
        WriteJsonLog(level: "Error", operation: "file_error", message: message, data: data);
    }

    static void Log(
        LogLevel level,
        string label,
        string color,
        string message,
        object?[] args,
        bool writeToFile = true,
        string? operation = null
    )
    {
        if (CurrentLogLevel > level)
            return;

        var formatted = args.Length > 0 ? Format(message, args) : message;

        if (!SuppressConsole)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            AnsiConsole.MarkupLine(
                $"[{color}][[{label}]][/] [dim]{timestamp}:[/] {Markup.Escape(formatted)}"
            );
        }

        if (writeToFile)
            WriteJsonLog(level: label, operation: operation ?? "log", message: formatted);
    }

    static void WriteJsonLog(string level, string operation, string message, object? data = null)
    {
        if (ActiveService is not ServiceType service)
            return;

        CreateDirectory(Paths.LogDirectory);

        var logEntry = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTime.UtcNow.ToString("o"),
            ["level"] = level,
            ["service"] = service.ToString().ToLowerInvariant(),
            ["sessionId"] = SessionId,
            ["operation"] = operation,
            ["message"] = message,
        };

        if (data is not null)
            logEntry["data"] = data;

        var logPath = Combine(
            Paths.LogDirectory,
            service switch
            {
                ServiceType.LastFm => "lastfm.jsonl",
                ServiceType.YouTube => "youtube.jsonl",
                ServiceType.Sheets => "sheets.jsonl",
                _ => "general.jsonl",
            }
        );

        var json = JsonSerializer.Serialize(logEntry, JsonOptions);

        lock (WriteLock)
            AppendAllText(logPath, json + Environment.NewLine);
    }
}
