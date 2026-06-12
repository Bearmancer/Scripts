using System.Diagnostics;

namespace Scripts.Tests.Logging;

internal sealed class BenDemystifierTests
{
	[Test]
	public async Task Demystify_Is_Available_As_Extension_Method()
	{
		var ex = new InvalidOperationException("test");

		var demystified = ex.Demystify();

		await Assert.That(demystified).IsNotNull();
		await Assert.That(demystified.Message).IsEqualTo("test");
	}

	[Test]
	public async Task Log_Exception_Overloads_Demystify_Exceptions()
	{
		var logPath = Path.Combine(
			Scripts.Core.Paths.ProjectRoot,
			"csharp",
			"src",
			"Core",
			"Log.cs"
		);
		var content = await File.ReadAllTextAsync(logPath);

		await Assert.That(content).Contains("exception: ex.Demystify()");
	}
}
