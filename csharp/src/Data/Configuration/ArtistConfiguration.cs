using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
	public void Configure(EntityTypeBuilder<Artist> b)
	{
		b.ToTable(name: "artists", schema: "music");
		b.Property(static a => a.Id).UseIdentityAlwaysColumn();
		b.Property(static a => a.Name).HasColumnType(typeName: "text").IsRequired();
		b.Property(static a => a.Metadata).HasColumnType(typeName: "jsonb");
		b.HasIndex(static a => a.Name).IsUnique().HasDatabaseName(name: "idx_artists_name");

		b.HasIndex(static a => a.Name)
			.HasDatabaseName(name: "idx_artists_name_trgm")
			.HasMethod("gin")
			.HasOperators("gin_trgm_ops");
	}
}
