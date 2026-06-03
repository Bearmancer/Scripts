using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Scripts.Data.Repositories;

/// <summary>
/// Factory for creating Polly resilience pipelines configured for database operations.
/// Handles transient Npgsql errors with retry and circuit breaker patterns.
/// </summary>
internal static class RepositoryResilienceFactory
{
	/// <summary>
	/// Creates a resilience pipeline for database operations.
	/// Retries on transient connection errors, opens circuit breaker on repeated failures.
	/// </summary>
	public static ResiliencePipeline CreateDatabasePipeline()
	{
		return new ResiliencePipelineBuilder()
			.AddRetry(
				new RetryStrategyOptions
				{
					MaxRetryAttempts = 3,
					Delay = TimeSpan.FromSeconds(1),
					BackoffType = DelayBackoffType.Exponential,
					UseJitter = true,
					ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>(IsTransientError),
				}
			)
			.AddCircuitBreaker(
				new CircuitBreakerStrategyOptions
				{
					FailureRatio = 0.5,
					SamplingDuration = TimeSpan.FromSeconds(30),
					MinimumThroughput = 5,
					BreakDuration = TimeSpan.FromSeconds(30),
				}
			)
			.Build();
	}

	/// <summary>
	/// Determines if an Npgsql exception is transient and should be retried.
	/// </summary>
	private static bool IsTransientError(NpgsqlException ex)
	{
		return ex.SqlState switch
		{
			"53300" => true,
			"08000" => true,
			"08003" => true,
			"08006" => true,
			_ => false,
		};
	}
}
