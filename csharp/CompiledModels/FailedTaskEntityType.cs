using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Scripts.Data.Entities;

#pragma warning disable 219, 612, 618
#nullable disable

namespace MyCompiledModels
{
	[EntityFrameworkInternal]
	public static partial class FailedTaskEntityType
	{
		public static RuntimeEntityType Create(
			RuntimeModel model,
			RuntimeEntityType baseEntityType = null
		)
		{
			var runtimeEntityType = model.AddEntityType(
				"Scripts.Data.Entities.FailedTask",
				typeof(FailedTask),
				baseEntityType,
				propertyCount: 4,
				unnamedIndexCount: 2,
				keyCount: 1
			);

			var id = runtimeEntityType.AddProperty(
				"Id",
				typeof(Guid),
				propertyInfo: typeof(FailedTask).GetProperty(
					"Id",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(FailedTask).GetField(
					"<Id>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				valueGenerated: ValueGenerated.OnAdd,
				afterSaveBehavior: PropertySaveBehavior.Throw,
				sentinel: new Guid("00000000-0000-0000-0000-000000000000")
			);
			id.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);

			var errorMessage = runtimeEntityType.AddProperty(
				"ErrorMessage",
				typeof(string),
				propertyInfo: typeof(FailedTask).GetProperty(
					"ErrorMessage",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(FailedTask).GetField(
					"<ErrorMessage>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				)
			);
			errorMessage.SetComparer(
				new ValueComparer<string>(
					bool (string l, string r) => string.Equals(l, r, StringComparison.Ordinal),
					int (string v) => (v == null ? 0 : StringComparer.Ordinal.GetHashCode(v)),
					string (string v) => v
				)
			);
			errorMessage.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.None
			);
			errorMessage.AddAnnotation("Relational:ColumnType", "text");

			var taskName = runtimeEntityType.AddProperty(
				"TaskName",
				typeof(string),
				propertyInfo: typeof(FailedTask).GetProperty(
					"TaskName",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(FailedTask).GetField(
					"<TaskName>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				)
			);
			taskName.SetComparer(
				new ValueComparer<string>(
					bool (string l, string r) => string.Equals(l, r, StringComparison.Ordinal),
					int (string v) => (v == null ? 0 : StringComparer.Ordinal.GetHashCode(v)),
					string (string v) => v
				)
			);
			taskName.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.None
			);
			taskName.AddAnnotation("Relational:ColumnType", "text");

			var timestamp = runtimeEntityType.AddProperty(
				"Timestamp",
				typeof(DateTimeOffset),
				propertyInfo: typeof(FailedTask).GetProperty(
					"Timestamp",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(FailedTask).GetField(
					"<Timestamp>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				valueGenerated: ValueGenerated.OnAdd,
				sentinel: new DateTimeOffset(
					new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
					new TimeSpan(0, 0, 0, 0, 0)
				)
			);
			timestamp.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.None
			);
			timestamp.AddAnnotation("Relational:ColumnType", "timestamptz");
			timestamp.AddAnnotation("Relational:DefaultValueSql", "CURRENT_TIMESTAMP");

			var key = runtimeEntityType.AddKey(new[] { id });
			runtimeEntityType.SetPrimaryKey(key);

			var index = runtimeEntityType.AddIndex(new[] { taskName });
			index.AddAnnotation("Relational:Name", "idx_failed_tasks_task_name");

			var index0 = runtimeEntityType.AddIndex(new[] { timestamp });
			index0.AddAnnotation("Relational:Name", "idx_failed_tasks_timestamp");

			return runtimeEntityType;
		}

		public static void CreateAnnotations(RuntimeEntityType runtimeEntityType)
		{
			runtimeEntityType.AddAnnotation("Relational:FunctionName", null);
			runtimeEntityType.AddAnnotation("Relational:Schema", null);
			runtimeEntityType.AddAnnotation("Relational:SqlQuery", null);
			runtimeEntityType.AddAnnotation("Relational:TableName", "failed_tasks");
			runtimeEntityType.AddAnnotation("Relational:ViewName", null);
			runtimeEntityType.AddAnnotation("Relational:ViewSchema", null);

			Customize(runtimeEntityType);
		}

		static partial void Customize(RuntimeEntityType runtimeEntityType);
	}
}
