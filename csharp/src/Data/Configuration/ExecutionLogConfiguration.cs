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
        
        builder.Property(x => x.TaskName).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Input).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Output).HasColumnType("jsonb");
        
        builder.Property(x => x.Timestamp).HasDefaultValueSql("NOW()");
        
        builder.HasOne(x => x.Issue)
            .WithMany(x => x.ExecutionLogs)
            .HasForeignKey(x => x.IssueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
