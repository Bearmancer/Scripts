using System.Text.Json;
using Scripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class ArtistEntityTests
{
	[Test]
	public async Task Artist_HasRequired_Properties()
	{
		var props = typeof(Artist).GetProperties().Select(p => p.Name).ToList();

		await Assert.That(props).Contains("Id");
		await Assert.That(props).Contains("Name");
		await Assert.That(props).Contains("Metadata");
		await Assert.That(props).Contains("Albums");
		await Assert.That(props).Contains("Tracks");
	}

	[Test]
	public async Task Artist_Id_IsInt()
	{
		var prop = typeof(Artist).GetProperty("Id");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(int));
	}

	[Test]
	public async Task Artist_Name_IsString()
	{
		var prop = typeof(Artist).GetProperty("Name");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(string));
	}

	[Test]
	public async Task Artist_Metadata_IsNullableJsonDocument()
	{
		var prop = typeof(Artist).GetProperty("Metadata");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(JsonDocument));
	}

	[Test]
	public async Task Artist_Albums_IsCollection()
	{
		var prop = typeof(Artist).GetProperty("Albums");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType.IsGenericType).IsTrue();
		await Assert
			.That(prop.PropertyType.GetGenericTypeDefinition())
			.IsEqualTo(typeof(ICollection<>));
	}

	[Test]
	public async Task Artist_CanBeInstantiated_WithDefaults()
	{
		var artist = new Artist { Name = "Radiohead" };
		await Assert.That(artist.Name).IsEqualTo("Radiohead");
		await Assert.That(artist.Metadata).IsNull();
		await Assert.That(artist.Albums).IsNotNull();
	}
}
