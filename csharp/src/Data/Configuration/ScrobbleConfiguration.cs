#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class ScrobbleConfiguration : IEntityTypeConfiguration<Entities.Scrobble>
{
        public void Configure(EntityTypeBuilder<Entities.Scrobble> b)	{
		b.ToTable("scrobbles");
		b.Property(s => s.Id).UseIdentityAlwaysColumn();
		b.HasIndex(s => s.TrackId);
		b.Property(s => s.ScrobbledAt).HasColumnType("timestamptz");
		b.HasIndex(s => new { s.TrackId, s.ScrobbledAt }).IsUnique().HasDatabaseName("idx_scrobbles_timestamp");
	}
}

