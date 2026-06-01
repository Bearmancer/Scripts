using TUnit;
using FluentAssertions;
using Scripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class FailedTaskEntityTests
{
	[Test]
	public void FailedTask_HasRequired_Properties()
	{
		var props = typeof(FailedTask).GetProperties().Select(p => p.Name).ToList();

		props.Should().Contain("Id");
		props.Should().Contain("TaskName");
		props.Should().Contain("ErrorMessage");
		props.Should().Contain("Timestamp");
	}

	[Test]
	public void FailedTask_Id_IsGuid()
	{
		var prop = typeof(FailedTask).GetProperty("Id");
		prop.Should().NotBeNull();
		prop!.PropertyType.Should().Be<Guid>();
	}

	[Test]
	public void FailedTask_Timestamp_IsDateTimeOffset()
	{
		var prop = typeof(FailedTask).GetProperty("Timestamp");
		prop.Should().NotBeNull();
		prop!.PropertyType.Should().Be<DateTimeOffset>();
	}
}
