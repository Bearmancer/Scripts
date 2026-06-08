using Scripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Scripts.Data.Configuration;

internal sealed class VideoConfiguration : IEntityTypeConfiguration<Video>
{
	public void Configure(EntityTypeBuilder<Video> b)
	{
		b.ToTable(name: "videos", schema: "youtube");
		b.Property(static v => v.Id).UseIdentityAlwaysColumn();
		b.Property(static v => v.Url).HasColumnType(typeName: "text").IsRequired();
		b.Property(static v => v.Title).HasColumnType(typeName: "text").IsRequired();
		b.Property(static v => v.Description).HasColumnType(typeName: "text");
		b.Property(static v => v.ChannelName).HasColumnType(typeName: "text");
		b.Property(static v => v.UploadDate).HasColumnType(typeName: "date");
		b.Property(static v => v.SyncedAt).HasColumnType(typeName: "timestamptz");
		b.Property(static v => v.Metadata).HasColumnType(typeName: "jsonb");
		b.HasIndex(static v => v.Url).IsUnique().HasDatabaseName(name: "idx_videos_url");
		b.HasIndex(static v => v.ChannelName).HasDatabaseName(name: "idx_videos_channel");
		b.HasIndex(static v => v.UploadDate).HasDatabaseName(name: "idx_videos_upload_date");
		b.HasIndex(static v => new { v.ChannelName, v.UploadDate })
			.HasDatabaseName(name: "idx_videos_channel_upload_date");
		b.HasIndex(static v => v.Title).HasDatabaseName(name: "idx_videos_title");

		b.HasIndex(static v => v.Title)
			.HasDatabaseName(name: "idx_videos_title_trgm")
			.HasMethod("gin")
			.HasOperators("gin_trgm_ops");
	}
}
