using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class MusicWorkConfiguration : IEntityTypeConfiguration<MusicWork>
{
	public void Configure(EntityTypeBuilder<MusicWork> b)
	{
		b.ToTable(name: "works", schema: "music");
		b.Property(static w => w.Id).UseIdentityAlwaysColumn();
		b.Property(static w => w.Title).HasColumnType(typeName: "text").IsRequired();
		b.Property(static w => w.Composer).HasColumnType(typeName: "text");
		b.Property(static w => w.Metadata).HasColumnType(typeName: "jsonb");
	}
}
