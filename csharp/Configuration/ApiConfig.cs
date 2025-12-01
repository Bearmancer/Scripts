namespace CSharpScripts.Configuration;

internal sealed class DailyQuotaExceededException(string service, string message)
    : Exception($"Daily quota exceeded for {service}. Try again tomorrow. Original: {message}")
{
    internal string Service { get; } = service;
}

internal sealed class RetryExhaustedException(
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

internal static class ApiConfig
{
    internal const int MaxRetries = 10;
    internal const int BaseDelaySeconds = 60;
    internal const int ApiDelayMs = 2000;

    internal static bool IsDailyQuotaExceeded(string message) =>
        message.Contains("daily limit", StringComparison.OrdinalIgnoreCase)
        || message.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase)
        || (
            message.Contains("quota", StringComparison.OrdinalIgnoreCase)
            && message.Contains("day", StringComparison.OrdinalIgnoreCase)
        );

    internal static bool IsRateLimitError(string message) =>
        !IsDailyQuotaExceeded(message)
        && (
            message.Contains("quota", StringComparison.OrdinalIgnoreCase)
            || message.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || message.Contains("too many requests", StringComparison.OrdinalIgnoreCase)
            || message.Contains("backend service failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("429", StringComparison.OrdinalIgnoreCase)
        );

    internal static void Delay(ServiceType service) => Thread.Sleep(ApiDelayMs);

    internal static T ExecuteWithRetry<T>(
        string operationName,
        Func<T> action,
        Action postAction,
        CancellationToken ct = default
    )
    {
        var attemptsMade = 0;
        var totalWaitTime = TimeSpan.Zero;

        var serviceName = operationName.Split('.')[0];

        var result = Policy
            .Handle<Exception>(ex =>
            {
                if (IsDailyQuotaExceeded(ex.Message))
                    throw new DailyQuotaExceededException(serviceName, ex.Message);

                return IsRateLimitError(ex.Message);
            })
            .WaitAndRetry(
                retryCount: MaxRetries,
                sleepDurationProvider: attempt =>
                {
                    var delay = TimeSpan.FromSeconds(BaseDelaySeconds * Math.Pow(2, attempt - 1));
                    totalWaitTime += delay;
                    return delay;
                },
                onRetry: (ex, delay, attempt, _) =>
                {
                    attemptsMade = attempt;

                    Logger.Warning(
                        "{0} rate limited (attempt {1}/{2}): {3}",
                        operationName,
                        attempt,
                        MaxRetries,
                        ex.Message
                    );
                    Logger.Info(
                        "Waiting {0} (resume at {1:HH:mm:ss})",
                        delay.ToString(@"hh\:mm\:ss"),
                        DateTime.Now.Add(delay)
                    );

                    for (var remaining = (int)delay.TotalSeconds; remaining > 0; remaining--)
                    {
                        if (ct.IsCancellationRequested)
                            break;
                        Thread.Sleep(millisecondsTimeout: 1000);
                    }
                }
            )
            .ExecuteAndCapture(action);

        if (result.Outcome == OutcomeType.Failure)
        {
            var ex = result.FinalException;

            if (IsDailyQuotaExceeded(ex.Message))
                throw new DailyQuotaExceededException(serviceName, ex.Message);

            if (attemptsMade >= MaxRetries)
                throw new RetryExhaustedException(operationName, attemptsMade, totalWaitTime, ex);

            throw new InvalidOperationException($"{operationName} failed: {ex.Message}", ex);
        }

        postAction();
        return result.Result;
    }

    internal static void ExecuteWithRetry(
        string operationName,
        Action action,
        Action postAction,
        CancellationToken ct = default
    ) =>
        ExecuteWithRetry(
            operationName: operationName,
            action: () =>
            {
                action();
                return true;
            },
            postAction: postAction,
            ct: ct
        );
}
