namespace CSharpScripts.Infrastructure;

public static class Resilience
{
    #region Execution

    public const int MaxRetries = 10;

    public static readonly TimeSpan ThrottleDelay = TimeSpan.FromMilliseconds(milliseconds: 1100);
    public static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(seconds: 5);
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(minutes: 5);

    private static DateTime lastCallTime = DateTime.MinValue;
    private static readonly SemaphoreSlim Semaphore = new(initialCount: 1, maxCount: 1);

    public static async Task<T> ExecuteAsync<T>(
        string operation,
        Func<Task<T>> action,
        CancellationToken ct = default
    )
    {
        await Semaphore.WaitAsync(cancellationToken: ct);
        try
        {
            await ApplyThrottleAsync(ct: ct);

            var pipeline = CreateAsyncPipeline<T>(operation: operation);
            return await pipeline.ExecuteAsync(async _ => await action(), cancellationToken: ct);
        }
        finally
        {
            lastCallTime = DateTime.Now;
            Semaphore.Release();
        }
    }

    public static async Task ExecuteAsync(
        string operation,
        Func<Task> action,
        CancellationToken ct = default
    )
    {
        await ExecuteAsync(
            operation: operation,
            async () =>
            {
                await action();
                return true;
            },
            ct: ct
        );
    }

    private static ResiliencePipeline<T> CreateAsyncPipeline<T>(string operation) =>
        new ResiliencePipelineBuilder<T>()
            .AddRetry(CreateRetryOptions<T>(operation: operation))
            .Build();

    private static ResiliencePipeline<T> CreateSyncPipeline<T>(string operation) =>
        new ResiliencePipelineBuilder<T>()
            .AddRetry(CreateRetryOptions<T>(operation: operation))
            .Build();

    public static T Execute<T>(string operation, Func<T> action, CancellationToken ct = default)
    {
        Semaphore.Wait(cancellationToken: ct);
        try
        {
            ApplyThrottle();

            var pipeline = CreateSyncPipeline<T>(operation: operation);
            return pipeline.Execute(_ => action(), cancellationToken: ct);
        }
        finally
        {
            lastCallTime = DateTime.Now;
            Semaphore.Release();
        }
    }

    public static void Execute(string operation, Action action, CancellationToken ct = default) =>
        Execute(
            operation: operation,
            () =>
            {
                action();
                return true;
            },
            ct: ct
        );

    private static void ApplyThrottle()
    {
        var elapsed = DateTime.Now - lastCallTime;
        if (elapsed < ThrottleDelay)
            Thread.Sleep(ThrottleDelay - elapsed);
    }

    private static RetryStrategyOptions<T> CreateRetryOptions<T>(string operation) =>
        new()
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
                string message = args.Outcome.Exception?.Message ?? "Unknown error";

                if (IsFatalQuotaError(message: message))
                {
                    string serviceName = operation.Split(separator: '.')[0];
                    throw new DailyQuotaExceededException(service: serviceName, message: message);
                }

                Console.Warning(
                    message: "{0} failed (attempt {1}/{2}): {3}",
                    operation,
                    args.AttemptNumber + 1,
                    MaxRetries,
                    message
                );
                Console.Info(
                    message: "Retrying in {0:F0}s (at {1:HH:mm:ss})",
                    args.RetryDelay.TotalSeconds,
                    DateTime.Now.Add(value: args.RetryDelay)
                );

                return ValueTask.CompletedTask;
            },
        };

    private static async Task ApplyThrottleAsync(CancellationToken ct)
    {
        var elapsed = DateTime.Now - lastCallTime;
        if (elapsed < ThrottleDelay)
            await Task.Delay(ThrottleDelay - elapsed, cancellationToken: ct);
    }

    #endregion

    #region Error Detection

    public static bool IsTransientError(string? message) =>
        message is { }
        && (
            message.Contains(value: "busy", comparisonType: StringComparison.OrdinalIgnoreCase)
            || message.Contains(
                value: "unavailable",
                comparisonType: StringComparison.OrdinalIgnoreCase
            )
            || message.Contains(value: "503", comparisonType: StringComparison.Ordinal)
            || message.Contains(value: "429", comparisonType: StringComparison.Ordinal)
            || message.Contains(
                value: "rate limit",
                comparisonType: StringComparison.OrdinalIgnoreCase
            )
            || message.Contains(
                value: "too many requests",
                comparisonType: StringComparison.OrdinalIgnoreCase
            )
            || message.Contains(
                value: "try again",
                comparisonType: StringComparison.OrdinalIgnoreCase
            )
        );

    public static bool IsFatalQuotaError(string message) =>
        message.Contains(value: "daily limit", comparisonType: StringComparison.OrdinalIgnoreCase)
        || message.Contains(
            value: "quota exceeded",
            comparisonType: StringComparison.OrdinalIgnoreCase
        )
        || message.Contains(value: "quota", comparisonType: StringComparison.OrdinalIgnoreCase)
            && message.Contains(value: "day", comparisonType: StringComparison.OrdinalIgnoreCase);

    #endregion
}

#region Exceptions

public sealed class DailyQuotaExceededException(string service, string message)
    : Exception($"Daily quota exceeded for {service}. Try again tomorrow. Original: {message}")
{
    internal string Service { get; } = service;
}

public sealed class RetryExhaustedException(
    string operation,
    int attempts,
    TimeSpan totalWait,
    Exception inner
)
    : Exception(
        $"{operation} failed after {attempts} retries ({totalWait:hh\\:mm\\:ss} total wait). Last error: {inner.Message}",
        innerException: inner
    )
{
    internal int Attempts { get; } = attempts;
    internal TimeSpan TotalWait { get; } = totalWait;
}

#endregion
