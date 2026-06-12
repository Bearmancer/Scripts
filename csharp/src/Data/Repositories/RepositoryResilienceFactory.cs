using Npgsql;
using Polly.CircuitBreaker;

namespace Scripts.Data.Repositories;

internal static class RepositoryResilienceFactory
{
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
