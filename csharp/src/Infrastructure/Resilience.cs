namespace CSharpScripts.Infrastructure;

public static class Resilience
{
    public const int MAX_RETRIES = 10;
    public static readonly TimeSpan ThrottleDelay = TimeSpan.FromMilliseconds(3000);
    public static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(5);

    private static DateTime lastCallTime = DateTime.MinValue;
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public static async Task<T> ExecuteAsync<T>(
        string operation,
        Func<Task<T>> action,
        CancellationToken ct = default
    )
    {
        await Semaphore.WaitAsync(ct);
        try
        {
            await ApplyThrottleAsync(ct);

            ResiliencePipeline<T> pipeline = CreateAsyncPipeline<T>(operation);
            return await pipeline.ExecuteAsync(async _ => await action(), ct);
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

    private static ResiliencePipeline<T> CreateSyncPipeline<T>(string operation) =>
        new ResiliencePipelineBuilder<T>().AddRetry(CreateRetryOptions<T>(operation)).Build();

    public static T Execute<T>(string operation, Func<T> action, CancellationToken ct = default)
    {
        Semaphore.Wait(ct);
        try
        {
            ApplyThrottle();

            ResiliencePipeline<T> pipeline = CreateSyncPipeline<T>(operation);
            return pipeline.Execute(_ => action(), ct);
        }
        finally
        {
            lastCallTime = DateTime.Now;
            Semaphore.Release();
        }
    }

    public static void Execute(string operation, Action action, CancellationToken ct = default) =>
        Execute(
            operation,
            () =>
            {
                action();
                return true;
            },
            ct
        );

    private static void ApplyThrottle()
    {
        TimeSpan elapsed = DateTime.Now - lastCallTime;
        if (elapsed < ThrottleDelay)
            Thread.Sleep(ThrottleDelay - elapsed);
    }

    private static RetryStrategyOptions<T> CreateRetryOptions<T>(string operation) =>
        new()
        {
            MaxRetryAttempts = MAX_RETRIES,
            Delay = BaseRetryDelay, // Base = 5s
            MaxDelay = MaxRetryDelay, // Cap at 5 minutes
            BackoffType = DelayBackoffType.Exponential, // 5s, 10s, 20s, 40s, 80s...
            UseJitter = true, // Add randomness to prevent thundering herd
            ShouldHandle = new PredicateBuilder<T>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutException>()
                .Handle<IOException>()
                .Handle<SocketException>()
                .HandleInner<HttpRequestException>()
                .HandleInner<TimeoutException>()
                .HandleInner<IOException>()
                .HandleInner<SocketException>(),
            OnRetry = args =>
            {
                string message = args.Outcome.Exception?.Message ?? "Unknown error";

                if (IsFatalQuotaError(message))
                {
                    string serviceName = operation.Split('.')[0];
                    throw new DailyQuotaExceededException(serviceName, message);
                }

                Console.Warning(
                    "{0} failed (attempt {1}/{2}): {3}",
                    operation,
                    args.AttemptNumber + 1,
                    MAX_RETRIES,
                    message
                );
                Console.Info(
                    "Retrying in {0:F0}s (at {1:HH:mm:ss})",
                    args.RetryDelay.TotalSeconds,
                    DateTime.Now.Add(args.RetryDelay)
                );

                return ValueTask.CompletedTask;
            },
        };

    private static async Task ApplyThrottleAsync(CancellationToken ct)
    {
        TimeSpan elapsed = DateTime.Now - lastCallTime;
        if (elapsed < ThrottleDelay)
            await Task.Delay(ThrottleDelay - elapsed, ct);
    }

    public static bool IsFatalQuotaError(string message) =>
        message.Contains("daily limit", StringComparison.OrdinalIgnoreCase)
        || message.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase)
        || (
            message.Contains("quota", StringComparison.OrdinalIgnoreCase)
            && message.Contains("day", StringComparison.OrdinalIgnoreCase)
        );
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
