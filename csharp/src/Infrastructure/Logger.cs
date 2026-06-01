namespace Scripts.Infrastructure;

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
		SessionId = Guid.NewGuid().ToString("N")[..8];
		CurrentSessionId = SessionId;

		CreateDirectory(Paths.LogDirectory);
		DetectCrashedSessions(service);

		Event(
			"SessionStart",
			new Dictionary<string, object>
			{
				["Service"] = service.ToString(),
				["ProcessId"] = ProcessId,
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

		var status = success ? "Completed" : "Failed";

		Dictionary<string, object> data = new()
		{
			["Status"] = status,
			["EndedAt"] = DateFormat.NowFull,
		};
		if (summary is { })
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
			["EndedAt"] = DateFormat.NowFull,
		};
		if (progress is { })
			data["Progress"] = progress;

		Event("SessionInterrupted", data, LogLevel.Warning);

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

	#endregion

	#region Private Helpers

	private static void DetectCrashedSessions(ServiceType service)
	{
		var logPath = GetLogPath(service);
		if (!File.Exists(logPath))
			return;

		Dictionary<string, string> openSessions = [];

		foreach (var line in ReadLines(logPath))
		{
			if (IsNullOrWhiteSpace(line))
				continue;

			LogEntry? entry = null;
			try
			{
				entry = JsonSerializer.Deserialize<LogEntry>(line, StateManager.JsonCompact);
			}
			catch (JsonException)
			{
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

		foreach ((var crashedId, var startTime) in openSessions)
		{
			Console.Warning("Detected crashed session {0} started at {1}", crashedId, startTime);

			AppendJsonLine(
				logPath,
				new LogEntry(
					DateFormat.NowFull,
					Level: "Error",
					Event: "SessionCrashed",
					SessionId: crashedId,
					new Dictionary<string, object>
					{
						["OriginalStart"] = startTime,
						["DetectedAt"] = DateFormat.NowFull,
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
			DateFormat.NowFull,
			level.ToString(),
			Event: eventName,
			SessionId: sessionId,
			data.Count > 0 ? data : null
		);

		AppendJsonLine(GetLogPath(service), entry);
	}

	internal static void AppendJsonLine(string path, LogEntry entry)
	{
		var json = JsonSerializer.Serialize(entry, StateManager.JsonCompact);
		lock (WriteLock)
		{
			AppendAllText(path, json + NewLine);
		}
	}

	internal static string GetLogPath(ServiceType service) =>
		Combine(
			Paths.LogDirectory,
			service switch
			{
				ServiceType.LastFm => "lastfm.jsonl",
				ServiceType.YouTube => "youtube.jsonl",
				ServiceType.Music => "musicbrainz.jsonl",
				ServiceType.Sheets => "sheets.jsonl",
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
