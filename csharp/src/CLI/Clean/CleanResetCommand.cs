using System.ComponentModel;

namespace CSharpScripts.CLI.Clean;

internal sealed class CleanResetCommand : AsyncCommand<CleanResetCommand.Settings>
{
	private const int TerminalCloseDelayMs = 2000;

	protected async override Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		var resetAll = settings.Service.EqualsIgnoreCase(other: "all");
		var resetLastFm = resetAll || settings.Service.EqualsIgnoreCase(other: "lastfm");
		var resetYouTube =
			resetAll
			|| settings.Service.EqualsIgnoreCase(other: "youtube")
			|| settings.Service.EqualsIgnoreCase(other: "yt");

		if (!resetLastFm && !resetYouTube)
		{
			Ui.Warn(message: "Invalid service: {0}. Use: yt, lastfm, or all", settings.Service);
			return 1;
		}

		Ui.Rule(text: "Clean Reset");

		if (resetLastFm)
			await ResetLastFmAsync(ct: cancellationToken);

		if (resetYouTube)
			await ResetYouTubeAsync(ct: cancellationToken);

		ResetCsvExports();
		ResetBuildArtifacts();

		Ui.NewLine();
		Ui.Ok(message: "Reset complete - terminal will close in 2 seconds...");
		Log.Information(messageTemplate: "ResetComplete");

		await Task.Delay(millisecondsDelay: TerminalCloseDelayMs, cancellationToken: cancellationToken);
		Exit(exitCode: 0);

		return 0;
	}

	private static async Task ResetLastFmAsync(CancellationToken ct)
	{
		Ui.Info(message: "Resetting Last.fm...");

		FetchState state = await StateManager.LoadStateAsync<FetchState>(
			fileName: StateManager.LastFmSyncFile,
			ct: ct
		);

		StateManager.DeleteLastFmStates();
		Ui.Ok(message: "  State files deleted");
		Log.Information(messageTemplate: "ResetLastFmState");
	}

	private static async Task ResetYouTubeAsync(CancellationToken ct)
	{
		Ui.Info(message: "Resetting YouTube...");

		YouTubeFetchState state = await StateManager.LoadStateAsync<YouTubeFetchState>(
			fileName: StateManager.YoutubeSyncFile,
			ct: ct
		);

		StateManager.DeleteAllYouTubeStates();
		Ui.Ok(message: "  State files deleted");
		Log.Information(messageTemplate: "ResetYouTubeState");
	}

	private static void ResetCsvExports()
	{
		Ui.Info(message: "Deleting CSV exports...");

		var csvDir = Path.Combine(path1: Paths.ProjectRoot, path2: "exports");
		if (Directory.Exists(path: csvDir))
		{
			Directory.Delete(path: csvDir, recursive: true);
			Ui.Ok(message: "  exports/ deleted");
			Log.Information(messageTemplate: "ResetCsvExports {Directory}", csvDir);
		}
		else
			Log.Debug(messageTemplate: "ResetCsvExports_NoExportsDirectory");
	}

	private static void ResetBuildArtifacts()
	{
		Ui.Info(message: "Deleting build artifacts...");

		var binDir = Path.Combine(path1: Paths.ProjectRoot, path2: "csharp", path3: "bin");
		var objDir = Path.Combine(path1: Paths.ProjectRoot, path2: "csharp", path3: "obj");

		try
		{
			if (Directory.Exists(path: binDir))
			{
				Directory.Delete(path: binDir, recursive: true);
				Ui.Ok(message: "  bin/ deleted");
				Log.Information(messageTemplate: "ResetBuildArtifacts {Directory}", "bin");
			}

			if (Directory.Exists(path: objDir))
			{
				Directory.Delete(path: objDir, recursive: true);
				Ui.Ok(message: "  obj/ deleted");
				Log.Information(messageTemplate: "ResetBuildArtifacts {Directory}", "obj");
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			ScheduleDeferredCleanup(binDir: binDir, objDir: objDir);
			return;
		}

		RebuildProject();
	}

	private static void ScheduleDeferredCleanup(string binDir, string objDir)
	{
		Ui.Warn(message: "  Build artifacts locked - scheduling deferred cleanup...");
		Log.Warning(messageTemplate: "ResetBuildArtifacts_DeferredCleanup {BinDir} {ObjDir}", binDir, objDir);

		var csprojDir = Path.Combine(path1: Paths.ProjectRoot, path2: "csharp");
		var script = """
		             Start-Sleep -Seconds 2
		             if (Test-Path $args[0]) { Remove-Item -Recurse -Force $args[0] }
		             if (Test-Path $args[1]) { Remove-Item -Recurse -Force $args[1] }
		             Set-Location $args[2]
		             dotnet build
		             """;

		try
		{
			ProcessStartInfo pwsh = new(fileName: "pwsh") { UseShellExecute = false, CreateNoWindow = false };
			pwsh.ArgumentList.Add(item: "-Command");
			pwsh.ArgumentList.Add(item: script);
			pwsh.ArgumentList.Add(item: binDir);
			pwsh.ArgumentList.Add(item: objDir);
			pwsh.ArgumentList.Add(item: csprojDir);
			Process.Start(startInfo: pwsh);
		}
		catch (Win32Exception)
		{
			Log.Warning(messageTemplate: "pwsh not found, falling back to powershell.exe");
			ProcessStartInfo ps5 = new(fileName: "powershell.exe")
			{
				UseShellExecute = false,
				CreateNoWindow = false
			};
			ps5.ArgumentList.Add(item: "-Command");
			ps5.ArgumentList.Add(item: script);
			ps5.ArgumentList.Add(item: binDir);
			ps5.ArgumentList.Add(item: objDir);
			ps5.ArgumentList.Add(item: csprojDir);
			Process.Start(startInfo: ps5);
		}

		Ui.Ok(message: "  Cleanup scheduled - will run after this process exits");
	}

	private static void RebuildProject()
	{
		Ui.NewLine();
		Ui.Info(message: "Rebuilding...");

		var csprojDir = Path.Combine(path1: Paths.ProjectRoot, path2: "csharp");
		Process? process = Process.Start(
			new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = "build",
				WorkingDirectory = csprojDir,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			}
		);

		process?.WaitForExit();

		if (process?.ExitCode == 0)
		{
			Ui.Ok(message: "Build complete");
			Log.Information(messageTemplate: "RebuildProject_Success");
		}
		else
		{
			Ui.Error(message: "Build failed. Run 'dotnet build' manually.");
			Log.Error(messageTemplate: "RebuildProject_Failed {ExitCode}", process?.ExitCode ?? -1);
		}
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(position: 0, template: "[service]")]
		[Description(description: "yt, lastfm, all (default: all)")]
		[DefaultValue(value: "all")]
		[AllowedValues("yt", "youtube", "lastfm", "all")]
		public string Service { get; init; } = "all";
	}
}
