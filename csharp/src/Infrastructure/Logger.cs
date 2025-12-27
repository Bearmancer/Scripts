namespace CSharpScripts.Infrastructure;

public static class Logger
{
    #region Configuration & State

    private static readonly Lock WriteLock = new();

    private static ServiceType? ActiveService;
    private static string? SessionId;

    public static LogLevel FileLevel { get; set; } = LogLevel.Info;
    public static string? CurrentSessionId { get; private set; }

    #endregion

    #region Session Management

    public static void Start(ServiceType service)
    {
        ActiveService = service;
        SessionId = Guid.NewGuid().ToString(format: "N")[..8];
        CurrentSessionId = SessionId;

        CreateDirectory(path: Paths.LogDirectory);
        DetectCrashedSessions(service: service);

        Event(
            eventName: "SessionStart",
            new Dictionary<string, object>
            {
                [key: "Service"] = service.ToString(),
                [key: "ProcessId"] = ProcessId,
            }
        );
    }

    public static void End(bool success, string? summary = null, Exception? exception = null)
    {
        if (ActiveService == null || SessionId == null)
            return;

        if (exception is { })
        {
            Dictionary<string, object> exData = new()
            {
                [key: "Type"] = exception.GetType().Name,
                [key: "Message"] = exception.Message,
            };
            if (exception.InnerException is { } inner)
            {
                exData[key: "InnerType"] = inner.GetType().Name;
                exData[key: "InnerMessage"] = inner.Message;
            }
            if (exception is AggregateException aex)
                exData[key: "ErrorCount"] = aex.InnerExceptions.Count;
            Event(eventName: "Exception", data: exData, level: LogLevel.Error);
        }

        string status = success ? "Completed" : "Failed";

        Dictionary<string, object> data = new()
        {
            [key: "Status"] = status,
            [key: "EndedAt"] = DateTime.Now.ToString(format: "yyyy/MM/dd HH:mm:ss"),
        };
        if (summary is { })
            data[key: "Summary"] = summary;

        Event(eventName: "SessionEnd", data: data);

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
            [key: "Status"] = "Interrupted",
            [key: "EndedAt"] = DateTime.Now.ToString(format: "yyyy/MM/dd HH:mm:ss"),
        };
        if (progress is { })
            data[key: "Progress"] = progress;

        Event(eventName: "SessionInterrupted", data: data, level: LogLevel.Warning);

