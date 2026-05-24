using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class ScrobbleEntityTests
{
    [Test]
    public void Scrobble_HasRequired_Properties()
    {
        var props = typeof(Scrobble).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("TrackId");
        props.Should().Contain("ScrobbledAt");
        props.Should().Contain("Platform");
        props.Should().Contain("Track");
    }

    [Test]
    public void Scrobble_Id_IsLong()
    {
        var prop = typeof(Scrobble).GetProperty("Id");
        prop.Should().NotBeNull();
        prop!.PropertyType.Should().Be<long>();
    }

    [Test]
    public void Scrobble_ScrobbledAt_IsDateTimeOffset()
    {
        var prop = typeof(Scrobble).GetProperty("ScrobbledAt");
        prop.Should().NotBeNull();
        prop!.PropertyType.Should().Be<DateTimeOffset>();
    }

    [Test]
    public void Scrobble_Platform_IsString()
    {
        var prop = typeof(Scrobble).GetProperty("Platform");
        prop.Should().NotBeNull();
        prop!.PropertyType.Should().Be<string>();
    }

    [Test]
    public void Scrobble_CanBeInstantiated_WithDefaults()
    {
        var scrobble = new Scrobble
        {
            Id = 1,
            TrackId = 1,
            ScrobbledAt = DateTimeOffset.UtcNow,
            Platform = "lastfm"
        };
        scrobble.Platform.Should().Be("lastfm");
    }
}
