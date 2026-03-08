namespace CSharpScripts.Services.Read.Validation;

using System.Diagnostics;

/// <summary>Wraps the calibredb CLI to ingest EPUBs into a Calibre library.</summary>
/// <remarks>
/// Requires Calibre to be installed with calibredb on PATH.
/// Supports local library paths and Content Server URLs.
/// </remarks>
internal static class CalibreClient
{
	/// <summary>Adds an EPUB to a Calibre library.</summary>
	/// <param name="epubPath">Path to the EPUB file.</param>
	/// <param name="library">Local library path or server URL (e.g. http://localhost:8080#library_id).</param>
	/// <param name="ct">Cancellation token.</param>
	public static async Task AddAsync(
		string epubPath,
		string library,
		CancellationToken ct = default
	)
	{
		UI.Info($"Calibre: adding {epubPath} to library {library}...");

		using Process process = new()
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "calibredb",
				Arguments = $"add \"{epubPath}\" --with-library \"{library}\"",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			},
		};

		try
		{
			process.Start();
			var stdout = await process.StandardOutput.ReadToEndAsync(ct);
			var stderr = await process.StandardError.ReadToEndAsync(ct);
			await process.WaitForExitAsync(ct);

			if (process.ExitCode == 0)
				UI.Ok($"Calibre: added successfully.\n{stdout.Trim()}");
			else
				UI.Warn(
					$"Calibre: add completed with warnings (exit {process.ExitCode}):\n{stderr.Trim()}"
				);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			UI.Warn($"Calibre not available: {ex.Message}. Skipping ingestion.");
		}
	}
}
