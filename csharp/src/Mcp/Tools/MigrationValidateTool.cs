using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Npgsql;
using Scripts.Data;

namespace Scripts.Mcp.Tools;





[McpServerToolType]
internal sealed class MigrationValidateTool(ScriptsDbContext db)
{
	private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

	[McpServerTool]
	[Description("Validates the database migration status. Reports applied migrations, pending migrations, and whether the database schema is in sync with the model.")]
	public async Task<string> migration_validate(CancellationToken cancellationToken = default)
	{
		try
		{
			var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
			var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);

			bool canConnect = await db.Database.CanConnectAsync(cancellationToken);

			string status;
			if (!canConnect)
				status = "disconnected";
			else if (pending.Any())
				status = "pending_migrations";
			else
				status = "up_to_date";

			return JsonSerializer.Serialize(new
			{
				status,
				canConnect,
				appliedMigrations = applied.ToList(),
				appliedCount = applied.Count(),
				pendingMigrations = pending.ToList(),
				pendingCount = pending.Count()
			}, s_jsonOptions);
		}
		catch (NpgsqlException ex)
		{
			return JsonSerializer.Serialize(new
			{
				status = "error",
				error = $"PostgreSQL error: {ex.Message}"
			}, s_jsonOptions);
		}
		catch (InvalidOperationException ex)
		{
			return JsonSerializer.Serialize(new
			{
				status = "error",
				error = $"Migration error: {ex.Message}"
			}, s_jsonOptions);
		}
	}
}
