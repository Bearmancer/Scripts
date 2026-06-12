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
	public static partial class VideoEntityType
	{
		public static RuntimeEntityType Create(
			RuntimeModel model,
			RuntimeEntityType baseEntityType = null
		)
		{
			var runtimeEntityType = model.AddEntityType(
				"Scripts.Data.Entities.Video",
				typeof(Video),
				baseEntityType,
				propertyCount: 8,
				unnamedIndexCount: 4,
				keyCount: 1
			);

			var id = runtimeEntityType.AddProperty(
				"Id",
				typeof(int),
				propertyInfo: typeof(Video).GetProperty(
					"Id",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(Video).GetField(
					"<Id>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				valueGenerated: ValueGenerated.OnAdd,
				afterSaveBehavior: PropertySaveBehavior.Throw,
				sentinel: 0
			);
			id.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
			);

			var channelName = runtimeEntityType.AddProperty(
				"ChannelName",
				typeof(string),
				propertyInfo: typeof(Video).GetProperty(
					"ChannelName",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(Video).GetField(
					"<ChannelName>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				)
			);
			channelName.SetComparer(
				new ValueComparer<string>(
					bool (string l, string r) => string.Equals(l, r, StringComparison.Ordinal),
					int (string v) => (v == null ? 0 : StringComparer.Ordinal.GetHashCode(v)),
					string (string v) => v
				)
			);
			channelName.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.None
			);
			channelName.AddAnnotation("Relational:ColumnType", "text");

			var description = runtimeEntityType.AddProperty(
				"Description",
				typeof(string),
				propertyInfo: typeof(Video).GetProperty(
					"Description",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(Video).GetField(
					"<Description>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				)
			);
			description.SetComparer(
				new ValueComparer<string>(
					bool (string l, string r) => string.Equals(l, r, StringComparison.Ordinal),
					int (string v) => (v == null ? 0 : StringComparer.Ordinal.GetHashCode(v)),
					string (string v) => v
				)
			);
			description.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.None
			);
			description.AddAnnotation("Relational:ColumnType", "text");

			var metadata = runtimeEntityType.AddProperty(
				"Metadata",
				typeof(JsonDocument),
				propertyInfo: typeof(Video).GetProperty(
					"Metadata",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(Video).GetField(
					"<Metadata>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				nullable: true
			);
			metadata.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.None
			);
			metadata.AddAnnotation("Relational:ColumnType", "jsonb");

			var syncedAt = runtimeEntityType.AddProperty(
				"SyncedAt",
				typeof(DateTimeOffset?),
				propertyInfo: typeof(Video).GetProperty(
					"SyncedAt",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(Video).GetField(
					"<SyncedAt>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				nullable: true
			);
			syncedAt.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.None
			);
			syncedAt.AddAnnotation("Relational:ColumnType", "timestamptz");

			var title = runtimeEntityType.AddProperty(
				"Title",
				typeof(string),
				propertyInfo: typeof(Video).GetProperty(
					"Title",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(Video).GetField(
					"<Title>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				)
			);
			title.SetComparer(
				new ValueComparer<string>(
					bool (string l, string r) => string.Equals(l, r, StringComparison.Ordinal),
					int (string v) => (v == null ? 0 : StringComparer.Ordinal.GetHashCode(v)),
					string (string v) => v
				)
			);
			title.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.None
			);
			title.AddAnnotation("Relational:ColumnType", "text");

			var uploadDate = runtimeEntityType.AddProperty(
				"UploadDate",
				typeof(DateOnly?),
				propertyInfo: typeof(Video).GetProperty(
					"UploadDate",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(Video).GetField(
					"<UploadDate>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				nullable: true
			);
			uploadDate.AddAnnotation(
				"Npgsql:ValueGenerationStrategy",
				NpgsqlValueGenerationStrategy.None
			);
			uploadDate.AddAnnotation("Relational:ColumnType", "date");

			var url = runtimeEntityType.AddProperty(
				"Url",
				typeof(string),
				propertyInfo: typeof(Video).GetProperty(
					"Url",
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
				),
				fieldInfo: typeof(Video).GetField(
					"<Url>k__BackingField",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
				)
			);
			url.SetComparer(
				new ValueComparer<string>(
					bool (string l, string r) => string.Equals(l, r, StringComparison.Ordinal),
					int (string v) => (v == null ? 0 : StringComparer.Ordinal.GetHashCode(v)),
					string (string v) => v
				)
			);
			url.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);
			url.AddAnnotation("Relational:ColumnType", "text");

			var key = runtimeEntityType.AddKey(new[] { id });
			runtimeEntityType.SetPrimaryKey(key);

			var index = runtimeEntityType.AddIndex(new[] { channelName });
			index.AddAnnotation("Relational:Name", "idx_videos_channel");

			var index0 = runtimeEntityType.AddIndex(new[] { title });
			index0.AddAnnotation("Relational:Filter", "true");
			index0.AddAnnotation("Relational:Name", "idx_videos_title_trgm");

			var index1 = runtimeEntityType.AddIndex(new[] { uploadDate });
			index1.AddAnnotation("Relational:Name", "idx_videos_upload_date");

			var index2 = runtimeEntityType.AddIndex(new[] { url }, unique: true);
			index2.AddAnnotation("Relational:Name", "idx_videos_url");

			return runtimeEntityType;
		}

		public static void CreateAnnotations(RuntimeEntityType runtimeEntityType)
		{
			runtimeEntityType.AddAnnotation("Relational:FunctionName", null);
			runtimeEntityType.AddAnnotation("Relational:Schema", "youtube");
			runtimeEntityType.AddAnnotation("Relational:SqlQuery", null);
			runtimeEntityType.AddAnnotation("Relational:TableName", "videos");
			runtimeEntityType.AddAnnotation("Relational:ViewName", null);
			runtimeEntityType.AddAnnotation("Relational:ViewSchema", null);

			Customize(runtimeEntityType);
		}

		static partial void Customize(RuntimeEntityType runtimeEntityType);
	}
}
