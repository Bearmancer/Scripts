using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class AlbumEntityTests
{
	[Test]
	public void Album_HasRequired_Properties()
	{
		var props = typeof(Album).GetProperties().Select(p => p.Name).ToList();

		props.Should().Contain("Id");
		props.Should().Contain("ArtistId");
		props.Should().Contain("Title");
		props.Should().Contain("ReleaseDate");
		props.Should().Contain("Artist");
		props.Should().Contain("Tracks");
	}

	[Test]
	public void Album_ArtistId_IsInt()
	{
		var prop = typeof(Album).GetProperty("ArtistId");
		prop.Should().NotBeNull();
		prop!.PropertyType.Should().Be<int>();
	}

	[Test]
	public void Album_ReleaseDate_IsNullableDateOnly()
	{
		var prop = typeof(Album).GetProperty("ReleaseDate");
		prop.Should().NotBeNull();
		prop!.PropertyType.Should().Be<DateOnly?>();
	}

	[Test]
	public void Album_Tracks_IsCollection()
	{
		var prop = typeof(Album).GetProperty("Tracks");
		prop.Should().NotBeNull();
		prop!.PropertyType.IsGenericType.Should().BeTrue();
		prop.PropertyType.GetGenericTypeDefinition().Should().Be(typeof(ICollection<>));
	}

	[Test]
	public void Album_CanBeInstantiated_WithDefaults()
	{
		var album = new Album { Title = "OK Computer", ArtistId = 1 };
		album.Title.Should().Be("OK Computer");
		album.ReleaseDate.Should().BeNull();
		album.Tracks.Should().NotBeNull();
	}
}
