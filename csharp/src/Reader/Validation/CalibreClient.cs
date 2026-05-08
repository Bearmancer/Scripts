namespace CSharpScripts.Services.Read.Validation;

internal static class CalibreClient
{
	public static async Task AddAsync(
		string epubPath,
		string library,
		CancellationToken ct = default
	)
	{
		UI.Info($"Calibre: adding {epubPath} to library {library}...");

		var startInfo = new ProcessStartInfo
		{
			FileName = "calibredb",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		startInfo.ArgumentList.Add("add");
		startInfo.ArgumentList.Add(epubPath);
		startInfo.ArgumentList.Add("--with-library");
		startInfo.ArgumentList.Add(library);

		using Process process = new() { StartInfo = startInfo };

		try
		{
			process.Start();
			var stdout = await process.StandardOutput.ReadToEndAsync(ct);
			var stderr = await process.StandardError.ReadToEndAsync(ct);
			await process.WaitForExitAsync(ct);

			if (process.ExitCode == 0)
				UI.Ok($"Calibre: added successfully.\n{stdout.Trim()}");
			else
			{
				UI.Warn(
					$"Calibre: add completed with warnings (exit {process.ExitCode}):\n{stderr.Trim()}"
				);
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning(ex, "Calibre not available. Skipping ingestion.");
		}
	}
}
