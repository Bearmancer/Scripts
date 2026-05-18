#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class VideoConfiguration : IEntityTypeConfiguration<Entities.Video>
{
    public void Configure(EntityTypeBuilder<Entities.Video> b)
    {
        b.ToTable("videos");
        b.Property(v => v.Id).UseIdentityAlwaysColumn();
        b.HasIndex(v => v.Url).IsUnique();
        b.HasIndex(v => v.ChannelName);
        b.HasIndex(v => v.UploadDate);
        b.Property(v => v.Metadata).HasColumnType("jsonb");
    }
}