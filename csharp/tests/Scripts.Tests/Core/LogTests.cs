namespace Scripts.Tests.Core;

using Scripts.Core;

internal sealed class LogTests
{
	[Test]
	public async Task Track_NoArgs_ThrowsNothing()
	{
		await Assert.That(() =>
		{
			using var _ = Log.Track();
		}).ThrowsNothing();
	}

	[Test]
	public async Task Track_WithArgs_ThrowsNothing()
	{
		await Assert.That(() =>
		{
			using var _ = Log.Track(new { Id = 123, Name = "Test" });
		}).ThrowsNothing();
	}
}
