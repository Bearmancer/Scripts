using TUnit;
using FluentAssertions;
using Scripts.Data.Entities;

namespace Scripts.Tests.EntityRefactoring;

internal sealed class ArtistMbidRemovalTests
{
	[Test]
	public void Artist_DoesNotHave_MbidProperty()
	{
		var mbidProp = typeof(Artist).GetProperty("Mbid");
		mbidProp.Should().BeNull(because: "Mbid has zero external references and should be removed");
	}
}
