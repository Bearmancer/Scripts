using TUnit.Core;

namespace Scripts.Tests.Attributes;

/// <summary>
/// Skips the test when PGCONNSTR is not set in the environment.
/// Used for integration tests that require a live PostgreSQL connection.
/// Ensure Docker is running and .env is loaded before running these tests.
/// </summary>
internal sealed class RequiresPgConnStrAttribute()
    : SkipAttribute("PGCONNSTR not set — start Docker, load .env, then re-run")
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
        Task.FromResult(string.IsNullOrWhiteSpace(
            System.Environment.GetEnvironmentVariable("PGCONNSTR")));
}
