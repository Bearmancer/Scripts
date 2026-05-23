#pragma warning disable CA2263, IDE0022
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class VideoEntityTests
{
    [Test]
    public void Video_HasRequired_Properties()
    {
        var props = typeof(Video).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("YoutubeId");
        props.Should().Contain("Title");
        props.Should().Contain("PlaylistId");
        props.Should().Contain("IsDeleted");
    }

    [Test]
    public void Video_YoutubeId_IsString()
    {
        typeof(Video).GetProperty("YoutubeId")!.PropertyType.Should().Be(typeof(string));
    }

    [Test]
    public void Video_IsDeleted_IsBool()
    {
        typeof(Video).GetProperty("IsDeleted")!.PropertyType.Should().Be(typeof(bool));
    }

    [Test]
    public void Video_CanBeInstantiated_WithDefaults()
    {
        var video = new Video { YoutubeId = "dQw4w9WgXcQ", Title = "Never Gonna Give You Up", PlaylistId = "PL123" };
        video.IsDeleted.Should().BeFalse();
    }
}
#pragma warning restore CA2263, IDE0022
