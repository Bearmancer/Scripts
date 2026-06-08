using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;
using Scripts.Services.Music;
using Scripts.Tests.Attributes;

namespace Scripts.Tests.Services.Music;

internal sealed class PurgeServiceTests : DatabaseTestBase
{
	[RequiresPgConnStr]
	[Test]
	public async Task PurgeOrphansAsync_PurgesEmptyAlbumsAndArtists()
	{
		await using var context = Fixture.GetContext();
		
		var artist = new Artist { Name = "Orphan Artist" };
		context.Artists.Add(artist);
		
		var album = new Album { Artist = artist, Title = "Orphan Album" };
		context.Albums.Add(album);
		
		await context.SaveChangesAsync();

		var factory = Fixture.GetContextFactory();
		var service = new PurgeService(factory);

		var result = await service.PurgeOrphansAsync();

		result.AlbumsPurged.Should().Be(1);
		result.ArtistsPurged.Should().Be(1);

		await using var verifyContext = Fixture.GetContext();
		(await verifyContext.Albums.AnyAsync()).Should().BeFalse();
		(await verifyContext.Artists.AnyAsync()).Should().BeFalse();
	}

	[RequiresPgConnStr]
	[Test]
	public async Task PurgeOrphansAsync_DoesNotPurgeAlbumsWithTracks()
	{
		await using var context = Fixture.GetContext();
		
		var artist = new Artist { Name = "Active Artist" };
		context.Artists.Add(artist);
		
		var album = new Album { Artist = artist, Title = "Active Album" };
		context.Albums.Add(album);
		
		var track = new Track { Album = album, Artist = artist, Title = "Active Track" };
		context.Tracks.Add(track);
		
		await context.SaveChangesAsync();

		var factory = Fixture.GetContextFactory();
		var service = new PurgeService(factory);

		var result = await service.PurgeOrphansAsync();

		result.AlbumsPurged.Should().Be(0);
		result.ArtistsPurged.Should().Be(0);

		await using var verifyContext = Fixture.GetContext();
		(await verifyContext.Albums.AnyAsync()).Should().BeTrue();
		(await verifyContext.Artists.AnyAsync()).Should().BeTrue();
	}
}
