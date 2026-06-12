using Scripts.Data.Entities;

namespace Scripts.Tests.EntityRefactoring;

internal sealed class ArtistMbidRemovalTests
{
	[Test]
	public async Task Artist_DoesNotHave_MbidProperty()
	{
		var mbidProp = typeof(Artist).GetProperty("Mbid");
		await Assert.That(mbidProp).IsNull();
	}
}
