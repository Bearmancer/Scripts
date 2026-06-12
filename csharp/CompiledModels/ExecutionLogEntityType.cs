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
	public static partial class ExecutionLogEntityType
	{
		public static RuntimeEntityType Create(
			RuntimeModel model,
			RuntimeEntityType baseEntityType = null
		)
		{
			var runtimeEntityType = model.AddEntityType(
				"Scripts.Data.Entities.ExecutionLog",
				typeof(ExecutionLog),
				baseEntityType,
				propertyCount: 5,
				unnamedIndexCount: 2,
				keyCount: 1
			);

			var id = runtimeEntityType.AddProperty(
				"Id",
				typeof(int),
				propertyInfo: typeof(ExecutionLog).GetProperty(
					"Id",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(ExecutionLog).GetField(
					"<Id>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				valueGenerated: ValueGenerated.OnAdd,
				afterSaveBehavior: PropertySaveBehavior.Throw,
				sentinel: 0
			);
			id.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
			);

			var exitCode = runtimeEntityType.AddProperty(
				"ExitCode",
				typeof(int),
				propertyInfo: typeof(ExecutionLog).GetProperty(
					"ExitCode",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(ExecutionLog).GetField(
					"<ExitCode>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				sentinel: 0
			);
			exitCode.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.None
			);
			exitCode.AddAnnotation("Relational:ColumnType", "integer");

			var payload = runtimeEntityType.AddProperty(
				"Payload",
				typeof(JsonDocument),
				propertyInfo: typeof(ExecutionLog).GetProperty(
					"Payload",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(ExecutionLog).GetField(
					"<Payload>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				)
			);
			payload.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.None
			);
			payload.AddAnnotation("Relational:ColumnType", "jsonb");

			var sessionId = runtimeEntityType.AddProperty(
				"SessionId",
				typeof(string),
				propertyInfo: typeof(ExecutionLog).GetProperty(
					"SessionId",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(ExecutionLog).GetField(
					"<SessionId>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				)
			);
			sessionId.SetComparer(
				new ValueComparer<string>(
					bool (string l, string r) => string.Equals(l, r, StringComparison.Ordinal),
					int (string v) => (v == null ? 0 : StringComparer.Ordinal.GetHashCode(v)),
					string (string v) => v
				)
			);
			sessionId.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.None
			);
			sessionId.AddAnnotation("Relational:ColumnType", "text");

			var timestamp = runtimeEntityType.AddProperty(
				"Timestamp",
				typeof(DateTimeOffset),
				propertyInfo: typeof(ExecutionLog).GetProperty(
					"Timestamp",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(ExecutionLog).GetField(
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

			var index = runtimeEntityType.AddIndex(new[] { sessionId });
			index.AddAnnotation("Relational:Name", "idx_execution_logs_session_id");

			var index0 = runtimeEntityType.AddIndex(new[] { timestamp });
			index0.AddAnnotation("Relational:Name", "idx_execution_logs_timestamp");

			return runtimeEntityType;
		}

		public static void CreateAnnotations(RuntimeEntityType runtimeEntityType)
		{
			runtimeEntityType.AddAnnotation("Relational:FunctionName", null);
			runtimeEntityType.AddAnnotation("Relational:Schema", null);
			runtimeEntityType.AddAnnotation("Relational:SqlQuery", null);
			runtimeEntityType.AddAnnotation("Relational:TableName", "execution_logs");
			runtimeEntityType.AddAnnotation("Relational:ViewName", null);
			runtimeEntityType.AddAnnotation("Relational:ViewSchema", null);

			Customize(runtimeEntityType);
		}

		static partial void Customize(RuntimeEntityType runtimeEntityType);
	}
}
