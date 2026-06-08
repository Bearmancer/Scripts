using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Scripts.Data;

namespace Scripts.Mcp.Resources;





[McpServerResourceType]
internal sealed class EntityMetadataResource(ScriptsDbContext db)
{
	private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

	[McpServerResource(
		UriTemplate = "pg://entities/metadata",
		Name = "Entity Metadata",
		MimeType = "application/json")]
	[Description("Returns metadata about all EF Core entities including DbSets, properties, relationships, and table mappings.")]
	public string GetEntityMetadata()
	{
		try
		{
			var model = db.Model;
			var entities = model.GetEntityTypes().Select(entity =>
			{
				var props = entity.GetProperties().Select(p => new
				{
					name = p.Name,
					columnName = p.GetColumnName(),
					type = p.ClrType.Name,
					columnType = p.GetColumnType(),
					isNullable = p.IsNullable,
					isPrimaryKey = p.IsPrimaryKey(),
					maxLength = p.GetMaxLength()
				}).ToList();

				var navProperties = entity.GetNavigations().Select(n => new
				{
					name = n.Name,
					targetEntity = n.TargetEntityType.ClrType.Name,
					isCollection = n.IsCollection,
					foreignKey = n.ForeignKey?.GetConstraintName()
				}).ToList();

				var relationships = entity.GetForeignKeys().Select(fk => new
				{
					dependentEntity = entity.ClrType.Name,
					principalEntity = fk.PrincipalEntityType.ClrType.Name,
					foreignKeyProperties = fk.Properties.Select(p => p.Name).ToList(),
					principalKeyProperties = fk.PrincipalKey.Properties.Select(p => p.Name).ToList(),
					deleteBehavior = fk.DeleteBehavior.ToString(),
					constraintName = fk.GetConstraintName()
				}).ToList();

				return new
				{
					entity = entity.ClrType.Name,
					table = entity.GetTableName(),
					schema = entity.GetSchema() ?? "public",
					properties = props,
					navigationProperties = navProperties,
					relationships
				};
			}).ToList();

			return JsonSerializer.Serialize(new
			{
				entityCount = entities.Count,
				entities
			}, s_jsonOptions);
		}
		catch (InvalidOperationException ex)
		{
			return JsonSerializer.Serialize(new { error = $"Model error: {ex.Message}" }, s_jsonOptions);
		}
	}
}
