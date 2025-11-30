using System.Text.Json;
using System.Text.Json.Serialization;

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

internal record PlaylistAction(
    string PlaylistId,
    string Title,
    string Action,
    int? VideoCount = null,
    List<string>? AddedVideos = null,
    List<string>? RemovedVideos = null,
    string? RenamedFrom = null
);

internal record SessionLog(
    string Id,
    string Date,
    string Started,
    string? Ended,
    string Status,
    string? Summary,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ScrobblesFetched,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ScrobblesWritten,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? PlaylistsProcessed,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? PlaylistsSkipped,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        List<PlaylistAction>? Actions
);

internal record LogFile(List<SessionLog> Sessions);

internal static class Logger
{
    static readonly Lock StateLock = new();
    static readonly Lock WriteLock = new();
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    internal static LogLevel CurrentLogLevel
    {
        get
        {
            lock (StateLock)
                return _currentLogLevel;
        }
        set
        {
            lock (StateLock)
                _currentLogLevel = value;
        }
    }

    internal static bool SuppressConsole
    {
        get
        {
            lock (StateLock)
                return _suppressConsole;
        }
        set
        {
            lock (StateLock)
                _suppressConsole = value;
        }
    }

    static LogLevel _currentLogLevel = LogLevel.Info;
    static bool _suppressConsole;
    static ServiceType? _activeService;
    static string? _sessionId;
    static string? _sessionDate;
    static string? _sessionStartTime;
    static int _scrobblesFetched;
    static int _scrobblesWritten;
    static int _playlistsProcessed;
    static int _playlistsSkipped;
    static List<PlaylistAction> _playlistActions = [];

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
        Log(level: LogLevel.Error, label: "Error", color: "red", message: message, args: args);

    internal static void Debug(string message, params object?[] args) =>
        Log(level: LogLevel.Debug, label: "Debug", color: "grey", message: message, args: args);

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

    internal static void FileError(string message, Exception? ex = null)
    {
        CreateDirectory(Paths.LogDirectory);
        var errorLogPath = Combine(Paths.LogDirectory, "errors.log");
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var content =
            ex != null ? $"[{timestamp}] {message}\n{ex}\n\n" : $"[{timestamp}] {message}\n\n";
        File.AppendAllText(errorLogPath, content);
    }

    internal static void Progress(string message, params object?[] args)
    {
        if (CurrentLogLevel > LogLevel.Info)
            return;

        var formatted = args.Length > 0 ? Format(message, args) : message;
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        AnsiConsole.MarkupLine(
            $"\r[cyan][[Progress]][/] [dim]{timestamp}:[/] {Markup.Escape(formatted)}"
        );
    }

    internal static void Start(ServiceType service)
    {
        lock (StateLock)
        {
            _activeService = service;
            _sessionId = Guid.NewGuid().ToString("N")[..8];
            _sessionDate = DateTime.Now.ToString("yyyy-MM-dd");
            _sessionStartTime = DateTime.Now.ToString("HH:mm:ss");
            _scrobblesFetched = 0;
            _scrobblesWritten = 0;
            _playlistsProcessed = 0;
            _playlistsSkipped = 0;
            _playlistActions = [];
        }
    }

    internal static void RecordScrobblesFetched(int count)
    {
        lock (StateLock)
            _scrobblesFetched = count;
    }

    internal static void RecordScrobblesWritten(int count)
    {
        lock (StateLock)
            _scrobblesWritten = count;
    }

    internal static void RecordPlaylistProcessed()
    {
        lock (StateLock)
            _playlistsProcessed++;
    }

    internal static void RecordPlaylistSkipped()
    {
        lock (StateLock)
            _playlistsSkipped++;
    }

    internal static void RecordPlaylistCreated(string playlistId, string title, int videoCount)
    {
        lock (StateLock)
            _playlistActions.Add(
                new PlaylistAction(
                    PlaylistId: playlistId,
                    Title: title,
                    Action: "created",
                    VideoCount: videoCount
                )
            );
    }

    internal static void RecordPlaylistUpdated(
        string playlistId,
        string title,
        List<string>? addedVideos = null,
        List<string>? removedVideos = null
    )
    {
        lock (StateLock)
            _playlistActions.Add(
                new PlaylistAction(
                    PlaylistId: playlistId,
                    Title: title,
                    Action: "updated",
                    AddedVideos: addedVideos?.Count > 0 ? addedVideos : null,
                    RemovedVideos: removedVideos?.Count > 0 ? removedVideos : null
                )
            );
    }

    internal static void RecordPlaylistRenamed(string playlistId, string oldTitle, string newTitle)
    {
        lock (StateLock)
            _playlistActions.Add(
                new PlaylistAction(
                    PlaylistId: playlistId,
                    Title: newTitle,
                    Action: "renamed",
                    RenamedFrom: oldTitle
                )
            );
    }

    internal static void RecordPlaylistDeleted(string playlistId, string title, int videoCount)
    {
        lock (StateLock)
            _playlistActions.Add(
                new PlaylistAction(
                    PlaylistId: playlistId,
                    Title: title,
                    Action: "deleted",
                    VideoCount: videoCount
                )
            );
    }

