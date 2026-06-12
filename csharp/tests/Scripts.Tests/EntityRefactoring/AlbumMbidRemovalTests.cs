using Scripts.Data.Entities;

namespace Scripts.Tests.EntityRefactoring;

internal sealed class AlbumMbidRemovalTests
{
	[Test]
	public async Task Album_DoesNotHave_MbidProperty()
	{
		var mbidProp = typeof(Album).GetProperty("Mbid");
		await Assert.That(mbidProp).IsNull();
	}
}
