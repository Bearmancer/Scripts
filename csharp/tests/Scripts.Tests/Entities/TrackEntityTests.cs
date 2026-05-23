#pragma warning disable CA2263
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class TrackEntityTests
{
    [Test]
    public void Track_HasRequired_Properties()
    {
        var props = typeof(Track).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("AlbumId");
        props.Should().Contain("ArtistId");
        props.Should().Contain("Title");
        props.Should().Contain("DurationSeconds");
        props.Should().Contain("Album");
        props.Should().Contain("Artist");
        props.Should().Contain("Scrobbles");
    }

    [Test]
    public void Track_DurationSeconds_IsNullableInt()
    {
        var prop = typeof(Track).GetProperty("DurationSeconds");
        prop.Should().NotBeNull();
        prop!.PropertyType.Should().Be(typeof(int?));
    }

    [Test]
    public void Track_Scrobbles_IsCollection()
    {
        var prop = typeof(Track).GetProperty("Scrobbles");
        prop!.PropertyType.IsGenericType.Should().BeTrue();
        prop.PropertyType.GetGenericTypeDefinition().Should().Be(typeof(ICollection<>));
    }

    [Test]
    public void Track_CanBeInstantiated_WithDefaults()
    {
        var track = new Track { Title = "Karma Police", AlbumId = 1, ArtistId = 1 };
        track.DurationSeconds.Should().BeNull();
        track.Scrobbles.Should().NotBeNull();
    }
}
