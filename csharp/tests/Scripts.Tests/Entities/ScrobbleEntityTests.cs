using Scripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class ScrobbleEntityTests
{
	[Test]
	public async Task Scrobble_HasRequired_Properties()
	{
		var props = typeof(Scrobble).GetProperties().Select(p => p.Name).ToList();

		await Assert.That(props).Contains("Id");
		await Assert.That(props).Contains("TrackId");
		await Assert.That(props).Contains("ScrobbledAt");
		await Assert.That(props).Contains("Platform");
		await Assert.That(props).Contains("Track");
	}

	[Test]
	public async Task Scrobble_Id_IsLong()
	{
		var prop = typeof(Scrobble).GetProperty("Id");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(long));
	}

	[Test]
	public async Task Scrobble_ScrobbledAt_IsDateTimeOffset()
	{
		var prop = typeof(Scrobble).GetProperty("ScrobbledAt");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(DateTimeOffset));
	}

	[Test]
	public async Task Scrobble_Platform_IsString()
	{
		var prop = typeof(Scrobble).GetProperty("Platform");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(string));
	}

	[Test]
	public async Task Scrobble_CanBeInstantiated_WithDefaults()
	{
		var scrobble = new Scrobble
		{
			Id = 1,
			TrackId = 1,
			ScrobbledAt = DateTimeOffset.UtcNow,
			Platform = "lastfm",
		};
		await Assert.That(scrobble.Platform).IsEqualTo("lastfm");
	}
}
