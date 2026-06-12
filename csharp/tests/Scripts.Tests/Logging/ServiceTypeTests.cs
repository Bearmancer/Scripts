namespace Scripts.Tests.Logging;

internal sealed class ServiceTypeTests
{
	[Test]
	public async Task ServiceType_Does_Not_Contain_Sheets()
	{
		var enumValues = System.Enum.GetNames<Core.ServiceType>();

		await Assert.That(enumValues).DoesNotContain("Sheets");
	}

	[Test]
	public async Task ServiceType_Has_Exactly_Five_Values()
	{
		var enumValues = System.Enum.GetNames<Core.ServiceType>();

		await Assert.That(enumValues).Count().IsEqualTo(5);
	}

	[Test]
	public async Task ServiceType_Contains_Expected_Values()
	{
		var enumValues = System.Enum.GetNames<Core.ServiceType>();

		await Assert
			.That(enumValues)
			.IsEquivalentTo(["LastFm", "YouTube", "Music", "Read", "Cloud"]);
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

		await Assert.That(content).DoesNotContain("Sheets,");
		await Assert.That(content).DoesNotContain("ServiceType.Sheets");
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

		await Assert.That(content).DoesNotContain("ServiceType.Sheets");
	}
}
