#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class FailedTaskConfiguration : IEntityTypeConfiguration<FailedTask>
{
	public void Configure(EntityTypeBuilder<FailedTask> b)
	{
		b.ToTable(name: "failed_tasks");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.Id).ValueGeneratedOnAdd();
		b.Property(static e => e.Timestamp)
			.HasColumnType(typeName: "timestamptz")
			.HasDefaultValueSql(sql: "CURRENT_TIMESTAMP");
	}
}
