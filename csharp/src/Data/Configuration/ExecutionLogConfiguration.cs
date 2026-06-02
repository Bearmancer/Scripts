using Scripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Scripts.Data.Configuration;

internal sealed class ExecutionLogConfiguration : IEntityTypeConfiguration<ExecutionLog>
{
	public void Configure(EntityTypeBuilder<ExecutionLog> b)
	{
		b.ToTable(name: "execution_logs");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.Id).ValueGeneratedOnAdd();
		b.Property(static e => e.Timestamp)
			.HasColumnType(typeName: "timestamptz")
			.HasDefaultValueSql(sql: "CURRENT_TIMESTAMP");
		b.Property(static e => e.SessionId).HasColumnType(typeName: "text");
		b.Property(static e => e.Payload).HasColumnType(typeName: "jsonb");
		b.Property(static e => e.ExitCode).HasColumnType(typeName: "integer");
		b.HasIndex(static e => e.SessionId).HasDatabaseName(name: "idx_execution_logs_session_id");
		b.HasIndex(static e => e.Timestamp).HasDatabaseName(name: "idx_execution_logs_timestamp");
	}
}
