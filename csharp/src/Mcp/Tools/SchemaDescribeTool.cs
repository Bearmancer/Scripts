using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Scripts.Data;

namespace Scripts.Mcp.Tools;





[McpServerToolType]
internal sealed class SchemaDescribeTool(ScriptsDbContext db)
{
	private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

	[McpServerTool]
	[Description("Describes the database schema including tables, columns, data types, primary keys, foreign keys, and indexes. Optionally filter by table name.")]
	public string schema_describe(
		[Description("Optional table name to describe. If omitted, describes all tables.")] string? tableName = null)
	{
		try
		{
			var model = db.Model;
			var entities = model.GetEntityTypes()
				.Where(e => string.IsNullOrWhiteSpace(tableName)
					|| e.GetTableName()?.Equals(tableName, StringComparison.OrdinalIgnoreCase) == true
					|| e.ClrType.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase))
				.ToList();

			if (entities.Count == 0)
			{
				return JsonSerializer.Serialize(new
				{
					error = $"No table found matching '{tableName}'.",
					availableTables = model.GetEntityTypes()
						.Select(e => new { schema = e.GetSchema(), table = e.GetTableName() })
						.Distinct()
						.ToList()
				}, s_jsonOptions);
			}

			var result = entities.Select(entity =>
			{
				var tableNameStr = entity.GetTableName() ?? entity.ClrType.Name;
				var schema = entity.GetSchema() ?? "public";

				var columns = entity.GetProperties().Select(p => new
				{
					name = p.Name,
					columnName = p.GetColumnName(),
					type = p.GetColumnType() ?? p.ClrType.Name,
					isNullable = p.IsNullable,
					isPrimaryKey = p.IsPrimaryKey(),
					maxLength = p.GetMaxLength(),
					defaultValue = p.GetDefaultValue()?.ToString()
				}).ToList();

				var foreignKeys = entity.GetForeignKeys().Select(fk => new
				{
					constraintName = fk.GetConstraintName(),
					columns = fk.Properties.Select(p => p.GetColumnName()).ToList(),
					principalTable = fk.PrincipalEntityType.GetTableName(),
					principalColumns = fk.PrincipalKey.Properties.Select(p => p.GetColumnName()).ToList(),
					deleteBehavior = fk.DeleteBehavior.ToString()
				}).ToList();

				var indexes = entity.GetIndexes().Select(ix => new
				{
					name = ix.GetDatabaseName(),
					columns = ix.Properties.Select(p => p.GetColumnName()).ToList(),
					isUnique = ix.IsUnique
				}).ToList();

				return new
				{
					entity = entity.ClrType.Name,
					schema,
					table = tableNameStr,
					columns,
					foreignKeys,
					indexes
				};
			}).ToList();

			return JsonSerializer.Serialize(result, s_jsonOptions);
		}
		catch (InvalidOperationException ex)
		{
			return JsonSerializer.Serialize(new { error = $"Model error: {ex.Message}" }, s_jsonOptions);
		}
	}
}
