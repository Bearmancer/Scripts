using Scripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Scripts.Data.Configuration;

internal sealed class FiberyEntityConfiguration : IEntityTypeConfiguration<FiberyEntity>
{
	public void Configure(EntityTypeBuilder<FiberyEntity> b)
	{
		b.ToTable(name: "fibery_entities");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.Id).HasDefaultValueSql(sql: "gen_random_uuid()");
		b.Property(static e => e.FiberyId).HasColumnType(typeName: "varchar(255)").IsRequired();
		b.Property(static e => e.EntityType).HasColumnType(typeName: "varchar(100)").IsRequired();
		b.Property(static e => e.RawData).HasColumnType(typeName: "jsonb");
		b.HasIndex(static e => e.EntityType)
			.HasDatabaseName(name: "idx_fibery_entities_entity_type");
		b.HasIndex(static e => new { e.FiberyId, e.EntityType })
			.IsUnique()
			.HasDatabaseName(name: "idx_fibery_entities_fibery_id_type");
	}
}
