using Scripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class FailedTaskEntityTests
{
	[Test]
	public async Task FailedTask_HasRequired_Properties()
	{
		var props = typeof(FailedTask).GetProperties().Select(p => p.Name).ToList();

		await Assert.That(props).Contains("Id");
		await Assert.That(props).Contains("TaskName");
		await Assert.That(props).Contains("ErrorMessage");
		await Assert.That(props).Contains("Timestamp");
	}

	[Test]
	public async Task FailedTask_Id_IsGuid()
	{
		var prop = typeof(FailedTask).GetProperty("Id");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(Guid));
	}

	[Test]
	public async Task FailedTask_Timestamp_IsDateTimeOffset()
	{
		var prop = typeof(FailedTask).GetProperty("Timestamp");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(DateTimeOffset));
	}
}
