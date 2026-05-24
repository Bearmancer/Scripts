#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class VideoConfiguration : IEntityTypeConfiguration<Video>
{
	public void Configure(EntityTypeBuilder<Video> b)
	{
		b.ToTable(name: "videos");
		b.Property(static v => v.Id).UseIdentityAlwaysColumn();
		b.HasIndex(static v => v.Url).IsUnique();
		b.HasIndex(static v => v.ChannelName);
		b.HasIndex(static v => v.UploadDate);
		b.Property(static v => v.Metadata).HasColumnType(typeName: "jsonb");
	}
}
