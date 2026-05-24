using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityRefactoring;

internal sealed class AlbumMbidRemovalTests
{
    [Test]
    public void Album_DoesNotHave_MbidProperty()
    {
        var mbidProp = typeof(Album).GetProperty("Mbid");
        mbidProp.Should().BeNull(because: "Mbid has zero external references and should be removed");
    }
}
