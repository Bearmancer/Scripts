using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scripts.Migrations
{
    public partial class AddYouTubePlaylistsAndSyncRuns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_playlists_PlaylistId",
                schema: "youtube",
                table: "playlists",
                column: "PlaylistId",
                unique: true);

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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_playlist_videos_videos_VideoId",
                        column: x => x.VideoId,
                        principalSchema: "youtube",
                        principalTable: "videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_playlist_videos_VideoId",
                schema: "youtube",
                table: "playlist_videos",
                column: "VideoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "playlist_videos",
                schema: "youtube");

            migrationBuilder.DropTable(
                name: "playlists",
                schema: "youtube");
        }
    }
}
