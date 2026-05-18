using CSharpScripts.Data.Entities;
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
	public void Configure(EntityTypeBuilder<Artist> b)
	{
		b.ToTable("artists");
		b.Property(a => a.Id).UseIdentityAlwaysColumn();
		b.HasIndex(a => a.Name).IsUnique().HasDatabaseName("idx_artists_name");
		b.Property(a => a.Metadata).HasColumnType("jsonb");
	}
}


