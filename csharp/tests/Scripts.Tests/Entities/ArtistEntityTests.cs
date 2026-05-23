#pragma warning disable CA2263
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
    }

    [Test]
    public void Artist_Id_IsInt()
    {
        var idProp = typeof(Artist).GetProperty("Id");
        idProp.Should().NotBeNull();
        idProp!.PropertyType.Should().Be(typeof(int));
    }

    [Test]
    public void Artist_Name_IsString()
    {
        var nameProp = typeof(Artist).GetProperty("Name");
        nameProp.Should().NotBeNull();
        nameProp!.PropertyType.Should().Be(typeof(string));
    }

    [Test]
    public void Artist_Metadata_IsNullableJsonDocument()
    {
        var metaProp = typeof(Artist).GetProperty("Metadata");
        metaProp.Should().NotBeNull();
        metaProp!.PropertyType.Should().Be(typeof(JsonDocument));
    }

    [Test]
    public void Artist_Albums_IsCollection()
    {
        var albumsProp = typeof(Artist).GetProperty("Albums");
        albumsProp.Should().NotBeNull();
        albumsProp!.PropertyType.IsGenericType.Should().BeTrue();
        albumsProp.PropertyType.GetGenericTypeDefinition().Should().Be(typeof(ICollection<>));
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
