using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Scripts.Core;

internal static class Resilience
{
	public const int MaxRetries = 10;

	public static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(seconds: 5);
	public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(minutes: 5);

	private static readonly ConcurrentDictionary<ServiceType, ResiliencePipeline> Pipelines = new();

	private static readonly ConcurrentDictionary<string, object> OperationPipelines = new();

	private static readonly SearchValues<string> TransientPatterns = SearchValues.Create(
		["busy", "unavailable", "503", "429", "rate limit", "too many requests", "try again"],
		comparisonType: OrdinalIgnoreCase
	);

	private static readonly SearchValues<string> QuotaPatterns = SearchValues.Create(
		["daily limit", "quota exceeded"],
		comparisonType: OrdinalIgnoreCase
	);

	public static ResiliencePipeline GetPipeline(ServiceType service) =>
		Pipelines.GetOrAdd(key: service, valueFactory: BuildPipeline);

	private static ResiliencePipeline BuildPipeline(ServiceType service)
	{
		ResiliencePipelineBuilder builder = new();
		BuildCircuitBreaker(builder: builder, service: service);
		BuildRateLimiter(builder: builder, service: service);
		BuildRetry(builder: builder, service: service);
		BuildTimeout(builder: builder, service: service);
		return builder.Build();
	}

	private static void BuildCircuitBreaker(
		ResiliencePipelineBuilder builder,
		ServiceType service
	) =>
		builder.AddCircuitBreaker(
			new CircuitBreakerStrategyOptions
			{
				FailureRatio = 0.5,
				SamplingDuration = TimeSpan.FromMinutes(minutes: 3),
				MinimumThroughput = 5,
				BreakDuration = TimeSpan.FromSeconds(seconds: 30),
				OnOpened = _ =>
				{
					Log.Warning(messageTemplate: "CircuitBreakerOpened {Service}", service);
					return ValueTask.CompletedTask;
				},
				OnClosed = _ =>
				{
					Log.Information(messageTemplate: "CircuitBreakerClosed {Service}", service);
					return ValueTask.CompletedTask;
				},
				OnHalfOpened = _ =>
				{
					Log.Debug(messageTemplate: "CircuitBreakerHalfOpen {Service}", service);
					return ValueTask.CompletedTask;
				},
			}
		);

	private static void BuildRateLimiter(ResiliencePipelineBuilder builder, ServiceType service)
	{
		if (service != ServiceType.LastFm)
			return;

		builder.AddRateLimiter(
			new SlidingWindowRateLimiter(
				new SlidingWindowRateLimiterOptions
				{
					PermitLimit = 1,
					Window = TimeSpan.FromSeconds(seconds: 1),
					SegmentsPerWindow = 1,
					QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
					QueueLimit = 20,
				}
			)
		);
	}

	private static void BuildRetry(ResiliencePipelineBuilder builder, ServiceType service) =>
		builder.AddRetry(
			new RetryStrategyOptions
			{
				MaxRetryAttempts = MaxRetries,
				Delay = BaseRetryDelay,
				MaxDelay = MaxRetryDelay,
				BackoffType = DelayBackoffType.Exponential,
				UseJitter = true,
				ShouldHandle = new PredicateBuilder()
					.Handle<HttpRequestException>()
					.Handle<TimeoutException>()
					.Handle<IOException>()
					.Handle<SocketException>()
					.HandleInner<HttpRequestException>()
					.HandleInner<TimeoutException>()
					.HandleInner<IOException>()
					.HandleInner<SocketException>()
					.Handle<Exception>(ex => IsTransientError(message: ex.Message))
					.HandleInner<Exception>(ex => IsTransientError(message: ex.Message)),
				OnRetry = args =>
				{
					var message = args.Outcome.Exception?.Message ?? "Unknown error";
					if (IsFatalQuotaError(message: message))
					{
						Log.Warning(
							messageTemplate: "DailyQuotaExceeded {Service}: {Message}",
							service,
							message
						);
						return ValueTask.FromException(
							new DailyQuotaExceededException(service.ToString(), message: message)
						);
					}
					Log.Warning(
						messageTemplate: "Retry {Attempt}/{MaxRetries} for {Service}: {Message}",
						args.AttemptNumber + 1,
						MaxRetries,
						service,
						message
					);
					Log.Debug(
						messageTemplate: "Retrying in {DelaySeconds:F0}s (at {RetryTime:HH:mm:ss})",
						args.RetryDelay.TotalSeconds,
						DateTimeOffset.Now.Add(timeSpan: args.RetryDelay)
					);
					return ValueTask.CompletedTask;
				},
			}
		);

	private static void BuildTimeout(ResiliencePipelineBuilder builder, ServiceType service)
	{
		var timeoutSeconds = service switch
		{
			ServiceType.LastFm => 30,
			ServiceType.YouTube => 60,
			ServiceType.Music => 30,
			ServiceType.Read => 60,
			ServiceType.Cloud => 60,
			_ => 30,
		};
		builder.AddTimeout(TimeSpan.FromSeconds(seconds: timeoutSeconds));
	}

