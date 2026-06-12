using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scripts.Migrations
{
    /// <inheritdoc />
    public partial class WaveB1_SchemaEvolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_videos_channel",
                schema: "youtube",
                table: "videos");

            migrationBuilder.DropIndex(
                name: "idx_videos_channel_upload_date",
                schema: "youtube",
                table: "videos");

            migrationBuilder.DropIndex(
                name: "idx_videos_title_trgm",
                schema: "youtube",
                table: "videos");

            migrationBuilder.DropIndex(
                name: "idx_videos_upload_date",
                schema: "youtube",
                table: "videos");

            migrationBuilder.DropIndex(
                name: "idx_videos_url",
                schema: "youtube",
                table: "videos");

            migrationBuilder.DropIndex(
                name: "idx_execution_logs_session_id",
                table: "execution_logs");

            migrationBuilder.DropIndex(
                name: "idx_execution_logs_timestamp",
                table: "execution_logs");

            migrationBuilder.EnsureSchema(
                name: "work");

            migrationBuilder.RenameTable(
                name: "execution_logs",
                newName: "execution_logs",
                newSchema: "work");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "SyncedAt",
                schema: "youtube",
                table: "videos",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "youtube",
                table: "videos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn);

            migrationBuilder.AddColumn<string>(
                name: "ChannelNameLower",
                schema: "youtube",
                table: "videos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleLower",
                schema: "youtube",
                table: "videos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TranslatedDescription",
                schema: "youtube",
                table: "videos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranslatedTitle",
                schema: "youtube",
                table: "videos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoId",
                schema: "youtube",
                table: "videos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Timestamp",
                schema: "work",
                table: "execution_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamptz",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateTable(
                name: "playlists",
                schema: "youtube",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlaylistId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleLower = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ChannelName = table.Column<string>(type: "text", nullable: false),
                    ChannelNameLower = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "work",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameLower = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "playlist_videos",
                schema: "youtube",
                columns: table => new
                {
                    PlaylistId = table.Column<int>(type: "integer", nullable: false),
                    VideoId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist_videos", x => new { x.PlaylistId, x.VideoId });
                    table.ForeignKey(
                        name: "FK_playlist_videos_playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalSchema: "youtube",
                        principalTable: "playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_playlist_videos_videos_VideoId",
                        column: x => x.VideoId,
                        principalSchema: "youtube",
                        principalTable: "videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "issues",
                schema: "work",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleLower = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    PrioritySort = table.Column<int>(type: "integer", nullable: false),
                    Estimate = table.Column<int>(type: "integer", nullable: true),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_issues_issues_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "work",
                        principalTable: "issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_issues_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "work",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_videos_VideoId",
                schema: "youtube",
                table: "videos",
                column: "VideoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issues_Identifier",
                schema: "work",
                table: "issues",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issues_ParentId",
                schema: "work",
                table: "issues",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_issues_ProjectId",
                schema: "work",
                table: "issues",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_playlist_videos_VideoId",
                schema: "youtube",
                table: "playlist_videos",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_playlists_PlaylistId",
                schema: "youtube",
                table: "playlists",
                column: "PlaylistId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issues",
                schema: "work");

            migrationBuilder.DropTable(
                name: "playlist_videos",
                schema: "youtube");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "work");

            migrationBuilder.DropTable(
                name: "playlists",
                schema: "youtube");

            migrationBuilder.DropIndex(
                name: "IX_videos_VideoId",
                schema: "youtube",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "ChannelNameLower",
                schema: "youtube",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "TitleLower",
                schema: "youtube",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "TranslatedDescription",
                schema: "youtube",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "TranslatedTitle",
                schema: "youtube",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "VideoId",
                schema: "youtube",
                table: "videos");

            migrationBuilder.RenameTable(
                name: "execution_logs",
                schema: "work",
                newName: "execution_logs");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "SyncedAt",
                schema: "youtube",
                table: "videos",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "youtube",
                table: "videos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Timestamp",
                table: "execution_logs",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

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

            migrationBuilder.CreateIndex(
                name: "idx_execution_logs_session_id",
                table: "execution_logs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "idx_execution_logs_timestamp",
                table: "execution_logs",
                column: "Timestamp");
        }
    }
}
