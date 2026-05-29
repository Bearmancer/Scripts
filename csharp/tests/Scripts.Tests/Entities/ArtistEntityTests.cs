using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;
using System.Text.Json;

namespace Scripts.Tests.Entities;

internal sealed class ArtistEntityTests
{
	[Test]
	public void Artist_HasRequired_Properties()
	{
		var props = typeof(Artist).GetProperties().Select(p => p.Name).ToList();

		props.Should().Contain("Id");
		props.Should().Contain("Name");
		props.Should().Contain("Metadata");
		props.Should().Contain("Albums");
		props.Should().Contain("Tracks");
	}

	[Test]
	public void Artist_Id_IsInt()
	{
		var prop = typeof(Artist).GetProperty("Id");
		prop.Should().NotBeNull();
		prop!.PropertyType.Should().Be<int>();
	}

	[Test]
	public void Artist_Name_IsString()
	{
		var prop = typeof(Artist).GetProperty("Name");
		prop.Should().NotBeNull();
		prop!.PropertyType.Should().Be<string>();
	}

	[Test]
	public void Artist_Metadata_IsNullableJsonDocument()
	{
		var prop = typeof(Artist).GetProperty("Metadata");
		prop.Should().NotBeNull();
		prop!.PropertyType.Should().Be<JsonDocument>();
	}

	[Test]
	public void Artist_Albums_IsCollection()
	{
		var prop = typeof(Artist).GetProperty("Albums");
		prop.Should().NotBeNull();
		prop!.PropertyType.IsGenericType.Should().BeTrue();
		prop.PropertyType.GetGenericTypeDefinition().Should().Be(typeof(ICollection<>));
	}

	[Test]
	public void Artist_CanBeInstantiated_WithDefaults()
	{
		var artist = new Artist { Name = "Radiohead" };
		artist.Name.Should().Be("Radiohead");
		artist.Metadata.Should().BeNull();
		artist.Albums.Should().NotBeNull();
	}
}
