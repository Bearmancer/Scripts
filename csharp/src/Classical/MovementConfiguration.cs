using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class MovementConfiguration : IEntityTypeConfiguration<Movement>
{
	public void Configure(EntityTypeBuilder<Movement> b)
	{
		b.ToTable(name: "movements", schema: "classical");
		b.Property(static m => m.Id).UseIdentityAlwaysColumn();
		b.Property(static m => m.Title).HasColumnType(typeName: "text").IsRequired();
		b.Property(static m => m.Position).HasColumnType(typeName: "integer");

		b.HasOne(static m => m.MusicWork)
			.WithMany(static w => w.Movements)
			.HasForeignKey(static m => m.WorkId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
