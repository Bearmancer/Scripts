namespace CSharpScripts.Services.Language;

internal sealed class LibreTranslateHostManager
{
	private const string ContainerName = "libretranslate";
	private const string DockerImage = "libretranslate/libretranslate:latest";
	public const int DefaultPort = 5000;
	private const int StartupWaitMs = 3000;

	public int Port { get; } = DefaultPort;

	public static string GetDefaultUrl() => $"http://localhost:{DefaultPort}/translate";

	public async Task<bool> EnsureRunningAsync(bool offline = true, CancellationToken ct = default)
	{
		Log.Debug("EnsureRunningAsync entry {Offline}", offline);
		if (IsRunning())
		{
			Log.Debug("LibreTranslate container already running");
			Log.Debug("EnsureRunningAsync exit true");
			return true;
		}

		Log.Information("Starting LibreTranslate container for translation...");
		if (!Start(offline))
		{
			Log.Error("Failed to start LibreTranslate container");
			Log.Debug("EnsureRunningAsync exit false");
			return false;
		}

		Log.Debug("Waiting {0}ms for container to initialize...", StartupWaitMs);
		await Task.Delay(StartupWaitMs, ct);
		Log.Debug("EnsureRunningAsync exit true");
		return true;
	}

	public static bool IsRunning() =>
		!IsNullOrWhiteSpace(RunDockerQuery($"ps -q -f name={ContainerName}"));

	public bool Start(bool offline = true)
	{
		LogDockerEvent("Start", "Checking", new() { ["OfflineMode"] = offline });

		if (IsRunning())
		{
			Log.Debug("LibreTranslate container already running");
			LogDockerEvent("Start", "AlreadyRunning");
			return true;
		}

		if (ContainerExists())
		{
			Log.Information("Starting LibreTranslate container...");
			LogDockerEvent("Start", "StartingExisting", new() { ["Container"] = ContainerName });
			var started = RunDockerCommand($"start {ContainerName}");
			LogDockerEvent(
				"Start",
				started ? "Started" : "Failed",
				null,
				started ? LogEventLevel.Information : LogEventLevel.Error
			);
			return started;
		}

		Log.Information("Creating LibreTranslate container (offline mode: {0})...", offline);
		var offlineFlag = offline ? "-e LT_UPDATE_MODELS=false" : "";
		LogDockerEvent(
			"Start",
			"CreatingNew",
			new()
			{
				["Container"] = ContainerName,
				["OfflineMode"] = offline,
				["Image"] = DockerImage,
			}
		);

		var created = RunDockerCommand(
			$"run -d --name {ContainerName} --restart unless-stopped -p {Port}:5000 {offlineFlag} {DockerImage}"
		);
		LogDockerEvent(
			"Start",
			created ? "Created" : "Failed",
			null,
			created ? LogEventLevel.Information : LogEventLevel.Error
		);
		return created;
	}

	public static bool Stop()
	{
		LogDockerEvent("Stop", "Checking");

		if (!IsRunning())
		{
			Log.Debug("LibreTranslate container not running");
			LogDockerEvent("Stop", "NotRunning");
			return true;
		}

		Log.Information("Stopping LibreTranslate container...");
		var stopped = RunDockerCommand($"stop {ContainerName}");
		LogDockerEvent(
			"Stop",
			stopped ? "Stopped" : "Failed",
			null,
			stopped ? LogEventLevel.Information : LogEventLevel.Error
		);
		return stopped;
	}

	public static bool Remove()
	{
		Stop();
		return !ContainerExists() || RunDockerCommand($"rm {ContainerName}");
	}

