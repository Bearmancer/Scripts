#pragma warning disable CA2263 // Prefer generic overload Be<T>
#pragma warning disable IDE0022 // Use expression body for method

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
        typeof(Scrobble).GetProperty("Id")!.PropertyType.Should().Be(typeof(long));
    }

    [Test]
    public void Scrobble_ScrobbledAt_IsDateTimeOffset()
    {
        typeof(Scrobble).GetProperty("ScrobbledAt")!.PropertyType
            .Should().Be(typeof(DateTimeOffset));
    }

    [Test]
    public void Scrobble_Platform_IsString()
    {
        typeof(Scrobble).GetProperty("Platform")!.PropertyType.Should().Be(typeof(string));
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
