using System.Text.Json;

namespace CSharpScripts.Infrastructure;

internal enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal,
}

internal enum ServiceType
{
    LastFm,
    YouTube,
    Sheets,
}

internal record LogEntry(
    string Timestamp,
    string Level,
    string Event,
    string? SessionId = null,
    Dictionary<string, object>? Data = null
);

internal static class Logger
{
    static readonly Lock WriteLock = new();
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    internal static LogLevel ConsoleLevel { get; set; } = LogLevel.Info;
    internal static LogLevel FileLevel { get; set; } = LogLevel.Info;
    internal static bool SuppressConsole { get; set; }
    internal static string? CurrentSessionId { get; private set; }

    static ServiceType? ActiveService;
    static string? SessionId;

    internal static void Debug(string message, params object?[] args) =>
        Log(LogLevel.Debug, message, args);

    internal static void Info(string message, params object?[] args) =>
        Log(LogLevel.Info, message, args);

    internal static void Warning(string message, params object?[] args) =>
        Log(LogLevel.Warning, message, args);

    internal static void Error(string message, params object?[] args) =>
        Log(LogLevel.Error, message, args);

    internal static void Fatal(string message, params object?[] args) =>
        Log(LogLevel.Fatal, message, args);

    internal static void Success(string message, params object?[] args)
    {
        var formatted = args.Length > 0 ? Format(message, args) : message;
        WriteConsole(LogLevel.Info, "green", formatted);
    }

    internal static void Progress(string message, params object?[] args)
    {
        if (ConsoleLevel > LogLevel.Info || SuppressConsole)
            return;

        var formatted = args.Length > 0 ? Format(message, args) : message;
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        AnsiConsole.MarkupLine(
            $"[cyan][[Progress]][/] [dim]{timestamp}:[/] {Markup.Escape(formatted)}"
        );
    }

    internal static void Link(string url, string text)
    {
        if (ConsoleLevel > LogLevel.Info || SuppressConsole)
            return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var escaped = Markup.Escape(url);
        AnsiConsole.MarkupLine(
            $"[blue][[Info]][/] [dim]{timestamp}:[/] {Markup.Escape(text)}: [link={escaped}]{escaped}[/]"
        );
    }

    internal static void NewLine() => AnsiConsole.WriteLine();

    internal static void Start(ServiceType service)
    {
        ActiveService = service;
        SessionId = Guid.NewGuid().ToString("N")[..8];
        CurrentSessionId = SessionId;

        CreateDirectory(Paths.LogDirectory);
        HandleStaleLock(service);
        CreateLockFile(service);

        WriteJsonEntry(
            LogLevel.Info,
            "SessionStart",
            new()
            {
                ["Service"] = service.ToString(),
                ["Machine"] = Environment.MachineName,
                ["ProcessId"] = Environment.ProcessId,
            }
        );
        Log(LogLevel.Info, "{0} sync started", service);
    }

