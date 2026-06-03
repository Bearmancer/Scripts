using Scripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Scripts.Data.Configuration;

internal sealed class SourceRecordConfiguration : IEntityTypeConfiguration<SourceRecord>
{
	public SourceRecordConfiguration() { }

	public void Configure(EntityTypeBuilder<SourceRecord> b)
	{
		b.ToTable(name: "source_records");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.Id).HasDefaultValueSql(sql: "gen_random_uuid()");
		b.Property(static e => e.SourceId).HasColumnType(typeName: "text").IsRequired();
		b.Property(static e => e.EntityType).HasColumnType(typeName: "text").IsRequired();
		b.Property(static e => e.RawData).HasColumnType(typeName: "jsonb");
		b.HasIndex(static e => e.SourceId).HasDatabaseName(name: "idx_source_records_source_id");
		b.HasIndex(static e => e.EntityType)
			.HasDatabaseName(name: "idx_source_records_entity_type");
		b.HasIndex(static e => new { e.SourceId, e.EntityType })
			.IsUnique()
			.HasDatabaseName(name: "idx_source_records_source_entity_type");
	}
}
