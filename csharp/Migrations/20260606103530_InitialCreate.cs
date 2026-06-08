using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scripts.Migrations
{
    
    public partial class InitialCreate : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "music");

            migrationBuilder.EnsureSchema(
                name: "fibery");

            migrationBuilder.EnsureSchema(
                name: "youtube");

            migrationBuilder.CreateTable(
                name: "artists",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "execution_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    ExitCode = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_execution_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fibery_entities",
                schema: "fibery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FiberyId = table.Column<string>(type: "varchar(255)", nullable: false),
                    EntityType = table.Column<string>(type: "varchar(100)", nullable: false),
                    RawData = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fibery_entities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "release_progress",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReleaseId = table.Column<string>(type: "text", nullable: false),
                    DiscNumber = table.Column<int>(type: "integer", nullable: false),
                    TrackNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Duration = table.Column<string>(type: "text", nullable: true),
                    RecordingYear = table.Column<int>(type: "integer", nullable: true),
                    Composer = table.Column<string>(type: "text", nullable: true),
                    WorkName = table.Column<string>(type: "text", nullable: true),
                    Conductor = table.Column<string>(type: "text", nullable: true),
                    Orchestra = table.Column<string>(type: "text", nullable: true),
                    Soloists = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    Artist = table.Column<string>(type: "text", nullable: true),
                    RecordingVenue = table.Column<string>(type: "text", nullable: true),
                    RecordingId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_release_progress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "source_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SourceId = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    RawData = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "videos",
                schema: "youtube",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ChannelName = table.Column<string>(type: "text", nullable: false),
                    UploadDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    Metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_videos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "albums",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ArtistId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_albums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_albums_artists_ArtistId",
                        column: x => x.ArtistId,
                        principalSchema: "music",
                        principalTable: "artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "failed_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskName = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExecutionLogId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_failed_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_failed_tasks_execution_logs_ExecutionLogId",
                        column: x => x.ExecutionLogId,
                        principalTable: "execution_logs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tracks",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    AlbumId = table.Column<int>(type: "integer", nullable: false),
                    ArtistId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tracks_albums_AlbumId",
                        column: x => x.AlbumId,
                        principalSchema: "music",
                        principalTable: "albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tracks_artists_ArtistId",
                        column: x => x.ArtistId,
                        principalSchema: "music",
                        principalTable: "artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scrobbles",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    TrackId = table.Column<int>(type: "integer", nullable: false),
                    ScrobbledAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Platform = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrobbles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scrobbles_tracks_TrackId",
                        column: x => x.TrackId,
                        principalSchema: "music",
                        principalTable: "tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_albums_release_date",
                schema: "music",
                table: "albums",
                column: "ReleaseDate");

            migrationBuilder.CreateIndex(
                name: "idx_albums_title",
                schema: "music",
                table: "albums",
                columns: new[] { "ArtistId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_albums_title_trgm",
                schema: "music",
                table: "albums",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_albums_ArtistId",
                schema: "music",
                table: "albums",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "idx_artists_name_trgm",
                schema: "music",
                table: "artists",
                column: "Name",
unique: false)
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "idx_execution_logs_session_id",
                table: "execution_logs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "idx_execution_logs_timestamp",
                table: "execution_logs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_failed_tasks_execution_log_id",
                table: "failed_tasks",
                column: "ExecutionLogId");

            migrationBuilder.CreateIndex(
                name: "idx_failed_tasks_task_name",
                table: "failed_tasks",
                column: "TaskName");

            migrationBuilder.CreateIndex(
                name: "idx_failed_tasks_timestamp",
                table: "failed_tasks",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_fibery_entities_entity_type",
                schema: "fibery",
                table: "fibery_entities",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "idx_fibery_entities_fibery_id_type",
                schema: "fibery",
                table: "fibery_entities",
                columns: new[] { "FiberyId", "EntityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_release_progress_created_at",
                schema: "music",
                table: "release_progress",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_release_progress_release_id",
                schema: "music",
                table: "release_progress",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "idx_release_progress_track",
                schema: "music",
                table: "release_progress",
                columns: new[] { "ReleaseId", "DiscNumber", "TrackNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_scrobbles_platform",
                schema: "music",
                table: "scrobbles",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "idx_scrobbles_platform_scrobbled_at",
                schema: "music",
                table: "scrobbles",
                columns: new[] { "Platform", "ScrobbledAt" });

            migrationBuilder.CreateIndex(
                name: "idx_scrobbles_scrobbled_at",
                schema: "music",
                table: "scrobbles",
                column: "ScrobbledAt");

            migrationBuilder.CreateIndex(
                name: "idx_scrobbles_timestamp",
                schema: "music",
                table: "scrobbles",
                columns: new[] { "TrackId", "ScrobbledAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scrobbles_TrackId",
                schema: "music",
                table: "scrobbles",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "idx_source_records_entity_type",
                table: "source_records",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "idx_source_records_source_entity_type",
                table: "source_records",
                columns: new[] { "SourceId", "EntityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_source_records_source_id",
                table: "source_records",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "idx_tracks_artist_title",
                schema: "music",
                table: "tracks",
                columns: new[] { "ArtistId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_tracks_title_trgm",
                schema: "music",
                table: "tracks",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_tracks_AlbumId",
                schema: "music",
                table: "tracks",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_tracks_ArtistId",
                schema: "music",
                table: "tracks",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "idx_videos_channel",
                schema: "youtube",
                table: "videos",
                column: "ChannelName");

            migrationBuilder.CreateIndex(
                name: "idx_videos_channel_upload_date",
                schema: "youtube",
                table: "videos",
                columns: new[] { "ChannelName", "UploadDate" });

            migrationBuilder.CreateIndex(
                name: "idx_videos_title_trgm",
                schema: "youtube",
                table: "videos",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "idx_videos_upload_date",
                schema: "youtube",
                table: "videos",
                column: "UploadDate");

            migrationBuilder.CreateIndex(
                name: "idx_videos_url",
                schema: "youtube",
                table: "videos",
                column: "Url",
                unique: true);
        }

        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "failed_tasks");

            migrationBuilder.DropTable(
                name: "fibery_entities",
                schema: "fibery");

            migrationBuilder.DropTable(
                name: "release_progress",
                schema: "music");

            migrationBuilder.DropTable(
                name: "scrobbles",
                schema: "music");

            migrationBuilder.DropTable(
                name: "source_records");

            migrationBuilder.DropTable(
                name: "videos",
                schema: "youtube");

            migrationBuilder.DropTable(
                name: "execution_logs");

            migrationBuilder.DropTable(
                name: "tracks",
                schema: "music");

            migrationBuilder.DropTable(
                name: "albums",
                schema: "music");

            migrationBuilder.DropTable(
                name: "artists",
                schema: "music");
        }
    }
}
