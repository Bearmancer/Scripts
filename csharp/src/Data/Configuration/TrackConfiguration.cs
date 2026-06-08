using Scripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Scripts.Data.Configuration;

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
	public void Configure(EntityTypeBuilder<Track> b)
	{
		b.ToTable(name: "tracks", schema: "music");
		b.Property(static t => t.Id).UseIdentityAlwaysColumn();
		b.Property(static t => t.AlbumId).HasColumnType(typeName: "integer").IsRequired(false);
		b.Property(static t => t.ArtistId).HasColumnType(typeName: "integer").IsRequired(false);
		b.Property(static t => t.WorkId).HasColumnType(typeName: "integer").IsRequired(false);
		b.Property(static t => t.MovementId).HasColumnType(typeName: "integer").IsRequired(false);
		b.Property(static t => t.Title).HasColumnType(typeName: "text").IsRequired();
		b.Property(static t => t.DurationSeconds).HasColumnType(typeName: "integer");
		b.HasIndex(static t => t.ArtistId);
		b.HasIndex(static t => t.AlbumId);
		b.HasIndex(static t => t.Title).HasDatabaseName(name: "idx_tracks_title");
		b.HasIndex(static t => new { t.ArtistId, t.Title })
			.IsUnique()
			.HasDatabaseName(name: "idx_tracks_artist_title");

		b.HasIndex(static t => t.Title)
			.HasDatabaseName(name: "idx_tracks_title_unaccent")
			.HasMethod("gin");

		b.HasIndex(static t => t.Title)
			.HasDatabaseName(name: "idx_tracks_title_trgm")
			.HasMethod("gin")
			.HasOperators("gin_trgm_ops");

		b.HasOne(static t => t.Artist)
			.WithMany(static a => a.Tracks)
			.HasForeignKey(static t => t.ArtistId)
			.OnDelete(DeleteBehavior.Restrict);

		b.HasOne(static t => t.Album)
			.WithMany(static a => a.Tracks)
			.HasForeignKey(static t => t.AlbumId)
			.OnDelete(DeleteBehavior.Restrict);

		b.HasOne(static t => t.MusicWork)
			.WithMany(static w => w.Tracks)
			.HasForeignKey(static t => t.WorkId)
			.OnDelete(DeleteBehavior.Restrict);

		b.HasOne(static t => t.Movement)
			.WithMany(static m => m.Tracks)
			.HasForeignKey(static t => t.MovementId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
