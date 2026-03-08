namespace CSharpScripts.CLI.Clean;

internal sealed class CleanResetCommand : AsyncCommand<CleanResetCommand.Settings>
{
	private const int TerminalCloseDelayMs = 2000;

	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		var resetAll = settings.Service.EqualsIgnoreCase("all");
		var resetLastFm = resetAll || settings.Service.EqualsIgnoreCase("lastfm");
		var resetYouTube =
			resetAll
			|| settings.Service.EqualsIgnoreCase("youtube")
			|| settings.Service.EqualsIgnoreCase("yt");

		if (!resetLastFm && !resetYouTube)
		{
			UI.Warn("Invalid service: {0}. Use: yt, lastfm, or all", settings.Service);
			return 1;
		}

		UI.Rule("Clean Reset");

		GoogleSheetsService? sheets = null;
		try
		{
			if (resetLastFm)
				sheets = await ResetLastFmAsync(sheets, cancellationToken);

			if (resetYouTube)
				sheets = await ResetYouTubeAsync(sheets, cancellationToken);

			ResetCsvExports();
			ResetBuildArtifacts();

			UI.NewLine();
			UI.Ok("Reset complete - terminal will close in 2 seconds...");
			Log.Information("ResetComplete");

			await Task.Delay(TerminalCloseDelayMs, cancellationToken);
			Exit(0);

			return 0;
		}
		finally
		{
			sheets?.Dispose();
		}
	}

	private static async Task<GoogleSheetsService?> ResetLastFmAsync(
		GoogleSheetsService? sheets,
		CancellationToken ct
	)
	{
		UI.Info("Resetting Last.fm...");

		FetchState state = StateManager.Load<FetchState>(StateManager.LastFmSyncFile);
		if (!IsNullOrEmpty(state.SpreadsheetId))
		{
			sheets ??= await GoogleSheetsService.CreateAsync(ct);
			await sheets.DeleteSpreadsheetAsync(state.SpreadsheetId, ct);
			Log.Information("ResetLastFmSpreadsheet {SpreadsheetId}", state.SpreadsheetId);
		}

		StateManager.DeleteLastFmStates();
		UI.Ok("  State files deleted");
		Log.Information("ResetLastFmState");
		return sheets;
	}

	private static async Task<GoogleSheetsService?> ResetYouTubeAsync(
		GoogleSheetsService? sheets,
		CancellationToken ct
	)
	{
		UI.Info("Resetting YouTube...");

		YouTubeFetchState state = StateManager.Load<YouTubeFetchState>(
			StateManager.YoutubeSyncFile
		);
		if (!IsNullOrEmpty(state.SpreadsheetId))
		{
			sheets ??= await GoogleSheetsService.CreateAsync(ct);
			await sheets.DeleteSpreadsheetAsync(state.SpreadsheetId, ct);
			Log.Information("ResetYouTubeSpreadsheet {SpreadsheetId}", state.SpreadsheetId);
		}

		StateManager.DeleteAllYouTubeStates();
		UI.Ok("  State files deleted");
		Log.Information("ResetYouTubeState");
		return sheets;
	}

	private static void ResetCsvExports()
	{
		UI.Info("Deleting CSV exports...");

		var csvDir = Path.Combine(Paths.ProjectRoot, "exports");
		if (Directory.Exists(csvDir))
		{
			Directory.Delete(csvDir, true);
			UI.Ok("  exports/ deleted");
			Log.Information("ResetCsvExports {Directory}", csvDir);
		}
		else
		{
			Log.Debug("ResetCsvExports_NoExportsDirectory");
		}
	}

	private static void ResetBuildArtifacts()
	{
		UI.Info("Deleting build artifacts...");

		var binDir = Path.Combine(Paths.ProjectRoot, "csharp", "bin");
		var objDir = Path.Combine(Paths.ProjectRoot, "csharp", "obj");

		try
		{
			if (Directory.Exists(binDir))
			{
				Directory.Delete(binDir, true);
				UI.Ok("  bin/ deleted");
				Log.Information("ResetBuildArtifacts {Directory}", "bin");
			}

			if (Directory.Exists(objDir))
			{
				Directory.Delete(objDir, true);
				UI.Ok("  obj/ deleted");
				Log.Information("ResetBuildArtifacts {Directory}", "obj");
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			ScheduleDeferredCleanup(binDir, objDir);
			return;
		}

		RebuildProject();
	}

	private static void ScheduleDeferredCleanup(string binDir, string objDir)
	{
		UI.Warn("  Build artifacts locked - scheduling deferred cleanup...");
		Log.Warning("ResetBuildArtifacts_DeferredCleanup {BinDir} {ObjDir}", binDir, objDir);

		var csprojDir = Path.Combine(Paths.ProjectRoot, "csharp");
		var script = $$"""
			Start-Sleep -Seconds 2
			if (Test-Path '{{binDir}}') { Remove-Item -Recurse -Force '{{binDir}}' }
			if (Test-Path '{{objDir}}') { Remove-Item -Recurse -Force '{{objDir}}' }
			Set-Location '{{csprojDir}}'
			dotnet build
			""";

		var arguments = $"-Command \"{script.Replace("\"", "\\\"")}\"";

		try
		{
			Process.Start(
				new ProcessStartInfo
				{
					FileName = "pwsh",
					Arguments = arguments,
					UseShellExecute = true,
					CreateNoWindow = false,
				}
			);
		}
		catch (Win32Exception)
		{
			Log.Warning("pwsh not found, falling back to powershell.exe");
			Process.Start(
				new ProcessStartInfo
				{
					FileName = "powershell.exe",
					Arguments = arguments,
					UseShellExecute = true,
					CreateNoWindow = false,
				}
			);
		}

		UI.Ok("  Cleanup scheduled - will run after this process exits");
	}

	private static void RebuildProject()
	{
		UI.NewLine();
		UI.Info("Rebuilding...");

		var csprojDir = Path.Combine(Paths.ProjectRoot, "csharp");
		var process = Process.Start(
			new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = "build",
				WorkingDirectory = csprojDir,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			}
		);

		process?.WaitForExit();

		if (process?.ExitCode == 0)
		{
			UI.Ok("Build complete");
			Log.Information("RebuildProject_Success");
		}
		else
		{
			UI.Error("Build failed. Run 'dotnet build' manually.");
			Log.Error("RebuildProject_Failed {ExitCode}", process?.ExitCode ?? -1);
		}
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(0, "[service]")]
		[Description("yt, lastfm, all (default: all)")]
		[DefaultValue("all")]
		[AllowedValues("yt", "youtube", "lastfm", "all")]
		public string Service { get; init; } = "all";
	}
}
