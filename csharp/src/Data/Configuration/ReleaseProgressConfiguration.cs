using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class ReleaseProgressConfiguration : IEntityTypeConfiguration<ReleaseProgress>
{
	public void Configure(EntityTypeBuilder<ReleaseProgress> b)
	{
		b.ToTable(name: "release_progress", schema: "music");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.Id).ValueGeneratedOnAdd();
		b.HasIndex(static e => new
		{
			e.ReleaseId,
			e.DiscNumber,
			e.TrackNumber,
		})
			.IsUnique()
			.HasDatabaseName(name: "idx_release_progress_track");
		b.HasIndex(static e => e.CreatedAt)
			.HasDatabaseName(name: "idx_release_progress_created_at");
		b.HasIndex(static e => e.ReleaseId)
			.HasDatabaseName(name: "idx_release_progress_release_id");
		b.Property(static e => e.ReleaseId).HasColumnType(typeName: "text");
		b.Property(static e => e.Soloists).HasColumnType(typeName: "jsonb");
		b.Property(static e => e.CreatedAt)
			.HasColumnType(typeName: "timestamptz")
			.HasDefaultValueSql(sql: "CURRENT_TIMESTAMP");
	}
}
