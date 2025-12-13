namespace CSharpScripts.Infrastructure;

public static class Logger
{
    private static readonly Lock WriteLock = new();

    public static LogLevel FileLevel { get; set; } = LogLevel.Info;
    public static string? CurrentSessionId { get; private set; }

    private static ServiceType? ActiveService;
    private static string? SessionId;

    // ═══════════════════════════════════════════════════════════════════════════
    // Session Management
    // ═══════════════════════════════════════════════════════════════════════════

    public static void Start(ServiceType service)
    {
        ActiveService = service;
        SessionId = Guid.NewGuid().ToString("N")[..8];
        CurrentSessionId = SessionId;

        CreateDirectory(Paths.LogDirectory);
        DetectCrashedSessions(service);

        Event(
            "SessionStart",
            new Dictionary<string, object>
            {
                ["Service"] = service.ToString(),
                ["ProcessId"] = Environment.ProcessId,
            }
        );
    }

    public static void End(bool success, string? summary = null, Exception? exception = null)
    {
        if (ActiveService == null || SessionId == null)
            return;

        if (exception is not null)
        {
            Dictionary<string, object> exData = new()
            {
                ["Type"] = exception.GetType().Name,
                ["Message"] = exception.Message,
            };
            if (exception.InnerException is { } inner)
            {
                exData["InnerType"] = inner.GetType().Name;
                exData["InnerMessage"] = inner.Message;
            }
            if (exception is AggregateException aex)
                exData["ErrorCount"] = aex.InnerExceptions.Count;
            Event("Exception", exData, LogLevel.Error);
        }

        string status = success ? "Completed" : "Failed";

        Dictionary<string, object> data = new()
        {
            ["Status"] = status,
            ["EndedAt"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
        };
        if (summary is not null)
            data["Summary"] = summary;

        Event("SessionEnd", data);

        ActiveService = null;
        SessionId = null;
        CurrentSessionId = null;
    }

    public static void Interrupted(string? progress = null)
    {
        if (ActiveService == null || SessionId == null)
            return;

        Dictionary<string, object> data = new()
        {
            ["Status"] = "Interrupted",
            ["EndedAt"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
        };
        if (progress is not null)
            data["Progress"] = progress;

        Event("SessionInterrupted", data, LogLevel.Warning);

        ActiveService = null;
        SessionId = null;
        CurrentSessionId = null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Structured Events
    // ═══════════════════════════════════════════════════════════════════════════

    public static void Event(
        string eventName,
        Dictionary<string, object>? data = null,
        LogLevel level = LogLevel.Info
    )
    {
        if (ActiveService == null || FileLevel > level)
            return;

        WriteJsonEntry(ActiveService.Value, level, eventName, data ?? [], SessionId);
    }

    public static void Message(LogLevel level, string text)
    {
        if (ActiveService == null || FileLevel > level)
            return;

        WriteJsonEntry(
            ActiveService.Value,
            level,
            "Message",
            new Dictionary<string, object> { ["Text"] = text },
            SessionId
        );
    }

    public static void PlaylistUpdated(
        string title,
        int added,
        int removed,
        List<string>? addedTitles = null,
        List<string>? removedTitles = null,
        List<string>? removedVideoIds = null
    )
    {
        if (added == 0 && removed == 0)
            return;

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
        if (removedVideoIds?.Count > 0)
            data["RemovedVideoUrls"] = removedVideoIds
                .Select(id => $"https://www.youtube.com/watch?v={id}")
                .ToList();

        Event("PlaylistUpdated", data);
    }

    public static void PlaylistRenamed(string oldTitle, string newTitle) =>
        Event(
            "PlaylistRenamed",
            new Dictionary<string, object> { ["OldTitle"] = oldTitle, ["NewTitle"] = newTitle }
        );

    public static void PlaylistDeleted(string title, int videoCount) =>
        Event(
            "PlaylistDeleted",
            new Dictionary<string, object> { ["Title"] = title, ["Videos"] = videoCount }
        );

    public static void ScrobblesProcessed(int fetched, int written) =>
        Event(
            "ScrobblesProcessed",
            new Dictionary<string, object> { ["Fetched"] = fetched, ["Written"] = written }
        );

    public static void ApiError(string api, string error, int? statusCode = null)
    {
        Dictionary<string, object> data = new() { ["Api"] = api, ["Error"] = error };
        if (statusCode.HasValue)
            data["StatusCode"] = statusCode.Value;

        Event("ApiError", data, LogLevel.Error);
    }

    public static void NetworkError(string operation, string error) =>
        Event(
            "NetworkError",
            new Dictionary<string, object> { ["Operation"] = operation, ["Error"] = error },
            LogLevel.Error
        );

    // ═══════════════════════════════════════════════════════════════════════════
    // Crash Detection
    // ═══════════════════════════════════════════════════════════════════════════

    private static void DetectCrashedSessions(ServiceType service)
    {
        string logPath = GetLogPath(service);
        if (!File.Exists(logPath))
            return;

        Dictionary<string, string> openSessions = [];

        foreach (string line in ReadLines(logPath))
        {
            if (IsNullOrWhiteSpace(line))
                continue;

            LogEntry? entry = null;
            try
            {
                entry = JsonSerializer.Deserialize<LogEntry>(line, StateManager.JsonCompact);
            }
            catch
            {
                // Intentionally skip malformed JSON lines - crash detection must be robust
                // to corrupted log files and should not fail the entire session startup
                continue;
            }

            if (entry?.SessionId is not { } sessionId)
                continue;

            _ = entry.Event switch
            {
                "SessionStart" => openSessions[sessionId] = entry.Timestamp,
                "SessionEnd" or "SessionInterrupted" or "SessionCrashed" => openSessions.Remove(
                    sessionId
                )
                    ? null
                    : null,
                _ => null,
            };
        }

        foreach ((string crashedId, string startTime) in openSessions)
        {
            Console.Warning("Detected crashed session {0} started at {1}", crashedId, startTime);

            AppendJsonLine(
                logPath,
                new LogEntry(
                    DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    "Error",
                    "SessionCrashed",
                    crashedId,
                    new Dictionary<string, object>
                    {
                        ["OriginalStart"] = startTime,
                        ["DetectedAt"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    }
                )
            );
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // File I/O
    // ═══════════════════════════════════════════════════════════════════════════

    private static void WriteJsonEntry(
        ServiceType service,
        LogLevel level,
        string eventName,
        Dictionary<string, object> data,
        string? sessionId
    )
    {
        LogEntry entry = new(
            DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
            level.ToString(),
            eventName,
            sessionId,
            data.Count > 0 ? data : null
        );

        AppendJsonLine(GetLogPath(service), entry);
    }

    private static void AppendJsonLine(string path, LogEntry entry)
    {
        string json = JsonSerializer.Serialize(entry, StateManager.JsonCompact);
        lock (WriteLock)
        {
            AppendAllText(path, json + NewLine);
        }
    }

    private static string GetLogPath(ServiceType service) =>
        Combine(
            Paths.LogDirectory,
            service switch
            {
                ServiceType.LastFm => "lastfm.jsonl",
                ServiceType.YouTube => "youtube.jsonl",
                _ => "general.jsonl",
            }
        );
}

// ═══════════════════════════════════════════════════════════════════════════
// Supporting Types
// ═══════════════════════════════════════════════════════════════════════════

public enum LogLevel
{
    Debug,
    Info,
    Success,
    Warning,
    Error,
    Fatal,
}

public enum ServiceType
{
    LastFm,
    YouTube,
    Sheets,
}

public record LogEntry(
    string Timestamp,
    string Level,
    string Event,
    string? SessionId = null,
    Dictionary<string, object>? Data = null
);
