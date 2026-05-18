#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Configuration;

internal sealed class FailedTaskConfiguration : IEntityTypeConfiguration<FailedTask>
{
	public void Configure(EntityTypeBuilder<FailedTask> b)
	{
		b.ToTable("failed_tasks");
		b.HasKey(e => e.Id);
		b.Property(e => e.Id).ValueGeneratedOnAdd();
		b.Property(e => e.Timestamp).HasColumnType("timestamptz").HasDefaultValueSql("CURRENT_TIMESTAMP");
	}
}

