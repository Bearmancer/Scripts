#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class FiberyEntityConfiguration : IEntityTypeConfiguration<FiberyEntity>
{
	public void Configure(EntityTypeBuilder<FiberyEntity> b)
	{
		b.ToTable(name: "fibery_entities");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.RawData).HasColumnType(typeName: "jsonb");
	}
}
