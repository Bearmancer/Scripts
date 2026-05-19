#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class VideoConfiguration : IEntityTypeConfiguration<Video>
{
	public void Configure(EntityTypeBuilder<Video> b)
	{
		b.ToTable(name: "videos");
		b.Property(v => v.Id).UseIdentityAlwaysColumn();
		b.HasIndex(v => v.Url).IsUnique();
		b.HasIndex(v => v.ChannelName);
		b.HasIndex(v => v.UploadDate);
		b.Property(v => v.Metadata).HasColumnType(typeName: "jsonb");
	}
}
