namespace Scripts.Tests.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class RequiresPgConnStrAttribute : Attribute
{
	public static void EnsureSet()
	{
		if (string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("PGCONNSTR")))
		{
			throw new InvalidOperationException(
				"PGCONNSTR is not set. Start Postgres (Docker or local) and ensure PGCONNSTR "
					+ "points to a writable database. Example: "
					+ "Host=localhost;Database=pg_db;Username=lance;Password=lance"
			);
		}
	}
}
