using System.Diagnostics;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Logging;

internal sealed class BenDemystifierTests
{
	[Test]
	public void Demystify_Is_Available_As_Extension_Method()
	{
		var ex = new InvalidOperationException("test");

		var demystified = ex.Demystify();

		demystified.Should().NotBeNull();
		demystified.Message.Should().Be("test");
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

		content.Should().Contain("exception: ex.Demystify()");
	}
}
