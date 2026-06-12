using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", "work");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.NameLower).IsRequired();
        builder.Property(x => x.Slug).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
    }
}
