using CSharpScripts.Data.Entities;

namespace CSharpScripts.Tests.Data;

public class EntityModelTests
{
	[Fact]
	public void LastFm_Entities_Match_Canonical_Model()
	{
		DateTimeOffset now = DateTimeOffset.UtcNow;

		var artist = new Artist { Id = 1, Name = "Radiohead" };
		_ = new Album { Id = 1, ArtistId = 1, Title = "OK Computer" };
		var track = new Track { Id = 1, ArtistId = 1, AlbumId = 1, Title = "Karma Police" };
		var scrobble = new Scrobble { Id = 1L, TrackId = 1, ScrobbledAt = now };

		Assert.IsType<int>(artist.Id);
		Assert.IsType<int>(track.ArtistId);
		Assert.Equal(typeof(int?), typeof(Track).GetProperty(nameof(Track.AlbumId))?.PropertyType);
		Assert.IsType<long>(scrobble.Id);
		Assert.IsType<DateTimeOffset>(scrobble.ScrobbledAt);
	}
}
