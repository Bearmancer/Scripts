using TUnit;
using FluentAssertions;

namespace Scripts.Tests.SyncService;

internal sealed class LastFmServiceDeleteTests
{
    [Test]
    public void LegacyLastFmService_FileDoesNotExist()
    {
        var path = Path.Combine(TestPaths.SrcRoot, "Services", "Sync", "LastFm", "LastFmService.cs");
        System.IO.File.Exists(path).Should().BeFalse(
            because: "Legacy duplicate LastFmService must be deleted — canonical version is at Services/Sync/LastFmService.cs");
    }

    [Test]
    public void CanonicalLastFmService_FileExists()
    {
        var path = Path.Combine(TestPaths.SrcRoot, "Services", "Sync", "LastFmService.cs");
        System.IO.File.Exists(path).Should().BeTrue(
            because: "Canonical LastFmService at Services/Sync/LastFmService.cs must be preserved");
    }

    [Test]
    public void LegacyNamespace_DoesNotContainInlineScrobbleDefinition()
    {
        
        var inlineType = Type.GetType("Scripts.Services.Sync.LastFm.Scrobble, Scripts");
        inlineType.Should().BeNull(because: "Inline Scrobble from legacy file must not exist");
    }
}
