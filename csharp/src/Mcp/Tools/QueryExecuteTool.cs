using System.ComponentModel;
using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Npgsql;
using Scripts.Data;

namespace Scripts.Mcp.Tools;





[McpServerToolType]
internal sealed class QueryExecuteTool(ScriptsDbContext db)
{
	private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

	[McpServerTool]
	[Description("Executes a read-only SQL query against the PostgreSQL database and returns results as JSON. Supports SELECT, WITH (CTE), EXPLAIN, SHOW, and TABLE statements. Parameters can be passed as @p0, @p1, etc.")]
	public async Task<string> query_execute(
		[Description("The SQL query to execute. Use @p0, @p1, etc. for parameters.")] string sql,
		[Description("Optional JSON array of parameter values, e.g. [\"value1\", 42].")] string? parameters = null,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(sql))
			return JsonSerializer.Serialize(new { error = "SQL query cannot be empty." }, s_jsonOptions);

		
		var trimmed = sql.TrimStart();
		var upperStart = trimmed.ToUpperInvariant();
		bool isReadOnly = upperStart.StartsWith("SELECT")
			|| upperStart.StartsWith("WITH")
			|| upperStart.StartsWith("EXPLAIN")
			|| upperStart.StartsWith("SHOW")
			|| upperStart.StartsWith("TABLE");

		if (!isReadOnly)
			return JsonSerializer.Serialize(new { error = "Only read-only queries (SELECT, WITH, EXPLAIN, SHOW, TABLE) are allowed." }, s_jsonOptions);

		try
		{
			var conn = db.Database.GetDbConnection();
			if (conn.State != ConnectionState.Open)
				await conn.OpenAsync(cancellationToken);

			await using var command = conn.CreateCommand();
			command.CommandText = sql;
			command.CommandTimeout = 30;

			
			if (!string.IsNullOrWhiteSpace(parameters))
			{
				var paramValues = JsonSerializer.Deserialize<JsonElement[]>(parameters);
				if (paramValues is not null)
				{
					for (int i = 0; i < paramValues.Length; i++)
					{
						var param = command.CreateParameter();
						param.ParameterName = $"p{i}";
						param.Value = paramValues[i].ValueKind switch
						{
							JsonValueKind.String => paramValues[i].GetString() ?? string.Empty,
							JsonValueKind.Number => paramValues[i].GetInt64(),
							JsonValueKind.True => true,
							JsonValueKind.False => false,
							JsonValueKind.Null => DBNull.Value,
							_ => paramValues[i].GetString() ?? (object)DBNull.Value,
						};
						command.Parameters.Add(param);
					}
				}
			}

			await using var reader = await command.ExecuteReaderAsync(cancellationToken);
			var rows = new List<Dictionary<string, object?>>();
			var columns = new List<string>();

			for (int i = 0; i < reader.FieldCount; i++)
				columns.Add(reader.GetName(i));

			while (await reader.ReadAsync(cancellationToken))
			{
				var row = new Dictionary<string, object?>();
				for (int i = 0; i < reader.FieldCount; i++)
				{
					var value = reader.GetValue(i);
					row[columns[i]] = value is DBNull ? null : value;
				}
				rows.Add(row);
			}

			return JsonSerializer.Serialize(new
			{
				columns,
				rows,
				rowCount = rows.Count
			}, s_jsonOptions);
		}
		catch (NpgsqlException ex)
		{
			return JsonSerializer.Serialize(new { error = $"PostgreSQL error: {ex.Message}" }, s_jsonOptions);
		}
		catch (DBConcurrencyException ex)
		{
			return JsonSerializer.Serialize(new { error = $"Concurrency error: {ex.Message}" }, s_jsonOptions);
		}
	}
}
