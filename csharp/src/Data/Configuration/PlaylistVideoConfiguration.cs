using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class PlaylistVideoConfiguration : IEntityTypeConfiguration<PlaylistVideo>
{
	public void Configure(EntityTypeBuilder<PlaylistVideo> builder)
	{
		builder.ToTable("playlist_videos", "youtube");
		builder.HasKey(x => new { x.PlaylistId, x.VideoId });

		builder
			.HasOne(x => x.Playlist)
			.WithMany(x => x.PlaylistVideos)
			.HasForeignKey(x => x.PlaylistId)
			.OnDelete(DeleteBehavior.Restrict);

		builder
			.HasOne(x => x.Video)
			.WithMany(x => x.PlaylistVideos)
			.HasForeignKey(x => x.VideoId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
