using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Tests.DbContext;

namespace CSharpScripts.Tests.EntityConfigs;

internal class AlbumTrackAdditionalTests : DatabaseTestBase
{
	[Test]
	public async Task Album_CanInsertWithArtist()
	{
		var context = Fixture.GetContext();

		var artist = new Artist { Name = "Test Artist" };
		var album = new Album { Artist = artist, Title = "Test Album", ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) };

		context.Artists.Add(artist);
		context.Albums.Add(album);
		await context.SaveChangesAsync();

		var retrieved = await context.Albums.FirstOrDefaultAsync(a => a.Title == "Test Album");

		retrieved.Should().NotBeNull();
		retrieved!.ArtistId.Should().Be(artist.Id);

	}

	[Test]
	public async Task Album_CanQueryByArtistId()
	{
		var context = Fixture.GetContext();

		var artist1 = new Artist { Name = "Artist 1" };
		var artist2 = new Artist { Name = "Artist 2" };

		context.Artists.AddRange(artist1, artist2);
		await context.SaveChangesAsync();

		var album1 = new Album { ArtistId = artist1.Id, Title = "Album 1", ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) };
		var album2 = new Album { ArtistId = artist2.Id, Title = "Album 2", ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) };

		context.Albums.AddRange(album1, album2);
		await context.SaveChangesAsync();

		var artist1Albums = await context.Albums
			.Where(a => a.ArtistId == artist1.Id)
			.ToListAsync();

		artist1Albums.Should().HaveCount(1);
		artist1Albums[0].Title.Should().Be("Album 1");

	}

	[Test]
	public async Task Track_CanInsertWithAlbumAndArtist()
	{
		var context = Fixture.GetContext();

		var artist = new Artist { Name = "Test Artist" };
		var album = new Album { Artist = artist, Title = "Test Album", ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) };
		var track = new Track { Album = album, Artist = artist, Title = "Test Track", DurationSeconds = 180 };

		context.Artists.Add(artist);
		context.Albums.Add(album);
		context.Tracks.Add(track);
		await context.SaveChangesAsync();

		var retrieved = await context.Tracks.FirstOrDefaultAsync(t => t.Title == "Test Track");

		retrieved.Should().NotBeNull();
		retrieved!.DurationSeconds.Should().Be(180);

	}

	[Test]
	public async Task Track_CanQueryByAlbumId()
	{
		var context = Fixture.GetContext();

		var artist = new Artist { Name = "Test Artist" };
		var album1 = new Album { Artist = artist, Title = "Album 1", ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) };
		var album2 = new Album { Artist = artist, Title = "Album 2", ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) };

		context.Artists.Add(artist);
		context.Albums.AddRange(album1, album2);
		await context.SaveChangesAsync();

		var track1 = new Track { AlbumId = album1.Id, ArtistId = artist.Id, Title = "Track 1", DurationSeconds = 180 };
		var track2 = new Track { AlbumId = album2.Id, ArtistId = artist.Id, Title = "Track 2", DurationSeconds = 200 };

		context.Tracks.AddRange(track1, track2);
		await context.SaveChangesAsync();

		var album1Tracks = await context.Tracks
			.Where(t => t.AlbumId == album1.Id)
			.ToListAsync();

		album1Tracks.Should().HaveCount(1);
		album1Tracks[0].Title.Should().Be("Track 1");

	}

	[Test]
	public async Task Track_CanQueryByArtistId()
	{
		var context = Fixture.GetContext();

		var artist1 = new Artist { Name = "Artist 1" };
		var artist2 = new Artist { Name = "Artist 2" };
		var album = new Album { Artist = artist1, Title = "Album", ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) };

		context.Artists.AddRange(artist1, artist2);
		context.Albums.Add(album);
		await context.SaveChangesAsync();

		var track1 = new Track { AlbumId = album.Id, ArtistId = artist1.Id, Title = "Track 1", DurationSeconds = 180 };
		var track2 = new Track { AlbumId = album.Id, ArtistId = artist2.Id, Title = "Track 2", DurationSeconds = 200 };

		context.Tracks.AddRange(track1, track2);
		await context.SaveChangesAsync();

		var artist1Tracks = await context.Tracks
			.Where(t => t.ArtistId == artist1.Id)
			.ToListAsync();

		artist1Tracks.Should().HaveCount(1);
		artist1Tracks[0].Title.Should().Be("Track 1");

	}

	[Test]
	public async Task Track_CanQueryByDurationRange()
	{
		var context = Fixture.GetContext();

		var artist = new Artist { Name = "Test Artist" };
		var album = new Album { Artist = artist, Title = "Album", ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) };

		context.Artists.Add(artist);
		context.Albums.Add(album);
		await context.SaveChangesAsync();

		var track1 = new Track { AlbumId = album.Id, ArtistId = artist.Id, Title = "Short Track", DurationSeconds = 120 };
		var track2 = new Track { AlbumId = album.Id, ArtistId = artist.Id, Title = "Medium Track", DurationSeconds = 180 };
		var track3 = new Track { AlbumId = album.Id, ArtistId = artist.Id, Title = "Long Track", DurationSeconds = 300 };

		context.Tracks.AddRange(track1, track2, track3);
		await context.SaveChangesAsync();

		var mediumTracks = await context.Tracks
			.Where(t => t.DurationSeconds >= 150 && t.DurationSeconds <= 200)
			.ToListAsync();

		mediumTracks.Should().HaveCount(1);
		mediumTracks[0].Title.Should().Be("Medium Track");

	}

	[Test]
	public async Task Album_CanUpdateReleaseDate()
	{
		var context = Fixture.GetContext();

		var artist = new Artist { Name = "Test Artist" };
		var originalDate = DateOnly.FromDateTime(DateTime.UtcNow);
		var album = new Album { Artist = artist, Title = "Test Album", ReleaseDate = originalDate };

		context.Artists.Add(artist);
		context.Albums.Add(album);
		await context.SaveChangesAsync();

		var newDate = originalDate.AddDays(-1);
		album.ReleaseDate = newDate;
		context.Albums.Update(album);
		await context.SaveChangesAsync();

		var retrieved = await context.Albums.FirstOrDefaultAsync(a => a.Title == "Test Album");

		retrieved.Should().NotBeNull();
		retrieved!.ReleaseDate.Should().Be(newDate);

	}

	[Test]
	public async Task Track_CanUpdateDuration()
	{
		var context = Fixture.GetContext();

		var artist = new Artist { Name = "Test Artist" };
		var album = new Album { Artist = artist, Title = "Album", ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) };
		var track = new Track { Album = album, Artist = artist, Title = "Test Track", DurationSeconds = 180 };

		context.Artists.Add(artist);
		context.Albums.Add(album);
		context.Tracks.Add(track);
		await context.SaveChangesAsync();

		track.DurationSeconds = 200;
		context.Tracks.Update(track);
		await context.SaveChangesAsync();

		var retrieved = await context.Tracks.FirstOrDefaultAsync(t => t.Title == "Test Track");

		retrieved.Should().NotBeNull();
		retrieved!.DurationSeconds.Should().Be(200);

	}
}
