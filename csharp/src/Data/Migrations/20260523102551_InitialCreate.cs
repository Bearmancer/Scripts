using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scripts.src.Data.Migrations
{
	/// <inheritdoc />
	public partial class InitialCreate : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "Artists",
				columns: table => new
				{
					Id = table
						.Column<int>(type: "integer", nullable: false)
						.Annotation(
							"Npgsql:ValueGenerationStrategy",
							NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
						),
					Name = table.Column<string>(type: "text", nullable: false),
					Metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Artists", x => x.Id);
				}
			);

			migrationBuilder.CreateTable(
				name: "ExecutionLogs",
				columns: table => new
				{
					Id = table
						.Column<int>(type: "integer", nullable: false)
						.Annotation(
							"Npgsql:ValueGenerationStrategy",
							NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
						),
					Timestamp = table.Column<DateTimeOffset>(
						type: "timestamp with time zone",
						nullable: false
					),
					SessionId = table.Column<string>(type: "text", nullable: false),
					Payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
					ExitCode = table.Column<int>(type: "integer", nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ExecutionLogs", x => x.Id);
				}
			);

			migrationBuilder.CreateTable(
				name: "FailedTasks",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					Operation = table.Column<string>(type: "text", nullable: false),
					ErrorMessage = table.Column<string>(type: "text", nullable: false),
					CreatedAt = table.Column<DateTimeOffset>(
						type: "timestamp with time zone",
						nullable: false
					),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_FailedTasks", x => x.Id);
				}
			);

			migrationBuilder.CreateTable(
				name: "Videos",
				columns: table => new
				{
					Id = table
						.Column<int>(type: "integer", nullable: false)
						.Annotation(
							"Npgsql:ValueGenerationStrategy",
							NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
						),
					YoutubeId = table.Column<string>(type: "text", nullable: false),
					Title = table.Column<string>(type: "text", nullable: false),
					PlaylistId = table.Column<string>(type: "text", nullable: false),
					IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Videos", x => x.Id);
				}
			);

			migrationBuilder.CreateTable(
				name: "Albums",
				columns: table => new
				{
					Id = table
						.Column<int>(type: "integer", nullable: false)
						.Annotation(
							"Npgsql:ValueGenerationStrategy",
							NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
						),
					ArtistId = table.Column<int>(type: "integer", nullable: false),
					Title = table.Column<string>(type: "text", nullable: false),
					ReleaseDate = table.Column<DateOnly>(type: "date", nullable: true),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Albums", x => x.Id);
					table.ForeignKey(
						name: "FK_Albums_Artists_ArtistId",
						column: x => x.ArtistId,
						principalTable: "Artists",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade
					);
				}
			);

			migrationBuilder.CreateTable(
				name: "Tracks",
				columns: table => new
				{
					Id = table
						.Column<int>(type: "integer", nullable: false)
						.Annotation(
							"Npgsql:ValueGenerationStrategy",
							NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
						),
					AlbumId = table.Column<int>(type: "integer", nullable: false),
					ArtistId = table.Column<int>(type: "integer", nullable: false),
					Title = table.Column<string>(type: "text", nullable: false),
					DurationSeconds = table.Column<int>(type: "integer", nullable: true),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Tracks", x => x.Id);
					table.ForeignKey(
						name: "FK_Tracks_Albums_AlbumId",
						column: x => x.AlbumId,
						principalTable: "Albums",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade
					);
					table.ForeignKey(
						name: "FK_Tracks_Artists_ArtistId",
						column: x => x.ArtistId,
						principalTable: "Artists",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade
					);
				}
			);

			migrationBuilder.CreateTable(
				name: "Scrobbles",
				columns: table => new
				{
					Id = table
						.Column<long>(type: "bigint", nullable: false)
						.Annotation(
							"Npgsql:ValueGenerationStrategy",
							NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
						),
					TrackId = table.Column<int>(type: "integer", nullable: false),
					ScrobbledAt = table.Column<DateTimeOffset>(
						type: "timestamp with time zone",
						nullable: false
					),
					Platform = table.Column<string>(type: "text", nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Scrobbles", x => x.Id);
					table.ForeignKey(
						name: "FK_Scrobbles_Tracks_TrackId",
						column: x => x.TrackId,
						principalTable: "Tracks",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade
					);
				}
			);

			migrationBuilder.CreateIndex(
				name: "IX_Albums_ArtistId",
				table: "Albums",
				column: "ArtistId"
			);

			migrationBuilder.CreateIndex(
				name: "IX_Scrobbles_TrackId",
				table: "Scrobbles",
				column: "TrackId"
			);

			migrationBuilder.CreateIndex(
				name: "IX_Tracks_AlbumId",
				table: "Tracks",
				column: "AlbumId"
			);

			migrationBuilder.CreateIndex(
				name: "IX_Tracks_ArtistId",
				table: "Tracks",
				column: "ArtistId"
			);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(name: "ExecutionLogs");

			migrationBuilder.DropTable(name: "FailedTasks");

			migrationBuilder.DropTable(name: "Scrobbles");

			migrationBuilder.DropTable(name: "Videos");

			migrationBuilder.DropTable(name: "Tracks");

			migrationBuilder.DropTable(name: "Albums");

			migrationBuilder.DropTable(name: "Artists");
		}
	}
}
