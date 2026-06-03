using TUnit;
using FluentAssertions;

namespace Scripts.Tests.Environment;

internal sealed class DockerEnvironmentTests
{
	[Test]
	public void Docker_IsRunning_WhenDockerPsSucceeds()
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
			}
		};

		process.Start();
		process.WaitForExit(10_000);

		process.ExitCode.Should().Be(0, "because Docker must be running for all EF Core and Testcontainers tests");
	}
}
