using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Logging;

internal sealed class ServiceTypeTests
{
	[Test]
	public void ServiceType_Does_Not_Contain_Sheets()
	{
		var enumValues = System.Enum.GetNames<Scripts.Core.ServiceType>();

		enumValues.Should().NotContain("Sheets");
	}

	[Test]
	public void ServiceType_Has_Exactly_Five_Values()
	{
		var enumValues = System.Enum.GetNames<Scripts.Core.ServiceType>();

		enumValues.Should().HaveCount(5);
	}

	[Test]
	public void ServiceType_Contains_Expected_Values()
	{
		var enumValues = System.Enum.GetNames<Scripts.Core.ServiceType>();

		enumValues.Should().Contain(["LastFm", "YouTube", "Music", "Read", "Cloud"]);
	}

	[Test]
	public async Task ServiceType_Sheets_Removed_From_Log_Enum()
	{
		var logPath = Path.Combine(
			Scripts.Core.Paths.ProjectRoot,
			"csharp",
			"src",
			"Core",
			"Log.cs"
		);
		var content = await File.ReadAllTextAsync(logPath);

		content.Should().NotContain("Sheets,");
		content.Should().NotContain("ServiceType.Sheets");
	}

	[Test]
	public async Task ServiceType_Sheets_Removed_From_Resilience_Timeout()
	{
		var resiliencePath = Path.Combine(
			Scripts.Core.Paths.ProjectRoot,
			"csharp",
			"src",
			"Core",
			"Resilience.cs"
		);
		var content = await File.ReadAllTextAsync(resiliencePath);

		content.Should().NotContain("ServiceType.Sheets");
	}
}
