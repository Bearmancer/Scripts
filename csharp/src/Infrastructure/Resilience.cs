namespace CSharpScripts.Infrastructure;

/// <summary>
/// Centralized resilience for all API/network operations.
/// Combines Polly retry with rate limiting and quota detection.
/// </summary>
public static class Resilience
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Configuration
    // ═══════════════════════════════════════════════════════════════════════════

    public const int MaxRetries = 10;
    public static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan LongDelay = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan DefaultThrottle = TimeSpan.FromMilliseconds(2000);

    private static readonly SemaphoreSlim Lock = new(1, 1);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error Detection
    // ═══════════════════════════════════════════════════════════════════════════

    public static bool IsFatalQuotaError(string message) =>
        message.Contains("daily limit", StringComparison.OrdinalIgnoreCase)
        || message.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase)
        || (
            message.Contains("quota", StringComparison.OrdinalIgnoreCase)
            && message.Contains("day", StringComparison.OrdinalIgnoreCase)
        );

    public static bool IsRetryableError(string message) =>
        !IsFatalQuotaError(message)
        && (
            message.Contains("quota", StringComparison.OrdinalIgnoreCase)
            || message.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || message.Contains("too many requests", StringComparison.OrdinalIgnoreCase)
            || message.Contains("backend service failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("429", StringComparison.OrdinalIgnoreCase)
            || message.Contains("503", StringComparison.OrdinalIgnoreCase)
            || message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
        );

    // ═══════════════════════════════════════════════════════════════════════════
    // Synchronous Execution with Retry
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Execute with exponential backoff retry and quota detection.</summary>
    public static T Execute<T>(
        string operationName,
        Func<T> action,
        Action? postAction = null,
        CancellationToken ct = default,
        TimeSpan? baseDelay = null
    )
    {
        TimeSpan delay = baseDelay ?? LongDelay;
        int attemptsMade = 0;
        TimeSpan totalWaitTime = TimeSpan.Zero;
        string serviceName = operationName.Split('.')[0];

        Exception? lastException = null;
        T? result = default;
        bool succeeded = false;

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                result = action();
                succeeded = true;
                break;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attemptsMade = attempt;

                if (IsFatalQuotaError(ex.Message))
                    throw new DailyQuotaExceededException(serviceName, ex.Message);

                if (!IsRetryableError(ex.Message))
                    throw;

                if (attempt >= MaxRetries)
                    break;

                TimeSpan waitTime = TimeSpan.FromSeconds(
                    delay.TotalSeconds * Math.Pow(2, attempt - 1)
                );
                totalWaitTime += waitTime;

                Console.Warning(
                    "{0} failed (attempt {1}/{2}): {3}",
                    operationName,
                    attempt,
                    MaxRetries,
                    ex.Message
                );
                Console.Info(
                    "Waiting {0} (resume at {1:HH:mm:ss})",
                    waitTime.ToString(@"hh\:mm\:ss"),
                    DateTime.Now.Add(waitTime)
                );

                for (int remaining = (int)waitTime.TotalSeconds; remaining > 0; remaining--)
                {
                    if (ct.IsCancellationRequested)
                        break;
                    Thread.Sleep(1000);
                }
            }
        }

        if (!succeeded && lastException is not null)
        {
            if (IsFatalQuotaError(lastException.Message))
                throw new DailyQuotaExceededException(serviceName, lastException.Message);

            if (attemptsMade >= MaxRetries)
                throw new RetryExhaustedException(
                    operationName,
                    attemptsMade,
                    totalWaitTime,
                    lastException
                );

            throw new InvalidOperationException(
                $"{operationName} failed: {lastException.Message}",
                lastException
            );
        }

        postAction?.Invoke();
        return result!;
    }

    /// <summary>Execute void action with retry.</summary>
    public static void Execute(
        string operationName,
        Action action,
        Action? postAction = null,
        CancellationToken ct = default,
        TimeSpan? baseDelay = null
    ) =>
        Execute(
            operationName,
            () =>
            {
                action();
                return true;
            },
            postAction,
            ct,
            baseDelay
        );

    // ═══════════════════════════════════════════════════════════════════════════
    // Async Rate-Limited Execution
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Execute async with semaphore lock, throttle, and retry.</summary>
    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        string source,
        TimeSpan? throttle = null,
        int maxRetries = MaxRetries
    )
    {
        await Lock.WaitAsync();
        try
        {
            if (throttle.HasValue && throttle.Value > TimeSpan.Zero)
                await Task.Delay(throttle.Value);

            ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
                .AddRetry(
                    new RetryStrategyOptions
                    {
                        MaxRetryAttempts = maxRetries,
                        Delay = BaseDelay,
                        BackoffType = DelayBackoffType.Exponential,
                        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                        OnRetry = args =>
                        {
                            Console.Warning(
                                "[{0}] Retry {1}/{2} in {3:F1}s: {4}",
                                source,
                                args.AttemptNumber,
                                maxRetries,
                                args.RetryDelay.TotalSeconds,
                                args.Outcome.Exception?.Message
                            );
                            return ValueTask.CompletedTask;
                        },
                    }
                )
                .Build();

            return await pipeline.ExecuteAsync(async _ => await action(), CancellationToken.None);
        }
        finally
        {
            Lock.Release();
        }
    }

    /// <summary>Delay helper for post-call throttling.</summary>
    public static void Delay(ServiceType service) =>
        Thread.Sleep((int)DefaultThrottle.TotalMilliseconds);
}

// ═══════════════════════════════════════════════════════════════════════════
// Exception Types
// ═══════════════════════════════════════════════════════════════════════════

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
        inner
    )
{
    internal int Attempts { get; } = attempts;
    internal TimeSpan TotalWait { get; } = totalWait;
}
