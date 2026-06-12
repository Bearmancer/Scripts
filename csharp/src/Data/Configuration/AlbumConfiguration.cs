using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
	public void Configure(EntityTypeBuilder<Album> b)
	{
		b.ToTable(name: "albums", schema: "music");
		b.Property(static a => a.Id).UseIdentityAlwaysColumn();
		b.Property(static a => a.ArtistId).HasColumnType(typeName: "integer").IsRequired(false);
		b.Property(static a => a.Title).HasColumnType(typeName: "text").IsRequired();
		b.Property(static a => a.ReleaseDate).HasColumnType(typeName: "date");
		b.HasIndex(static a => a.ArtistId);
		b.HasIndex(static a => new { a.ArtistId, a.Title })
			.IsUnique()
			.HasDatabaseName(name: "idx_albums_title");
		b.HasIndex(static a => a.ReleaseDate).HasDatabaseName(name: "idx_albums_release_date");

		b.HasIndex(static a => a.Title)
			.HasDatabaseName(name: "idx_albums_title_unaccent")
			.HasMethod("gin");

		b.HasIndex(static a => a.Title)
			.HasDatabaseName(name: "idx_albums_title_trgm")
			.HasMethod("gin")
			.HasOperators("gin_trgm_ops");

		b.HasOne(static a => a.Artist)
			.WithMany(static a => a.Albums)
			.HasForeignKey(static a => a.ArtistId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
