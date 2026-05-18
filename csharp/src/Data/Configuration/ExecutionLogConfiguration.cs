#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Configuration;

internal sealed class ExecutionLogConfiguration : IEntityTypeConfiguration<ExecutionLog>
{
	public void Configure(EntityTypeBuilder<ExecutionLog> b)
	{
		b.ToTable("execution_logs");
		b.HasKey(e => e.Id);
		b.Property(e => e.Id).ValueGeneratedOnAdd();
		b.Property(e => e.Timestamp).HasColumnType("timestamptz").HasDefaultValueSql("CURRENT_TIMESTAMP");
		b.Property(e => e.Payload).HasColumnType("jsonb");
	}
}

