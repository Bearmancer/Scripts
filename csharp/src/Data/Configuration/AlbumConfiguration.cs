#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
	public void Configure(EntityTypeBuilder<Album> b)
	{
		b.ToTable(name: "albums");
		b.Property(static a => a.Id).UseIdentityAlwaysColumn();
		b.Property(static a => a.ArtistId).HasColumnType(typeName: "integer");
		b.Property(static a => a.Title).HasColumnType(typeName: "text").IsRequired();
		b.Property(static a => a.ReleaseDate).HasColumnType(typeName: "date");
		b.HasIndex(static a => a.ArtistId);
		b.HasIndex(static a => new { a.ArtistId, a.Title })
			.IsUnique()
			.HasDatabaseName(name: "idx_albums_title");
		b.HasIndex(static a => a.ReleaseDate).HasDatabaseName(name: "idx_albums_release_date");

		// Functional index for case-insensitive search (requires unaccent extension)
		b.HasIndex(static a => a.Title)
			.HasDatabaseName(name: "idx_albums_title_unaccent")
			.HasFilter("true"); // Placeholder - actual functional index via migration

		// Trigram GIN index for fuzzy search (requires pg_trgm extension)
		b.HasIndex(static a => a.Title)
			.HasDatabaseName(name: "idx_albums_title_trgm")
			.HasFilter("true"); // Placeholder - actual trigram index via migration

		b.HasOne(static a => a.Artist)
			.WithMany(static a => a.Albums)
			.HasForeignKey(static a => a.ArtistId);
	}
}

