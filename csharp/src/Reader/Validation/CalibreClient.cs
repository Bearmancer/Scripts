namespace Scripts.Services.Read.Validation;

internal static class CalibreClient
{
	public static async Task AddAsync(
		string epubPath,
		string library,
		CancellationToken ct = default
	)
	{
		Ui.Info($"Calibre: adding {epubPath} to library {library}...");

		ProcessStartInfo startInfo = new()
		{
			FileName = "calibredb",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		startInfo.ArgumentList.Add(item: "add");
		startInfo.ArgumentList.Add(item: epubPath);
		startInfo.ArgumentList.Add(item: "--with-library");
		startInfo.ArgumentList.Add(item: library);

		using Process process = new() { StartInfo = startInfo };

		try
		{
			process.Start();
			var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken: ct);
			var stderr = await process.StandardError.ReadToEndAsync(cancellationToken: ct);
			await process.WaitForExitAsync(cancellationToken: ct);

			if (process.ExitCode == 0)
				Ui.Ok($"Calibre: added successfully.\n{stdout.Trim()}");
			else
			{
				Ui.Warn(
					$"Calibre: add completed with warnings (exit {process.ExitCode}):\n{stderr.Trim()}"
				);
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Error(ex, "Calibre not available. Skipping ingestion.");
		}
	}
}
