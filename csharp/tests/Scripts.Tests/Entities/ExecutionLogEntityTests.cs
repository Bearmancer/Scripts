using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;
using System.Text.Json;

namespace Scripts.Tests.Entities;

internal sealed class ExecutionLogEntityTests
{
	[Test]
	public void ExecutionLog_HasRequired_Properties()
	{
		var props = typeof(ExecutionLog).GetProperties().Select(p => p.Name).ToList();

		props.Should().Contain("Id");
		props.Should().Contain("Timestamp");
		props.Should().Contain("SessionId");
		props.Should().Contain("Payload");
		props.Should().Contain("ExitCode");
	}

	[Test]
	public void ExecutionLog_Payload_IsJsonDocument()
	{
		var prop = typeof(ExecutionLog).GetProperty("Payload");
		prop.Should().NotBeNull();
		prop!.PropertyType.Should().Be<JsonDocument>();
	}

	[Test]
	public void ExecutionLog_Timestamp_IsDateTimeOffset()
	{
		var prop = typeof(ExecutionLog).GetProperty("Timestamp");
		prop.Should().NotBeNull();
		prop!.PropertyType.Should().Be<DateTimeOffset>();
	}
}
