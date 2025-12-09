namespace CSharpScripts.Infrastructure;

public static class Resilience
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Configuration
    // ═══════════════════════════════════════════════════════════════════════════

    public const int MaxRetries = 10;
    public static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan LongDelay = TimeSpan.FromSeconds(90);
    public static readonly TimeSpan DefaultThrottle = TimeSpan.FromMilliseconds(3000);
    public static readonly TimeSpan MaxBackoffDelay = TimeSpan.FromMinutes(5);

    private static readonly Random Jitter = new();
    private static readonly SemaphoreSlim LastFmSemaphore = new(1, 1);
    private static readonly SemaphoreSlim SheetsSemaphore = new(1, 1);
    private static readonly SemaphoreSlim YouTubeSemaphore = new(1, 1);
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
            || message.Contains("502", StringComparison.OrdinalIgnoreCase)
            || message.Contains("500", StringComparison.OrdinalIgnoreCase)
            || message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || message.Contains("connection reset", StringComparison.OrdinalIgnoreCase)
            || message.Contains("connection closed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("socket", StringComparison.OrdinalIgnoreCase)
            || message.Contains("network", StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>
    /// Transient exception types that should always be retried regardless of message content.
    /// These indicate server-side issues (API returning HTML instead of XML, network issues, etc.)
    /// </summary>
    private static bool IsTransientExceptionType(Exception ex)
    {
        string typeName = ex.GetType().Name;
        return typeName.Contains("Xml", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("Http", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("Socket", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("Timeout", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("IOException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("WebException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("TaskCanceled", StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan CalculateBackoffWithJitter(TimeSpan baseDelay, int attempt)
    {
        double exponential = Math.Pow(2, attempt - 1);
        double baseSeconds = baseDelay.TotalSeconds * exponential;
        double jitterSeconds = Jitter.NextDouble() * baseSeconds * 0.3;
        double totalSeconds = Math.Min(baseSeconds + jitterSeconds, MaxBackoffDelay.TotalSeconds);
        return TimeSpan.FromSeconds(totalSeconds);
    }

    private static SemaphoreSlim GetServiceSemaphore(string operationName)
    {
        if (operationName.StartsWith("LastFm", StringComparison.OrdinalIgnoreCase))
            return LastFmSemaphore;
        if (operationName.StartsWith("Sheets", StringComparison.OrdinalIgnoreCase))
            return SheetsSemaphore;
        if (operationName.StartsWith("YouTube", StringComparison.OrdinalIgnoreCase))
            return YouTubeSemaphore;
        return Lock;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Synchronous Execution with Retry
    // ═══════════════════════════════════════════════════════════════════════════

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
        SemaphoreSlim semaphore = GetServiceSemaphore(operationName);

        Exception? lastException = null;
        T? result = default;
        bool succeeded = false;

        semaphore.Wait(ct);
        try
        {
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

                    bool isTransient = IsTransientExceptionType(ex) || IsRetryableError(ex.Message);
                    if (!isTransient)
                        throw;

                    if (attempt >= MaxRetries)
                        break;

                    TimeSpan waitTime = CalculateBackoffWithJitter(delay, attempt);
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
        finally
        {
            semaphore.Release();
        }
    }

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
