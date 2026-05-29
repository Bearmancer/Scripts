using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.ReleaseProgressTests;

internal sealed class ReleaseProgressEntityTests
{
    [Test]
    public void ReleaseProgress_HasRequired_Properties()
    {
        var props = typeof(CSharpScripts.Data.Entities.ReleaseProgress).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("ReleaseId");
        props.Should().Contain("DiscNumber");
        props.Should().Contain("TrackNumber");
        props.Should().Contain("Title");
        props.Should().Contain("Duration");
        props.Should().Contain("RecordingYear");
        props.Should().Contain("Composer");
        props.Should().Contain("WorkName");
        props.Should().Contain("Conductor");
        props.Should().Contain("Orchestra");
        props.Should().Contain("Soloists");
        props.Should().Contain("Artist");
        props.Should().Contain("RecordingVenue");
        props.Should().Contain("RecordingId");
        props.Should().Contain("CreatedAt");
    }

    [Test]
    public void ReleaseProgress_Id_IsLong()
    {
        typeof(CSharpScripts.Data.Entities.ReleaseProgress)
            .GetProperty("Id")!.PropertyType.Should().Be<long>();
    }

    [Test]
    public void ReleaseProgress_CanBeInstantiated_WithDefaults()
    {
        var rp = new CSharpScripts.Data.Entities.ReleaseProgress
        {
            ReleaseId = "abc123",
            DiscNumber = 1,
            TrackNumber = 1,
            Title = "Test Track"
        };

        rp.ReleaseId.Should().Be("abc123");
        rp.DiscNumber.Should().Be(1);
        rp.Soloists.Should().BeNull();
        rp.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
