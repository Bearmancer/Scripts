using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Npgsql;
using Scripts.Data;

namespace Scripts.Mcp.Resources;





[McpServerResourceType]
internal sealed class ConnectionStatusResource(ScriptsDbContext db)
{
	private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

	[McpServerResource(
		UriTemplate = "pg://connection/status",
		Name = "Connection Status",
		MimeType = "application/json")]
	[Description("Returns the current PostgreSQL connection status including connectivity, server version, database name, and latency.")]
	public async Task<string> GetConnectionStatus()
	{
		try
		{
			var canConnect = await db.Database.CanConnectAsync();
			if (!canConnect)
			{
				return JsonSerializer.Serialize(new
				{
					connected = false,
					error = "Cannot connect to database."
				}, s_jsonOptions);
			}

			var conn = db.Database.GetDbConnection();
			var serverVersion = conn.ServerVersion ?? "unknown";
			var database = conn.Database;
			var dataSource = conn.DataSource;

			
			var sw = System.Diagnostics.Stopwatch.StartNew();
			await db.Database.ExecuteSqlRawAsync("SELECT 1");
			sw.Stop();

			return JsonSerializer.Serialize(new
			{
				connected = true,
				database,
				server = dataSource,
				serverVersion,
				latencyMs = sw.ElapsedMilliseconds,
				connectionString = MaskConnectionString(conn.ConnectionString)
			}, s_jsonOptions);
		}
		catch (NpgsqlException ex)
		{
			return JsonSerializer.Serialize(new
			{
				connected = false,
				error = $"PostgreSQL error: {ex.Message}"
			}, s_jsonOptions);
		}
	}

	private static string MaskConnectionString(string? connStr)
	{
		if (string.IsNullOrWhiteSpace(connStr)) return "N/A";
		return System.Text.RegularExpressions.Regex.Replace(
			connStr,
			"(Password=)[^;]*",
			"$1***",
			System.Text.RegularExpressions.RegexOptions.IgnoreCase);
	}
}
