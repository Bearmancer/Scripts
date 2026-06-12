namespace Scripts.Tests.ReleaseProgress;

internal sealed class ReleaseProgressEntityTests
{
	[Test]
	public async Task ReleaseProgress_HasRequired_Properties()
	{
		var props = typeof(Data.Entities.ReleaseProgress)
			.GetProperties()
			.Select(p => p.Name)
			.ToList();

		await Assert.That(props).Contains("Id");
		await Assert.That(props).Contains("ReleaseId");
		await Assert.That(props).Contains("DiscNumber");
		await Assert.That(props).Contains("TrackNumber");
		await Assert.That(props).Contains("Title");
		await Assert.That(props).Contains("Duration");
		await Assert.That(props).Contains("RecordingYear");
		await Assert.That(props).Contains("Composer");
		await Assert.That(props).Contains("WorkName");
		await Assert.That(props).Contains("Conductor");
		await Assert.That(props).Contains("Orchestra");
		await Assert.That(props).Contains("Soloists");
		await Assert.That(props).Contains("Artist");
		await Assert.That(props).Contains("RecordingVenue");
		await Assert.That(props).Contains("RecordingId");
		await Assert.That(props).Contains("CreatedAt");
	}

	[Test]
	public async Task ReleaseProgress_Id_IsLong()
	{
		await Assert
			.That(typeof(Data.Entities.ReleaseProgress).GetProperty("Id")!.PropertyType)
			.IsEqualTo(typeof(long));
	}

	[Test]
	public async Task ReleaseProgress_CanBeInstantiated_WithDefaults()
	{
		var rp = new Data.Entities.ReleaseProgress
		{
			ReleaseId = "abc123",
			DiscNumber = 1,
			TrackNumber = 1,
			Title = "Test Track",
		};

		await Assert.That(rp.ReleaseId).IsEqualTo("abc123");
		await Assert.That(rp.DiscNumber).IsEqualTo(1);
		await Assert.That(rp.Soloists).IsNull();
		await Assert.That(rp.CreatedAt).IsEqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5));
	}
}
