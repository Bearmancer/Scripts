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
		if (!Start(offline: offline))
		{
			Log.Error("Failed to start LibreTranslate container");
			Log.Debug("EnsureRunningAsync exit false");
			return false;
		}

		Log.Debug("Waiting {0}ms for container to initialize...", StartupWaitMs);
		await Task.Delay(millisecondsDelay: StartupWaitMs, ct);
		Log.Debug("EnsureRunningAsync exit true");
		return true;
	}

	public static bool IsRunning() =>
		!IsNullOrWhiteSpace(RunDockerQuery($"ps -q -f name={ContainerName}"));

	public bool Start(bool offline = true)
	{
		LogDockerEvent(
			operation: "Start",
			status: "Checking",
			new Dictionary<string, object> { [key: "OfflineMode"] = offline }
		);

		if (IsRunning())
		{
			Log.Debug("LibreTranslate container already running");
			LogDockerEvent(operation: "Start", status: "AlreadyRunning");
			return true;
		}

		if (ContainerExists())
		{
			Log.Information("Starting LibreTranslate container...");
			LogDockerEvent(
				operation: "Start",
				status: "StartingExisting",
				new Dictionary<string, object> { [key: "Container"] = ContainerName }
			);
			var started = RunDockerCommand($"start {ContainerName}");
			LogDockerEvent(
				operation: "Start",
				started ? "Started" : "Failed",
				data: null,
				started ? LogEventLevel.Information : LogEventLevel.Error
			);
			return started;
		}

		Log.Information("Creating LibreTranslate container (offline mode: {0})...", offline);
		var offlineFlag = offline ? "-e LT_UPDATE_MODELS=false" : "";
		LogDockerEvent(
			operation: "Start",
			status: "CreatingNew",
			new Dictionary<string, object>
			{
				[key: "Container"] = ContainerName,
				[key: "OfflineMode"] = offline,
				[key: "Image"] = DockerImage,
			}
		);

		var created = RunDockerCommand(
			$"run -d --name {ContainerName} --restart unless-stopped -p {Port}:5000 {offlineFlag} {DockerImage}"
		);
		LogDockerEvent(
			operation: "Start",
			created ? "Created" : "Failed",
			data: null,
			created ? LogEventLevel.Information : LogEventLevel.Error
		);
		return created;
	}

	public static bool Stop()
	{
		LogDockerEvent(operation: "Stop", status: "Checking");

		if (!IsRunning())
		{
			Log.Debug("LibreTranslate container not running");
			LogDockerEvent(operation: "Stop", status: "NotRunning");
			return true;
		}

		Log.Information("Stopping LibreTranslate container...");
		var stopped = RunDockerCommand($"stop {ContainerName}");
		LogDockerEvent(
			operation: "Stop",
			stopped ? "Stopped" : "Failed",
			data: null,
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
		LogDockerEvent(
			operation: "PullImage",
			status: "Starting",
			new Dictionary<string, object> { [key: "Image"] = DockerImage }
		);

		using Process? process = StartDockerProcess($"pull {DockerImage}", redirectError: true);
		if (process is null)
		{
			LogDockerEvent(
				operation: "PullImage",
				status: "Failed",
				new Dictionary<string, object>
				{
					[key: "Reason"] = "Docker process failed to start",
				},
				level: LogEventLevel.Error
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
				operation: "PullImage",
				status: "Failed",
				new Dictionary<string, object>
				{
					[key: "ExitCode"] = process.ExitCode,
					[key: "Error"] = errorOutput,
				},
				level: LogEventLevel.Error
			);
			return false;
		}

		Log.Information("LibreTranslate image downloaded successfully");
		LogDockerEvent(
			operation: "PullImage",
			status: "Complete",
			new Dictionary<string, object> { [key: "Image"] = DockerImage }
		);
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
		using Process? process = StartDockerProcess(arguments: arguments);
		if (process is null)
			return null;

		var output = process.StandardOutput.ReadToEnd();
		process.WaitForExit();
		return output;
	}

	private static bool RunDockerCommand(string arguments)
	{
		using Process? process = StartDockerProcess(arguments: arguments, redirectError: true);
		if (process is null)
		{
			Log.ForService(service: ServiceType.Music)
				.Error(
					messageTemplate: "DockerCommandProcessStartFailed {Arguments}",
					propertyValue: arguments
				);
			LogDockerEvent(
				operation: "DockerCommand",
				status: "ProcessStartFailed",
				new Dictionary<string, object> { [key: "Arguments"] = arguments },
				level: LogEventLevel.Error
			);
			return false;
		}

		process.WaitForExit();

		if (process.ExitCode != 0)
		{
			var errorOutput = process.StandardError.ReadToEnd();
			Log.ForService(service: ServiceType.Music)
				.Error(
					messageTemplate: "DockerCommandFailed {Arguments} {ExitCode} {ErrorOutput}",
					propertyValue0: arguments,
					propertyValue1: process.ExitCode,
					propertyValue2: errorOutput
				);
			LogDockerEvent(
				operation: "DockerCommand",
				status: "Failed",
				new Dictionary<string, object>
				{
					[key: "Arguments"] = arguments,
					[key: "ExitCode"] = process.ExitCode,
					[key: "Error"] = errorOutput,
				},
				level: LogEventLevel.Error
			);
			return false;
		}

		return true;
	}

	private static Process? StartDockerProcess(string arguments, bool redirectError = false) =>
		Process.Start(
			new ProcessStartInfo(fileName: "docker", arguments: arguments)
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
		var props = Enumerable.ToDictionary(
			Enumerable.Concat(
				new Dictionary<string, object>
				{
					[key: "Status"] = status,
					[key: "Container"] = ContainerName,
				},
				data ?? []
			),
			kvp => kvp.Key,
			kvp => kvp.Value
		);
		Log.ForService(service: ServiceType.Music)
			.Write(
				level: level,
				messageTemplate: "Docker_{Operation} {@Props}",
				propertyValue0: operation,
				propertyValue1: props
			);
	}
}
