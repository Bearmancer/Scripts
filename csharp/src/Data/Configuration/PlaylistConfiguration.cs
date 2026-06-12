using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
{
	public void Configure(EntityTypeBuilder<Playlist> builder)
	{
		builder.ToTable("playlists", "youtube");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.PlaylistId).IsRequired();
		builder.HasIndex(x => x.PlaylistId).IsUnique();

		builder.Property(x => x.Title).IsRequired();
		builder.Property(x => x.TitleLower).IsRequired();
		builder.Property(x => x.Description).IsRequired();
		builder.Property(x => x.ChannelName).IsRequired();
		builder.Property(x => x.ChannelNameLower).IsRequired();
	}
}
