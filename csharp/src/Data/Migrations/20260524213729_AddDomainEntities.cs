using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CSharpScripts.src.Data.Migrations
{
	/// <inheritdoc />
	public partial class AddDomainEntities : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(name: "FK_Albums_Artists_ArtistId", table: "Albums");

			migrationBuilder.DropForeignKey(
				name: "FK_Scrobbles_Tracks_TrackId",
				table: "Scrobbles"
			);

			migrationBuilder.DropForeignKey(name: "FK_Tracks_Albums_AlbumId", table: "Tracks");

			migrationBuilder.DropForeignKey(name: "FK_Tracks_Artists_ArtistId", table: "Tracks");

			migrationBuilder.DropPrimaryKey(name: "PK_Videos", table: "Videos");

			migrationBuilder.DropPrimaryKey(name: "PK_Tracks", table: "Tracks");

			migrationBuilder.DropPrimaryKey(name: "PK_Scrobbles", table: "Scrobbles");

			migrationBuilder.DropPrimaryKey(name: "PK_Artists", table: "Artists");

			migrationBuilder.DropPrimaryKey(name: "PK_Albums", table: "Albums");

			migrationBuilder.DropPrimaryKey(name: "PK_SourceRecords", table: "SourceRecords");

			migrationBuilder.DropPrimaryKey(name: "PK_FiberyEntities", table: "FiberyEntities");

			migrationBuilder.DropPrimaryKey(name: "PK_FailedTasks", table: "FailedTasks");

			migrationBuilder.DropPrimaryKey(name: "PK_ExecutionLogs", table: "ExecutionLogs");

			migrationBuilder.DropColumn(name: "IsDeleted", table: "Videos");

			migrationBuilder.DropColumn(name: "CreatedAt", table: "FailedTasks");

			migrationBuilder.RenameTable(name: "Videos", newName: "videos");

			migrationBuilder.RenameTable(name: "Tracks", newName: "tracks");

			migrationBuilder.RenameTable(name: "Scrobbles", newName: "scrobbles");

			migrationBuilder.RenameTable(name: "Artists", newName: "artists");

			migrationBuilder.RenameTable(name: "Albums", newName: "albums");

			migrationBuilder.RenameTable(name: "SourceRecords", newName: "source_records");

			migrationBuilder.RenameTable(name: "FiberyEntities", newName: "fibery_entities");

			migrationBuilder.RenameTable(name: "FailedTasks", newName: "failed_tasks");

			migrationBuilder.RenameTable(name: "ExecutionLogs", newName: "execution_logs");

			migrationBuilder.RenameColumn(name: "YoutubeId", table: "videos", newName: "Url");

			migrationBuilder.RenameColumn(
				name: "PlaylistId",
				table: "videos",
				newName: "Description"
			);

			migrationBuilder.RenameIndex(
				name: "IX_Tracks_ArtistId",
				table: "tracks",
				newName: "IX_tracks_ArtistId"
			);

			migrationBuilder.RenameIndex(
				name: "IX_Tracks_AlbumId",
				table: "tracks",
				newName: "IX_tracks_AlbumId"
			);

			migrationBuilder.RenameIndex(
				name: "IX_Scrobbles_TrackId",
				table: "scrobbles",
				newName: "IX_scrobbles_TrackId"
			);

			migrationBuilder.RenameIndex(
				name: "IX_Albums_ArtistId",
				table: "albums",
				newName: "IX_albums_ArtistId"
			);

			migrationBuilder.RenameColumn(
				name: "Operation",
				table: "failed_tasks",
				newName: "TaskName"
			);

			migrationBuilder
				.AlterColumn<int>(
					name: "Id",
					table: "videos",
					type: "integer",
					nullable: false,
					oldClrType: typeof(int),
					oldType: "integer"
				)
				.Annotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
				)
				.OldAnnotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
				);

			migrationBuilder.AddColumn<string>(
				name: "ChannelName",
				table: "videos",
				type: "text",
				nullable: false,
				defaultValue: ""
			);

			migrationBuilder.AddColumn<JsonDocument>(
				name: "Metadata",
				table: "videos",
				type: "jsonb",
				nullable: true
			);

			migrationBuilder.AddColumn<DateTimeOffset>(
				name: "SyncedAt",
				table: "videos",
				type: "timestamptz",
				nullable: true
			);

			migrationBuilder.AddColumn<DateOnly>(
				name: "UploadDate",
				table: "videos",
				type: "date",
				nullable: true
			);

			migrationBuilder
				.AlterColumn<int>(
					name: "Id",
					table: "tracks",
					type: "integer",
					nullable: false,
					oldClrType: typeof(int),
					oldType: "integer"
				)
				.Annotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
				)
				.OldAnnotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
				);

			migrationBuilder.AlterColumn<DateTimeOffset>(
				name: "ScrobbledAt",
				table: "scrobbles",
				type: "timestamptz",
				nullable: false,
				oldClrType: typeof(DateTimeOffset),
				oldType: "timestamp with time zone"
			);

			migrationBuilder.AlterColumn<string>(
				name: "Platform",
				table: "scrobbles",
				type: "varchar(50)",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "text"
			);

			migrationBuilder
				.AlterColumn<long>(
					name: "Id",
					table: "scrobbles",
					type: "bigint",
					nullable: false,
					oldClrType: typeof(long),
					oldType: "bigint"
				)
				.Annotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
				)
				.OldAnnotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
				);

			migrationBuilder
				.AlterColumn<int>(
					name: "Id",
					table: "artists",
					type: "integer",
					nullable: false,
					oldClrType: typeof(int),
					oldType: "integer"
				)
				.Annotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
				)
				.OldAnnotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
				);

			migrationBuilder
				.AlterColumn<int>(
					name: "Id",
					table: "albums",
					type: "integer",
					nullable: false,
					oldClrType: typeof(int),
					oldType: "integer"
				)
				.Annotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
				)
				.OldAnnotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
				);

			migrationBuilder.AlterColumn<Guid>(
				name: "Id",
				table: "source_records",
				type: "uuid",
				nullable: false,
				defaultValueSql: "gen_random_uuid()",
				oldClrType: typeof(Guid),
				oldType: "uuid"
			);

			migrationBuilder.AlterColumn<string>(
				name: "FiberyId",
				table: "fibery_entities",
				type: "varchar(255)",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "text"
			);

			migrationBuilder.AlterColumn<string>(
				name: "EntityType",
				table: "fibery_entities",
				type: "varchar(100)",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "text"
			);

			migrationBuilder.AlterColumn<Guid>(
				name: "Id",
				table: "fibery_entities",
				type: "uuid",
				nullable: false,
				defaultValueSql: "gen_random_uuid()",
				oldClrType: typeof(Guid),
				oldType: "uuid"
			);

			migrationBuilder.AddColumn<DateTimeOffset>(
				name: "Timestamp",
				table: "failed_tasks",
				type: "timestamptz",
				nullable: false,
				defaultValueSql: "CURRENT_TIMESTAMP"
			);

			migrationBuilder.AlterColumn<DateTimeOffset>(
				name: "Timestamp",
				table: "execution_logs",
				type: "timestamptz",
				nullable: false,
				defaultValueSql: "CURRENT_TIMESTAMP",
				oldClrType: typeof(DateTimeOffset),
				oldType: "timestamp with time zone"
			);

			migrationBuilder.AddPrimaryKey(name: "PK_videos", table: "videos", column: "Id");

			migrationBuilder.AddPrimaryKey(name: "PK_tracks", table: "tracks", column: "Id");

			migrationBuilder.AddPrimaryKey(name: "PK_scrobbles", table: "scrobbles", column: "Id");

			migrationBuilder.AddPrimaryKey(name: "PK_artists", table: "artists", column: "Id");

			migrationBuilder.AddPrimaryKey(name: "PK_albums", table: "albums", column: "Id");

			migrationBuilder.AddPrimaryKey(
				name: "PK_source_records",
				table: "source_records",
				column: "Id"
			);

			migrationBuilder.AddPrimaryKey(
				name: "PK_fibery_entities",
				table: "fibery_entities",
				column: "Id"
			);

			migrationBuilder.AddPrimaryKey(
				name: "PK_failed_tasks",
				table: "failed_tasks",
				column: "Id"
			);

			migrationBuilder.AddPrimaryKey(
				name: "PK_execution_logs",
				table: "execution_logs",
				column: "Id"
			);

			migrationBuilder.CreateIndex(
				name: "idx_videos_channel",
				table: "videos",
				column: "ChannelName"
			);

			migrationBuilder.CreateIndex(
				name: "idx_videos_title",
				table: "videos",
				column: "Title"
			);

			migrationBuilder.CreateIndex(
				name: "idx_videos_upload_date",
				table: "videos",
				column: "UploadDate"
			);

			migrationBuilder.CreateIndex(
				name: "idx_videos_url",
				table: "videos",
				column: "Url",
				unique: true
			);

			migrationBuilder.CreateIndex(
				name: "idx_tracks_artist_title",
				table: "tracks",
				columns: new[] { "ArtistId", "Title" },
				unique: true
			);

			migrationBuilder.CreateIndex(
				name: "idx_tracks_title",
				table: "tracks",
				column: "Title"
			);

			migrationBuilder.CreateIndex(
				name: "idx_scrobbles_platform",
				table: "scrobbles",
				column: "Platform"
			);

			migrationBuilder.CreateIndex(
				name: "idx_scrobbles_scrobbled_at",
				table: "scrobbles",
				column: "ScrobbledAt"
			);

			migrationBuilder.CreateIndex(
				name: "idx_scrobbles_timestamp",
				table: "scrobbles",
				columns: new[] { "TrackId", "ScrobbledAt" },
				unique: true
			);

			migrationBuilder.CreateIndex(
				name: "idx_artists_name",
				table: "artists",
				column: "Name",
				unique: true
			);

			migrationBuilder.CreateIndex(
				name: "idx_albums_release_date",
				table: "albums",
				column: "ReleaseDate"
			);

			migrationBuilder.CreateIndex(
				name: "idx_albums_title",
				table: "albums",
				columns: new[] { "ArtistId", "Title" },
				unique: true
			);

			migrationBuilder.CreateIndex(
				name: "idx_source_records_entity_type",
				table: "source_records",
				column: "EntityType"
			);

			migrationBuilder.CreateIndex(
				name: "idx_source_records_source_entity_type",
				table: "source_records",
				columns: new[] { "SourceId", "EntityType" },
				unique: true
			);

			migrationBuilder.CreateIndex(
				name: "idx_source_records_source_id",
				table: "source_records",
				column: "SourceId"
			);

			migrationBuilder.CreateIndex(
				name: "idx_fibery_entities_entity_type",
				table: "fibery_entities",
				column: "EntityType"
			);

			migrationBuilder.CreateIndex(
				name: "idx_fibery_entities_fibery_id_type",
				table: "fibery_entities",
				columns: new[] { "FiberyId", "EntityType" },
				unique: true
			);

			migrationBuilder.CreateIndex(
				name: "idx_failed_tasks_task_name",
				table: "failed_tasks",
				column: "TaskName"
			);

			migrationBuilder.CreateIndex(
				name: "idx_failed_tasks_timestamp",
				table: "failed_tasks",
				column: "Timestamp"
			);

			migrationBuilder.CreateIndex(
				name: "idx_execution_logs_session_id",
				table: "execution_logs",
				column: "SessionId"
			);

			migrationBuilder.CreateIndex(
				name: "idx_execution_logs_timestamp",
				table: "execution_logs",
				column: "Timestamp"
			);

			migrationBuilder.AddForeignKey(
				name: "FK_albums_artists_ArtistId",
				table: "albums",
				column: "ArtistId",
				principalTable: "artists",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade
			);

			migrationBuilder.AddForeignKey(
				name: "FK_scrobbles_tracks_TrackId",
				table: "scrobbles",
				column: "TrackId",
				principalTable: "tracks",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade
			);

			migrationBuilder.AddForeignKey(
				name: "FK_tracks_albums_AlbumId",
				table: "tracks",
				column: "AlbumId",
				principalTable: "albums",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade
			);

			migrationBuilder.AddForeignKey(
				name: "FK_tracks_artists_ArtistId",
				table: "tracks",
				column: "ArtistId",
				principalTable: "artists",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade
			);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(name: "FK_albums_artists_ArtistId", table: "albums");

			migrationBuilder.DropForeignKey(
				name: "FK_scrobbles_tracks_TrackId",
				table: "scrobbles"
			);

			migrationBuilder.DropForeignKey(name: "FK_tracks_albums_AlbumId", table: "tracks");

			migrationBuilder.DropForeignKey(name: "FK_tracks_artists_ArtistId", table: "tracks");

			migrationBuilder.DropPrimaryKey(name: "PK_videos", table: "videos");

			migrationBuilder.DropIndex(name: "idx_videos_channel", table: "videos");

			migrationBuilder.DropIndex(name: "idx_videos_title", table: "videos");

			migrationBuilder.DropIndex(name: "idx_videos_upload_date", table: "videos");

			migrationBuilder.DropIndex(name: "idx_videos_url", table: "videos");

			migrationBuilder.DropPrimaryKey(name: "PK_tracks", table: "tracks");

			migrationBuilder.DropIndex(name: "idx_tracks_artist_title", table: "tracks");

			migrationBuilder.DropIndex(name: "idx_tracks_title", table: "tracks");

			migrationBuilder.DropPrimaryKey(name: "PK_scrobbles", table: "scrobbles");

			migrationBuilder.DropIndex(name: "idx_scrobbles_platform", table: "scrobbles");

			migrationBuilder.DropIndex(name: "idx_scrobbles_scrobbled_at", table: "scrobbles");

			migrationBuilder.DropIndex(name: "idx_scrobbles_timestamp", table: "scrobbles");

			migrationBuilder.DropPrimaryKey(name: "PK_artists", table: "artists");

			migrationBuilder.DropIndex(name: "idx_artists_name", table: "artists");

			migrationBuilder.DropPrimaryKey(name: "PK_albums", table: "albums");

			migrationBuilder.DropIndex(name: "idx_albums_release_date", table: "albums");

			migrationBuilder.DropIndex(name: "idx_albums_title", table: "albums");

			migrationBuilder.DropPrimaryKey(name: "PK_source_records", table: "source_records");

			migrationBuilder.DropIndex(
				name: "idx_source_records_entity_type",
				table: "source_records"
			);

			migrationBuilder.DropIndex(
				name: "idx_source_records_source_entity_type",
				table: "source_records"
			);

			migrationBuilder.DropIndex(
				name: "idx_source_records_source_id",
				table: "source_records"
			);

			migrationBuilder.DropPrimaryKey(name: "PK_fibery_entities", table: "fibery_entities");

			migrationBuilder.DropIndex(
				name: "idx_fibery_entities_entity_type",
				table: "fibery_entities"
			);

			migrationBuilder.DropIndex(
				name: "idx_fibery_entities_fibery_id_type",
				table: "fibery_entities"
			);

			migrationBuilder.DropPrimaryKey(name: "PK_failed_tasks", table: "failed_tasks");

			migrationBuilder.DropIndex(name: "idx_failed_tasks_task_name", table: "failed_tasks");

			migrationBuilder.DropIndex(name: "idx_failed_tasks_timestamp", table: "failed_tasks");

			migrationBuilder.DropPrimaryKey(name: "PK_execution_logs", table: "execution_logs");

			migrationBuilder.DropIndex(
				name: "idx_execution_logs_session_id",
				table: "execution_logs"
			);

			migrationBuilder.DropIndex(
				name: "idx_execution_logs_timestamp",
				table: "execution_logs"
			);

			migrationBuilder.DropColumn(name: "ChannelName", table: "videos");

			migrationBuilder.DropColumn(name: "Metadata", table: "videos");

			migrationBuilder.DropColumn(name: "SyncedAt", table: "videos");

			migrationBuilder.DropColumn(name: "UploadDate", table: "videos");

			migrationBuilder.DropColumn(name: "Timestamp", table: "failed_tasks");

			migrationBuilder.RenameTable(name: "videos", newName: "Videos");

			migrationBuilder.RenameTable(name: "tracks", newName: "Tracks");

			migrationBuilder.RenameTable(name: "scrobbles", newName: "Scrobbles");

			migrationBuilder.RenameTable(name: "artists", newName: "Artists");

			migrationBuilder.RenameTable(name: "albums", newName: "Albums");

			migrationBuilder.RenameTable(name: "source_records", newName: "SourceRecords");

			migrationBuilder.RenameTable(name: "fibery_entities", newName: "FiberyEntities");

			migrationBuilder.RenameTable(name: "failed_tasks", newName: "FailedTasks");

			migrationBuilder.RenameTable(name: "execution_logs", newName: "ExecutionLogs");

			migrationBuilder.RenameColumn(name: "Url", table: "Videos", newName: "YoutubeId");

			migrationBuilder.RenameColumn(
				name: "Description",
				table: "Videos",
				newName: "PlaylistId"
			);

			migrationBuilder.RenameIndex(
				name: "IX_tracks_ArtistId",
				table: "Tracks",
				newName: "IX_Tracks_ArtistId"
			);

			migrationBuilder.RenameIndex(
				name: "IX_tracks_AlbumId",
				table: "Tracks",
				newName: "IX_Tracks_AlbumId"
			);

			migrationBuilder.RenameIndex(
				name: "IX_scrobbles_TrackId",
				table: "Scrobbles",
				newName: "IX_Scrobbles_TrackId"
			);

			migrationBuilder.RenameIndex(
				name: "IX_albums_ArtistId",
				table: "Albums",
				newName: "IX_Albums_ArtistId"
			);

			migrationBuilder.RenameColumn(
				name: "TaskName",
				table: "FailedTasks",
				newName: "Operation"
			);

			migrationBuilder
				.AlterColumn<int>(
					name: "Id",
					table: "Videos",
					type: "integer",
					nullable: false,
					oldClrType: typeof(int),
					oldType: "integer"
				)
				.Annotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
				)
				.OldAnnotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
				);

			migrationBuilder.AddColumn<bool>(
				name: "IsDeleted",
				table: "Videos",
				type: "boolean",
				nullable: false,
				defaultValue: false
			);

			migrationBuilder
				.AlterColumn<int>(
					name: "Id",
					table: "Tracks",
					type: "integer",
					nullable: false,
					oldClrType: typeof(int),
					oldType: "integer"
				)
				.Annotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
				)
				.OldAnnotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
				);

			migrationBuilder.AlterColumn<DateTimeOffset>(
				name: "ScrobbledAt",
				table: "Scrobbles",
				type: "timestamp with time zone",
				nullable: false,
				oldClrType: typeof(DateTimeOffset),
				oldType: "timestamptz"
			);

			migrationBuilder.AlterColumn<string>(
				name: "Platform",
				table: "Scrobbles",
				type: "text",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "varchar(50)"
			);

			migrationBuilder
				.AlterColumn<long>(
					name: "Id",
					table: "Scrobbles",
					type: "bigint",
					nullable: false,
					oldClrType: typeof(long),
					oldType: "bigint"
				)
				.Annotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
				)
				.OldAnnotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
				);

			migrationBuilder
				.AlterColumn<int>(
					name: "Id",
					table: "Artists",
					type: "integer",
					nullable: false,
					oldClrType: typeof(int),
					oldType: "integer"
				)
				.Annotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
				)
				.OldAnnotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
				);

			migrationBuilder
				.AlterColumn<int>(
					name: "Id",
					table: "Albums",
					type: "integer",
					nullable: false,
					oldClrType: typeof(int),
					oldType: "integer"
				)
				.Annotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
				)
				.OldAnnotation(
					"Npgsql:ValueGenerationStrategy",
					NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
				);

			migrationBuilder.AlterColumn<Guid>(
				name: "Id",
				table: "SourceRecords",
				type: "uuid",
				nullable: false,
				oldClrType: typeof(Guid),
				oldType: "uuid",
				oldDefaultValueSql: "gen_random_uuid()"
			);

			migrationBuilder.AlterColumn<string>(
				name: "FiberyId",
				table: "FiberyEntities",
				type: "text",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "varchar(255)"
			);

			migrationBuilder.AlterColumn<string>(
				name: "EntityType",
				table: "FiberyEntities",
				type: "text",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "varchar(100)"
			);

			migrationBuilder.AlterColumn<Guid>(
				name: "Id",
				table: "FiberyEntities",
				type: "uuid",
				nullable: false,
				oldClrType: typeof(Guid),
				oldType: "uuid",
				oldDefaultValueSql: "gen_random_uuid()"
			);

			migrationBuilder.AddColumn<DateTimeOffset>(
				name: "CreatedAt",
				table: "FailedTasks",
				type: "timestamp with time zone",
				nullable: false,
				defaultValue: new DateTimeOffset(
					new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
					new TimeSpan(0, 0, 0, 0, 0)
				)
			);

			migrationBuilder.AlterColumn<DateTimeOffset>(
				name: "Timestamp",
				table: "ExecutionLogs",
				type: "timestamp with time zone",
				nullable: false,
				oldClrType: typeof(DateTimeOffset),
				oldType: "timestamptz",
				oldDefaultValueSql: "CURRENT_TIMESTAMP"
			);

			migrationBuilder.AddPrimaryKey(name: "PK_Videos", table: "Videos", column: "Id");

			migrationBuilder.AddPrimaryKey(name: "PK_Tracks", table: "Tracks", column: "Id");

			migrationBuilder.AddPrimaryKey(name: "PK_Scrobbles", table: "Scrobbles", column: "Id");

			migrationBuilder.AddPrimaryKey(name: "PK_Artists", table: "Artists", column: "Id");

			migrationBuilder.AddPrimaryKey(name: "PK_Albums", table: "Albums", column: "Id");

			migrationBuilder.AddPrimaryKey(
				name: "PK_SourceRecords",
				table: "SourceRecords",
				column: "Id"
			);

			migrationBuilder.AddPrimaryKey(
				name: "PK_FiberyEntities",
				table: "FiberyEntities",
				column: "Id"
			);

			migrationBuilder.AddPrimaryKey(
				name: "PK_FailedTasks",
				table: "FailedTasks",
				column: "Id"
			);

			migrationBuilder.AddPrimaryKey(
				name: "PK_ExecutionLogs",
				table: "ExecutionLogs",
				column: "Id"
			);

			migrationBuilder.AddForeignKey(
				name: "FK_Albums_Artists_ArtistId",
				table: "Albums",
				column: "ArtistId",
				principalTable: "Artists",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade
			);

			migrationBuilder.AddForeignKey(
				name: "FK_Scrobbles_Tracks_TrackId",
				table: "Scrobbles",
				column: "TrackId",
				principalTable: "Tracks",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade
			);

			migrationBuilder.AddForeignKey(
				name: "FK_Tracks_Albums_AlbumId",
				table: "Tracks",
				column: "AlbumId",
				principalTable: "Albums",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade
			);

			migrationBuilder.AddForeignKey(
				name: "FK_Tracks_Artists_ArtistId",
				table: "Tracks",
				column: "ArtistId",
				principalTable: "Artists",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade
			);
		}
	}
}
