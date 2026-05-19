#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
	public void Configure(EntityTypeBuilder<Album> b)
	{
		b.ToTable(name: "albums");
		b.Property(static a => a.Id).UseIdentityAlwaysColumn();
		b.HasIndex(static a => a.ArtistId);
		b.HasIndex(static a => new { a.ArtistId, a.Title }).IsUnique().HasDatabaseName(name: "idx_albums_title");

		b.HasOne(static a => a.Artist)
			.WithMany(static a => a.Albums)
			.HasForeignKey(static a => a.ArtistId)
			.ExcludeForeignKeyFromMigrations();
	}
}
