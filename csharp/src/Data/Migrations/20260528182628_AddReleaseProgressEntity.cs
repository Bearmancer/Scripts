using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSharpScripts.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReleaseProgressEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_release_progress_created_at",
                table: "release_progress",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_release_progress_release_id",
                table: "release_progress",
                column: "ReleaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_release_progress_created_at",
                table: "release_progress");

            migrationBuilder.DropIndex(
                name: "idx_release_progress_release_id",
                table: "release_progress");
        }
    }
}
