namespace CSharpScripts.Services.Read.Validation;

using System.Diagnostics;
using System.Text;

/// <summary>Wraps the EPUBCheck CLI to validate EPUB files against the EPUB 3.3 specification.</summary>
/// <remarks>
/// Requires EPUBCheck to be installed and available on PATH as 'epubcheck' or 'java -jar epubcheck.jar'.
/// Download from https://github.com/w3c/epubcheck/releases
/// </remarks>
internal static class EpubValidator
{
	/// <summary>Validates an EPUB file using EPUBCheck.</summary>
	/// <returns>True if validation passed with no fatal errors; false otherwise.</returns>
	public static async Task<EpubValidationResult> ValidateAsync(
		string epubPath,
		CancellationToken ct = default
	)
	{
		UI.Info($"EPUBCheck: validating {epubPath}...");

		using Process process = new()
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "epubcheck",
				Arguments = $"\"{epubPath}\"",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			},
		};

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
			UI.Warn($"EPUBCheck not available: {ex.Message}. Skipping validation.");
			return new EpubValidationResult(Skipped: true, Passed: true, Output: "");
		}

		var combined = output.ToString() + errors.ToString();
		var hasFatal = combined.Contains("FATAL") || combined.Contains("ERROR(S)");
		var passed = process.ExitCode == 0 && !hasFatal;

		if (passed)
			UI.Ok($"EPUBCheck: validation passed.");
		else
			UI.Warn($"EPUBCheck: validation issues found:\n{combined.Trim()}");

		return new EpubValidationResult(Skipped: false, Passed: passed, Output: combined.Trim());
	}
}

internal sealed record EpubValidationResult(bool Skipped, bool Passed, string Output);
