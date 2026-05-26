using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Serilog.Formatting.Compact;

namespace CSharpScripts.Core;

internal enum ServiceType
{
	LastFm,
	YouTube,
	Sheets,
	Music,
	Read,
	Cloud,
}

internal static class Log
{
	private static readonly FrozenDictionary<ServiceType, ILogger> ServiceLoggers;
	private static readonly AsyncLocal<ServiceType?> ActiveServiceLocal = new();

#pragma warning disable CA1810
	static Log()
	{
		ServiceLoggers = new Dictionary<ServiceType, ILogger>
		{
			[key: ServiceType.LastFm] = BuildServiceLogger(filename: "lastfm.jsonl"),
			[key: ServiceType.YouTube] = BuildServiceLogger(filename: "youtube.jsonl"),
			[key: ServiceType.Music] = BuildServiceLogger(filename: "music.jsonl"),
			[key: ServiceType.Sheets] = BuildServiceLogger(filename: "sheets.jsonl"),
			[key: ServiceType.Read] = BuildServiceLogger(filename: "read.jsonl"),
			[key: ServiceType.Cloud] = BuildServiceLogger(filename: "cloud.jsonl"),
		}.ToFrozenDictionary();
	}
#pragma warning restore CA1810

	private static ServiceType? ActiveService
	{
		get => ActiveServiceLocal.Value;
		set => ActiveServiceLocal.Value = value;
	}

	internal static ILogger ActiveLogger =>
		ActiveService.HasValue ? ServiceLoggers[key: ActiveService.Value] : Serilog.Log.Logger;

	internal static Logger BuildServiceLogger(string filename) =>
		new LoggerConfiguration()
			.MinimumLevel.Debug()
			.Enrich.FromLogContext()
			.Enrich.WithProcessId()
			.Enrich.WithThreadId()
			.Enrich.WithProperty(name: "Application", value: "CSharpScripts")
			.WriteTo.File(
				new CompactJsonFormatter(),
				Path.Combine(path1: Paths.LogDirectory, path2: filename),
				rollingInterval: RollingInterval.Infinite,
				shared: true
			)
			.WriteTo.Console(
				outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
			)
			.CreateLogger();

	internal static Logger BuildAppLogger(string filename) =>
		new LoggerConfiguration()
			.MinimumLevel.Debug()
			.Enrich.FromLogContext()
			.Enrich.WithProcessId()
			.Enrich.WithThreadId()
			.Enrich.WithProperty(name: "Application", value: "CSharpScripts")
			.WriteTo.File(
				new CompactJsonFormatter(),
				Path.Combine(path1: Paths.LogDirectory, path2: filename),
				rollingInterval: RollingInterval.Day,
				retainedFileCountLimit: 30,
				shared: true
			)
			.WriteTo.Console(
				outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
			)
			.CreateLogger();

	public static ILogger ForService(ServiceType service) => ServiceLoggers[key: service];

	public static IDisposable BeginSession(ServiceType service)
	{
		ActiveService = service;
		var sessionId = Guid.NewGuid().ToString(format: "N")[..8];
		IDisposable scope = LogContext.Push(
			new PropertyEnricher(name: "SessionId", value: sessionId),
			new PropertyEnricher(name: "Service", service.ToString())
		);
		ActiveLogger.Information(
			messageTemplate: "SessionStart {SessionId}",
			propertyValue: sessionId
		);
		Ui.Starting(message: "{0} session started", service);
		return new SessionScope(service: service, logScope: scope, () => ActiveService = null);
	}

	public static void Debug(string messageTemplate, params object?[] args) =>
		ActiveLogger.Debug(messageTemplate: messageTemplate, propertyValues: args);

	public static void Information(string messageTemplate, params object?[] args) =>
		ActiveLogger.Information(messageTemplate: messageTemplate, propertyValues: args);