    internal static void End(bool success, string? summary = null)
    {
        if (ActiveService == null || SessionId == null)
            return;

        var status = success ? "Completed" : "Failed";

        Dictionary<string, object> data = new()
        {
            ["Status"] = status,
            ["Summary"] = summary ?? string.Empty,
            ["EndedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        };
        if (summary == null)
            data.Remove("Summary");

        Log(
            LogLevel.Info,
            "{0} sync {1}{2}",
            ActiveService,
            status.ToLowerInvariant(),
            summary != null ? $": {summary}" : ""
        );
        WriteJsonEntry(LogLevel.Info, "SessionEnd", data);
        DeleteLockFile(ActiveService.Value);
        PrintLogLocation(GetLogPath(ActiveService.Value));

        ActiveService = null;
        SessionId = null;
        CurrentSessionId = null;
    }

    internal static void Interrupted(string? progress = null)
    {
        if (ActiveService == null || SessionId == null)
            return;

        Dictionary<string, object> data = new()
        {
            ["Status"] = "Interrupted",
            ["Progress"] = progress ?? string.Empty,
            ["EndedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        };
        if (progress == null)
            data.Remove("Progress");

        Log(
            LogLevel.Warning,
            "{0} sync interrupted{1}",
            ActiveService,
            progress != null ? $": {progress}" : ""
        );
        WriteJsonEntry(LogLevel.Warning, "SessionInterrupted", data);
        DeleteLockFile(ActiveService.Value);
        PrintLogLocation(GetLogPath(ActiveService.Value));

        ActiveService = null;
        SessionId = null;
        CurrentSessionId = null;
    }

    internal static void PlaylistCreated(string title, int videoCount)
    {
        Log(LogLevel.Info, "Created playlist \"{0}\" with {1} videos", title, videoCount);
        WriteJsonEntry(
            LogLevel.Info,
            "PlaylistCreated",
            new() { ["Title"] = title, ["Videos"] = videoCount }
        );
    }

    internal static void PlaylistUpdated(
        string title,
        int added,
        int removed,
        List<string>? addedTitles = null,
        List<string>? removedTitles = null
    )
    {
        Log(LogLevel.Info, "Updated \"{0}\": +{1} -{2}", title, added, removed);

        Dictionary<string, object> data = new()
        {
            ["Title"] = title,
            ["Added"] = added,
            ["Removed"] = removed,
        };
        if (addedTitles?.Count > 0)
            data["AddedVideos"] = addedTitles;
        if (removedTitles?.Count > 0)
            data["RemovedVideos"] = removedTitles;

        WriteJsonEntry(LogLevel.Info, "PlaylistUpdated", data);

        if (addedTitles != null)
            foreach (var video in addedTitles)
                Log(LogLevel.Debug, "  + {0}", video);

        if (removedTitles != null)
            foreach (var video in removedTitles)
                Log(LogLevel.Debug, "  - {0}", video);
    }

    internal static void PlaylistRenamed(string oldTitle, string newTitle)
    {
        Log(LogLevel.Info, "Renamed \"{0}\" → \"{1}\"", oldTitle, newTitle);
        WriteJsonEntry(
            LogLevel.Info,
            "PlaylistRenamed",
            new() { ["OldTitle"] = oldTitle, ["NewTitle"] = newTitle }
        );
    }

    internal static void PlaylistDeleted(string title, int videoCount)
    {
        Log(LogLevel.Info, "Deleted playlist \"{0}\" ({1} videos)", title, videoCount);
        WriteJsonEntry(
            LogLevel.Info,
            "PlaylistDeleted",
            new() { ["Title"] = title, ["Videos"] = videoCount }
        );
    }

    internal static void PlaylistSkipped(string title) =>
        Log(LogLevel.Debug, "Skipped \"{0}\" (no changes)", title);

    internal static void ScrobblesProcessed(int fetched, int written)
    {
        Log(LogLevel.Info, "Processed {0} scrobbles, wrote {1} new", fetched, written);
        WriteJsonEntry(
            LogLevel.Info,
            "ScrobblesProcessed",
            new() { ["Fetched"] = fetched, ["Written"] = written }
        );
    }

    internal static void ApiError(string api, string error, int? statusCode = null)
    {
        Log(LogLevel.Error, "{0} API error: {1}", api, error);

        Dictionary<string, object> data = new() { ["Api"] = api, ["Error"] = error };
        if (statusCode.HasValue)
            data["StatusCode"] = statusCode.Value;

        WriteJsonEntry(LogLevel.Error, "ApiError", data);
    }

    internal static void NetworkError(string operation, string error)
    {
        Log(LogLevel.Error, "Network error during {0}: {1}", operation, error);
        WriteJsonEntry(
            LogLevel.Error,
            "NetworkError",
            new() { ["Operation"] = operation, ["Error"] = error }
        );
    }

    static void HandleStaleLock(ServiceType service)
    {
        var lockPath = GetLockPath(service);
        if (!File.Exists(lockPath))
            return;

        var raw = File.ReadAllText(lockPath);
        var parts = raw.Split('|');
        var crashedSession = parts.Length > 0 ? parts[0] : "unknown";
        var startedAt = parts.Length > 1 ? parts[1] : "unknown";

        Warning("Detected crashed session {0} started at {1}", crashedSession, startedAt);

        WriteJsonEntryForService(
            service,
            LogLevel.Error,
            "SessionCrashed",
            new()
            {
                ["OriginalStart"] = startedAt,
                ["DetectedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            },
            crashedSession
        );

        DeleteLockFile(service);
    }

    static void CreateLockFile(ServiceType service)
    {
        var lockPath = GetLockPath(service);
        var content = SessionId + "|" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        File.WriteAllText(lockPath, content);
    }

    static void DeleteLockFile(ServiceType service)
    {
        var lockPath = GetLockPath(service);
        if (File.Exists(lockPath))
            File.Delete(lockPath);
    }

    static void Log(LogLevel level, string message, params object?[] args)
    {
        var formatted = args.Length > 0 ? Format(message, args) : message;

        if (ConsoleLevel <= level)
            WriteConsole(level, GetColor(level), formatted);

        if (ActiveService != null && FileLevel <= level)
            WriteJsonEntry(level, "Message", new() { ["Text"] = formatted });
    }

    static void WriteConsole(LogLevel level, string color, string message)
    {
        if (SuppressConsole)
            return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        AnsiConsole.MarkupLine(
            $"[{color}][[{level}]][/] [dim]{timestamp}:[/] {Markup.Escape(message)}"
        );
    }

    static void WriteJsonEntry(LogLevel level, string eventName, Dictionary<string, object> data)
    {
        if (ActiveService == null || FileLevel > level)
            return;

        WriteJsonEntryForService(ActiveService.Value, level, eventName, data, SessionId);
    }

    static void WriteJsonEntryForService(
        ServiceType service,
        LogLevel level,
        string eventName,
        Dictionary<string, object> data,
        string? sessionId
    )
    {
        var entry = new LogEntry(
            Timestamp: DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Level: level.ToString(),
            Event: eventName,
            SessionId: sessionId,
            Data: data.Count > 0 ? data : null
        );

        AppendJsonArray(GetLogPath(service), entry);
    }

    static void AppendJsonArray(string path, LogEntry entry)
    {
        lock (WriteLock)
        {
            List<LogEntry> entries = [];
            if (File.Exists(path))
            {
                using var existing = File.OpenRead(path);
                entries = JsonSerializer.Deserialize<List<LogEntry>>(existing, JsonOptions) ?? [];
            }

            entries.Add(entry);

            var tempPath = path + ".tmp";
            using var tempStream = File.Create(tempPath);
            JsonSerializer.Serialize(tempStream, entries, JsonOptions);
            tempStream.Flush(true);
            File.Move(tempPath, path, true);
        }
    }

    static string GetLogPath(ServiceType service) =>
        Combine(
            Paths.LogDirectory,
            service switch
            {
                ServiceType.LastFm => "lastfm.json",
                ServiceType.YouTube => "youtube.json",
                _ => "general.json",
            }
        );

    static string GetLockPath(ServiceType service) =>
        Combine(
            Paths.LogDirectory,
            service switch
            {
                ServiceType.LastFm => "lastfm.lock",
                ServiceType.YouTube => "youtube.lock",
                _ => "general.lock",
            }
        );

    static string GetColor(LogLevel level) =>
        level switch
        {
            LogLevel.Debug => "grey",
            LogLevel.Info => "blue",
            LogLevel.Warning => "yellow",
            LogLevel.Error => "red",
            LogLevel.Fatal => "red bold",
            _ => "white",
        };

    static void PrintLogLocation(string path)
    {
        if (SuppressConsole)
            return;

        var uri = new Uri(path).AbsoluteUri;
        AnsiConsole.MarkupLine($"[dim]Log:[/] [link={uri}]{uri}[/]");
    }
}
