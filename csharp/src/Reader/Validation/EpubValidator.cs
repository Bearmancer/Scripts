using System.Text;

namespace CSharpScripts.Services.Read.Validation;

internal static class EpubValidator
{
	public static async Task<EpubValidationResult> ValidateAsync(
		string epubPath,
		CancellationToken ct = default
	)
	{
		UI.Info($"EPUBCheck: validating {epubPath}...");

		var startInfo = new ProcessStartInfo
		{
			FileName = "epubcheck",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		startInfo.ArgumentList.Add(epubPath);

		using Process process = new() { StartInfo = startInfo };

		StringBuilder output = new();
		StringBuilder errors = new();

		process.OutputDataReceived += (_, e) =>
		{
			if (e.Data is not null)
				output.AppendLine(e.Data);
		};
		process.ErrorDataReceived += (_, e) =>
		{
			if (e.Data is not null)
				errors.AppendLine(e.Data);
		};

		try
		{
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			await process.WaitForExitAsync(ct);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning(ex, "EPUBCheck not available. Skipping validation.");
			return new EpubValidationResult(true, true, "");
		}

		var combined = output.ToString() + errors.ToString();
		var hasFatal = combined.Contains("FATAL") || combined.Contains("ERROR(S)");
		var passed = process.ExitCode == 0 && !hasFatal;

		if (passed)
			UI.Ok($"EPUBCheck: validation passed.");
		else
			UI.Warn($"EPUBCheck: validation issues found:\n{combined.Trim()}");

		return new EpubValidationResult(false, passed, combined.Trim());
	}
}

internal sealed record EpubValidationResult(bool Skipped, bool Passed, string Output);


