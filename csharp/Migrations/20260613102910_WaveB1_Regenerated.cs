using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scripts.Migrations
{
    /// <inheritdoc />
    public partial class WaveB1_Regenerated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tracks_movements_MovementId",
                schema: "music",
                table: "tracks");

            migrationBuilder.DropForeignKey(
                name: "FK_tracks_works_WorkId",
                schema: "music",
                table: "tracks");

            migrationBuilder.DropTable(
                name: "movements",
                schema: "classical");

            migrationBuilder.DropTable(
                name: "works",
                schema: "music");

            migrationBuilder.DropIndex(
                name: "IX_tracks_MovementId",
                schema: "music",
                table: "tracks");

            migrationBuilder.DropIndex(
                name: "IX_tracks_WorkId",
                schema: "music",
                table: "tracks");

            migrationBuilder.DropColumn(
                name: "MovementId",
                schema: "music",
                table: "tracks");

            migrationBuilder.DropColumn(
                name: "WorkId",
                schema: "music",
                table: "tracks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "classical");

            migrationBuilder.AddColumn<int>(
                name: "MovementId",
                schema: "music",
                table: "tracks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkId",
                schema: "music",
                table: "tracks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "works",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Composer = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_works", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "movements",
                schema: "classical",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    WorkId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_movements_works_WorkId",
                        column: x => x.WorkId,
                        principalSchema: "music",
                        principalTable: "works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tracks_MovementId",
                schema: "music",
                table: "tracks",
                column: "MovementId");

            migrationBuilder.CreateIndex(
                name: "IX_tracks_WorkId",
                schema: "music",
                table: "tracks",
                column: "WorkId");

            migrationBuilder.CreateIndex(
                name: "IX_movements_WorkId",
                schema: "classical",
                table: "movements",
                column: "WorkId");

            migrationBuilder.AddForeignKey(
                name: "FK_tracks_movements_MovementId",
                schema: "music",
                table: "tracks",
                column: "MovementId",
                principalSchema: "classical",
                principalTable: "movements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tracks_works_WorkId",
                schema: "music",
                table: "tracks",
                column: "WorkId",
                principalSchema: "music",
                principalTable: "works",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
