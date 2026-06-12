using System.Text.Json;
using Scripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class ExecutionLogEntityTests
{
	[Test]
	public async Task ExecutionLog_HasRequired_Properties()
	{
		var props = typeof(ExecutionLog).GetProperties().Select(p => p.Name).ToList();

		await Assert.That(props).Contains("Id");
		await Assert.That(props).Contains("Timestamp");
		await Assert.That(props).Contains("SessionId");
		await Assert.That(props).Contains("Payload");
		await Assert.That(props).Contains("ExitCode");
	}

	[Test]
	public async Task ExecutionLog_Payload_IsJsonDocument()
	{
		var prop = typeof(ExecutionLog).GetProperty("Payload");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(JsonDocument));
	}

	[Test]
	public async Task ExecutionLog_Timestamp_IsDateTimeOffset()
	{
		var prop = typeof(ExecutionLog).GetProperty("Timestamp");
		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.PropertyType).IsEqualTo(typeof(DateTimeOffset));
	}
}
