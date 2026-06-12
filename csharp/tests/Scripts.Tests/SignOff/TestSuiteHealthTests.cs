namespace Scripts.Tests.SignOff;

internal sealed class TestSuiteHealthTests
{
	private static string TestsRoot => TestPaths.Combine("csharp", "tests", "Scripts.Tests");

	[Test]
	public async Task Test_Directories_Exist_And_Contain_Tests()
	{
		var testDirs = new[]
		{
			Path.Combine(TestsRoot, "Environment"),
			Path.Combine(TestsRoot, "Guards"),
			Path.Combine(TestsRoot, "Logging"),
			Path.Combine(TestsRoot, "Language"),
			Path.Combine(TestsRoot, "Repositories"),
			Path.Combine(TestsRoot, "StateManager"),
			Path.Combine(TestsRoot, "DbContext"),
			Path.Combine(TestsRoot, "SignOff"),
		};

		var missing = new List<string>();
		foreach (var dir in testDirs)
		{
			if (!Directory.Exists(dir))
				missing.Add(dir);
		}

		await Assert.That(missing).IsEmpty();
	}

	[Test]
	public async Task Each_Test_Directory_Has_At_Least_One_Cs_File()
	{
		var testDirs = new[]
		{
			Path.Combine(TestsRoot, "Environment"),
			Path.Combine(TestsRoot, "Guards"),
			Path.Combine(TestsRoot, "Logging"),
			Path.Combine(TestsRoot, "Language"),
		};

		var empty = new List<string>();
		foreach (var dir in testDirs)
		{
			if (Directory.Exists(dir) && !Directory.EnumerateFiles(dir, "*.cs").Any())
				empty.Add(dir);
		}

		await Assert.That(empty).IsEmpty();
	}

	[Test]
	public async Task LogDirectory_Points_To_UserProfile_Cache()
	{
		var expectedBase = Path.Combine(
			System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
			".cache",
			"logs",
			"scripts"
		);

		var logDir = Scripts.Core.Paths.LogDirectory;

		await Assert.That(logDir).IsEqualTo(expectedBase);
	}
}
