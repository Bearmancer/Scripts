#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Configuration;

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
	public void Configure(EntityTypeBuilder<Track> b)
	{
		b.ToTable("tracks");
		b.Property(t => t.Id).UseIdentityAlwaysColumn();
		b.HasIndex(t => t.ArtistId);
		b.HasIndex(t => t.AlbumId);
		b.HasIndex(t => t.Title).HasDatabaseName("idx_tracks_title");
	}
}

