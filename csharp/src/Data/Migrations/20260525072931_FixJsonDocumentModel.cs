using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scripts.src.Data.Migrations
{
	/// <inheritdoc />
	public partial class FixJsonDocumentModel : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(name: "idx_videos_title", table: "videos");

			migrationBuilder.DropIndex(name: "idx_tracks_title", table: "tracks");

			migrationBuilder.DropIndex(name: "idx_artists_name", table: "artists");

			migrationBuilder.CreateIndex(
				name: "idx_videos_title_trgm",
				table: "videos",
				column: "Title",
				filter: "true"
			);

			migrationBuilder.CreateIndex(
				name: "idx_tracks_title_trgm",
				table: "tracks",
				column: "Title",
				filter: "true"
			);

			migrationBuilder.CreateIndex(
				name: "idx_artists_name_trgm",
				table: "artists",
				column: "Name",
				unique: true,
				filter: "true"
			);

			migrationBuilder.CreateIndex(
				name: "idx_albums_title_trgm",
				table: "albums",
				column: "Title",
				filter: "true"
			);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(name: "idx_videos_title_trgm", table: "videos");

			migrationBuilder.DropIndex(name: "idx_tracks_title_trgm", table: "tracks");

			migrationBuilder.DropIndex(name: "idx_artists_name_trgm", table: "artists");

			migrationBuilder.DropIndex(name: "idx_albums_title_trgm", table: "albums");

			migrationBuilder.CreateIndex(
				name: "idx_videos_title",
				table: "videos",
				column: "Title"
			);

			migrationBuilder.CreateIndex(
				name: "idx_tracks_title",
				table: "tracks",
				column: "Title"
			);

			migrationBuilder.CreateIndex(
				name: "idx_artists_name",
				table: "artists",
				column: "Name",
				unique: true
			);
		}
	}
}
