using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("issues", "work");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Identifier).IsRequired();
        builder.HasIndex(x => x.Identifier).IsUnique();
        
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.TitleLower).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        
        builder.HasOne(x => x.Project)
            .WithMany(x => x.Issues)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
