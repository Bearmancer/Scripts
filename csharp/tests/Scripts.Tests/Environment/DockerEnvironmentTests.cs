namespace Scripts.Tests.Environment;

internal sealed class DockerEnvironmentTests
{
	[Test]
	public async Task Docker_IsRunning_WhenDockerPsSucceeds()
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
		process.WaitForExit(10_000);

		await Assert.That(process.ExitCode).IsEqualTo(0);
	}
}
