using System.ComponentModel;

namespace Scripts.CLI.Subtitle;

internal sealed class SubtitleCommand : BaseAsyncCommand<SubtitleCommand.Settings>
{
	private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".wav", ".mp3", ".m4a", ".flac", ".ogg", ".opus", ".aac", ".wma",
	};

	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken ct
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			service: ServiceType.OpenAI,
			async () =>
			{
				if (!File.Exists(path: settings.Input))
				{
					Ui.Error(message: $"Input file not found: {settings.Input}");
					throw new FileNotFoundException(
						$"Input file not found: {settings.Input}",
						fileName: settings.Input);
				}

				var outputPath = settings.Output ?? GetDefaultOutputPath(input: settings.Input);
				var isAudio = IsAudioFile(path: settings.Input);
				var tempWav = isAudio ? null : GetTempWavPath(input: settings.Input);

				try
				{
					byte[] audioBytes;
					string audioFilename;

					if (isAudio)
					{
						Ui.Info(message: "Reading audio file...");
						audioBytes = await File.ReadAllBytesAsync(
							path: settings.Input,
							cancellationToken: ct);
						audioFilename = GetFileName(path: settings.Input);
					}
					else
					{
						Ui.Info(message: "Extracting audio from video...");
						await ExtractAudioAsync(
							inputPath: settings.Input,
							outputPath: tempWav!,
							ct: ct);
						Ui.Ok(message: "Audio extracted");
						audioBytes = await File.ReadAllBytesAsync(
							path: tempWav!,
							cancellationToken: ct);
						audioFilename = GetFileName(path: settings.Input);
					}

					Ui.Info(message: "Transcribing with Azure OpenAI Whisper (SRT format)...");
					var srtContent = await AzureOpenAIService.TranscribeAudioSrtAsync(
						audioBytes: audioBytes,
						audioFilename: audioFilename,
						ct: ct);

					if (string.IsNullOrWhiteSpace(value: srtContent))
					{
						Ui.Error(message: "Transcription returned empty result");
						throw new InvalidOperationException(
							"Whisper SRT transcription returned empty content");
					}

					await File.WriteAllTextAsync(
						path: outputPath,
						contents: srtContent,
						cancellationToken: ct);
					Ui.Ok($"SRT saved to {outputPath}");
				}
				finally
				{
					if (tempWav is { })
						CleanupTempFile(path: tempWav);
				}
			});
	}

	private static bool IsAudioFile(string path)
	{
		var ext = GetExtension(path: path);
		return AudioExtensions.Contains(ext);
	}

	private static string GetDefaultOutputPath(string input)
	{
		var dir = GetDirectoryName(path: input) ?? ".";
		var name = GetFileNameWithoutExtension(path: input);
		return Combine(path1: dir, path2: $"{name}.srt");
	}

	private static string GetTempWavPath(string input)
	{
		var name = GetFileNameWithoutExtension(path: input);
		return Combine(
			path1: GetTempPath(),
			path2: $"{name}_{Guid.NewGuid():N}.wav");
	}

	private static async Task ExtractAudioAsync(
		string inputPath,
		string outputPath,
		CancellationToken ct
	)
	{
		var ffmpegPath = FindFfmpeg();
		var args =
			$"-i \"{inputPath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 -y \"{outputPath}\"";

		using var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = ffmpegPath,
				Arguments = args,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			},
			EnableRaisingEvents = true,
		};

		process.Start();
		await process.WaitForExitAsync(cancellationToken: ct);

		if (process.ExitCode != 0)
		{
			var stderr = await process.StandardError.ReadToEndAsync(cancellationToken: ct);
			throw new InvalidOperationException(
				$"ffmpeg exited with code {process.ExitCode}: {stderr.Trim()}");
		}
	}

	private static string FindFfmpeg()
	{
		var paths = GetEnvironmentVariable("PATH")?.Split(';') ?? [];
		var exeNames = OperatingSystem.IsWindows()
			? new[] { "ffmpeg.exe" }
			: new[] { "ffmpeg" };

		foreach (var dir in paths)
		{
			foreach (var name in exeNames)
			{
				var candidate = Combine(path1: dir, path2: name);
				if (File.Exists(path: candidate))
					return candidate;
			}
		}

		throw new InvalidOperationException(
			"ffmpeg not found on PATH. Install ffmpeg: https://ffmpeg.org/download.html");
	}

	private static void CleanupTempFile(string path)
	{
		try
		{
			if (File.Exists(path: path))
				File.Delete(path: path);
		}
		catch
		{
		}
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(position: 0, template: "<input>")]
		[Description(description: "Path to video or audio file (mp4, mkv, wav, mp3, opus, etc.)")]
		public required string Input { get; init; }

		[CommandOption("-o|--output")]
		[Description(description: "Output SRT file path (defaults to <input>.srt)")]
		public string? Output { get; init; }

		[CommandOption("-l|--language")]
		[Description(description: "Language code for transcription (default: en)")]
		public string Language { get; init; } = "en";
	}
}
