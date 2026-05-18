#pragma warning disable CA2000
using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace CSharpScripts.Core;

internal static class Resilience
{
	public const int MAX_RETRIES = 10;

	public static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(seconds: 5);
	public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(minutes: 5);

	private static readonly ConcurrentDictionary<ServiceType, ResiliencePipeline> Pipelines = new();

	public static ResiliencePipeline GetPipeline(ServiceType service) =>
		Pipelines.GetOrAdd(service, BuildPipeline);

	private static ResiliencePipeline BuildPipeline(ServiceType service)
	{
		var builder = new ResiliencePipelineBuilder();
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
				SamplingDuration = TimeSpan.FromMinutes(3),
				MinimumThroughput = 5,
				BreakDuration = TimeSpan.FromSeconds(30),
				OnOpened = _ =>
				{
					Log.Warning("CircuitBreakerOpened {Service}", service);
					return ValueTask.CompletedTask;
				},
				OnClosed = _ =>
				{
					Log.Information("CircuitBreakerClosed {Service}", service);
					return ValueTask.CompletedTask;
				},
				OnHalfOpened = _ =>
				{
					Log.Debug("CircuitBreakerHalfOpen {Service}", service);
					return ValueTask.CompletedTask;
				},
			}
		);

	private static void BuildRateLimiter(ResiliencePipelineBuilder builder, ServiceType service)
	{
		if (service != ServiceType.LastFm)
			return;
		// creating a SlidingWindowRateLimiter here transfers ownership to the pipeline builder
		// which will manage its lifetime; intentionally not disposing here so the pipeline
		// controls the limiter's lifetime.
		builder.AddRateLimiter(
			new SlidingWindowRateLimiter(
				new SlidingWindowRateLimiterOptions
				{
					PermitLimit = 1,
					Window = TimeSpan.FromSeconds(1),
					SegmentsPerWindow = 1,
					QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
					QueueLimit = 20,
				}
			)
		);
		// ownership transferred to builder (see comment above)
	}

	private static void BuildRetry(ResiliencePipelineBuilder builder, ServiceType service) =>
		builder.AddRetry(
			new RetryStrategyOptions
			{
				MaxRetryAttempts = MAX_RETRIES,
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
					.Handle<Exception>(ex => IsTransientError(ex.Message))
					.HandleInner<Exception>(ex => IsTransientError(ex.Message)),
				OnRetry = args =>
				{
					var message = args.Outcome.Exception?.Message ?? "Unknown error";
					if (IsFatalQuotaError(message))
					{
						Log.Warning("DailyQuotaExceeded {Service}: {Message}", service, message);
						return ValueTask.FromException(
							new DailyQuotaExceededException(service.ToString(), message)
						);
					}
					Log.Warning(
						"Retry {Attempt}/{MaxRetries} for {Service}: {Message}",
						args.AttemptNumber + 1,
						MAX_RETRIES,
						service,
						message
					);
					Log.Debug(
						"Retrying in {DelaySeconds:F0}s (at {RetryTime:HH:mm:ss})",
						args.RetryDelay.TotalSeconds,
						DateTime.Now.Add(args.RetryDelay)
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
			ServiceType.Sheets => 120,
			ServiceType.Music => 30,
			ServiceType.Read => 60,
			ServiceType.Cloud => 60,
			_ => 30,
		};
		builder.AddTimeout(TimeSpan.FromSeconds(timeoutSeconds));
	}

	private static readonly ConcurrentDictionary<string, object> OperationPipelines = new();

	public static async Task<T> ExecuteAsync<T>(
		string operation,
		Func<Task<T>> action,
		CancellationToken ct = default
	)
	{
		var pipeline =
			(ResiliencePipeline<T>)
				OperationPipelines.GetOrAdd(operation, static op => BuildTypedPipeline<T>(op));
		return await pipeline.ExecuteAsync(token => new ValueTask<T>(action()), ct);
	}

	private static ResiliencePipeline<T> BuildTypedPipeline<T>(string operation)
	{
		return new ResiliencePipelineBuilder<T>()
			.AddRetry(
				new RetryStrategyOptions<T>
				{
					MaxRetryAttempts = MAX_RETRIES,
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
						.Handle<Exception>(ex => IsTransientError(ex.Message))
						.HandleInner<Exception>(ex => IsTransientError(ex.Message)),
					OnRetry = args =>
					{
						var message = args.Outcome.Exception?.Message ?? "Unknown error";
						if (IsFatalQuotaError(message))
						{
							var dot = operation.IndexOf('.');
							var svc = dot >= 0 ? operation[..dot] : operation;
							return ValueTask.FromException(
								new DailyQuotaExceededException(svc, message)
							);
						}
						Log.Warning(
							"{Operation} failed (attempt {Attempt}/{MaxRetries}): {Message}",
							operation,
							args.AttemptNumber + 1,
							MAX_RETRIES,
							message
						);
						Log.Debug(
							"Retrying in {DelaySeconds:F0}s (at {RetryTime:HH:mm:ss})",
							args.RetryDelay.TotalSeconds,
							DateTime.Now.Add(args.RetryDelay)
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
			operation,
			async () =>
			{
				await action();
				return default!;
			},
			ct
		);

	private static readonly SearchValues<string> TransientPatterns = SearchValues.Create(
		["busy", "unavailable", "503", "429", "rate limit", "too many requests", "try again"],
		OrdinalIgnoreCase
	);

	private static readonly SearchValues<string> QuotaPatterns = SearchValues.Create(
		["daily limit", "quota exceeded"],
		OrdinalIgnoreCase
	);

	public static bool IsTransientError(string? message) =>
		message is not null && TransientPatterns.Contains(message);

	public static bool IsFatalQuotaError(string message) =>
		QuotaPatterns.Contains(message)
		|| (message.ContainsIgnoreCase("quota") && message.ContainsIgnoreCase("day"));

	public static Task<T> ExecuteMusicApiAsync<T>(
		string service,
		Func<Task<T>> action,
		CancellationToken ct = default
	) => ExecuteAsync($"Music.{service}", action, ct);

	public static Task ExecuteMusicApiAsync(
		string service,
		Func<Task> action,
		CancellationToken ct = default
	) => ExecuteAsync($"Music.{service}", action, ct);
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





