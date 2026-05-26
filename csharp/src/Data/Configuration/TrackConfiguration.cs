#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
	public void Configure(EntityTypeBuilder<Track> b)
	{
		b.ToTable(name: "tracks");
		b.Property(static t => t.Id).UseIdentityAlwaysColumn();
		b.Property(static t => t.AlbumId).HasColumnType(typeName: "integer");
		b.Property(static t => t.ArtistId).HasColumnType(typeName: "integer");
		b.Property(static t => t.Title).HasColumnType(typeName: "text").IsRequired();
		b.Property(static t => t.DurationSeconds).HasColumnType(typeName: "integer");
		b.HasIndex(static t => t.ArtistId);
		b.HasIndex(static t => t.AlbumId);
		b.HasIndex(static t => t.Title).HasDatabaseName(name: "idx_tracks_title");
		b.HasIndex(static t => new { t.ArtistId, t.Title })
			.IsUnique()
			.HasDatabaseName(name: "idx_tracks_artist_title");

		// Functional index for case-insensitive search (requires unaccent extension)
		b.HasIndex(static t => t.Title)
			.HasDatabaseName(name: "idx_tracks_title_unaccent")
			.HasFilter("true"); // Placeholder - actual functional index via migration

		// Trigram GIN index for fuzzy search (requires pg_trgm extension)
		b.HasIndex(static t => t.Title)
			.HasDatabaseName(name: "idx_tracks_title_trgm")
			.HasFilter("true"); // Placeholder - actual trigram index via migration

		b.HasOne(static t => t.Artist)
			.WithMany(static a => a.Tracks)
			.HasForeignKey(static t => t.ArtistId);

		b.HasOne(static t => t.Album)
			.WithMany(static a => a.Tracks)
			.HasForeignKey(static t => t.AlbumId);
	}
}

