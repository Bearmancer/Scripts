using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CSharpScripts.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReleaseProgress : Migration
    {
        private static readonly string[] IndexColumns = ["ReleaseId", "DiscNumber", "TrackNumber"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.CreateTable(
                name: "release_progress",
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
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table => table.PrimaryKey("PK_release_progress", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "idx_release_progress_track",
                table: "release_progress",
                columns: IndexColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropTable(
                name: "release_progress");
        }
    }
}