	public static void Warning(string messageTemplate, params object?[] args) =>
		ActiveLogger.Warning(messageTemplate: messageTemplate, propertyValues: args);

	public static void Error(string messageTemplate, params object?[] args) =>
		ActiveLogger.Error(messageTemplate: messageTemplate, propertyValues: args);

	public static void Error(Exception ex, string messageTemplate, params object?[] args) =>
		ActiveLogger.Error(exception: ex, messageTemplate: messageTemplate, propertyValues: args);

	public static void Fatal(string messageTemplate, params object?[] args) =>
		ActiveLogger.Fatal(messageTemplate: messageTemplate, propertyValues: args);

	public static void Fatal(Exception ex, string messageTemplate, params object?[] args) =>
		ActiveLogger.Fatal(exception: ex, messageTemplate: messageTemplate, propertyValues: args);

	public static void ApiRequest(string api, string method, string url) =>
		ActiveLogger.Debug(
			messageTemplate: "ApiRequest {Api} {Method} {Url}",
			propertyValue0: api,
			propertyValue1: method,
			propertyValue2: url
		);

	public static void ApiResponse(string api, int statusCode, TimeSpan elapsed) =>
		ActiveLogger.Write(
			statusCode >= 500 ? LogEventLevel.Error
				: statusCode >= 400 ? LogEventLevel.Warning
				: LogEventLevel.Debug,
			messageTemplate: "ApiResponse {Api} {StatusCode} in {ElapsedMs}ms",
			propertyValue0: api,
			propertyValue1: statusCode,
			propertyValue2: elapsed.TotalMilliseconds
		);

	public static void PlaylistUpdated(
		string title,
		int added,
		int removed,
		List<string>? addedTitles = null
	)
	{
		if (added == 0 && removed == 0)
			return;

		ActiveLogger.Information(
			messageTemplate: "PlaylistUpdated {Title} +{Added} -{Removed}",
			propertyValue0: title,
			propertyValue1: added,
			propertyValue2: removed
		);
		Ui.Progress(message: "Synced {0}: +{1} / -{2}", title, added, removed);
		if (addedTitles is { Count: > 0 })
		{
			ActiveLogger.Debug(
				messageTemplate: "PlaylistUpdated {Title} AddedTitles {@AddedTitles}",
				propertyValue0: title,
				propertyValue1: addedTitles
			);
		}
	}

	public static void ScrobblesProcessed(int fetched, int written, int skipped)
	{
		ActiveLogger.Information(
			messageTemplate: "ScrobblesProcessed {Fetched} written {Written} skipped {Skipped}",
			propertyValue0: fetched,
			propertyValue1: written,
			propertyValue2: skipped
		);
		Ui.Progress(
			message: "Processed {0} scrobbles: {1} written, {2} skipped",
			fetched,
			written,
			skipped
		);
	}

	internal sealed class Operation : IDisposable
	{
		private readonly string OperationName;
		private readonly Stopwatch Sw = Stopwatch.StartNew();

		private Operation(string operationName)
		{
			OperationName = operationName;
			ActiveLogger.Debug(
				messageTemplate: "Operation {Operation} started",
				propertyValue: operationName
			);
		}

		public TimeSpan Elapsed => Sw.Elapsed;

		public void Dispose()
		{
			Sw.Stop();
			ActiveLogger.Debug(
				messageTemplate: "Operation {Operation} completed in {ElapsedMs:F0}ms",
				propertyValue0: OperationName,
				propertyValue1: Sw.Elapsed.TotalMilliseconds
			);
		}

		public static Operation Begin(string operationName) => new(operationName: operationName);
	}
}

file sealed class SessionScope(ServiceType service, IDisposable logScope, Action onDispose)
	: IDisposable
{
	public void Dispose()
	{
		Log.ForService(service: service).Information(messageTemplate: "SessionEnd");
		Ui.Complete(message: "{0} session ended", service);
		logScope.Dispose();
		onDispose();
	}
}