	public static bool PullImage()
	{
		Log.Information("Pulling LibreTranslate Docker image (this may take a while)...");
		Log.Information("Image: {0}", DockerImage);
		LogDockerEvent("PullImage", "Starting", new() { ["Image"] = DockerImage });

		using Process? process = StartDockerProcess($"pull {DockerImage}", redirectError: true);
		if (process is null)
		{
			LogDockerEvent(
				"PullImage",
				"Failed",
				new() { ["Reason"] = "Docker process failed to start" },
				LogEventLevel.Error
			);
			return false;
		}

		while (process.StandardOutput.ReadLine() is { } line)
			Log.Debug("{0}", line);

		process.WaitForExit();

		if (process.ExitCode != 0)
		{
			var errorOutput = process.StandardError.ReadToEnd();
			Log.Error("Docker pull failed: {0}", errorOutput);
			LogDockerEvent(
				"PullImage",
				"Failed",
				new() { ["ExitCode"] = process.ExitCode, ["Error"] = errorOutput },
				LogEventLevel.Error
			);
			return false;
		}

		Log.Information("LibreTranslate image downloaded successfully");
		LogDockerEvent("PullImage", "Complete", new() { ["Image"] = DockerImage });
		return true;
	}

	public static bool ImageExists() =>
		!IsNullOrWhiteSpace(RunDockerQuery($"images -q {DockerImage}"));

	public static void ShowStatus(string url)
	{
		Log.Information("=== LibreTranslate Status ===");
		Log.Information("Docker Image:  {0}", ImageExists() ? "Downloaded" : "Not downloaded");
		Log.Information("Container:     {0}", ContainerExists() ? "Exists" : "Not created");
		Log.Information("Status:        {0}", IsRunning() ? "Running" : "Stopped");
		Log.Information("URL:           {0}", url);
		Log.Information("Languages:     All (auto-detect enabled)");
	}

	private static bool ContainerExists() =>
		!IsNullOrWhiteSpace(RunDockerQuery($"ps -aq -f name={ContainerName}"));

	private static string? RunDockerQuery(string arguments)
	{
		using Process? process = StartDockerProcess(arguments);
		if (process is null)
			return null;

		var output = process.StandardOutput.ReadToEnd();
		process.WaitForExit();
		return output;
	}

	private static bool RunDockerCommand(string arguments)
	{
		using Process? process = StartDockerProcess(arguments, redirectError: true);
		if (process is null)
		{
			Log.ForService(ServiceType.Music)
				.Error("DockerCommandProcessStartFailed {Arguments}", arguments);
			LogDockerEvent(
				"DockerCommand",
				"ProcessStartFailed",
				new() { ["Arguments"] = arguments },
				LogEventLevel.Error
			);
			return false;
		}

		process.WaitForExit();

		if (process.ExitCode != 0)
		{
			var errorOutput = process.StandardError.ReadToEnd();
			Log.ForService(ServiceType.Music)
				.Error(
					"DockerCommandFailed {Arguments} {ExitCode} {ErrorOutput}",
					arguments,
					process.ExitCode,
					errorOutput
				);
			LogDockerEvent(
				"DockerCommand",
				"Failed",
				new()
				{
					["Arguments"] = arguments,
					["ExitCode"] = process.ExitCode,
					["Error"] = errorOutput,
				},
				LogEventLevel.Error
			);
			return false;
		}

		return true;
	}

	private static Process? StartDockerProcess(string arguments, bool redirectError = false) =>
		Process.Start(
			new ProcessStartInfo("docker", arguments)
			{
				RedirectStandardOutput = true,
				RedirectStandardError = redirectError,
				UseShellExecute = false,
				CreateNoWindow = true,
			}
		);

	private static void LogDockerEvent(
		string operation,
		string status,
		Dictionary<string, object>? data = null,
		LogEventLevel level = LogEventLevel.Debug
	)
	{
		var props = new Dictionary<string, object>
		{
			["Status"] = status,
			["Container"] = ContainerName,
		}
			.Concat(data ?? [])
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		Log.ForService(ServiceType.Music)
			.Write(level, "Docker_{Operation} {@Props}", operation, props);
	}
}
