using Scripts.Data.Entities;

namespace Scripts.Tests.EntityRefactoring;

internal sealed class TrackMbidRemovalTests
{
	[Test]
	public async Task Track_DoesNotHave_MbidProperty()
	{
		var mbidProp = typeof(Track).GetProperty("Mbid");
		await Assert.That(mbidProp).IsNull();
	}
}
