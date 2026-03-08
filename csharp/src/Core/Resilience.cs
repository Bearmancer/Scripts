using System.Net.Sockets;
using System.Threading.RateLimiting;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace CSharpScripts.Core;

internal static class Resilience
{
	public const int MAX_RETRIES = 10;

	public static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(5);
	public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(5);

	private static readonly Dictionary<ServiceType, ResiliencePipeline> Pipelines = [];

	public static ResiliencePipeline GetPipeline(ServiceType service)
	{
		if (!Pipelines.TryGetValue(service, out ResiliencePipeline? pipeline))
		{
			pipeline = BuildPipeline(service);
			Pipelines[service] = pipeline;
		}
		return pipeline;
	}

	private static ResiliencePipeline BuildPipeline(ServiceType service)
	{
		var builder = new ResiliencePipelineBuilder();
		BuildCircuitBreaker(builder, service);
		BuildRateLimiter(builder, service);
		BuildRetry(builder, service);
		BuildTimeout(builder, service);
		return builder.Build();
	}

	private static void BuildCircuitBreaker(ResiliencePipelineBuilder builder, ServiceType service)
	{
		builder.AddCircuitBreaker(
			new CircuitBreakerStrategyOptions
			{
				FailureRatio = 0.5,
				SamplingDuration = TimeSpan.FromMinutes(3),
				MinimumThroughput = 5,
				BreakDuration = TimeSpan.FromSeconds(30),
				OnOpened = args =>
				{
					Log.Warning("CircuitBreakerOpened {Service}", service);
					return ValueTask.CompletedTask;
				},
				OnClosed = args =>
				{
					Log.Information("CircuitBreakerClosed {Service}", service);
					return ValueTask.CompletedTask;
				},
				OnHalfOpened = args =>
				{
					Log.Debug("CircuitBreakerHalfOpen {Service}", service);
					return ValueTask.CompletedTask;
				},
			}
		);
	}

	private static void BuildRateLimiter(ResiliencePipelineBuilder builder, ServiceType service)
	{
		if (service == ServiceType.LastFm)
		{
#pragma warning disable CA2000
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
#pragma warning restore CA2000
		}
	}

	private static void BuildRetry(ResiliencePipelineBuilder builder, ServiceType service)
	{
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
						throw new DailyQuotaExceededException(service.ToString(), message);

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
	}

	private static void BuildTimeout(ResiliencePipelineBuilder builder, ServiceType service)
	{
		var timeoutSeconds = service switch
		{
			ServiceType.LastFm => 30,
			ServiceType.YouTube => 60,
			ServiceType.Sheets => 120,
			ServiceType.Music => 30,
			ServiceType.Read => 60,
			ServiceType.Cloud => 30,
			_ => 30,
		};

		builder.AddTimeout(TimeSpan.FromSeconds(timeoutSeconds));
	}

	public static async Task<T> ExecuteAsync<T>(
		string operation,
		Func<Task<T>> action,
		CancellationToken ct = default
	)
	{
		ResiliencePipeline<T> pipeline = CreateAsyncPipeline<T>(operation);
		return await pipeline.ExecuteAsync(async _ => await action(), ct);
	}

	public static async Task ExecuteAsync(
		string operation,
		Func<Task> action,
		CancellationToken ct = default
	)
	{
		await ExecuteAsync(
			operation,
			async () =>
			{
				await action();
				return true;
			},
			ct
		);
	}

	private static ResiliencePipeline<T> CreateAsyncPipeline<T>(string operation) =>
		new ResiliencePipelineBuilder<T>().AddRetry(CreateRetryOptions<T>(operation)).Build();

	private static RetryStrategyOptions<T> CreateRetryOptions<T>(string operation) =>
		new()
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
					var serviceName = operation.Split('.')[0];
					throw new DailyQuotaExceededException(serviceName, message);
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
		};

	public static bool IsTransientError(string? message) =>
		message is not null
		&& (
			message.Contains("busy")
			|| message.ContainsIgnoreCase("unavailable")
			|| message.Contains("503")
			|| message.Contains("429")
			|| message.ContainsIgnoreCase("rate limit")
			|| message.ContainsIgnoreCase("too many requests")
			|| message.ContainsIgnoreCase("try again")
		);

	public static bool IsFatalQuotaError(string message) =>
		message.Contains("daily limit")
		|| message.ContainsIgnoreCase("quota exceeded")
		|| (message.Contains("quota") && message.Contains("day"));

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

internal sealed class DailyQuotaExceededException : Exception
{
	internal string Service { get; }

	internal DailyQuotaExceededException()
		: base("Daily quota exceeded") => Service = "";

	internal DailyQuotaExceededException(string message)
		: base(message) => Service = "";

	internal DailyQuotaExceededException(string message, Exception innerException)
		: base(message, innerException) => Service = "";

	internal DailyQuotaExceededException(string service, string message)
		: base($"Daily quota exceeded for {service}. Try again tomorrow. Original: {message}") =>
		Service = service;
}