    internal static void End(bool success, string? summary = null)
    {
        ServiceType? service;
        string? sessionId;
        string? sessionDate;
        string? startTime;
        int scrobblesFetched;
        int scrobblesWritten;
        int playlistsProcessed;
        int playlistsSkipped;
        List<PlaylistAction> actions;

        lock (StateLock)
        {
            service = _activeService;
            sessionId = _sessionId;
            sessionDate = _sessionDate;
            startTime = _sessionStartTime;
            scrobblesFetched = _scrobblesFetched;
            scrobblesWritten = _scrobblesWritten;
            playlistsProcessed = _playlistsProcessed;
            playlistsSkipped = _playlistsSkipped;
            actions = [.. _playlistActions];

            if (service == null || sessionId == null)
                return;
        }

        var endTime = DateTime.Now.ToString("HH:mm:ss");
        var status = success ? "completed" : "failed";

        var isLastFm = service == ServiceType.LastFm;
        var isYouTube = service == ServiceType.YouTube;

        WriteSessionLog(
            service: service.Value,
            session: new SessionLog(
                Id: sessionId,
                Date: sessionDate ?? DateTime.Now.ToString("yyyy-MM-dd"),
                Started: startTime ?? endTime,
                Ended: endTime,
                Status: status,
                Summary: summary,
                ScrobblesFetched: isLastFm && scrobblesFetched > 0 ? scrobblesFetched : null,
                ScrobblesWritten: isLastFm && scrobblesWritten > 0 ? scrobblesWritten : null,
                PlaylistsProcessed: isYouTube && playlistsProcessed > 0 ? playlistsProcessed : null,
                PlaylistsSkipped: isYouTube && playlistsSkipped > 0 ? playlistsSkipped : null,
                Actions: isYouTube && actions.Count > 0 ? actions : null
            )
        );

        lock (StateLock)
        {
            _activeService = null;
            _sessionId = null;
            _sessionDate = null;
            _sessionStartTime = null;
            _scrobblesFetched = 0;
            _scrobblesWritten = 0;
            _playlistsProcessed = 0;
            _playlistsSkipped = 0;
            _playlistActions = [];
        }
    }

    internal static void Interrupted(string? progress = null)
    {
        ServiceType? service;
        string? sessionId;
        string? sessionDate;
        string? startTime;
        int scrobblesFetched;
        int scrobblesWritten;
        int playlistsProcessed;
        int playlistsSkipped;
        List<PlaylistAction> actions;

        lock (StateLock)
        {
            service = _activeService;
            sessionId = _sessionId;
            sessionDate = _sessionDate;
            startTime = _sessionStartTime;
            scrobblesFetched = _scrobblesFetched;
            scrobblesWritten = _scrobblesWritten;
            playlistsProcessed = _playlistsProcessed;
            playlistsSkipped = _playlistsSkipped;
            actions = [.. _playlistActions];

            if (service == null || sessionId == null)
                return;
        }

        var endTime = DateTime.Now.ToString("HH:mm:ss");

        var isLastFm = service == ServiceType.LastFm;
        var isYouTube = service == ServiceType.YouTube;

        WriteSessionLog(
            service: service.Value,
            session: new SessionLog(
                Id: sessionId,
                Date: sessionDate ?? DateTime.Now.ToString("yyyy-MM-dd"),
                Started: startTime ?? endTime,
                Ended: endTime,
                Status: "interrupted",
                Summary: progress ?? "Session interrupted by user",
                ScrobblesFetched: isLastFm && scrobblesFetched > 0 ? scrobblesFetched : null,
                ScrobblesWritten: isLastFm && scrobblesWritten > 0 ? scrobblesWritten : null,
                PlaylistsProcessed: isYouTube && playlistsProcessed > 0 ? playlistsProcessed : null,
                PlaylistsSkipped: isYouTube && playlistsSkipped > 0 ? playlistsSkipped : null,
                Actions: isYouTube && actions.Count > 0 ? actions : null
            )
        );

        lock (StateLock)
        {
            _activeService = null;
            _sessionId = null;
            _sessionDate = null;
            _sessionStartTime = null;
            _scrobblesFetched = 0;
            _scrobblesWritten = 0;
            _playlistsProcessed = 0;
            _playlistsSkipped = 0;
            _playlistActions = [];
        }
    }

    static void Log(LogLevel level, string label, string color, string message, object?[] args)
    {
        if (CurrentLogLevel > level)
            return;

        var formatted = args.Length > 0 ? Format(message, args) : message;

        if (SuppressConsole)
            return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        AnsiConsole.MarkupLine(
            $"[{color}][[{label}]][/] [dim]{timestamp}:[/] {Markup.Escape(formatted)}"
        );
    }

    static void WriteSessionLog(ServiceType service, SessionLog session)
    {
        CreateDirectory(Paths.LogDirectory);

        var logPath = Combine(
            Paths.LogDirectory,
            service switch
            {
                ServiceType.LastFm => "lastfm.json",
                ServiceType.YouTube => "youtube.json",
                _ => "general.json",
            }
        );

        LogFile logFile;
        lock (WriteLock)
        {
            logFile = File.Exists(logPath)
                ? JsonSerializer.Deserialize<LogFile>(ReadAllText(logPath), JsonOptions)
                    ?? new LogFile([])
                : new LogFile([]);

            logFile.Sessions.Add(session);

            WriteAllText(logPath, JsonSerializer.Serialize(logFile, JsonOptions));
        }
    }
}
