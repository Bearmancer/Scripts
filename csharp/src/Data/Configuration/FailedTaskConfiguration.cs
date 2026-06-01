#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using Scripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Scripts.Data.Configuration;

internal sealed class FailedTaskConfiguration : IEntityTypeConfiguration<FailedTask>
{
	public void Configure(EntityTypeBuilder<FailedTask> b)
	{
		b.ToTable(name: "failed_tasks");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.Id).ValueGeneratedOnAdd();
		b.Property(static e => e.TaskName).HasColumnType(typeName: "text");
		b.Property(static e => e.ErrorMessage).HasColumnType(typeName: "text");
		b.Property(static e => e.Timestamp)
			.HasColumnType(typeName: "timestamptz")
			.HasDefaultValueSql(sql: "CURRENT_TIMESTAMP");
		b.HasIndex(static e => e.TaskName).HasDatabaseName(name: "idx_failed_tasks_task_name");
		b.HasIndex(static e => e.Timestamp).HasDatabaseName(name: "idx_failed_tasks_timestamp");
	}
}
