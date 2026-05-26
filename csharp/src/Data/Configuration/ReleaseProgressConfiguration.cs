using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class ReleaseProgressConfiguration : IEntityTypeConfiguration<ReleaseProgress>
{
	public void Configure(EntityTypeBuilder<ReleaseProgress> b)
	{
		b.ToTable(name: "release_progress");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.Id).ValueGeneratedOnAdd();
		b.HasIndex(static e => new { e.ReleaseId, e.DiscNumber, e.TrackNumber })
			.IsUnique()
			.HasDatabaseName(name: "idx_release_progress_track");
		b.Property(static e => e.ReleaseId).HasColumnType(typeName: "text");
		b.Property(static e => e.Soloists).HasColumnType(typeName: "jsonb");
		b.Property(static e => e.CreatedAt)
			.HasColumnType(typeName: "timestamptz")
			.HasDefaultValueSql(sql: "CURRENT_TIMESTAMP");
	}
}
