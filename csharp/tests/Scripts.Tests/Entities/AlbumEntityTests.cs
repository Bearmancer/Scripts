using Scripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class AlbumEntityTests
{
	[Test]
	public async Task Album_HasRequired_Properties()
	{
		var props = typeof(Album).GetProperties().Select(p => p.Name).ToList();

		await Assert.That(props).Contains("Id");
		await Assert.That(props).Contains("ArtistId");
		await Assert.That(props).Contains("Title");
		await Assert.That(props).Contains("ReleaseDate");
		await Assert.That(props).Contains("Artist");
		await Assert.That(props).Contains("Tracks");
	}

	[Test]
	public async Task Album_ArtistId_IsInt()
	{
		var prop = typeof(Album).GetProperty("ArtistId");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(int));
	}

	[Test]
	public async Task Album_ReleaseDate_IsNullableDateOnly()
	{
		var prop = typeof(Album).GetProperty("ReleaseDate");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(DateOnly?));
	}

	[Test]
	public async Task Album_Tracks_IsCollection()
	{
		var prop = typeof(Album).GetProperty("Tracks");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType.IsGenericType).IsTrue();
		await Assert
			.That(prop.PropertyType.GetGenericTypeDefinition())
			.IsEqualTo(typeof(ICollection<>));
	}

	[Test]
	public async Task Album_CanBeInstantiated_WithDefaults()
	{
		var album = new Album { Title = "OK Computer", ArtistId = 1 };
		await Assert.That(album.Title).IsEqualTo("OK Computer");
		await Assert.That(album.ReleaseDate).IsNull();
		await Assert.That(album.Tracks).IsNotNull();
	}
}
