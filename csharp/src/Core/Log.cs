namespace CSharpScripts.Core;

using System.Diagnostics;
using Serilog;
using Serilog.Context;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Serilog.Formatting.Compact;

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
	private static readonly Dictionary<ServiceType, ILogger> ServiceLoggers = [];
	private static ServiceType? ActiveService;

	static Log()
	{
		ServiceLoggers[ServiceType.LastFm] = CreateServiceLogger("lastfm.jsonl");
		ServiceLoggers[ServiceType.YouTube] = CreateServiceLogger("youtube.jsonl");
		ServiceLoggers[ServiceType.Music] = CreateServiceLogger("music.jsonl");
		ServiceLoggers[ServiceType.Sheets] = CreateServiceLogger("sheets.jsonl");
		ServiceLoggers[ServiceType.Read] = CreateServiceLogger("read.jsonl");
		ServiceLoggers[ServiceType.Cloud] = CreateServiceLogger("cloud.jsonl");
	}

	private static Serilog.Core.Logger CreateServiceLogger(string filename) =>
		new LoggerConfiguration()
			.MinimumLevel.Debug()
			.Enrich.FromLogContext()
			.Enrich.WithProcessId()
			.Enrich.WithThreadId()
			.Enrich.WithProperty("Application", "CSharpScripts")
			.WriteTo.File(
				new CompactJsonFormatter(),
				path: Path.Combine(Paths.LogDirectory, filename),
				rollingInterval: RollingInterval.Infinite,
				shared: true
			)
			.CreateLogger();

	internal static ILogger ActiveLogger =>
		ActiveService.HasValue ? ServiceLoggers[ActiveService.Value] : Serilog.Log.Logger;

	public static ILogger ForService(ServiceType service) => ServiceLoggers[service];

	public static IDisposable BeginSession(ServiceType service)
	{
		ActiveService = service;
		var sessionId = Guid.NewGuid().ToString("N")[..8];
		IDisposable scope = LogContext.Push(
			new PropertyEnricher("SessionId", sessionId),
			new PropertyEnricher("Service", service.ToString())
		);
		ActiveLogger.Information("SessionStart {SessionId}", sessionId);
		UI.Starting("{0} session started", service);
		return new SessionScope(service, scope, () => ActiveService = null);
	}

	public static void Debug(string messageTemplate, params object?[] args) =>
		ActiveLogger.Debug(messageTemplate, args);

	public static void Information(string messageTemplate, params object?[] args) =>
		ActiveLogger.Information(messageTemplate, args);

	public static void Warning(string messageTemplate, params object?[] args) =>
		ActiveLogger.Warning(messageTemplate, args);

	public static void Error(string messageTemplate, params object?[] args) =>
		ActiveLogger.Error(messageTemplate, args);

	public static void Error(Exception ex, string messageTemplate, params object?[] args) =>
		ActiveLogger.Error(ex, messageTemplate, args);

	public static void Fatal(string messageTemplate, params object?[] args) =>
		ActiveLogger.Fatal(messageTemplate, args);

	public static void Fatal(Exception ex, string messageTemplate, params object?[] args) =>
		ActiveLogger.Fatal(ex, messageTemplate, args);

	public static void ApiRequest(string api, string method, string url) =>
		ActiveLogger.Debug("ApiRequest {Api} {Method} {Url}", api, method, url);

	public static void ApiResponse(string api, int statusCode, TimeSpan elapsed) =>
		ActiveLogger.Write(
			statusCode >= 500 ? LogEventLevel.Error
				: statusCode >= 400 ? LogEventLevel.Warning
				: LogEventLevel.Debug,
			"ApiResponse {Api} {StatusCode} in {ElapsedMs}ms",
			api,
			statusCode,
			elapsed.TotalMilliseconds
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
			"PlaylistUpdated {Title} +{Added} -{Removed}",
			title,
			added,
			removed
		);
		UI.Progress("Synced {0}: +{1} / -{2}", title, added, removed);
		if (addedTitles is { Count: > 0 })
			ActiveLogger.Debug(
				"PlaylistUpdated {Title} AddedTitles {@AddedTitles}",
				title,
				addedTitles
			);
	}

	public static void ScrobblesProcessed(int fetched, int written, int skipped)
	{
		ActiveLogger.Information(
			"ScrobblesProcessed {Fetched} written {Written} skipped {Skipped}",
			fetched,
			written,
			skipped
		);
		UI.Progress("Processed {0} scrobbles: {1} written, {2} skipped", fetched, written, skipped);
	}

	internal sealed class Operation : IDisposable
	{
		private readonly Stopwatch Sw = Stopwatch.StartNew();
		private readonly string OperationName;

		private Operation(string operationName)
		{
			OperationName = operationName;
			ActiveLogger.Debug("Operation {Operation} started", operationName);
		}

		public static Operation Begin(string operationName) => new(operationName);

		public TimeSpan Elapsed => Sw.Elapsed;

		public void Dispose()
		{
			Sw.Stop();
			ActiveLogger.Debug(
				"Operation {Operation} completed in {ElapsedMs:F0}ms",
				OperationName,
				Sw.Elapsed.TotalMilliseconds
			);
		}
	}
}

file sealed class SessionScope(ServiceType service, IDisposable logScope, Action onDispose)
	: IDisposable
{
	public void Dispose()
	{
		Log.ForService(service).Information("SessionEnd");
		UI.Complete("{0} session ended", service);
		logScope.Dispose();
		onDispose();
	}
}
