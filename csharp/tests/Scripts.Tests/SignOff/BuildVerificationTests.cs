namespace Scripts.Tests.SignOff;

internal sealed class BuildVerificationTests
{
	[Test]
	public async Task Dotnet_Build_Slnx_Zero_Errors()
	{
		using var process = new System.Diagnostics.Process
		{
			StartInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"build {Path.Combine(TestPaths.CSharpRoot, "Scripts.slnx")}",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			},
		};

		process.Start();
		var output = await process.StandardOutput.ReadToEndAsync();
		var error = await process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();

		await Assert.That(process.ExitCode).IsEqualTo(0);
	}

	[Test]
	public async Task Dotnet_Restore_Succeeds()
	{
		using var process = new System.Diagnostics.Process
		{
			StartInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"restore {Path.Combine(TestPaths.CSharpRoot, "Scripts.slnx")}",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			},
		};

		process.Start();
		var output = await process.StandardOutput.ReadToEndAsync();
		var error = await process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();

		await Assert.That(process.ExitCode).IsEqualTo(0);
	}
}
