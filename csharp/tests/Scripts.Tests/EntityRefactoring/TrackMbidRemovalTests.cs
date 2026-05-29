using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityRefactoring;

internal sealed class TrackMbidRemovalTests
{
	[Test]
	public void Track_DoesNotHave_MbidProperty()
	{
		var mbidProp = typeof(Track).GetProperty("Mbid");
		mbidProp.Should().BeNull(because: "Mbid has zero external references and should be removed");
	}
}