        ActiveService = null;
        SessionId = null;
        CurrentSessionId = null;
    }

    #endregion

    #region Core Logging

    public static void Event(
        string eventName,
        Dictionary<string, object>? data = null,
        LogLevel level = LogLevel.Info
    )
    {
        if (ActiveService == null || FileLevel > level)
            return;

        WriteJsonEntry(
            service: ActiveService.Value,
            level: level,
            eventName: eventName,
            data ?? [],
            sessionId: SessionId
        );
    }

    public static void Message(LogLevel level, string text)
    {
        if (ActiveService == null || FileLevel > level)
            return;

        WriteJsonEntry(
            service: ActiveService.Value,
            level: level,
            eventName: "Message",
            new Dictionary<string, object> { [key: "Text"] = text },
            sessionId: SessionId
        );
    }

    #endregion

    #region Domain Events

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
            [key: "Title"] = title,
            [key: "Added"] = added,
            [key: "Removed"] = removed,
        };

        if (addedTitles?.Count > 0)
            data[key: "AddedVideos"] = addedTitles;
        if (removedTitles?.Count > 0)
            data[key: "RemovedVideos"] = removedTitles;
        if (removedVideoIds?.Count > 0)
            data[key: "RemovedVideoUrls"] = removedVideoIds
                .Select(id => $"https://www.youtube.com/watch?v={id}")
                .ToList();

        Event(eventName: "PlaylistUpdated", data: data);
    }

    public static void PlaylistRenamed(string oldTitle, string newTitle) =>
        Event(
            eventName: "PlaylistRenamed",
            new Dictionary<string, object>
            {
                [key: "OldTitle"] = oldTitle,
                [key: "NewTitle"] = newTitle,
            }
        );

    public static void PlaylistDeleted(string title, int videoCount) =>
        Event(
            eventName: "PlaylistDeleted",
            new Dictionary<string, object> { [key: "Title"] = title, [key: "Videos"] = videoCount }
        );

    public static void ScrobblesProcessed(int fetched, int written) =>
        Event(
            eventName: "ScrobblesProcessed",
            new Dictionary<string, object>
            {
                [key: "Fetched"] = fetched,
                [key: "Written"] = written,
            }
        );

    public static void ApiError(string api, string error, int? statusCode = null)
    {
        Dictionary<string, object> data = new() { [key: "Api"] = api, [key: "Error"] = error };
        if (statusCode.HasValue)
            data[key: "StatusCode"] = statusCode.Value;

        Event(eventName: "ApiError", data: data, level: LogLevel.Error);
    }

    public static void NetworkError(string operation, string error) =>
        Event(
            eventName: "NetworkError",
            new Dictionary<string, object>
            {
                [key: "Operation"] = operation,
                [key: "Error"] = error,
            },
            level: LogLevel.Error
        );

    #endregion

    #region Private Helpers

    private static void DetectCrashedSessions(ServiceType service)
    {
        string logPath = GetLogPath(service: service);
        if (!File.Exists(path: logPath))
            return;

        Dictionary<string, string> openSessions = [];

        foreach (string line in ReadLines(path: logPath))
        {
            if (IsNullOrWhiteSpace(value: line))
                continue;

            LogEntry? entry = null;
            try
            {
                entry = JsonSerializer.Deserialize<LogEntry>(
                    json: line,
                    options: StateManager.JsonCompact
                );
            }
            catch
            {
                continue;
            }

            if (entry?.SessionId is not { } sessionId)
                continue;

            _ = entry.Event switch
            {
                "SessionStart" => openSessions[key: sessionId] = entry.Timestamp,
                "SessionEnd" or "SessionInterrupted" or "SessionCrashed" => openSessions.Remove(
                    key: sessionId
                )
                    ? null
                    : null,
                _ => null,
            };
        }

        foreach ((string crashedId, string startTime) in openSessions)
        {
            Console.Warning(
                message: "Detected crashed session {0} started at {1}",
                crashedId,
                startTime
            );

            AppendJsonLine(
                path: logPath,
                new LogEntry(
                    DateTime.Now.ToString(format: "yyyy/MM/dd HH:mm:ss"),
                    Level: "Error",
                    Event: "SessionCrashed",
                    SessionId: crashedId,
                    new Dictionary<string, object>
                    {
                        [key: "OriginalStart"] = startTime,
                        [key: "DetectedAt"] = DateTime.Now.ToString(format: "yyyy/MM/dd HH:mm:ss"),
                    }
                )
            );
        }
    }

    private static void WriteJsonEntry(
        ServiceType service,
        LogLevel level,
        string eventName,
        Dictionary<string, object> data,
        string? sessionId
    )
    {
        LogEntry entry = new(
            DateTime.Now.ToString(format: "yyyy/MM/dd HH:mm:ss"),
            level.ToString(),
            Event: eventName,
            SessionId: sessionId,
            data.Count > 0 ? data : null
        );

        AppendJsonLine(GetLogPath(service: service), entry: entry);
    }

    internal static void AppendJsonLine(string path, LogEntry entry)
    {
        string json = JsonSerializer.Serialize(value: entry, options: StateManager.JsonCompact);
        lock (WriteLock)
        {
            AppendAllText(path: path, json + NewLine);
        }
    }

    internal static string GetLogPath(ServiceType service) =>
        Combine(
            path1: Paths.LogDirectory,
            service switch
            {
                ServiceType.LastFm => "lastfm.jsonl",
                ServiceType.YouTube => "youtube.jsonl",
                ServiceType.Music => "musicbrainz.jsonl",
                _ => "general.jsonl",
            }
        );

    #endregion
}

#region Supporting Types

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
    Music,
}

public record LogEntry(
    string Timestamp,
    string Level,
    string Event,
    string? SessionId = null,
    Dictionary<string, object>? Data = null
);

#endregion
