using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class ExecutionLogConfiguration : IEntityTypeConfiguration<ExecutionLog>
{
    public void Configure(EntityTypeBuilder<ExecutionLog> builder)
    {
        builder.ToTable("execution_logs", "work");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.SessionId).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        
        builder.Property(x => x.Timestamp).HasDefaultValueSql("NOW()");
    }
}
