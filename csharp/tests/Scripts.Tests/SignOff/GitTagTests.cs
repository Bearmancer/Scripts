namespace Scripts.Tests.SignOff;

internal sealed class GitTagTests
{
	[Test]
	public async Task Git_Tag_T1_SignOff_Exists()
	{
		using var process = new System.Diagnostics.Process
		{
			StartInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "git",
				Arguments = $"-C {TestPaths.RepoRoot} tag -l t1-sign-off",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			},
		};

		process.Start();
		var output = (await process.StandardOutput.ReadToEndAsync()).Trim();
		await process.WaitForExitAsync();

		await Assert.That(output).IsEqualTo("t1-sign-off");
	}

	[Test]
	public async Task Git_Log_Contains_All_Phase_Commits()
	{
		var requiredPrefixes = new[]
		{
			"feat(t1-12",
			"feat(t1-13",
			"feat(t1-14",
			"feat(t1-15",
			"feat(t1-16",
		};

		using var process = new System.Diagnostics.Process
		{
			StartInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "git",
				Arguments = $"-C {TestPaths.RepoRoot} log --oneline",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			},
		};

		process.Start();
		var log = await process.StandardOutput.ReadToEndAsync();
		await process.WaitForExitAsync();

		var missing = requiredPrefixes.Where(p => !log.Contains(p)).ToList();

		await Assert.That(missing).IsEmpty();
	}
}