	public static async Task<T> ExecuteAsync<T>(
		string operation,
		Func<Task<T>> action,
		CancellationToken ct = default
	)
	{
		ResiliencePipeline<T> pipeline =
			(ResiliencePipeline<T>)
				OperationPipelines.GetOrAdd(
					key: operation,
					static op => BuildTypedPipeline<T>(operation: op)
				);
		return await pipeline.ExecuteAsync(_ => new ValueTask<T>(action()), cancellationToken: ct);
	}

	private static ResiliencePipeline<T> BuildTypedPipeline<T>(string operation)
	{
		return new ResiliencePipelineBuilder<T>()
			.AddRetry(
				new RetryStrategyOptions<T>
				{
					MaxRetryAttempts = MaxRetries,
					Delay = BaseRetryDelay,
					MaxDelay = MaxRetryDelay,
					BackoffType = DelayBackoffType.Exponential,
					UseJitter = true,
					ShouldHandle = new PredicateBuilder<T>()
						.Handle<HttpRequestException>()
						.Handle<TimeoutException>()
						.Handle<IOException>()
						.Handle<SocketException>()
						.HandleInner<HttpRequestException>()
						.HandleInner<TimeoutException>()
						.HandleInner<IOException>()
						.HandleInner<SocketException>()
						.Handle<Exception>(ex => IsTransientError(message: ex.Message))
						.HandleInner<Exception>(ex => IsTransientError(message: ex.Message)),
					OnRetry = args =>
					{
						var message = args.Outcome.Exception?.Message ?? "Unknown error";
						if (IsFatalQuotaError(message: message))
						{
							var dot = operation.IndexOf(value: '.');
							var svc = dot >= 0 ? operation[..dot] : operation;
							return ValueTask.FromException(
								new DailyQuotaExceededException(service: svc, message: message)
							);
						}
						Log.Warning(
							messageTemplate: "{Operation} failed (attempt {Attempt}/{MaxRetries}): {Message}",
							operation,
							args.AttemptNumber + 1,
							MaxRetries,
							message
						);
						Log.Debug(
							messageTemplate: "Retrying in {DelaySeconds:F0}s (at {RetryTime:HH:mm:ss})",
							args.RetryDelay.TotalSeconds,
							DateTimeOffset.Now.Add(timeSpan: args.RetryDelay)
						);
						return ValueTask.CompletedTask;
					},
				}
			)
			.Build();
	}

	public static Task ExecuteAsync(
		string operation,
		Func<Task> action,
		CancellationToken ct = default
	) =>
		ExecuteAsync<object>(
			operation: operation,
			async () =>
			{
				await action();
				return null!;
			},
			ct: ct
		);

	public static bool IsTransientError(string? message) =>
		message is { } && TransientPatterns.Contains(value: message);

	public static bool IsFatalQuotaError(string message) =>
		QuotaPatterns.Contains(value: message)
		|| message.ContainsIgnoreCase(substring: "quota")
			&& message.ContainsIgnoreCase(substring: "day");

	public static Task<T> ExecuteMusicApiAsync<T>(
		string service,
		Func<Task<T>> action,
		CancellationToken ct = default
	) => ExecuteAsync($"Music.{service}", action: action, ct: ct);

	public static Task ExecuteMusicApiAsync(
		string service,
		Func<Task> action,
		CancellationToken ct = default
	) => ExecuteAsync($"Music.{service}", action: action, ct: ct);

	public static T Execute<T>(
		string operation,
		Func<T> action,
		CancellationToken ct = default
	) =>
		ExecuteAsync<T>(
			operation: operation,
			action: () => Task.FromResult(result: action()),
			ct: ct
		)
			.GetAwaiter()
			.GetResult();

	public static void Execute(
		string operation,
		Action action,
		CancellationToken ct = default
	) =>
		ExecuteAsync<object>(
			operation: operation,
			action: () =>
			{
				action();
				return Task.FromResult<object>(result: null!);
			},
			ct: ct
		)
			.GetAwaiter()
			.GetResult();
}

public sealed class DailyQuotaExceededException : Exception
{
	internal DailyQuotaExceededException()
		: base(message: "Daily quota exceeded") => Service = "";

	internal DailyQuotaExceededException(string message)
		: base(message: message) => Service = "";

	internal DailyQuotaExceededException(string message, Exception innerException)
		: base(message: message, innerException: innerException) => Service = "";

	internal DailyQuotaExceededException(string service, string message)
		: base($"Daily quota exceeded for {service}. Try again tomorrow. Original: {message}") =>
		Service = service;

	internal string Service { get; }
}

public sealed class RetryExhaustedException(
	string operation,
	int attempts,
	TimeSpan totalWait,
	Exception inner
)
	: Exception(
		$"{operation} failed after {attempts} retries ({totalWait:hh\\:mm\\:ss} total wait). Last error: {inner.Message}",
		inner
	)
{
	internal int Attempts { get; } = attempts;
	internal TimeSpan TotalWait { get; } = totalWait;
}
