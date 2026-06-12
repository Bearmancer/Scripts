using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class VideoConfiguration : IEntityTypeConfiguration<Video>
{
	public void Configure(EntityTypeBuilder<Video> builder)
	{
		builder.ToTable("videos", "youtube");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.VideoId).IsRequired();
		builder.HasIndex(x => x.VideoId).IsUnique();

		builder.Property(x => x.Url).IsRequired();
		builder.Property(x => x.Title).IsRequired();
		builder.Property(x => x.TitleLower).IsRequired();
		builder.Property(x => x.Description).IsRequired();
		builder.Property(x => x.ChannelName).IsRequired();
		builder.Property(x => x.ChannelNameLower).IsRequired();

		builder.Property(x => x.Metadata).HasColumnType("jsonb");
	}
}
