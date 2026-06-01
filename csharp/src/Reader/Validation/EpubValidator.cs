using System.Text;

namespace Scripts.Services.Read.Validation;

internal static class EpubValidator
{
	public static async Task<EpubValidationResult> ValidateAsync(
		string epubPath,
		CancellationToken ct = default
	)
	{
		Ui.Info($"EPUBCheck: validating {epubPath}...");

		ProcessStartInfo startInfo = new()
		{
			FileName = "epubcheck",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		startInfo.ArgumentList.Add(item: epubPath);

		using Process process = new() { StartInfo = startInfo };

		StringBuilder output = new();
		StringBuilder errors = new();

		process.OutputDataReceived += (_, e) =>
		{
			if (e.Data is { })
				output.AppendLine(value: e.Data);
		};
		process.ErrorDataReceived += (_, e) =>
		{
			if (e.Data is { })
				errors.AppendLine(value: e.Data);
		};

		try
		{
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			await process.WaitForExitAsync(cancellationToken: ct);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Error(ex, "EPUBCheck not available. Skipping validation.");
			return new EpubValidationResult(Skipped: true, Passed: true, Output: "");
		}

		var combined = output.ToString() + errors.ToString();
		var hasFatal = combined.Contains(value: "FATAL") || combined.Contains(value: "ERROR(S)");
		var passed = process.ExitCode == 0 && !hasFatal;

		if (passed)
			Ui.Ok(message: "EPUBCheck: validation passed.");
		else
			Ui.Warn($"EPUBCheck: validation issues found:\n{combined.Trim()}");

		return new EpubValidationResult(Skipped: false, Passed: passed, combined.Trim());
	}
}

internal sealed record EpubValidationResult(bool Skipped, bool Passed, string Output);
