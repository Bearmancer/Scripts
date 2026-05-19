#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scrobble = CSharpScripts.Data.Entities.Scrobble;

namespace CSharpScripts.Data.Configuration;

internal sealed class ScrobbleConfiguration : IEntityTypeConfiguration<Scrobble>
{
	public void Configure(EntityTypeBuilder<Scrobble> b)
	{
		b.ToTable(name: "scrobbles");
		b.Property(static s => s.Id).UseIdentityAlwaysColumn();
		b.HasIndex(static s => s.TrackId);
		b.Property(static s => s.ScrobbledAt).HasColumnType(typeName: "timestamptz");
		b.HasIndex(static s => new { s.TrackId, s.ScrobbledAt })
			.IsUnique()
			.HasDatabaseName(name: "idx_scrobbles_timestamp");

		b.HasOne(static s => s.Track)
			.WithMany(static t => t.Scrobbles)
			.HasForeignKey(static s => s.TrackId)
			.ExcludeForeignKeyFromMigrations();
	}
}
