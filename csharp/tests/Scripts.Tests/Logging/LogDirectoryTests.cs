namespace Scripts.Tests.Logging;

internal sealed class LogDirectoryTests
{
	[Test]
	public async Task LogDirectory_Points_To_UserProfile_Cache_Logs_Scripts()
	{
		var logDir = Scripts.Core.Paths.LogDirectory;

		var expectedBase = Path.Combine(
			System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
			".cache",
			"logs",
			"scripts"
		);

		await Assert.That(logDir).IsEqualTo(expectedBase);
	}

	[Test]
	public async Task LogDirectory_Does_Not_Point_To_ProjectRoot()
	{
		var logDir = Scripts.Core.Paths.LogDirectory;
		var projectRoot = Scripts.Core.Paths.ProjectRoot;

		await Assert.That(logDir).DoesNotContain(projectRoot);
	}

	[Test]
	public async Task LogDirectory_Is_Absolute_Path()
	{
		var logDir = Scripts.Core.Paths.LogDirectory;

		await Assert.That(Path.IsPathRooted(logDir)).IsTrue();
	}

	[Test]
	public async Task LogDirectory_Is_Created_Automatically()
	{
		var logDir = Scripts.Core.Paths.LogDirectory;

		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(
			typeof(Core.Log).TypeHandle
		);

		await Assert.That(new DirectoryInfo(logDir).Exists).IsTrue();
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

		await Assert.That(content).Contains("Directory.CreateDirectory(path: Paths.LogDirectory)");
	}
}
