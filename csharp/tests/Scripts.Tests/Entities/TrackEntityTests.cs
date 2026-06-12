using Scripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class TrackEntityTests
{
	[Test]
	public async Task Track_HasRequired_Properties()
	{
		var props = typeof(Track).GetProperties().Select(p => p.Name).ToList();

		await Assert.That(props).Contains("Id");
		await Assert.That(props).Contains("AlbumId");
		await Assert.That(props).Contains("ArtistId");
		await Assert.That(props).Contains("Title");
		await Assert.That(props).Contains("DurationSeconds");
		await Assert.That(props).Contains("Album");
		await Assert.That(props).Contains("Artist");
		await Assert.That(props).Contains("Scrobbles");
	}

	[Test]
	public async Task Track_DurationSeconds_IsNullableInt()
	{
		var prop = typeof(Track).GetProperty("DurationSeconds");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(int?));
	}

	[Test]
	public async Task Track_Scrobbles_IsCollection()
	{
		var prop = typeof(Track).GetProperty("Scrobbles");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType.IsGenericType).IsTrue();
		await Assert
			.That(prop.PropertyType.GetGenericTypeDefinition())
			.IsEqualTo(typeof(ICollection<>));
	}

	[Test]
	public async Task Track_CanBeInstantiated_WithDefaults()
	{
		var track = new Track
		{
			Title = "Karma Police",
			AlbumId = 1,
			ArtistId = 1,
		};
		await Assert.That(track.DurationSeconds).IsNull();
		await Assert.That(track.Scrobbles).IsNotNull();
	}
}
