#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
	public void Configure(EntityTypeBuilder<Artist> b)
	{
		b.ToTable(name: "artists");
		b.Property(static a => a.Id).UseIdentityAlwaysColumn();
		b.Property(static a => a.Name).HasColumnType(typeName: "text").IsRequired();
		b.Property(static a => a.Metadata).HasColumnType(typeName: "jsonb");
		b.HasIndex(static a => a.Name).IsUnique().HasDatabaseName(name: "idx_artists_name");

		b.HasIndex(static a => a.Name)
			.HasDatabaseName(name: "idx_artists_name_unaccent")
			.HasFilter("true");

		b.HasIndex(static a => a.Name)
			.HasDatabaseName(name: "idx_artists_name_trgm")
			.HasFilter("true");
	}
}
