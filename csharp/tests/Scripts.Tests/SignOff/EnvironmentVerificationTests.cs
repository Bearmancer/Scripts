namespace Scripts.Tests.SignOff;

internal sealed class EnvironmentVerificationTests
{
	[Test]
	public async Task Docker_Is_Running()
	{
		using var process = new System.Diagnostics.Process
		{
			StartInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "docker",
				Arguments = "ps",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			},
		};

		process.Start();
		await process.WaitForExitAsync();

		await Assert.That(process.ExitCode).IsEqualTo(0);
	}

	[Test]
	public async Task Docker_Compose_File_Is_Valid()
	{
		using var process = new System.Diagnostics.Process
		{
			StartInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "docker",
				Arguments = $"compose -f {TestPaths.Combine("docker-compose.yml")} config",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			},
		};

		process.Start();
		await process.WaitForExitAsync();

		await Assert.That(process.ExitCode).IsEqualTo(0);
	}

	[Test]
	public async Task Dot_Env_File_Exists()
	{
		var envPath = TestPaths.Combine(".env");
		await Assert.That(File.Exists(envPath)).IsTrue();
	}

	[Test]
	public async Task Dot_Env_Contains_PGCONNSTR()
	{
		var envPath = TestPaths.Combine(".env");
		var content = File.ReadAllText(envPath);
		await Assert.That(content).Contains("PGCONNSTR");
	}

	[Test]
	public async Task Compiled_Model_Directory_Exists()
	{
		var compiledModelDir = Path.Combine(TestPaths.CSharpRoot, "CompiledModels");
		await Assert.That(Directory.Exists(compiledModelDir)).IsTrue();
		await Assert.That(Directory.GetFiles(compiledModelDir, "*.cs")).IsNotEmpty();
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
