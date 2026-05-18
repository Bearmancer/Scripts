#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Configuration;

internal sealed class FiberyEntityConfiguration : IEntityTypeConfiguration<FiberyEntity>
{
	public void Configure(EntityTypeBuilder<FiberyEntity> b)
	{
		b.ToTable("fibery_entities");
		b.HasKey(e => e.Id);
		b.Property(e => e.RawData).HasColumnType("jsonb");
	}
}

