#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Configuration;

internal sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
	public void Configure(EntityTypeBuilder<Album> b)
	{
		b.ToTable("albums");
		b.Property(a => a.Id).UseIdentityAlwaysColumn();
		b.HasIndex(a => a.ArtistId);
		b.HasIndex(a => new { a.ArtistId, a.Title }).IsUnique().HasDatabaseName("idx_albums_title");
	}
}

