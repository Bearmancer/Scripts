#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
	public void Configure(EntityTypeBuilder<Track> b)
	{
		b.ToTable(name: "tracks");
		b.Property(static t => t.Id).UseIdentityAlwaysColumn();
		b.HasIndex(static t => t.ArtistId);
		b.HasIndex(static t => t.AlbumId);
		b.HasIndex(static t => t.Title).HasDatabaseName(name: "idx_tracks_title");

		b.HasOne(static t => t.Artist)
			.WithMany(static a => a.Tracks)
			.HasForeignKey(static t => t.ArtistId)
			.ExcludeForeignKeyFromMigrations();

		b.HasOne(static t => t.Album)
			.WithMany(static a => a.Tracks)
			.HasForeignKey(static t => t.AlbumId)
			.ExcludeForeignKeyFromMigrations();
	}
}
