using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Logging;

internal sealed class LogDirectoryTests
{
	[Test]
	public void LogDirectory_Points_To_UserProfile_Cache_Logs_Scripts()
	{
		var logDir = Scripts.Core.Paths.LogDirectory;

		var expectedBase = Path.Combine(
			System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
			".cache",
			"logs",
			"scripts"
		);

		logDir.Should().Be(expectedBase);
	}

	[Test]
	public void LogDirectory_Does_Not_Point_To_ProjectRoot()
	{
		var logDir = Scripts.Core.Paths.LogDirectory;
		var projectRoot = Scripts.Core.Paths.ProjectRoot;

		logDir.Should().NotContain(projectRoot);
	}

	[Test]
	public void LogDirectory_Is_Absolute_Path()
	{
		var logDir = Scripts.Core.Paths.LogDirectory;

		Path.IsPathRooted(logDir).Should().BeTrue();
	}

	[Test]
	public void LogDirectory_Is_Created_Automatically()
	{
		var logDir = Scripts.Core.Paths.LogDirectory;

		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(
			typeof(Scripts.Core.Log).TypeHandle
		);

		new DirectoryInfo(logDir).Exists.Should().BeTrue();
	}

	[Test]
	public async Task LogStaticConstructor_Creates_LogDirectory()
	{
		var logPath = Path.Combine(
			Scripts.Core.Paths.ProjectRoot,
			"csharp",
			"src",
			"Core",
			"Log.cs"
		);
		var content = await File.ReadAllTextAsync(logPath);

		content.Should().Contain("Directory.CreateDirectory(path: Paths.LogDirectory)");
	}
}
