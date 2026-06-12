namespace Scripts.Tests.Attributes;

internal sealed class RequiresPgConnStrAttribute()
	: SkipAttribute("PGCONNSTR not set — start Docker, load .env, then re-run")
{
	public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
		Task.FromResult(
			string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("PGCONNSTR"))
		);
}
