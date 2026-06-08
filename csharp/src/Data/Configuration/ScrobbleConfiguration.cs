using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class ScrobbleConfiguration : IEntityTypeConfiguration<Scrobble>
{
	public void Configure(EntityTypeBuilder<Scrobble> b)
	{
		b.ToTable(name: "scrobbles", schema: "music");
		b.Property(static s => s.Id).UseIdentityAlwaysColumn();
		b.Property(static s => s.TrackId).HasColumnType(typeName: "integer");
		b.Property(static s => s.ScrobbledAt).HasColumnType(typeName: "timestamptz");
		b.Property(static s => s.Platform).HasColumnType(typeName: "varchar(50)");
		b.HasIndex(static s => s.TrackId);
		b.HasIndex(static s => new { s.TrackId, s.ScrobbledAt })
			.IsUnique()
			.HasDatabaseName(name: "idx_scrobbles_timestamp");
		b.HasIndex(static s => s.Platform).HasDatabaseName(name: "idx_scrobbles_platform");
		b.HasIndex(static s => s.ScrobbledAt).HasDatabaseName(name: "idx_scrobbles_scrobbled_at");
		b.HasIndex(static s => new { s.Platform, s.ScrobbledAt })
			.HasDatabaseName(name: "idx_scrobbles_platform_scrobbled_at");

		b.HasOne(static s => s.Track)
			.WithMany(static t => t.Scrobbles)
			.HasForeignKey(static s => s.TrackId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
